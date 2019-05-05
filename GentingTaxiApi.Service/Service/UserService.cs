using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using System.Data.Entity.Validation;
using System.Net;
using Quarks.EnumExtensions;
using System.ComponentModel;
using GentingTaxiApi.Service.Constant;
using System.Device.Location;

namespace GentingTaxiApi.Service
{
    public class UserService
    {
        const int nearbyradius = 1000; //1km radius

        public UserResponse RegisterUserService(RegisterUserRequest request )
        {
            //create user 
            using (var context = new entity.gtaxidbEntities())
            {
                //check existing username
                var entityuser = from d in context.Users
                                 where d.username == request.username
                                 select d;

                //check matching password 
                if(request.password != request.confirm_password)
                {
                    throw new CustomException(CustomErrorType.UserPasswordMatchfailed);
                }

                if (entityuser == null || entityuser.Count() == 0)
                {
                    entity.User newUser = new entity.User();
                    newUser.username = request.username;
                    newUser.email = request.username;
                    newUser.name = request.name;
                    newUser.phone = request.countrycode + request.phone.TrimStart('0');

                    //create one way hash password token
                    string key = Security.RandomString(60);
                    string pass = Security.checkHMAC(key, request.password);

                    newUser.password = pass;
                    newUser.password_salt = key;
                    newUser.created_date = DateTime.Now;
                    newUser.status = (int)UserStatus.Inactive;

                    //generate activation code 
                    var code = Guid.NewGuid().ToString().Substring(0, 6);
                    newUser.activation_code = code;

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    //send sms
                    if(!string.IsNullOrEmpty(newUser.phone))
                    {
                        if (newUser.phone.Length == 10 || newUser.phone.Length == 11)
                        {
                            var msg = "Genting Taxi " + Environment.NewLine + "Verification code : " + code;

                            bool issent = Helper.SendSMS(newUser.activation_code, newUser.phone, msg , newUser.userId , (int)UserType.user);

                            if(issent)
                            {
                                //update sent time 
                                newUser.code_sent_date = DateTime.Now;

                                context.SaveChanges();
                            }

                        }
                    }

                    //send email
                    Helper.SendEmail(newUser.email, newUser.activation_code);

                    return new UserResponse() { sts = 0, pin = code };
                }
                else
                {
                    return new UserResponse() { sts = 1, msg = CustomErrorType.UserAlreadyAssign.GetAttributeDescription() };
                }
            }
        }

        public UserResponse ActivateUserService(ActivateUserRequest request)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //check user created 
                var entityuser = from d in context.Users
                                 where d.username == request.username && d.activation_code == request.pin
                                 select d;
                if (entityuser.Count() > 0)
                {
                    var userobj = entityuser.First();
                    userobj.status = (int)UserStatus.Active;
                    context.SaveChanges();

                    //return token if successfully activated 
                    var entityusertoken = from d in context.User_token
                                          where d.unique_id == userobj.userId
                                          select d; 

                    string tokenstr = ""; 

                    if(entityusertoken.Count() > 0)
                    {
                        var usertokenobj = entityusertoken.First();
                        string apikey = usertokenobj.token;
                        tokenstr = Helper.EncodeTo64(new string[] { apikey, userobj.username, userobj.userId.ToString() });
                    }

                    return new UserResponse() { sts = 0 , token = tokenstr };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public UserResponse ResendUserCodeService(ResendUserCodeRequest request)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //check username exist 
                var entityuser = from d in context.Users
                                 where d.username == request.username 
                                 select d;

                if (entityuser.Count() > 0)
                {
                    bool isActivated = false;
                    bool isWithinTimeFrame = false;

                    var user = entityuser.First(); 

                    //check activation 
                    if (user.status == (int)UserStatus.Active) isActivated = true; 

                    //check time frame 
                    DateTime codesenttime;
                    if (DateTime.TryParse(user.code_sent_date.ToString(), out codesenttime))
                    {
                        if (DateTime.Now.Subtract(codesenttime).TotalMinutes < 5) isWithinTimeFrame = true; 
                    }

                    if(!isActivated && !isWithinTimeFrame)
                    {
                        //re-send activation code and update profile is meet criteria 
                        //create new activation code 
                        var code = Guid.NewGuid().ToString().Substring(0, 6);

                        //update new code 
                        entityuser.First().activation_code = code;
                        context.SaveChanges();

                        bool isValidPhoneNumber = false;

                        string phonenum = request.countrycode + request.phone.TrimStart('0');

                        //send sms
                        if (!string.IsNullOrEmpty(request.phone))
                        {
                            if (phonenum.Length == 10 || phonenum.Length == 11)
                            {
                                var msg = "Genting Taxi " + Environment.NewLine + "Verification code : " + code;

                                bool issent = Helper.SendSMS(code, phonenum, msg, user.userId, (int)UserType.user);

                                //update sent time 
                                if (issent)
                                {
                                    user.code_sent_date = DateTime.Now;
                                    context.SaveChanges();

                                    //update user phone number 
                                    user.phone = phonenum; 

                                    context.SaveChanges();

                                    isValidPhoneNumber = true;
                                }
                            }
                        }

                        if (!isValidPhoneNumber) throw new CustomException(CustomErrorType.InvalidPhoneNumber);

                        return new UserResponse() { sts = 0, pin = code };
                    }
                    else if(isActivated)
                    {
                        throw new CustomException(CustomErrorType.UnableSentCodeActivated);
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.UnableSentCodeWithinTime);
                    }
                    
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public UserResponse LoginUserService(LoginUserRequest request)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //check user exist 
                var entityuser = from d in context.Users
                                 where d.username == request.username
                                 select d;

                if (entityuser.Count() > 0 &&
                    (Security.checkHMAC(
                        entityuser.First().password_salt, request.password) == entityuser.First().password
                        )
                    )
                {
                    var userobj = entityuser.First();

                    //update login status 
                    userobj.islogin_flag = true;
                    context.SaveChanges(); 

                    //return user 
                    UserVM user = new UserVM();
                    user.username = userobj.username;
                    user.email = userobj.email;
                    user.phone = userobj.phone;
                    user.name = userobj.name;
                    user.userId = userobj.userId;
                    user.status = userobj.status;

                    user.isRWuser = user.email.Contains("@rwgenting.com") ? 1 : 0;

                    //generate user token
                    string apikey = Helper.GenerateUserToken(request.username, user.userId, request.deviceId);
                    string tokenstr = "";

                    //only return token if activated 
                    if(userobj.status == (int)UserStatus.Active)
                    {
                        tokenstr = Helper.EncodeTo64(new string[] { apikey, request.username, user.userId.ToString() });
                    }

                    return new UserResponse()
                    {
                        sts = 0,
                        result = user,
                        token = tokenstr
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserInvalid);
                }
            }
        }

        public bool ValidateUserApp(int userID)
        {
            using(var context = new entity.gtaxidbEntities())
            {
                var usertokenentities = from d in context.User_token
                                        where d.unique_id == userID
                                        select d;
                if (usertokenentities.Count() > 0)
                {
                    bool isCurrentVersion = false;

                    var usertokenobj = usertokenentities.First(); 

                    //get adminsetting 
                    var adminsettingsentities = from d in context.AdminSettings
                                        select d;

                    if (adminsettingsentities.Count() > 0)
                    {
                        if (usertokenobj.app_type == (int)App_Type.android)
                        {
                            if (adminsettingsentities.First().current_user_android_app_version == usertokenobj.app_version)
                                isCurrentVersion = true;
                        }

                        if (usertokenobj.app_type == (int)App_Type.ios)
                        {
                            if (adminsettingsentities.First().current_user_ios_app_version == usertokenobj.app_version)
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

        public UserResponse UpdateUserDeviceIdService(UpdateUserDeviceIdRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                var entityuserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                        select d;

                if (entityuserToken.Count() > 0)
                {
                    var entityusertokenobj = entityuserToken.First();

                    if (string.IsNullOrEmpty(entityusertokenobj.deviceId))
                    {
                        entityusertokenobj.deviceId = request.deviceId;
                        context.SaveChanges();
                    }


                    return new UserResponse()
                    {
                        sts = 0
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public UserResponse GetCurrentUserService(GetCurrentUserRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                if (usertokenobj != null)
                {
                    var userid = usertokenobj.userId;
                    //get User
                    var entityuser = from d in context.Users
                                     where d.userId == userid
                                     select d;
                    if (entityuser.First().status == (int)UserStatus.Inactive)
                    {
                        throw new CustomException(CustomErrorType.Unauthenticated);
                    }

                    //return user 
                    UserVM user = new UserVM();
                    user.username = entityuser.First().username;
                    user.email = entityuser.First().email;
                    user.phone = entityuser.First().phone;
                    user.name = entityuser.First().name;
                    user.userId = entityuser.First().userId;

                    return new UserResponse()
                    {
                        sts = 0,
                        result = user
                    };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }

            }
        }

        public UserResponse LogoutUserService(LogoutCurrentUserRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.unique_id == request.userid
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var entityUsers = from d in context.Users
                                     where d.userId == request.userid
                                     select d;

                    if (entityUsers.Count() > 0)
                    {
                        //update login status 
                        entityUsers.First().islogin_flag = false;
                        context.SaveChanges(); 
                    }

                    var currentEntityUserToken = entityUserToken.First();
                    context.User_token.Remove(currentEntityUserToken);
                    context.SaveChanges();

                    return new UserResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }

            }
        }

        public UserResponse UpdateCurrentUser(UpdateUserRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var userId = entityUserToken.First().unique_id;
                    var entityuser = from d in context.Users
                                     where d.userId == userId
                                     select d;

                    var userobj = entityuser.First();
                    userobj.name = request.name;
                    //userobj.phone = request.countrycode + request.phone.TrimStart('0');
                    //userobj.email = request.email;

                    userobj.last_updated = DateTime.Now;

                    context.SaveChanges();

                    return new UserResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public PreferredDriverResponse GetCurrentUserPreferredDriverService(
            GetCurrentUserPreferredDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var preferredDriverEntity = context.Database.SqlQuery<DriverVM>(
                                            "Select t1.name , t1.ic , t1.gender , t1.car_Plate ,  " +
                                            "t1.priority , t1.photo_url , t1.car_Type , t0.driverId " +
                                            "from Preferred_driver t0 " +
                                            "Left join Driver t1 on t0.driverId = t1.driverId " +
                                            "Where t0.userId = {0} and t1.status = 1 ",
                                            usertokenobj.userId
                                        );
                    List<DriverVM> prefferedDriverlist = new List<DriverVM>();

                    if (preferredDriverEntity.Count() > 0)
                    {

                        prefferedDriverlist = preferredDriverEntity.ToList();

                        foreach (var driver in prefferedDriverlist)
                        {
                            driver.photo_url = string.IsNullOrEmpty(driver.photo_url) ? "" : Constants.uploadprefix + driver.photo_url;
                        }
                    }

                    return new PreferredDriverResponse() { sts = 0, result = prefferedDriverlist };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public PreferredDriverResponse SetCurrentUserPreferredDriverService(
            SetCurrentUserPreferredDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var userid = entityUserToken.First().unique_id;
                    //check existing 
                    var userpreferredEntity = from d in context.Preferred_driver
                                              where d.driverId == request.driverID && d.userId == userid
                                              select d;

                    //create if not exist 
                    if (userpreferredEntity.Count() == 0)
                    {
                        entity.Preferred_driver newEntity = new entity.Preferred_driver();
                        newEntity.userId = userid;
                        newEntity.driverId = request.driverID;
                        newEntity.created_date = DateTime.Now;

                        context.Preferred_driver.Add(newEntity);
                        context.SaveChanges();
                    }

                    return new PreferredDriverResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public PreferredDriverResponse DeleteCurrentUserPreferredDriverService(
            DeleteCurrentUserPreferredDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var userid = entityUserToken.First().unique_id;
                    //check existing 
                    var userpreferredEntity = from d in context.Preferred_driver
                                              where d.driverId == request.did && d.userId == userid
                                              select d;

                    //delete if exist 
                    if (userpreferredEntity.Count() > 0)
                    {
                        context.Preferred_driver.Remove(userpreferredEntity.First());
                        context.SaveChanges();
                    }

                    return new PreferredDriverResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }
        public UserResponse SubmitFeedbackService(
           SubmitFeedbackRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var newentityFeedback = new entity.Feedback();
                    newentityFeedback.remarks = request.remarks;
                    newentityFeedback.bookingid = request.bookingid;
                    newentityFeedback.status = (int)FeedbackStatus.Active;
                    newentityFeedback.userId = entityUserToken.First().unique_id; 
                    context.Feedbacks.Add(newentityFeedback);
                    context.SaveChanges();

                    //update booking id feedback status 
                    var bookingentity = from d in context.Booking_trx
                                        where d.bookingId == request.bookingid
                                        select d;

                    if(bookingentity.Count() > 0)
                    {
                        bookingentity.First().feedbackstatus = (int)FeedbackStatus.Active;
                        context.SaveChanges();
                    }

                    return new UserResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }


        public PreferredDriverResponse GetDriversRequestByUserService(GetAllDriverRequest request, ServiceStack.Auth.IAuthSession session = null )
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var entitydrivers = context.Database.SqlQuery<DriverVM>(
                            "select * from Driver where status = {0}", (int)DriverStatus.Active
                            );

                    List<DriverVM> driverlist = new List<DriverVM>();

                    if(entitydrivers.Count()> 0 )
                    {
                        driverlist = entitydrivers.ToList();
                        foreach (var driver in driverlist)
                        {
                            driver.photo_url = string.IsNullOrEmpty(driver.photo_url) ? "" : Constants.uploadprefix + driver.photo_url;
                        }
                    }

                    return new PreferredDriverResponse() { result = driverlist ,  sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public DriverLocationResponse GetDriversLocationRequestByUserService(GetDriverLocationRequest request, 
                        ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                //get user token entity to be removed
                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    //get driver 
                    var entitydriver = from d in context.Drivers
                                       join c in context.Booking_trx on d.driverId equals c.assigned_driverId
                                       where c.bookingId == request.bookingId
                                       select d;

                    if (entitydriver.Count() > 0)
                    {
                        return new DriverLocationResponse() {
                            lat = entitydriver.First().current_lat.ToString(),
                            lng = entitydriver.First().current_lng.ToString(),
                            sts = 0 };
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.DriverNotFound);
                    }

                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public BookingFromListResponse GetBookingFromRequestByUserService(GetCurrentUserBookingFromRequest request,
                   ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var userid = entityUserToken.First().unique_id;

                    var entityBookings = context.Database.SqlQuery<BookingRecentVM>(
                            "select top 15 " +
                            "booking_datetime , from_locationname as 'from' , from_lat as frompx ,from_lng as frompy " +
                            "from Booking_trx " +
                            "where userId = {0} and booking_datetime is not null " +
                            "order by booking_datetime desc ",
                            userid
                        );

                    if (entityBookings.Count() > 0)
                    {

                        List<BookingRecentVM> bookingrecentlist = new List<BookingRecentVM>();

                        DateTime? currentbookingDatetime = null;
                        string currentloc = "";

                        foreach (var bookingitem in entityBookings)
                        {
                            bookingitem.date = (bookingitem.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm");

                            if (currentbookingDatetime != ((DateTime)bookingitem.booking_datetime).Date ||
                                currentloc != bookingitem.from)
                            {
                                currentbookingDatetime = ((DateTime)bookingitem.booking_datetime).Date;
                                currentloc = bookingitem.from;

                                bookingrecentlist.Add(bookingitem);
                            }

                        }

                        return new BookingFromListResponse()
                        {
                            /*
                            result = entityBookings.Select(x =>
                            {
                                x.date = (x.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:MM");
                                return x;
                            }).ToList(),   */
                            result = bookingrecentlist,
                            sts = 0
                        };
                    }
                    else
                    {
                        //return empty result 
                        return new BookingFromListResponse() { sts = 0 }; 
                    }

                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public BookingToListResponse GetBookingToRequestByUserService(GetCurrentUserBookingToRequest request,
           ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    var userid = entityUserToken.First().unique_id;

                    var entityBookings = context.Database.SqlQuery<BookingRecentVM>(
                            "select top 15 " +
                            "booking_datetime , to_locationname as 'to' , to_lat as topx ,to_lng as topy " +
                            "from Booking_trx " +
                            "where userId = {0} and booking_datetime is not null " +
                            "order by booking_datetime desc ",
                            userid
                        );

                    if (entityBookings.Count() > 0)
                    {

                        List<BookingRecentVM> bookingrecentlist = new List<BookingRecentVM>();

                        DateTime? currentbookingDatetime = null;
                        string currentloc = "";

                        foreach (var bookingitem in entityBookings)
                        {
                            bookingitem.date = (bookingitem.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm");

                            if (currentbookingDatetime != ((DateTime)bookingitem.booking_datetime).Date ||
                                currentloc != bookingitem.from)
                            {
                                currentbookingDatetime = ((DateTime)bookingitem.booking_datetime).Date;
                                currentloc = bookingitem.from;

                                bookingrecentlist.Add(bookingitem);
                            }

                        }

                        return new BookingToListResponse()
                        {
                            /*
                            result = entityBookings.Select(x =>
                            {
                                x.date = (x.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:MM");
                                return x;
                            }).ToList(),   */
                            result = bookingrecentlist,
                            sts = 0
                        };
                    }
                    else
                    {
                        //return empty 
                        return new BookingToListResponse()
                        {
                            sts = 0
                        };
                    }

                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        
        public UserResponse CheckUserTokenService(CheckUserTokenRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                return new UserResponse() { sts = 0 }; 
            }
        }

        public NearByDriverResponse GetNearbyDriverService(GetNearbyDriversRequest request, ServiceStack.Auth.IAuthSession session = null )
        {
            using (var context = new entity.gtaxidbEntities())
            {
                 var usertokenobj = Helper.AuthToken(request.token, session);

                var entityUserToken = from d in context.User_token
                                      where d.token == usertokenobj.key
                                      select d;

                if (entityUserToken.Count() > 0)
                {
                    List<DriverVM> Nearbydrivers = new List<DriverVM>();
                    List<CoordinateVM> NearbyCoords = new List<CoordinateVM>();

                    if (request.lat > 0 && request.lng > 0)
                    {
                        //get driver list 
                        var driverEntities = context.Database.SqlQuery<DriverVM>(
                                        "Select * from Driver " +
                                        "where status = {0} " , (int)DriverStatus.Active
                                        ).ToList();

                        if (driverEntities.Count() > 0)
                        {
                            double distance = 0;

                            foreach (var driver in driverEntities)
                            {
                                //calculate distance  
                                decimal driver_lat = driver.current_lat ?? default(decimal);
                                decimal driver_lng = driver.current_lng ?? default(decimal);
                                var driverCoord = new GeoCoordinate((double)driver_lat, (double)driver_lng);

                                var userCoord = new GeoCoordinate((double)request.lat, (double)request.lng);

                                distance = driverCoord.GetDistanceTo(userCoord);
                                driver.distance = distance;
                            }

                            //get 10 nearest driver 
                            Nearbydrivers = (from e in driverEntities
                                             where e.distance < nearbyradius
                                              orderby e.distance ascending
                                              select e).Skip(0).Take(10).ToList();

                            foreach (var nearbydriver in Nearbydrivers)
                            {
                                NearbyCoords.Add(new CoordinateVM() { lat = nearbydriver.current_lat ?? default(decimal), lng = nearbydriver.current_lng ?? default(decimal) });
                            }
                        }
                    }

                    return new NearByDriverResponse() { result = NearbyCoords , sts = 0 }; 
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }


        #region ForCMSWeb
        public List<UserVM> GetAllUsers(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<UserVM> result = new List<UserVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Users
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Username")
                        ett = ett.Where(m => m.username.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Email")
                        ett = ett.Where(m => m.email.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Status")
                        ett = ett.Where(m => m.status == int.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.name);
                    else if (orderBy == "Username")
                        ett = ett.OrderBy(m => m.username);
                    else if (orderBy == "Email")
                        ett = ett.OrderBy(m => m.email);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.status);
                    else
                        ett = ett.OrderBy(m => m.userId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.name);
                    else if (orderBy == "Username")
                        ett = ett.OrderByDescending(m => m.username);
                    else if (orderBy == "Email")
                        ett = ett.OrderByDescending(m => m.email);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.status);
                    else
                        ett = ett.OrderByDescending(m => m.userId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    UserVM vm = new UserVM();
                    vm.email = v.email;
                    vm.name = v.name;
                    vm.phone = v.phone;
                    vm.status = v.status;
                    vm.userId = v.userId;
                    vm.username = v.username;
                    vm.created_date = v.created_date;
                    result.Add(vm);
                }
            }

            return result;
        }

        public List<UserVM> GetAllSuspendedUsers(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<UserVM> result = new List<UserVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Users
                          where d.status == (int)UserStatus.Suspended
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Username")
                        ett = ett.Where(m => m.username.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Email")
                        ett = ett.Where(m => m.email.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Status")
                        ett = ett.Where(m => m.status == int.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.name);
                    else if (orderBy == "Username")
                        ett = ett.OrderBy(m => m.username);
                    else if (orderBy == "Email")
                        ett = ett.OrderBy(m => m.email);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.status);
                    else
                        ett = ett.OrderBy(m => m.userId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.name);
                    else if (orderBy == "Username")
                        ett = ett.OrderByDescending(m => m.username);
                    else if (orderBy == "Email")
                        ett = ett.OrderByDescending(m => m.email);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.status);
                    else
                        ett = ett.OrderByDescending(m => m.userId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    UserVM vm = new UserVM();
                    vm.email = v.email;
                    vm.name = v.name;
                    vm.phone = v.phone;
                    vm.status = v.status;
                    vm.userId = v.userId;
                    vm.username = v.username;
                    vm.created_date = v.created_date;
                    result.Add(vm);
                }
            }

            return result;
        }

        public UserVM GetUser(int UserId)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Users
                          where d.userId == UserId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();
                    UserVM vm = new UserVM();
                    vm.userId = v.userId;
                    vm.email = v.email;
                    vm.name = v.name;
                    vm.phone = v.phone;
                    vm.status = v.status;
                    vm.userId = v.userId;
                    vm.username = v.username;
                    vm.created_date = v.created_date;

                    return vm;
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public bool EditUser(UserVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Users
                          where d.userId == vm.userId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();

                    v.name = vm.name;
                    v.email = vm.email;
                    v.phone = vm.phone;
                    v.username = vm.username;
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
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public int GetUserCount()
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Users
                          select d;

                return ett.Count();
            }
        }

        public List<FeedbackVM> GetAllUserFeedbacks(int startIdx, int length, ref int TotalCount)
        {
            List<FeedbackVM> result = new List<FeedbackVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Feedbacks
                          join e in context.Users on d.userId equals e.userId
                          where d.status == (int)FeedbackStatus.Active
                          orderby d.feedbackId descending
                          select new
                          {
                              Feedback = d,
                              User = e
                          };

                TotalCount = ett.Count();
                ett = ett.Skip(startIdx).Take(length);

                foreach (var v in ett)
                {
                    FeedbackVM item = new FeedbackVM();
                    item.feedbackId = v.Feedback.feedbackId;
                    item.remarks = v.Feedback.remarks;
                    item.status = v.Feedback.status;

                    item.User = new UserVM();
                    item.User.userId = v.User.userId;
                    item.User.name = v.User.name;
                    item.User.email = v.User.email;

                    result.Add(item);
                }
            }

            return result;
        }

        public FeedbackVM GetUserFeedback(int FeedbackId)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Feedbacks
                          join e in context.Users on d.userId equals e.userId
                          where d.feedbackId == FeedbackId
                          && d.status == (int)FeedbackStatus.Active
                          select new
                          {
                              Feedback = d,
                              User = e
                          };

                if (ett.Count() > 0)
                {
                    var v = ett.First();

                    FeedbackVM item = new FeedbackVM();
                    item.feedbackId = v.Feedback.feedbackId;
                    item.remarks = v.Feedback.remarks;
                    item.status = v.Feedback.status;

                    item.User = new UserVM();
                    item.User.userId = v.User.userId;
                    item.User.name = v.User.name;
                    item.User.email = v.User.email;

                    //next-previous
                    var Feedbacks = from d in context.Feedbacks
                                    where d.status == (int)FeedbackStatus.Active
                                    select d;

                    var next = Feedbacks.Where(obj => obj.feedbackId < FeedbackId).OrderByDescending(d => d.feedbackId);
                    if (next.Count() > 0)
                    {
                        item.Next = next.First().feedbackId;
                    }
                    else
                    {
                        item.Next = 0;
                    }
                    var previous = Feedbacks.Where(obj => obj.feedbackId > FeedbackId).OrderBy(d => d.feedbackId);
                    if (previous.Count() > 0)
                    {
                        item.Previous = previous.First().feedbackId;
                    }
                    else
                    {
                        item.Previous = 0;
                    }

                    return item;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion
    }
}