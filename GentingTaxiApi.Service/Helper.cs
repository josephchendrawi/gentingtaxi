using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;
using GentingTaxiApi.Service;
using GentingTaxiApi.Interface.Types;
using ServiceStack;
using PushSharp;
using PushSharp.Android;
using PushSharp.Apple;
using PushSharp.Core;
using System.IO;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using RestSharp;
using System.Threading;
using System.Reflection;
using Quarks.EnumExtensions;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using Microsoft.AspNet.SignalR;
using GentingTaxiApi.Hubs;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace GentingTaxiApi.Service
{
    public class Helper
    {
        public static string serverpath = ConfigurationManager.AppSettings["UploadPath"];

        public static UserTokenVM AuthToken(string requestToken , ServiceStack.Auth.IAuthSession session)
        {
            string key = ""; int userId = 0;
            //check authentication
            if (session == null)
            {
                //if servstack session not available , manually authenticate from request 
                var decoded = Helper.DecodeFrom64(requestToken);
                AuthUserSession userauth = Helper.checkAuth(decoded[0], decoded[1], decoded[2]);

                key = decoded[0];
            }
            else
            {
                //check exception
                if (!session.IsAuthenticated) { throw new CustomException(CustomErrorType.Unauthenticated);  }

                //servstack session available , use request header for key authentication
                key = session.Id;
            }

            using (var context = new entity.gtaxidbEntities())
            {
                var entityUserToken = from d in context.User_token
                                    where d.token == key
                                    select d;
                if(entityUserToken.Count() > 0){
                    userId = entityUserToken.First().unique_id;

                    //return key after token authentication
                    return new UserTokenVM()
                    {
                        key = key,
                        userId = userId
                    };
                }
                else
                {
                    return null;
                }
            }

        }

        public static string GenerateUserToken(string username, int userID , string deviceId = "" , string app_version = "" , int app_type = 0)
        {
            string tokensalt = Security.RandomString(60);
            string token = Security.Encrypt(tokensalt, username.ToLower());

            using (var context = new entity.gtaxidbEntities())
            {
                var entityUser = from d in context.Users
                                   where d.username.ToLower() == username.ToLower()
                                   select d;

                if (entityUser.Count() > 0)
                {
                    var entityUserToken = from d in context.User_token
                                     where d.unique_id == userID
                                          select d;
                    //case exist , update token entry
                    if (entityUserToken.Count() > 0)
                    {
                        entityUserToken.First().token = token;
                        entityUserToken.First().token_salt = tokensalt;
                        entityUserToken.First().token_expire = DateTime.UtcNow.AddMonths(1);
                        entityUserToken.First().last_updated = DateTime.Now;
                        entityUserToken.First().app_version = app_version;
                        entityUserToken.First().app_type = app_type;

                        if (entityUserToken.First().deviceId != deviceId)
                        {
                            //update if not matching 
                            entityUserToken.First().deviceId = deviceId; 
                        }
                        context.SaveChanges();
                    }
                    //case nonexist , create token entry
                    else
                    {
                        entity.User_token newentity = new entity.User_token();
                        newentity.token = token;
                        newentity.token_salt = tokensalt;
                        newentity.token_expire = DateTime.UtcNow.AddMonths(1);
                        newentity.unique_id = userID;
                        newentity.created_date = DateTime.UtcNow;
                        newentity.deviceId = deviceId;
                        newentity.app_version = app_version;
                        newentity.app_type = app_type;

                        //context.AddTodsRunnerAPIs(newentity);
                        context.User_token.Add(newentity);
                        context.SaveChanges();
                    }

                    return token;
                }
            }
            return null;
        }

        public static AuthUserSession checkAuth(string key, string username, string userIdstr)
        {
            try
            {
                using (var context = new entity.gtaxidbEntities())
                {
                    var entityUser = from d in context.Users
                                       where d.username.ToLower() == username.ToLower()
                                       select d;
                    if (entityUser.Count() > 0)
                    {
                        int id;
                        int userId = int.TryParse(userIdstr, out id) ? (int)id : 0;

                        var entityUserToken = from d in context.User_token
                                              where d.token == key 
                                         && d.token_expire >= DateTime.UtcNow
                                         && d.unique_id == userId
                                         select d;
                        if (entityUserToken.Count() > 0)
                        {
                            return new AuthUserSession
                            {
                                FullName = entityUser.First().name,
                                Email = entityUser.First().email,
                                PhoneNumber = entityUser.First().phone,
                                UserName = entityUser.First().username,
                            };
                        }
                    }
                }
            }
            catch
            {
                throw new CustomException(CustomErrorType.Unauthenticated);
            }

            return null;
        }

        public static DriverTokenVM AuthDriverToken(string requestToken, ServiceStack.Auth.IAuthSession session)
        {
            string key = ""; int driverId = 0;
            //check authentication
            if (session == null)
            {
                //if servstack session not available , manually authenticate from request 
                var decoded = Helper.DecodeFrom64(requestToken);
                AuthUserSession driverauth = Helper.checkDriverAuth(decoded[0], decoded[1], decoded[2]);

                key = decoded[0];
            }
            else
            {
                //check exception
                if (!session.IsAuthenticated) { throw new CustomException(CustomErrorType.Unauthenticated); }

                //servstack session available , use request header for key authentication
                key = session.Id;
            }

            using (var context = new entity.gtaxidbEntities())
            {
                var entityDriverToken = from d in context.Driver_token
                                      where d.token == key
                                      select d;
                if (entityDriverToken.Count() > 0)
                {
                    driverId = entityDriverToken.First().unique_id;

                    //return key after token authentication
                    return new DriverTokenVM()
                    {
                        key = key,
                        driverId = driverId
                    };
                }
                else
                {
                    return null;
                }
            }

        }

        public static string GenerateDriverToken(string username, int driverID , string deviceId , string app_version = "" , int app_type = 0)
        {
            string tokensalt = Security.RandomString(60);
            string token = Security.Encrypt(tokensalt, username.ToLower());

            using (var context = new entity.gtaxidbEntities())
            {
                var entityDriver = from d in context.Drivers
                                 where d.ic.ToLower() == username.ToLower()
                                 select d;

                if (entityDriver.Count() > 0)
                {
                    var entityDriverToken = from d in context.Driver_token
                                          where d.unique_id == driverID
                                          select d;
                    //case exist , update token entry
                    if (entityDriverToken.Count() > 0)
                    {
                        entityDriverToken.First().token = token;
                        entityDriverToken.First().token_salt = tokensalt;
                        entityDriverToken.First().token_expire = DateTime.UtcNow.AddMonths(1);
                        entityDriverToken.First().last_updated = DateTime.Now;
                        entityDriverToken.First().app_version = app_version;
                        entityDriverToken.First().app_type = app_type;

                        if (entityDriverToken.First().deviceid != deviceId)
                        {
                            //update if not matching 
                            entityDriverToken.First().deviceid = deviceId;
                        }
                        context.SaveChanges();
                    }
                    //case nonexist , create token entry
                    else
                    {
                        entity.Driver_token newentity = new entity.Driver_token();
                        newentity.token = token;
                        newentity.token_salt = tokensalt;
                        newentity.token_expire = DateTime.UtcNow.AddMonths(1);
                        newentity.unique_id = driverID;
                        newentity.created_date = DateTime.UtcNow;
                        newentity.deviceid = deviceId;
                        newentity.app_version = app_version;
                        newentity.app_type = app_type;

                        //context.AddTodsRunnerAPIs(newentity);
                        context.Driver_token.Add(newentity);
                        context.SaveChanges();
                    }

                    return token;
                }
            }
            return null;
        }

        public static AuthUserSession checkDriverAuth(string key, string ic, string driverIdStr)
        {
            try
            {
                using (var context = new entity.gtaxidbEntities())
                {
                    var entitydriver = from d in context.Drivers
                                     where d.ic.ToLower() == ic.ToLower()
                                     select d;
                    if (entitydriver.Count() > 0)
                    {
                        int id;
                        int driverId = int.TryParse(driverIdStr, out id) ? (int)id : 0;

                        var entityDriveroken = from d in context.Driver_token
                                              where d.token == key
                                         && d.token_expire >= DateTime.UtcNow
                                         && d.unique_id == driverId
                                              select d;
                        if (entityDriveroken.Count() > 0)
                        {
                            return new AuthUserSession
                            {
                                FullName = entitydriver.First().name,
                                UserName = entitydriver.First().ic,
                            };
                        }
                    }
                }
            }
            catch
            {
                throw new CustomException(CustomErrorType.Unauthenticated);
            }

            return null;
        }

        public static string EncodeTo64(string[] StringsToEncode)
        {
            string toEncode = "";
            foreach (var i in StringsToEncode)
            {
                toEncode = toEncode + i + "|";
            }
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public static string[] DecodeFrom64(string encodedData)
        {
            try
            {
                byte[] encodedDataAsBytes
                    = System.Convert.FromBase64String(encodedData);
                string decoded =
                   System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

                string[] returnValue = decoded.Split('|');

                return returnValue;
            }
            catch
            {
                throw new CustomException(CustomErrorType.TokenNotFound);
            }
        }

        public static void PushNotificationsToDriver(int driverId, string message, int bookingId = 0)
        {
                using (var context = new entity.gtaxidbEntities())
                {
                    //get driver login information
                    var driverEntity = from d in context.Driver_token
                                       where d.unique_id == driverId
                                       select d;

                    if (driverEntity.Count() > 0)
                    {
                        var driverEntityobj = driverEntity.First();
                        var deviceid = driverEntityobj.deviceid;

                        ThreadPool.QueueUserWorkItem((x) =>
                        {
                            PushNotifications(deviceid, message, bookingId);
                        });
                    }
                }
        }

        public static void PushNotificationsToUser(int userId, string message, int bookingId = 0)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //get driver login information
                var userEntity = from d in context.User_token
                                   where d.unique_id == userId
                                   select d;

                if (userEntity.Count() > 0)
                {
                    var userEntityobj = userEntity.First();
                    var deviceid = userEntityobj.deviceId;

                    ThreadPool.QueueUserWorkItem((x) =>
                    {
                        PushNotifications(deviceid, message, bookingId);
                    });
                }
            }
        }


        public static bool SendSMS(string code , string phonenum , string message , int targetID = 0 , int userType = 0)
        {
            bool issent = true;

            var client = new RestClient("https://www.etracker.cc");

            /*
            var request = new RestRequest("mes/mesbulk.aspx/", Method.POST);
            request.AddQueryParameter("user", "srihighlandapi");
            request.AddQueryParameter("pass", "SHC28257ssb");
            request.AddQueryParameter("type", "0");
            request.AddQueryParameter("to", phonenum);
            request.AddQueryParameter("from", "Genting Taxi");
            request.AddQueryParameter("text", message);
            request.AddQueryParameter("servid", "MES01");
             * */

            var request = new RestRequest(ConfigurationManager.AppSettings["sms_endpoint"], Method.POST);
            request.AddQueryParameter("user", ConfigurationManager.AppSettings["sms_user"]);
            request.AddQueryParameter("pass", ConfigurationManager.AppSettings["sms_password"]);
            request.AddQueryParameter("type", "0");
            request.AddQueryParameter("to", phonenum);
            request.AddQueryParameter("from", "Genting Taxi");
            request.AddQueryParameter("text", message);
            request.AddQueryParameter("servid", "MES01");

            /*
            var request = new RestRequest("bulksms/mesapi.aspx", Method.POST);
            request.AddQueryParameter("user", "TEST017");
            request.AddQueryParameter("pass", "94PK3lv\"");
            request.AddQueryParameter("type", "0");
            request.AddQueryParameter("to", phonenum);
            request.AddQueryParameter("from", "Genting Taxi");
            request.AddQueryParameter("text", message);
            request.AddQueryParameter("servid", "MES01");
             */
            var response = client.Execute(request);
            var content = response.Content; // raw content as string
            var statuscode = response.StatusCode; 

            if(statuscode != System.Net.HttpStatusCode.OK || response.Content == "401")
            {
                issent = false;
                LogError(new Exception(response.StatusDescription + " " + phonenum +" " +response.Content) { Source = "SMS Error" });
            }

            //create sms history
            if(issent)
            {
                using (var context = new entity.gtaxidbEntities())
                {
                    //add to history table 
                    var newSmsEntity = new entity.SMShistory();
                    newSmsEntity.created_date = DateTime.Now;
                    
                    if(userType == (int)UserType.driver)
                    {
                        newSmsEntity.driverId = targetID; 
                    }
                    else if (userType == (int)UserType.user)
                    {
                        newSmsEntity.userId = targetID; 
                    }

                    newSmsEntity.message = message;
                    newSmsEntity.phoneNo = phonenum;

                    context.SMShistories.Add(newSmsEntity);
                    context.SaveChanges();
                }
            }

            return issent; 
        }

        public static void SendEmail(string emailaddr , string message)
        {
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = "mail.gentingtaxi.com";
            smtpClient.Port = 587;
            smtpClient.EnableSsl = false;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential("noreply@gentingtaxi.com", "gentingtaxi123");
            System.Net.Mail.MailMessage email = new System.Net.Mail.MailMessage();
            email.From = new MailAddress("noreply@gentingtaxi.com", "ADMIN");
            email.To.Add(emailaddr);
            email.Subject = "Activation";
            email.IsBodyHtml = true;
            email.Body += message;
            try
            {
                //try to send port 587  first 
                smtpClient.Send(email);
            }
            catch(Exception ex)
            {
                LogError(new Exception(emailaddr + " " + ex.Message) { Source = "Email Error" });
            }
        }

        public static void PushNotifications (string deviceId , string message , int bookingId = 0)
        {
            var push = new PushBroker();

            //Wire up the events for all the services that the broker registers
            push.OnNotificationSent += NotificationSent;
            push.OnChannelException += ChannelException;
            push.OnServiceException += ServiceException;
            push.OnNotificationFailed += NotificationFailed;
            push.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
            push.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;
            push.OnChannelCreated += ChannelCreated;
            push.OnChannelDestroyed += ChannelDestroyed;

            try
            {
                //check device type by token length 
                if (deviceId != null && deviceId.Length != 0)
                {
                    //get bookingstatus if available
                    int bookingstatus = 0;
                    string dest = ""; string pickup = ""; 
                    string bookingdatetimestr = "";
                    string name = "";
                    string from_lat = ""; string from_lng = "";
                    string to_lat = ""; string to_lng = "";
                    int manualassign = 0; 

                    using (var context = new entity.gtaxidbEntities())
                    {
                        var bookingEntity = from d in context.Booking_trx
                                            where d.bookingId == bookingId
                                            select d;
                        if (bookingEntity.Count() > 0)
                        {
                            bookingstatus = bookingEntity.First().booking_status ?? default(int);
                            dest = bookingEntity.First().to_locationname;
                            pickup = bookingEntity.First().from_locationname; 
                            from_lat = bookingEntity.First().from_lat.ToString();
                            from_lng = bookingEntity.First().from_lng.ToString();
                            to_lng = bookingEntity.First().to_lng.ToString();
                            to_lat = bookingEntity.First().to_Lat.ToString();


                            if (bookingEntity.First().manual_Assign_Flag != null)
                            {
                                manualassign = (bool)bookingEntity.First().manual_Assign_Flag ? 1 : 0; 
                            }
                            
                            if(bookingEntity.First().booking_datetime != null)
                            {
                                DateTime bookingdatetime = (DateTime)bookingEntity.First().booking_datetime;
                                bookingdatetimestr =
                                    bookingdatetime.Date == DateTime.Now.Date ? "Today, " + string.Format("{0:hh:mm tt}", bookingdatetime) :
                                                                    string.Format("{0:dd MM yyyy hh:mm tt}", bookingdatetime);
                            }
                            var userid = bookingEntity.First().userId;
                            var userEntity = from d in context.Users
                                             where d.userId == userid
                                             select d;

                            if (userEntity.Count() > 0)
                            {
                                name = userEntity.First().username; 
                            }

                        }
                    }

                    var tokenlength = deviceId.Length;

                    if (tokenlength == 64)
                    {
                        //-------------------------
                        // APPLE NOTIFICATIONS
                        //-------------------------
                        //Configure and start Apple APNS
                        var appleCert = File.ReadAllBytes(System.Web.Hosting.HostingEnvironment.MapPath(ConfigurationManager.AppSettings["AppleCertDevLocation"]));
                        //var appleCert = File.ReadAllBytes(System.Web.Hosting.HostingEnvironment.MapPath(ConfigurationManager.AppSettings["AppleCertProdLocation"]));

                        push.RegisterAppleService(new ApplePushChannelSettings(false, appleCert, ConfigurationManager.AppSettings["AppleCertPassword"]));
                        //Extension method
                        //Fluent construction of an iOS notification

                        push.QueueNotification(new AppleNotification()
                                                    .ForDeviceToken(deviceId)//the recipient device id
                                                    .WithAlert(message)//the message
                                                    .WithBadge(1)
                                                    .WithCustomItem("bid", bookingId.ToString())
                                                    .WithCustomItem("bookingstatus", bookingstatus)
                                                    .WithCustomItem("name", name)
                                                    .WithCustomItem("dest", dest)
                                                    .WithCustomItem("pickup", pickup)
                                                    .WithCustomItem("frompx", from_lat)
                                                    .WithCustomItem("frompy", from_lng)
                                                    .WithCustomItem("topx", to_lat)
                                                    .WithCustomItem("topy", to_lng)
                                                    .WithCustomItem("manualassign", manualassign)
                                                    );
                    }
                    else if (tokenlength > 64)
                    {
                        //---------------------------
                        // ANDROID GCM NOTIFICATIONS
                        //---------------------------
                        //Configure and start Android GCM

                        PNobject pnobject = new PNobject();
                        pnobject.alert = message;
                        pnobject.bid = bookingId;
                        pnobject.bookingstatus = bookingstatus;
                        pnobject.badge = 7;
                        pnobject.dest = dest;
                        pnobject.pickup = pickup;
                        pnobject.bookingtime = bookingdatetimestr;
                        pnobject.name = name;
                        pnobject.frompx = from_lat;
                        pnobject.frompy = from_lng;
                        pnobject.topx = to_lat;
                        pnobject.topy = to_lng;
                        pnobject.manualassign = manualassign;

                        var jsonstring = new JavaScriptSerializer().Serialize(pnobject);

                        push.RegisterGcmService(new GcmPushChannelSettings(ConfigurationManager.AppSettings["AndroidServerApiKey"]));
                        //push.RegisterGcmService(new GcmPushChannelSettings("AIzaSyC3jvi5a_e92fuIAGztKwQWy1V72xKAAdc"));

                        //Fluent construction of an Android GCM Notification

                        /*
                        push.QueueNotification(new GcmNotification()
                         .ForDeviceRegistrationId(deviceId)
                         .WithJson("{\"alert\":\"" + message + "\",\"badge\":7 , \"bid\":" + bookingId.ToString() +
                                  ", \"bookingstatus\":\"" + bookingstatus + "\"}")
                        );
                         * */
                        push.QueueNotification(new GcmNotification()
                         .ForDeviceRegistrationId(deviceId)
                         .WithJson(jsonstring)
                        );
                    }
                    else
                    {
                        throw new Exception("DeviceId not found , bookingid : " + bookingId.ToString()) { Source = "Push notification error" };
                    }

                    push.StopAllServices(waitForQueuesToFinish: true);
                }
                else
                {
                    throw new Exception("DeviceId not found , bookingid : " + bookingId.ToString()) { Source = "Push notification error" };
                }
            }
            catch(Exception ex)
            {
                LogError(ex);
            }
            

        }


        #region pushbroker events
        //Currently it will raise only for android devices
        static void DeviceSubscriptionChanged(object sender,
        string oldSubscriptionId, string newSubscriptionId, INotification notification)
        {
            //Do something here
        }

        //this even raised when a notification is successfully sent
        static void NotificationSent(object sender, INotification notification)
        {
            //Do something here
        }

        //this is raised when a notification is failed due to some reason
        static void NotificationFailed(object sender,
        INotification notification, Exception notificationFailureException)
        {
            LogError(notificationFailureException);
        }

        //this is fired when there is exception is raised by the channel
        static void ChannelException
            (object sender, IPushChannel channel, Exception exception)
        {
            LogError(exception);
        }

        //this is fired when there is exception is raised by the service
        static void ServiceException(object sender, Exception exception)
        {
            LogError(exception);
        }

        //this is raised when the particular device subscription is expired
        static void DeviceSubscriptionExpired(object sender,
        string expiredDeviceSubscriptionId,
            DateTime timestamp, INotification notification)
        {;
            //Do something here
        }

        //this is raised when the channel is destroyed
        static void ChannelDestroyed(object sender)
        {
            //Do something here
        }

        //this is raised when the channel is created
        static void ChannelCreated(object sender, IPushChannel pushChannel)
        {
            //Do something here
        }
        #endregion

        public static void LogError(Exception ex)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                    var entityerror = new entity.Errorlog();
                    entityerror.errormessage = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;

                    //trycast for PN exception
                    if (ex is PushSharp.Apple.NotificationFailureException)
                    {
                        var appleEx = (PushSharp.Apple.NotificationFailureException)ex;
                        entityerror.errormessage += " " + appleEx.ErrorStatusDescription;
                    }

                    if (ex.Source != null)
                    {
                        entityerror.errorsource = ex.Source.Replace("'", "''");
                    }


                    /*
                    if (ex.StackTrace != null)
                    {
                        entityerror.errorstacktrace = ex.StackTrace.Replace("'", "''");
                    }*/
                    entityerror.created_date = DateTime.Now;
                    context.Errorlogs.Add(entityerror);
                    context.SaveChanges();

            }
        }

        public static void PushNotificationToWeb(string Message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            context.Clients.All.addNotification(Message);

            AdminNotificationService AdminNotificationService = new AdminNotificationService();
            AdminNotificationService.AddAdminNotification(new AdminNotificationVM()
            {
                message = Message
            });
        }
    }
    public static class EnumHelper
    {
        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        /// <example>string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;</example>
        public static T GetAttributeOfType<T>(this Enum enumVal) where T:System.Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }
        
        public static string GetAttributeDescription(this Enum enumValue)
        {
            var attribute = enumValue.GetAttributeOfType<DescriptionAttribute>();

            return attribute == null ? String.Empty : attribute.Description;
        } 
    }
}
