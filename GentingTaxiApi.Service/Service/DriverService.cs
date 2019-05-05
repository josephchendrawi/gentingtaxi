using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using System.Data.Entity.Validation;
using GentingTaxiApi.Service.entity;
using GentingTaxiApi.Service.Constant;
using System.Data.Objects;
using System.Device.Location;
using System.Linq.Expressions;

namespace GentingTaxiApi.Service
{
    public class DriverService
    {
        public DriverResponse RegisterDriverService(RegisterDriverRequest request)
        {   
            //create user 
            using (var context = new entity.gtaxidbEntities())
            {
                //check existing username
                var entitydriver = from d in context.Drivers
                                   where d.ic == request.username
                                   select d;

                //check matching password 
                if (request.password != request.confirm_password)
                {
                    throw new CustomException(CustomErrorType.UserPasswordMatchfailed);
                }

                if (entitydriver == null || entitydriver.Count() == 0)
                {
                    entity.Driver newDriver = new entity.Driver();
                    newDriver.ic = request.username;
                    newDriver.name = request.name;
                    newDriver.phone = request.countrycode + request.phone.TrimStart('0');

                    //create one way hash password token
                    string key = Security.RandomString(60);
                    string pass = Security.checkHMAC(key, request.password);

                    newDriver.password = pass;
                    newDriver.password_salt = key;
                    newDriver.created_date = DateTime.Now;
                    newDriver.status = (int)UserStatus.Inactive;

                    //generate activation code 
                    var code = Guid.NewGuid().ToString().Substring(0, 6);
                    newDriver.activation_code = code;

                    context.Drivers.Add(newDriver);
                    context.SaveChanges();

                    //send sms
                    if (!string.IsNullOrEmpty(newDriver.phone))
                    {
                        if (newDriver.phone.Length == 10 || newDriver.phone.Length == 11)
                        {
                            var msg = "Genting Taxi " + Environment.NewLine + "Verification code : " + code;

                            bool issent = Helper.SendSMS(newDriver.activation_code, newDriver.phone, msg , newDriver.driverId , (int)UserType.driver);

                            if (issent)
                            {
                                //update sent time 
                                newDriver.code_sent_date = DateTime.Now;
                                context.SaveChanges();
                            }
                        }
                    }

                    return new DriverResponse() { sts = 0, pin = code };
                }
                else
                {
                    return new DriverResponse() { sts = 1, msg = CustomErrorType.UserAlreadyAssign.GetAttributeDescription() };
                }
            }
        }


        public DriverResponse ActivateDriverService(ActivateDriverRequest request)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //check user created 
                var entitydriver = from d in context.Drivers
                                   where d.ic == request.username && d.activation_code == request.pin
                                   select d;
                if (entitydriver.Count() > 0)
                {
                    var driverobj = entitydriver.First();
                    driverobj.status = (int)DriverStatus.Active;
                    context.SaveChanges();

                    //return token if successfully activated 
                    var entitydrivertoken = from d in context.Driver_token
                                          where d.unique_id == driverobj.driverId
                                          select d;

                    string tokenstr = "";

                    if (entitydrivertoken.Count() > 0)
                    {
                        var drivertokenobj = entitydrivertoken.First();
                        string apikey = drivertokenobj.token;
                        tokenstr = Helper.EncodeTo64(new string[] { apikey, driverobj.ic, driverobj.driverId.ToString() });
                    }

                    return new DriverResponse() { sts = 0 , token = tokenstr};
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverAlreadyAssign);
                }
            }
        }

        public DriverResponse LoginDriverService(LoginDriverRequest request)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //check user exist 
                var entitydriver = from d in context.Drivers
                                   where d.ic == request.username
                                   select d;

                if (entitydriver.Count() > 0 &&
                (Security.checkHMAC(
                    entitydriver.First().password_salt, request.password) == entitydriver.First().password
                    )
                )
                {
                    //return user 
                    var driverobj = entitydriver.First();

                    //update login status 
                    driverobj.islogin_flag = true;
                    driverobj.last_updated = DateTime.Now; 
                    context.SaveChanges();

                    DriverVM driver = new DriverVM();
                    driver.ic = driverobj.ic;
                    driver.name = driverobj.name;
                    driver.driverId = driverobj.driverId;
                    driver.status = driverobj.status;

                    //generate user token
                    var apikey = Helper.GenerateDriverToken(request.username, driver.driverId, request.deviceId, request.app_version , request.app_type);
                    string tokenstr = "";

                    //only return token if activated 
                    if (driverobj.status == (int)DriverStatus.Active)
                    {
                        tokenstr = Helper.EncodeTo64(new string[] { apikey, request.username, driver.driverId.ToString() });
                    }

                    return new DriverResponse()
                    {
                        sts = 0,
                        result = driver,
                        token = tokenstr
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverInvalid);
                }
            }
        }

        public bool ValidateDriverApp(int userID)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenentities = from d in context.Driver_token
                                        where d.unique_id == userID
                                        select d;
                if (drivertokenentities.Count() > 0)
                {
                    bool isCurrentVersion = false;

                    var drivertokenobj = drivertokenentities.First();

                    //get adminsetting 
                    var adminsettingsentities = from d in context.AdminSettings
                                                select d;

                    if (adminsettingsentities.Count() > 0)
                    {
                        if (drivertokenobj.app_type == (int)App_Type.android)
                        {
                            if (adminsettingsentities.First().current_user_android_app_version == drivertokenobj.app_version)
                                isCurrentVersion = true;
                        }

                        if (drivertokenobj.app_type == (int)App_Type.ios)
                        {
                            if (adminsettingsentities.First().current_user_ios_app_version == drivertokenobj.app_version)
                                isCurrentVersion = true;
                        }
                    }

                    return isCurrentVersion;
                }
                else
                {
                    throw new Exception("User Not found");
                }
            }
        }
        public DriverResponse UpdateDriverDeviceIdService(UpdateDriverDeviceIdRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenobj = Helper.AuthDriverToken(request.token, session);

                var entityDriverToken = from d in context.Driver_token
                                      where d.token == drivertokenobj.key
                                      select d;

                if (entityDriverToken.Count()> 0)
                {
                    var entityDriverTokenObj = entityDriverToken.First();

                    if (string.IsNullOrEmpty(entityDriverTokenObj.deviceid))
                    {
                        entityDriverTokenObj.deviceid = request.deviceId;
                        context.SaveChanges();
                    }

                    return new DriverResponse()
                    {
                        sts = 0
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }
            }
        }

        public DriverResponse UpdateCurrentDriverService(UpdateDriverRequest request , ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenobj = Helper.AuthDriverToken(request.token, session);

                var entityDriverToken = from d in context.Driver_token
                                      where d.token == drivertokenobj.key
                                      select d;

                if (entityDriverToken != null)
                {
                    var driverId = entityDriverToken.First().unique_id;

                    var entitydrivers = from d in context.Drivers
                                       where d.driverId == driverId
                                       select d; 

                    //overwrite properties 
                    var driverobj = entitydrivers.First();
                    driverobj.name = request.name;
                    driverobj.car_Plate = request.car_plate;
                    driverobj.car_Type = request.type;
                    driverobj.photo_url = request.photo_url;
                    driverobj.phone = request.countrycode + request.phone.TrimStart('0');
                    //driverobj.last_updated = DateTime.Now;  -used for on/off

                    context.SaveChanges();

                    return new DriverResponse()
                    {
                        sts = 0
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }
            }
        }

        public DriverResponse GetCurrentDriverService(GetCurrentDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenobj = Helper.AuthDriverToken(request.token, session);

                if (drivertokenobj != null)
                {
                    var driverid = drivertokenobj.driverId;
                    //get User
                    var entitydriver = from d in context.Drivers
                                       where d.driverId == driverid
                                       select d;
                    if (entitydriver.First().status == (int)UserStatus.Inactive)
                    {
                        throw new CustomException(CustomErrorType.Unauthenticated);
                    }

                    //return driver 
                    DriverVM driver = new DriverVM();
                    var driverobj = entitydriver.First();
                    driver.ic = driverobj.ic;
                    driver.name = driverobj.name;
                    driver.driverId = driverobj.driverId;
                    driver.phone = driverobj.phone;
                    driver.photo_url = driverobj.photo_url;
                    driver.current_lat = driverobj.current_lat;
                    driver.current_lng = driverobj.current_lng;
                    driver.car_Plate = driverobj.car_Plate;
                    driver.car_Type = driverobj.car_Type;
                    driverobj.dateofbirth = driverobj.dateofbirth; 

                    return new DriverResponse()
                    {
                        sts = 0,
                        result = driver
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }

            }
        }

        public DriverResponse SetDriverAceBookingService(SetDriverAceBookingRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenobj = Helper.AuthDriverToken(request.token, session);

                if (drivertokenobj != null)
                {
                    var driverentities = from d in context.Drivers
                                         where d.driverId == request.driverid
                                         select d;

                    if (driverentities.Count() > 0)
                    {

                        var driverentity = driverentities.First();
                        driverentity.flg_ace_booking = true;

                        context.SaveChanges();

                        return new DriverResponse()
                        {
                            sts = 0
                        };
                    }
                    else
                    {
                        throw new Exception("Target driver not found");
                    }
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }

            }
        }

        public DriverResponse LogoutDriverService(LogoutCurrentDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {

                //get user token entity to be removed
                var entityDriverToken = from d in context.Driver_token
                                        where d.unique_id == request.driverid
                                        select d;

                if (entityDriverToken.Count() > 0)
                {
                    //update logout status 
                    var entitydrivers = from d in context.Drivers
                                        where d.driverId == request.driverid
                                        select d;
                    if (entitydrivers.Count() > 0)
                    {
                        entitydrivers.First().islogin_flag = false;
                        context.SaveChanges(); 
                    }

                    var currentEntityDriverToken = entityDriverToken.First();
                    context.Driver_token.Remove(currentEntityDriverToken);
                    context.SaveChanges();

                    return new DriverResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }

            }
        }

        public DriverResponse UpdateDriverLocationService(UpdateDriverLocationRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenobj = Helper.AuthDriverToken(request.token, session);

                //get user token entity to be removed
                var entityDriverToken = from d in context.Driver_token
                                        where d.token == drivertokenobj.key
                                        select d;

                if (entityDriverToken.Count() > 0)
                {
                    var currentEntityDriverToken = entityDriverToken.First();

                    //get driver 
                    var entitydriver = from d in context.Drivers
                                       where d.driverId == drivertokenobj.driverId
                                       select d;
                    entitydriver.First().current_lat = request.current_lat;
                    entitydriver.First().current_lng = request.current_lng;
                    entitydriver.First().last_updated = DateTime.Now;

                    context.SaveChanges();

                    return new DriverResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }
            }
        }

        
        public Response SendNotificationToDriver(SendNotificationRequest request)
        {

            System.Threading.ThreadPool.QueueUserWorkItem((x) =>
            {
                Helper.PushNotifications(request.deviceid, request.msg , request.bookingId);
            });

            return new Response() { sts = 0 };
        }


        public DriverResponse CheckDriverTokenService(CheckDriverTokenRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var drivertokenobj = Helper.AuthDriverToken(request.token, session);

                return new DriverResponse() { sts = 0 };
            }
        }

        #region ForCMSWeb
        public List<DriverVM> GetAllDrivers(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<DriverVM> result = new List<DriverVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "IC")
                        ett = ett.Where(m => m.ic.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarPlate")
                        ett = ett.Where(m => m.car_Plate.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarType")
                        ett = ett.Where(m => m.car_Type == int.Parse(filterQuery));
                    else if (filterBy == "Status")
                        ett = ett.Where(m => m.status == int.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderBy(m => m.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderBy(m => m.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderBy(m => m.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.status);
                    else if (orderBy == "LastUpdated")
                        ett = ett.OrderBy(m => m.last_updated);
                    else
                        ett = ett.OrderBy(m => m.driverId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderByDescending(m => m.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderByDescending(m => m.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderByDescending(m => m.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.status);
                    else if (orderBy == "LastUpdated")
                        ett = ett.OrderByDescending(m => m.last_updated);
                    else
                        ett = ett.OrderByDescending(m => m.driverId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    DriverVM vm = new DriverVM();
                    vm.driverId = v.driverId;
                    vm.name = v.name;
                    vm.ic = v.ic;
                    vm.car_Plate = v.car_Plate;
                    vm.car_Type = v.car_Type;
                    vm.status = v.status;
                    vm.last_updated = v.last_updated;
                    result.Add(vm);
                }
            }

            return result;
        }

        public List<DriverVMwithOnOffStatus> GetAllDrivers(int startIdx, int length, ref int TotalCount, ref int OnCount, ref int OffCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<DriverVMwithOnOffStatus> result = new List<DriverVMwithOnOffStatus>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          join e in context.Driver_token on d.driverId equals e.unique_id
                          select new
                          {
                              Driver = d,
                              OnOffStatus = (d.last_updated ?? DateTime.MinValue) >= EntityFunctions.AddMinutes(DateTime.Now, -6) && 
                                            (d.islogin_flag == null || (d.islogin_flag.HasValue && d.islogin_flag.Value)) 
                                            ? "On" : "Off" , 
                              Appversion = e.app_version , 
                              Apptype = e.app_type
                          };

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.Driver.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "IC")
                        ett = ett.Where(m => m.Driver.ic.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarPlate")
                        ett = ett.Where(m => m.Driver.car_Plate.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarType")
                        ett = ett.Where(m => m.Driver.car_Type == int.Parse(filterQuery));
                    else if (filterBy == "Status")
                        ett = ett.Where(m => m.Driver.status == int.Parse(filterQuery));
                }

                TotalCount = ett.Count();
                OnCount = ett.Where(m => m.OnOffStatus == "On").Count();
                OffCount = ett.Where(m => m.OnOffStatus == "Off").Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.Driver.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderBy(m => m.Driver.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderBy(m => m.Driver.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderBy(m => m.Driver.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.Driver.status);
                    else if (orderBy == "LastUpdated")
                        ett = ett.OrderBy(m => m.Driver.last_updated);
                    else if (orderBy == "OnOffStatus")
                        ett = ett.OrderBy(m => m.OnOffStatus).OrderBy(m => m.Driver.name);
                    else
                        ett = ett.OrderBy(m => m.Driver.driverId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.Driver.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderByDescending(m => m.Driver.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderByDescending(m => m.Driver.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderByDescending(m => m.Driver.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.Driver.status);
                    else if (orderBy == "LastUpdated")
                        ett = ett.OrderByDescending(m => m.Driver.last_updated);
                    else if (orderBy == "OnOffStatus")
                        ett = ett.OrderByDescending(m => m.OnOffStatus).OrderBy(m => m.Driver.name);
                    else
                        ett = ett.OrderByDescending(m => m.Driver.driverId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    DriverVMwithOnOffStatus vm = new DriverVMwithOnOffStatus();
                    vm.driverId = v.Driver.driverId;
                    vm.name = v.Driver.name;
                    vm.ic = v.Driver.ic;
                    vm.car_Plate = v.Driver.car_Plate;
                    vm.car_Type = v.Driver.car_Type;
                    vm.status = v.Driver.status;
                    vm.last_updated = v.Driver.last_updated;
                    vm.OnOffStatus = v.OnOffStatus;
                    vm.app_version = v.Appversion;
                    vm.app_type = v.Apptype ?? default(int);
                    result.Add(vm);
                }
            }

            return result;
        }

        public List<DriverVM> GetAllSuspendedDrivers(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<DriverVM> result = new List<DriverVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          where d.status == (int)DriverStatus.Suspended
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "IC")
                        ett = ett.Where(m => m.ic.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarPlate")
                        ett = ett.Where(m => m.car_Plate.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarType")
                        ett = ett.Where(m => m.car_Type == int.Parse(filterQuery));
                    else if (filterBy == "Status")
                        ett = ett.Where(m => m.status == int.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderBy(m => m.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderBy(m => m.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderBy(m => m.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.status);
                    else
                        ett = ett.OrderBy(m => m.driverId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderByDescending(m => m.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderByDescending(m => m.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderByDescending(m => m.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.status);
                    else
                        ett = ett.OrderByDescending(m => m.driverId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    DriverVM vm = new DriverVM();
                    vm.driverId = v.driverId;
                    vm.name = v.name;
                    vm.ic = v.ic;
                    vm.car_Plate = v.car_Plate;
                    vm.car_Type = v.car_Type;
                    vm.status = v.status;
                    result.Add(vm);
                }
            }

            return result;
        }

        public DriverVM GetDriver(int DriverId)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          where d.driverId == DriverId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();
                    DriverVM vm = new DriverVM();
                    vm.driverId = v.driverId;
                    vm.name = v.name;
                    vm.ic = v.ic;
                    vm.car_Plate = v.car_Plate;
                    vm.car_Type = v.car_Type;
                    vm.status = v.status;
                    vm.gender = v.gender;
                    vm.created_date = v.created_date;
                    vm.photo_url = Constants.uploadprefix + v.photo_url;
                    vm.dateofbirth = v.dateofbirth;
                    vm.phone = v.phone;
                    vm.current_lat = v.current_lat;
                    vm.current_lng = v.current_lng;

                    return vm;
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }
            }
        }

        public bool EditDriver(DriverVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          where d.driverId == vm.driverId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();

                    v.name = vm.name;
                    v.ic = vm.ic;
                    v.gender = vm.gender;
                    v.car_Plate = vm.car_Plate;
                    v.car_Type = vm.car_Type;
                    v.phone = vm.phone;
                    v.dateofbirth = vm.dateofbirth;

                    //password
                    if (vm.password != null && vm.password != "")
                    {
                        string key = Security.RandomString(60);
                        string pass = Security.checkHMAC(key, vm.password);

                        v.password = pass;
                        v.password_salt = key;
                    }
                    if (vm.status != null)
                    {
                        v.status = (int)vm.status;
                    }

                    v.last_updated = DateTime.Now;

                    context.SaveChanges();

                    return true;
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }
            }
        }

        public void EditDriverPhotoURL(int driverId, string PhotoURL)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          where d.driverId == driverId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();

                    v.photo_url = PhotoURL;

                    context.SaveChanges();
                }
                else
                {
                    throw new CustomException(CustomErrorType.DriverNotFound);
                }
            }
        }

        public int AddDriver(DriverVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                Driver v = new Driver();
                v.name = vm.name;
                v.ic = vm.ic;
                v.gender = vm.gender;
                v.car_Plate = vm.car_Plate;
                v.car_Type = vm.car_Type;

                v.phone = vm.phone;
                v.dateofbirth = vm.dateofbirth;

                //password
                string key = Security.RandomString(60);
                string pass = Security.checkHMAC(key, vm.password);

                v.password = pass;
                v.password_salt = key;

                v.status = (int)DriverStatus.Active;

                v.created_date = DateTime.Now;

                context.Drivers.Add(v);
                context.SaveChanges();

                return v.driverId;
            }
        }

        public List<int> GetAllNotAvailableDriversIds()
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var SystemAssignedDrivers = (from e in context.DriverBookings
                                             join f in context.Booking_trx on e.bookingId equals f.bookingId
                                             where e.status == (int)DriverBookingStatus.Rejected && f.booking_status == (int)BookingStatus.Pickup
                                             select e.driverId).Distinct();

                var ManualAssignedDrivers = (from d in context.Booking_trx
                                             where d.booking_status == (int)BookingStatus.Assigned && d.assigned_driverId != null
                                             select d.assigned_driverId == null ? 0 : (int)d.assigned_driverId).Distinct();
                
                return SystemAssignedDrivers.Union(ManualAssignedDrivers).ToList();
            }
        }

        public List<DriverVM> GetAllAvailableDrivers(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "", int BookingId = 0)
        {
            List<DriverVM> result = new List<DriverVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var NADriversIds = GetAllNotAvailableDriversIds();
                var ett = from d in context.Drivers
                          where (NADriversIds.Contains(d.driverId) == false)
                          && d.status != (int)DriverStatus.Inactive ///
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "IC")
                        ett = ett.Where(m => m.ic.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarPlate")
                        ett = ett.Where(m => m.car_Plate.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "CarType")
                        ett = ett.Where(m => m.car_Type == int.Parse(filterQuery));
                    else if (filterBy == "Status")
                        ett = ett.Where(m => m.status == int.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderBy(m => m.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderBy(m => m.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderBy(m => m.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.status);
                    else if (orderBy == "LastUpdated")
                        ett = ett.OrderBy(m => m.last_updated);
                    else
                        ett = ett.OrderBy(m => m.driverId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.name);
                    else if (orderBy == "IC")
                        ett = ett.OrderByDescending(m => m.ic);
                    else if (orderBy == "CarPlate")
                        ett = ett.OrderByDescending(m => m.car_Plate);
                    else if (orderBy == "CarType")
                        ett = ett.OrderByDescending(m => m.car_Type);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.status);
                    else if (orderBy == "LastUpdated")
                        ett = ett.OrderByDescending(m => m.last_updated);
                    else
                        ett = ett.OrderByDescending(m => m.driverId);
                }

                if (orderBy == "Nearest" && BookingId != 0)
                {
                    var Booking = from d in context.Booking_trx
                                  where d.bookingId == BookingId
                                  select d;

                    if (Booking.Count() > 0)
                    {
                        var ett_temp = (from d in ett
                                       select new Temp()
                                       {
                                           Driver = d,
                                           Distance = new double()
                                       }).ToList();

                        foreach (var v in ett_temp)
                        {
                            if (v.Driver.current_lat == null || v.Driver.current_lng == null)
                            {
                                v.Distance = double.MaxValue;
                            }
                            else
                            {
                                var driverCoord = new GeoCoordinate((double)v.Driver.current_lat, (double)v.Driver.current_lng);
                                var locationCoord = new GeoCoordinate((double)Booking.First().from_lat, (double)Booking.First().from_lng);
                                v.Distance = (driverCoord).GetDistanceTo(locationCoord);
                            }
                        }

                        ett = ett_temp.OrderBy(m => m.Distance).Select(m => m.Driver).AsQueryable();
                    }
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    DriverVM vm = new DriverVM();
                    vm.driverId = v.driverId;
                    vm.name = v.name;
                    vm.ic = v.ic;
                    vm.car_Plate = v.car_Plate;
                    vm.car_Type = v.car_Type;
                    vm.status = v.status;
                    vm.phone = v.phone;
                    vm.last_updated = v.last_updated;
                    result.Add(vm);
                }
            }

            return result;
        }

        public static Expression<Func<int, bool>> IsEven()
        {
            return number => number % 2 == 0;
        }

        public class Temp
        {
            public Driver Driver { get; set; }
            public double Distance { get; set; }
        }

        public int GetDriverCount()
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          select d;

                return ett.Count();
            }
        }

        public int GetDriverIdByIC(string DriverIC)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Drivers
                          where d.ic == DriverIC
                          select d;

                if (ett.Count() > 0)
                {
                    return ett.First().driverId;
                }
                else
                {
                    return 0;
                }
            }
        }

        #endregion

    }
}
