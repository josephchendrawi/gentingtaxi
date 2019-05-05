using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using System.Data.Entity.Validation;
using System.Device.Location;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Configuration; 

namespace GentingTaxiApi.Service
{
    public class BookingService
    {
        public BookingResponse AddBookingService(AddBookingRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                int bookingid = 0;

                if (usertokenobj != null)
                {
                    decimal tmpvalue;

                    entity.Booking_trx newEntity = new entity.Booking_trx();
                    newEntity.userId = usertokenobj.userId;
                    newEntity.booking_status = (int)BookingStatus.Pending;

                    newEntity.from_lat = decimal.TryParse((string)request.frmpx, out tmpvalue) ?
                                         tmpvalue : (decimal?)null;
                    newEntity.from_lng = decimal.TryParse((string)request.frmpy, out tmpvalue) ?
                                         tmpvalue : (decimal?)null;
                    newEntity.to_Lat = decimal.TryParse((string)request.topx, out tmpvalue) ?
                                         tmpvalue : (decimal?)null;
                    newEntity.to_lng = decimal.TryParse((string)request.topy, out tmpvalue) ?
                                         tmpvalue : (decimal?)null;
                    newEntity.from_locationname = request.from;
                    newEntity.to_locationname = request.to;
                    newEntity.est_Distance = request.edst;
                    newEntity.est_Fares = request.eprc;
                    newEntity.request_Cartype = request.type;
                    newEntity.remarks = request.rmk;
                    newEntity.created_date = DateTime.Now;
                    newEntity.fdid_selected = request.fdid ?? default(int);

                    DateTime booking_datetime;
                    booking_datetime = DateTime.TryParseExact(request.date,
                                        "dd-MM-yyyy HH:mm" , 
                                        new CultureInfo("en-US") , 
                                        DateTimeStyles.None , 
                                        out booking_datetime) ? 
                                        booking_datetime : DateTime.Now;

                    newEntity.booking_datetime = booking_datetime;

                    context.Booking_trx.Add(newEntity);
                    context.SaveChanges();

                    bookingid = newEntity.bookingId;

                    //create async pushnotification request 
                    int requestFavDriverid = request.fdid ?? default(int);

                    //only search driver for PN if more booking less than 24 hours 
                    if(booking_datetime <= DateTime.Now.AddHours(24))
                    {
                        AsyncSearchDriverTimer(newEntity.bookingId, requestFavDriverid);
                    }
                    else
                    {
                        newEntity.manual_Assign_Flag = true;
                        context.SaveChanges(); 
                    }

                    //push notification to Admin Portal
                    //Helper.PushNotificationToWeb("New Booking Transaction Created with Booking ID : " + newEntity.bookingId);
                    string Message = "New Booking Transaction Created with Booking ID : " + newEntity.bookingId;

                    string PushWebNotifPath = ConfigurationManager.AppSettings["PushNotification2WebPath"];
                    string URLAuth = PushWebNotifPath + "&message=" + Message;
                    WebClient webClient = new WebClient();
                    webClient.Headers["Content-Type"] = "application/json; charset=utf-8";
                    webClient.Encoding = System.Text.Encoding.UTF8;
                    string result = webClient.UploadString(URLAuth, "POST", "");
                    webClient.Dispose();

                    return new BookingResponse() { sts = 0, booking_id = bookingid, fdid = requestFavDriverid };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }

            }

        }

        public BookingResponse ResendBookingService(ResendBookingRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                if (usertokenobj != null)
                {
                    //get booking 
                    var bookingentities = from d in context.Booking_trx
                                          where d.bookingId == request.booking_id
                                          select d;

                    if (bookingentities.Count() == 0)
                    {
                        throw new Exception("Booking not found");
                    }

                    DateTime booking_datetime;
                    booking_datetime = DateTime.TryParseExact(bookingentities.First().booking_datetime.ToString(),
                                        "dd-MM-yyyy HH:mm",
                                        new CultureInfo("en-US"),
                                        DateTimeStyles.None,
                                        out booking_datetime) ?
                                        booking_datetime : DateTime.Now;

                    //create async pushnotification request 
                    int requestFavDriverid = request.fdid;

                    //only search driver for PN if more booking less than 24 hours 
                    if (booking_datetime <= DateTime.Now.AddHours(24))
                    {
                        //isResend set to true 
                        AsyncSearchDriverTimer(bookingentities.First().bookingId, requestFavDriverid , true);
                    }
                    else
                    {
                        bookingentities.First().manual_Assign_Flag = true;
                        context.SaveChanges();
                    }

                    return new BookingResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }

            }

        }

        public BookingResponse ExpandDriverSearchservice(ExpandDriverSearchrequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                if (usertokenobj != null)
                { 
                    var bookingentity = from d in context.Booking_trx
                                        where d.bookingId == request.BookingId
                                        select d;
                    if (bookingentity.Count() > 0)
                    {
                        //update booking status , set to pending
                        bookingentity.First().booking_status = (int)BookingStatus.Pending;
                        context.SaveChanges();

                        AsyncSearchDriverTimer(request.BookingId);
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.UserNotFound);
                    }
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }


            return new BookingResponse() { sts = 0, result = null };
        }

        public BookingResponse GetBookingByID(GetBookingbyIDrequest request, ServiceStack.Auth.IAuthSession session = null)
        {

            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                if (usertokenobj != null)
                {
                    var bookingEntity = context.Database.SqlQuery<BookingVM>(
                                            "Select t0.bookingId , t0.booking_datetime , t0.assigned_driverId ,t0.booking_datetime , t0.from_locationname as 'from' , t0.to_locationname as 'to' ,  " +
                                            "t0.from_lat as frompx , t0.from_lng as frompy , t0.to_lat as topx , t0.to_lng as topy, t0.booking_status , t0.remarks, " +
                                            "t0.est_Distance as edst , t0.est_Fares as eprc, t0.feedbackstatus , " +
                                            "t1.ic , t1.name as driver_name , t1.car_Plate , t1.photo_url , t1.phone as driver_phoneno " +
                                            "from Booking_trx t0 " +
                                            "left join Driver t1 on t0.assigned_driverId = t1.driverId " +
                                            "Where t0.bookingId = {0} " +
                                            "order by t0.booking_datetime desc",
                                            request.BookingId
                                        );
                    
                    BookingVM bookingitem = new BookingVM();

                    if (bookingEntity.Count() > 0)
                    {
                        bookingitem = bookingEntity.Select(x =>
                        {
                            x.feedbackstatus = x.feedbackstatus ?? default(int);
                            x.date = (x.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm");

                            //hardcode file path 
                            if (x.photo_url != null)
                            {
                                x.photo_url = Helper.serverpath + x.photo_url;
                            }

                            return x;
                        })
                        .First();

                        return new BookingResponse() { sts = 0, result = bookingitem };
                    }
                    else
                    {
                        return new BookingResponse() { sts = 0, result = null };
                    }

                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public BookingResponse GetBookingByIDForDriverService(GetBookingbyIDForDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {

            using (var context = new entity.gtaxidbEntities())
            {
                var driverTokenObj = Helper.AuthDriverToken(request.token, session);

                if (driverTokenObj != null)
                {
                    var bookingEntity = context.Database.SqlQuery<BookingVM>(
                                            "Select t0.bookingId , t0.booking_datetime , t0.assigned_driverId ,t0.booking_datetime , t0.from_locationname as 'from' , t0.to_locationname as 'to' ,  " +
                                            "t0.from_lat as frompx , t0.from_lng as frompy , t0.to_lat as topx , t0.to_lng as topy, t0.booking_status , t0.remarks, " +
                                            "t0.est_Distance as edst , t0.est_Fares as eprc, " +
                                            "t1.ic , t1.name as driver_name , t1.car_Plate , t1.photo_url , t1.phone as driver_phoneno , " +
                                            "t2.name as user_name , t2.phone as user_phone " +
                                            "from Booking_trx t0 " +
                                            "left join Driver t1 on t0.assigned_driverId = t1.driverId " +
                                            "left join [User] t2 on t0.userid = t2.userid " +
                                            "Where t0.bookingId = {0} " +
                                            "order by t0.booking_datetime desc",
                                            request.BookingId
                                        );

                    BookingVM bookingitem = new BookingVM();

                    if (bookingEntity.Count() > 0)
                    {
                        bookingitem = bookingEntity.Select(x =>
                        {
                            x.date = (x.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm");
                            x.user_phone = string.IsNullOrEmpty(x.user_phone) ? "" : x.user_phone; 

                            //hardcode file path 
                            if (x.photo_url != null)
                            {
                                x.photo_url = Helper.serverpath + x.photo_url;
                            }

                            return x;
                        })
                        .First();

                        return new BookingResponse() { sts = 0, result = bookingitem };
                    }
                    else
                    {
                        return new BookingResponse() { sts = 0, result = null };
                    }

                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public BookingListResponse GetBookingsforCurrentUser(GetBookingByCurrentUserRequest request, ServiceStack.Auth.IAuthSession session = null)
        {

            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                if (usertokenobj != null)
                {
                    var bookingEntity = context.Database.SqlQuery<BookingVM>(
                                            "Select t0.bookingId , t0.booking_datetime , t0.assigned_driverId ,t0.booking_datetime , t0.from_locationname as 'from' , t0.to_locationname as 'to' ,  " +
                                            "t0.from_lat as frompx , t0.from_lng as frompy , t0.to_lat as topx , t0.to_lng as topy, t0.booking_status , t0.remarks, " +
                                            "t0.est_Distance as edst , t0.est_Fares as eprc,t0.feedbackstatus, " +
                                            "t1.ic , t1.name as driver_name , t1.car_Plate , t1.photo_url , t1.phone as driver_phoneno , t0.fdid_selected " +
                                            "from Booking_trx t0 " +
                                            "left join Driver t1 on t0.assigned_driverId = t1.driverId " +
                                            "Where t0.UserId = {0} " +
                                            "order by t0.booking_datetime desc",
                                            usertokenobj.userId
                                        );

                    List<BookingVM> bookinglist = new List<BookingVM>();

                    if (bookingEntity.Count() > 0)
                    {
                        bookinglist = bookingEntity.Select(x =>
                        {
                            x.fdid_selected = x.fdid_selected ?? default(int);
                            x.feedbackstatus = x.feedbackstatus ?? default(int);
                            x.date = (x.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm");

                            //hardcode file path 
                            if (x.photo_url != null)
                            {
                                x.photo_url = Helper.serverpath + x.photo_url;
                            }

                            return x;
                        }).ToList();
                    }

                    return new BookingListResponse() { sts = 0, result = bookinglist };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public BookingListResponse GetBookingsforCurrentDriver(GetBookingByCurrentDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {

            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthDriverToken(request.token, session);

                if (usertokenobj != null)
                {
                    var bookingEntity = context.Database.SqlQuery<BookingVM>(
                                            "Select * , t0.from_locationname as 'from' , t0.to_locationname as 'to' ,  " +
                                            "t0.from_lat as frompx , t0.from_lng as frompy , t0.to_lat as topx , t0.to_lng as topy, " +
                                            "t0.est_Distance as edst , t0.est_Fares as eprc , t1.name as user_name , t1.phone as user_phone " +
                                            "from Booking_trx t0 " +
                                            "left join [User] t1 on t0.userid = t1.userid " +
                                            "Where assigned_driverId = {0} ",
                                            usertokenobj.driverId
                                        );

                    List<BookingVM> bookinglist = new List<BookingVM>();

                    if (bookingEntity.Count() > 0)
                    {
                        bookinglist = bookingEntity.Select(x =>
                        {
                            x.date = (x.booking_datetime ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm");
                            x.user_phone = string.IsNullOrEmpty(x.user_phone) ? "" : x.user_phone; 

                            return x;
                        }).ToList();
                    }

                    return new BookingListResponse() { sts = 0, result = bookinglist };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }


        public BookingResponse CancelBookingforCurrentUser(CancelBookingByCurrentUserRequest request, ServiceStack.Auth.IAuthSession session = null)
        {

            using (var context = new entity.gtaxidbEntities())
            {
                var usertokenobj = Helper.AuthToken(request.token, session);

                if (usertokenobj != null)
                {
                    var bookingEntity = from d in context.Booking_trx
                                        where d.userId == usertokenobj.userId && d.bookingId == request.bookingId
                                        select d;

                    if (bookingEntity.Count() > 0)
                    {
                        var bookingobj = bookingEntity.First();
                        bookingobj.booking_status = (int)BookingStatus.Cancelled;
                        context.SaveChanges();

                        //send notification to driver 
                        var assigned_driver = bookingobj.assigned_driverId ?? default(int);

                        if (assigned_driver > 0)
                        {
                            string message = "Booking ID " + bookingobj.bookingId + " cancelled by user . ";
                            Helper.PushNotificationsToDriver(assigned_driver, message, bookingobj.bookingId);
                        }
                    };

                    return new BookingResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.UserNotFound);
                }
            }
        }

        public BookingResponse AcceptBookingForCurrentDriverService(AcceptDriverBookingsRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var driverTokenObj = Helper.AuthDriverToken(request.token, session);

                if (driverTokenObj != null)
                {
                    int driverid = driverTokenObj.driverId;
                    //get bookingid's driverbooking entry 
                    var driverBookingEntity = from d in context.DriverBookings
                                              where d.bookingId == request.bookingId
                                              && d.driverId == driverid && 
                                              d.status == (int)DriverBookingStatus.Pending 
                                              select d;

                    var isRespondedBookingEntity = from d in context.DriverBookings
                                                   where d.bookingId == request.bookingId
                                                   && (d.status == (int)DriverBookingStatus.Accepted)
                                                   select d; 

                    if (driverBookingEntity.Count() > 0 && isRespondedBookingEntity.Count() == 0)
                    {
                        var driverBooking = driverBookingEntity.First();

                        if (driverBooking.driverId != driverTokenObj.driverId)
                        {
                            throw new CustomException(CustomErrorType.BookingInvalidForDriver);
                        }

                        int driverBookingstatusInt = request.booking_status ?? default(int);
                        driverBooking.status = driverBookingstatusInt;
                        driverBooking.response_datettime = DateTime.Now;
                        context.SaveChanges();

                        if (driverBookingstatusInt == (int)DriverBookingStatus.Accepted)
                        {
                            //set assigned driver to booking 
                            var bookingEntity = from d in context.Booking_trx
                                                where d.bookingId == request.bookingId
                                                select d;
                            if (bookingEntity.Count() > 0)
                            {
                                var bookingobj = bookingEntity.First();
                                bookingobj.assigned_driverId = driverTokenObj.driverId;
                                bookingobj.booking_status = (int)BookingStatus.Assigned;
                                bookingobj.last_updated = DateTime.Now;
                                context.SaveChanges();

                                //send notification to user 
                                Helper.PushNotificationsToUser((int)bookingobj.userId ,
                                    CustomMessage.UserBookingUpdated(), bookingobj.bookingId);
                            }
                        }
                        else if (driverBookingstatusInt == (int)DriverBookingStatus.Rejected)
                        { 
                            var bookingEntity = from d in context.Booking_trx
                                                where d.bookingId == request.bookingId
                                                select d;
                            if (bookingEntity.Count() > 0)
                            {
                                var bookingobj = bookingEntity.First();

                                //send notification to user 
                                //Helper.PushNotificationsToUser((int)bookingobj.userId, CustomMessage.UserBookingUpdated(), bookingobj.bookingId);
                            }
                        }
                        else
                        {
                            throw new CustomException(CustomErrorType.BookingInvalidStatusUpdate);
                        }
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.BookingResponded);
                    }

                    return new BookingResponse() { sts = 0 };
                }
                else
                {
                    throw new CustomException(CustomErrorType.Unauthenticated);
                }
            }
        }

        public BookingResponse UpdateBookingStatusForCurrentDriverService(
            UpdateBookingStatusForCurrentDriverRequest request, ServiceStack.Auth.IAuthSession session = null)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var driverTokenObj = Helper.AuthDriverToken(request.token, session);

                if (driverTokenObj != null)
                {
                    //get driver's booking_trx entry 
                    var bookingEntity = from d in context.Booking_trx
                                        where d.bookingId == request.bookingId && d.assigned_driverId == driverTokenObj.driverId
                                        select d;
                    /*
                    var bookingList = context.Database.SqlQuery<BookingVM>(
                                        "select * from booking_trx t0 " +
                                        "left join driverbookings t1 on t0.bookingid = t1.bookingid " +
                                        "where "
                                        );*/

                    if (bookingEntity.Count() > 0)
                    {
                        var bookingitem = bookingEntity.First();

                        if (request.booking_status == (int)BookingStatus.Pickup ||
                           request.booking_status == (int)BookingStatus.Completed)
                        {
                            //set booking status 
                            if (request.booking_status == (int)BookingStatus.Pickup)
                                bookingitem.pickup_Datetime = DateTime.Now;

                            if (request.booking_status == (int)BookingStatus.Completed)
                                bookingitem.journey_End_Datetime = DateTime.Now; 

                            bookingitem.booking_status = request.booking_status;
                            context.SaveChanges();

                            int userid = bookingitem.userId ?? default(int);

                            string msg = "Booking updated , status : " + ((BookingStatus)request.booking_status).ToString();

                            //push notification to user on status changed 
                            Helper.PushNotificationsToUser(userid,
                                msg , bookingitem.bookingId);

                            return new BookingResponse() { sts = 0 };
                        }
                        else 
                        {
                            throw new CustomException(CustomErrorType.BookingInvalidStatusUpdate);
                        }
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.BookingNotFound);
                    }

                }
                else
                {
                    throw new CustomException(CustomErrorType.Unauthenticated);
                }
            }
        }

        public Response AddJourneyEntryService(AddJourneyEntryRequest request)
        {

            using (var context = new entity.gtaxidbEntities())
            {
                //create journey entry 
                var newEntity = new entity.Journey();
                newEntity.bookingId = request.bookingId;
                newEntity.current_datetime = request.current_datetime;
                newEntity.current_lat = request.lat;
                newEntity.current_lng = request.lng;

                context.Journeys.Add(newEntity);
                context.SaveChanges();

                return new Response() { sts = 0 }; 
            }
        }

        //Timer methods 
        Timer atimer = new Timer(); 
        private volatile bool isDriverAssigned = false;
        //private volatile bool _requeststop = false;
        private volatile int sequenceCount = 1;
        private volatile int sequenceEnd = 2;
        private volatile List<int> driver_notifiedlist = new List<int>();

        public void AsyncSearchDriverTimer(int bookingId, int fdid = 0 , bool isResend = false)
        {
            //int buffersecs = 60000; default
            AdminService adminservice = new AdminService();
            long bufferinseconds = adminservice.GetAdminSetting().searchdriverbuffer_sec > 0 ?
                (long)adminservice.GetAdminSetting().searchdriverbuffer_sec : 60;

            int bufferInMiliSeconds = (int)bufferinseconds * 1000; 

            //get initial notified driver list 
            driver_notifiedlist = SearchAndPNDriver(bookingId, fdid, isResend);

            atimer.Interval = bufferInMiliSeconds;
            atimer.Elapsed += (s,e) => OnCheckDriverElapsed(s , e , bookingId, fdid);
            atimer.AutoReset = false;
            atimer.Start(); 
            
        }

        private void OnCheckDriverElapsed(object s , ElapsedEventArgs e , int bookingId , int fdid)
        {
            #region //Check Driver assigned//

            if (driver_notifiedlist.Count() > 0)
            {
                //check assigned booking 
                using (var context = new entity.gtaxidbEntities())
                {
                    var bookingEntity = from d in context.Booking_trx
                                        where d.bookingId == bookingId
                                        select d;

                    //exit
                    if (bookingEntity.Count() == 0) throw new CustomException(CustomErrorType.BookingNotFound);

                    //exit if cancelled 
                    if (bookingEntity.First().booking_status == (int)BookingStatus.Cancelled) return;

                    var assignedBookingsEntity = from d in bookingEntity
                                                    where driver_notifiedlist.Contains((int)d.assigned_driverId)
                                                    select d;

                    var notifiedDriverBookingsEntity = from d in context.DriverBookings
                                                        where driver_notifiedlist.Contains(d.driverId) 
                                                        && d.status == (int)DriverBookingStatus.Pending
                                                        && d.bookingId == bookingId
                                                        select d;

                    //set expired to all notified drivers in pending status  
                    foreach (var notifiedDriverbooking in notifiedDriverBookingsEntity)
                    {
                        notifiedDriverbooking.status = (int)DriverBookingStatus.Expired;
                        notifiedDriverbooking.last_updated = DateTime.Now;
                    }
                    context.SaveChanges();

                    if (assignedBookingsEntity.Count() > 0)
                    {
                        //driver assigned means one of the driver accepted the booking 
                        isDriverAssigned = true;

                        //stop timer for next sequence
                        atimer.Stop();
                    }
                    else
                    {
                        //condition if fav driver exist and not responding after PN
                        if (driver_notifiedlist.First() == fdid)
                        {
                            var userid = bookingEntity.First().userId ?? default(int);

                            //update booking status 
                            bookingEntity.First().booking_status = (int)BookingStatus.PreferredNoRespond;
                            bookingEntity.First().last_updated = DateTime.Now;
                            context.SaveChanges();

                            //notify user 
                            Helper.PushNotificationsToUser(userid, CustomMessage.BookingSetManual(), bookingId);

                            //stop timer for next sequence
                            atimer.Stop();
                        }
                        else
                        {
                            //Search next round of drivers 
                            if (sequenceCount < sequenceEnd)    //indicate sequence end
                            driver_notifiedlist = SearchAndPNDriver(bookingId);
                        }
                    }
                }
            }
            else
            {
                //no driver to notify stop sequence and set manual assign
                using (var context = new entity.gtaxidbEntities())
                {
                    var bookingentity = from d in context.Booking_trx
                                        where d.bookingId == bookingId
                                        select d;
                    if (bookingentity.Count() > 0)
                    {
                        //update booking status 
                        bookingentity.First().manual_Assign_Flag = true;
                        bookingentity.First().last_updated = DateTime.Now;
                        context.SaveChanges();

                        //notify user 
                        Helper.PushNotificationsToUser(bookingentity.First().userId ?? default(int), CustomMessage.BookingSetManual(), bookingId);
                    }
                }
                atimer.Stop();
            }
            #endregion

            #region //case stop timer loop//
            if (sequenceCount < sequenceEnd)
            {
                //iterate another sequence 
                atimer.Start();
            }
            else
            {
                //End of sequence 
                if (!isDriverAssigned)
                {
                    using (var context = new entity.gtaxidbEntities())
                    {
                        var bookingentity = from d in context.Booking_trx
                                            where d.bookingId == bookingId
                                            select d;
                        if (bookingentity.Count() > 0)
                        {
                            //update booking status 
                            bookingentity.First().manual_Assign_Flag = true;
                            bookingentity.First().last_updated = DateTime.Now;
                            context.SaveChanges();
                        }
                    }
                }
            }

            #endregion

            sequenceCount++;
        }

        public void AsyncSearchDriver(int bookingId, int fdid = 0)
        {

            System.Threading.ThreadPool.QueueUserWorkItem((x) =>
            {
                bool isDriverAssigned = false; 

                int sequenceCount = 1;
                int sequenceEnd = 3;  //3 total broadcast sequence 

                //60sec buffer time 
                AdminService adminservice = new AdminService();
                long bufferinseconds = adminservice.GetAdminSetting().searchdriverbuffer_sec > 0 ?
                    (long)adminservice.GetAdminSetting().searchdriverbuffer_sec : 10;

                int bufferInMiliSeconds = (int)bufferinseconds * 1000; 

                while (sequenceCount <= sequenceEnd)
                {
                    List<int> driver_notifiedlist = SearchAndPNDriver(bookingId, fdid);

                    //start with 2mins/120secs buffer
                    System.Threading.Thread.Sleep(bufferInMiliSeconds);

                    #region Queue Reference 
                    /*
                    if (driver_notifiedID != 0)
                    {
                        using (var context = new entity.gtaxidbEntities())
                        {
                            var driverbookingsEntity = from d in context.DriverBookings
                                                       where d.bookingId == bookingId
                                                       && d.driverId == driver_notifiedID
                                                       select d;

                            if (driverbookingsEntity.Count() > 0)
                            {
                                var driverBookingitem = driverbookingsEntity.First();

                                if (driverBookingitem.status == (int)DriverBookingStatus.Accepted ||
                                    driverBookingitem.status == (int)DriverBookingStatus.Rejected)
                                {
                                    //response detected , exit 
                                    isDriverAssigned = true;
                                    break;  //immediate exit if responded
                                }
                                else
                                {
                                    //no response found for booking , update driverbooking entry as expired 
                                    driverbookingsEntity.First().status = (int)DriverBookingStatus.Expired;
                                    context.SaveChanges();
                                }
                            }
                        }
                    }
                     * */
                    #endregion

                    if(driver_notifiedlist.Count() > 0)
                    {
                        //check assigned booking 
                        using (var context = new entity.gtaxidbEntities())
                        {
                            var bookingEntity = from d in context.Booking_trx
                                                where d.bookingId == bookingId
                                                select d;

                            //exit
                            if(bookingEntity.Count() == 0) throw new CustomException(CustomErrorType.BookingNotFound);

                            var assignedBookingsEntity = from d in bookingEntity
                                                         where driver_notifiedlist.Contains((int)d.assigned_driverId)
                                                         select d;

                            if (assignedBookingsEntity.Count() > 0)
                            {
                                //response detected , exit 
                                isDriverAssigned = true;
                                break;  //immediate exit if responded
                            }
                            else
                            {

                                //set expired to all notified drivers ; get all driverbookings for the notified driver 
                                var notifiedDriverBookingsEntity = from d in context.DriverBookings
                                                             where driver_notifiedlist.Contains(d.driverId) &&
                                                             d.bookingId == bookingId
                                                             select d; 

                                foreach(var notifiedDriverbooking in notifiedDriverBookingsEntity)
                                {
                                    notifiedDriverbooking.status = (int)DriverBookingStatus.Expired;
                                }
                                context.SaveChanges();

                                //condition if fav driver exist and not responding after PN
                                if (driver_notifiedlist.First() == fdid)
                                {
                                    var userid = bookingEntity.First().userId ?? default(int);
                                    Helper.PushNotificationsToUser(userid, CustomMessage.FavDriverNoRespond(), bookingId);
                                    
                                    //stop PN sequence if favourite driver 
                                    break;
                                }
                            }
                        }
                    }
                    sequenceCount++;
                }

                //scenario if manual assign not captured in time limit 
                if (!isDriverAssigned)
                {
                    using (var context = new entity.gtaxidbEntities())
                    {
                        var bookingentity = from d in context.Booking_trx
                                            where d.bookingId == bookingId
                                            select d;
                        if (bookingentity.Count() > 0)
                        {
                            //update booking status 
                            bookingentity.First().manual_Assign_Flag = true;
                            context.SaveChanges();
                        }
                    }
                }
            });
        }

        public static List<int> SearchAndPNDriver(int bookingId, int fdid = 0, bool isResend = false)
        {
            List<int> driver_notifiedList = new List<int>();

            //default value 120 second 
            AdminService adminservice = new AdminService();

            int driveravailability_range = 
                    adminservice.GetAdminSetting().driveravailabilityrange_min > 0 ? 
                    (int)adminservice.GetAdminSetting().driveravailabilityrange_min : 120; 

            using (var context = new entity.gtaxidbEntities())
            {
                //get bookingid entity
                entity.Booking_trx bookingobj = new entity.Booking_trx();

                var bookingentity = from d in context.Booking_trx
                                    where d.bookingId == bookingId
                                    select d;

                string curr_bookingtime = ""; 

                if (bookingentity.Count() > 0)  
                {
                    bookingobj = bookingentity.First();
                    //if car type not specified use type 0 
                    bookingobj.request_Cartype = bookingobj.request_Cartype ?? default(int);
                    curr_bookingtime = bookingobj.booking_datetime != null ? ((DateTime)bookingobj.booking_datetime).ToString("yyyy-MM-dd HH:mm") : "";
                }
                else
                {
                    //exit 
                    throw new CustomException(CustomErrorType.BookingNotFound);
                }

                bool isSearchNearestdrivers = false;
                bool isWaitUserRespondFromFavDriverBooked = false; 

                //get fav driver 
                var fav_driverEntity = from d in context.Drivers
                                       where d.driverId == fdid
                                       select d;

                if (fdid == 0)
                {
                    //no favourite driver specified
                    isSearchNearestdrivers = true; 
                }
                else
                {
                    if (fav_driverEntity.Count() > 0)
                    {
                        int target_favdriver = fav_driverEntity.First().driverId;

                        //get driverbookings list //push notifications 
                        var fav_driverbookingsEntity = from d in context.DriverBookings
                                                       where d.driverId == target_favdriver
                                                         && d.bookingId == bookingId
                                                         && d.created_date != null
                                                       select d;

                        //isresend status set to true , skip checking created driverbookings entry 
                        if (fav_driverbookingsEntity.Count() > 0 && !isResend)
                        {
                            isWaitUserRespondFromFavDriverBooked = true;
                        }

                        //get booking associated that is assigned (hardcode range 120mins)
                        var bookings_pickupEntity = context.Database.SqlQuery<BookingVM>(
                                        "Select * from Booking_trx t0 " +
                                        "where t0.assigned_driverid = {0} and " +
                                        "(t0.booking_status in (1,2)) and " +
                                        "DATEDIFF(MINUTE , t0.booking_datetime , '" + curr_bookingtime + "') <= {1} and " +
                                        "DATEDIFF(MINUTE , t0.booking_datetime , '"+curr_bookingtime+"') >= -{1}",
                                        fdid , driveravailability_range
                                    );

                        if (bookings_pickupEntity.Count() > 0)
                        {
                            //fav driver not available , wait respond 
                            isWaitUserRespondFromFavDriverBooked = true;
                        }
                    }
                    else
                    {
                        //fav driver not found , wait respond 
                        isWaitUserRespondFromFavDriverBooked = true;
                    }
                }

                if (isWaitUserRespondFromFavDriverBooked)
                {
                    #region //Wait fav driver respond //

                    //fav driver booked , return empty list to stop sequence and wait respond from user
                    var userid = bookingobj.userId ?? default(int);

                    //update booking status 
                    bookingobj.booking_status = (int)BookingStatus.PreferredNoRespond;
                    context.SaveChanges();

                    //notify user 
                    Helper.PushNotificationsToUser(userid, CustomMessage.FavDriverNoRespond(), bookingId);

                    //create no respond driverbooking for fav driver if not available (prevent search to find fav driver)
                    entity.DriverBooking newdriverbookingentity = new entity.DriverBooking();
                    newdriverbookingentity.bookingId = bookingId;
                    newdriverbookingentity.driverId = fdid;  //use preferredID specified 
                    newdriverbookingentity.status = (int)DriverBookingStatus.Rejected;
                    newdriverbookingentity.created_date = DateTime.Now;
                    context.DriverBookings.Add(newdriverbookingentity);
                    context.SaveChanges();

                    #endregion
                }

                //send PN to fav driver 
                if (!isWaitUserRespondFromFavDriverBooked && !isSearchNearestdrivers)
                {
                    #region //fav driver available to be PN //

                    //create driverbooking entry 
                    entity.DriverBooking newdriverbookingentity = new entity.DriverBooking();
                    newdriverbookingentity.bookingId = bookingId;
                    newdriverbookingentity.driverId = fdid;  //use preferredID specified 
                    newdriverbookingentity.status = (int)DriverBookingStatus.Pending;
                    newdriverbookingentity.created_date = DateTime.Now;
                    context.DriverBookings.Add(newdriverbookingentity);
                    context.SaveChanges();

                    //add to driver notified list
                    driver_notifiedList.Add(fdid);

                    //send push notification
                    Helper.PushNotificationsToDriver(fdid, CustomMessage.NewDriverBooking(), bookingId);

                    #endregion

                }

                if(isSearchNearestdrivers)
                {
                    #region //Search nearest//
                    //no preferred driver , assign nearest non-preferred driver 
                    //check driver with no booking ongoing status associated and 
                    //driversbooking no rejected recorded  

                    //get other driver 
                    var checkduplicatesql = "";

                    var otherdriverEntity = new List<DriverVM>();

                    if (!isResend)
                    {
                        otherdriverEntity = context.Database.SqlQuery<DriverVM>(
                                           "Select * from Driver " +
                                           "where driverId != {0} " +
                                           "and ((car_Type is null or car_Type = {1}) or {1} = {3}) " +
                                           "and driverId not in " +
                                           "(select driverId from DriverBookings where bookingId = {2})",
                                           fdid, bookingobj.request_Cartype, bookingId, (int)Cartype.RW
                                           ).ToList();
                    }
                    else
                    {
                        otherdriverEntity = context.Database.SqlQuery<DriverVM>(
                                           "Select * from Driver " +
                                           "where driverId != {0} " +
                                           "and ((car_Type is null or car_Type = {1}) or {1} = {2}) ",
                                           fdid, bookingobj.request_Cartype, (int)Cartype.RW
                                           ).ToList();
                    }

                    List<DriverVM> AvailableDrivers = new List<DriverVM>();

                    if (otherdriverEntity.Count() > 0)
                    {
                        foreach (var driver in otherdriverEntity)
                        {
                            var isAddDriver = true;

                            //filter driver with bookings matched 
                            //get booking associated that is assigned (range 120mins) 
                            var otherdriverEntity_bookings = context.Database.SqlQuery<BookingVM>(
                                                            "Select * from Booking_trx t0 " +
                                                            "where t0.assigned_driverid = {0} and " +
                                                            "(t0.booking_status in (1,2)) and " +
                                                            "DATEDIFF(MINUTE , t0.booking_datetime , '"+curr_bookingtime+"') <= {1} and " + 
                                                            "DATEDIFF(MINUTE , t0.booking_datetime , '"+curr_bookingtime+"') >= -{1}",
                                                            driver.driverId , driveravailability_range
                                                            );

                            if (otherdriverEntity_bookings.Count() > 0) isAddDriver = false;

                            if (isAddDriver)
                            {
                                AvailableDrivers.Add(driver);
                            }
                        }
                    }

                    if (AvailableDrivers.Count() > 0)
                    {
                        List<DriverVM> nearestdrivers = new List<DriverVM>();
                        double distance = 0;   //distance between current driver location and booking pickup loc

                        foreach (var driver in AvailableDrivers)
                        {
                            //calculate distance  
                            decimal driver_lat = driver.current_lat ?? default(decimal);
                            decimal driver_lng = driver.current_lng ?? default(decimal);
                            var driverCoord = new GeoCoordinate((double)driver_lat, (double)driver_lng);

                            decimal loc_lat = bookingobj.from_lat ?? default(decimal);
                            decimal loc_lng = bookingobj.from_lng ?? default(decimal);
                            var locationCoord = new GeoCoordinate((double)loc_lat, (double)loc_lng);

                            distance = driverCoord.GetDistanceTo(locationCoord);
                            driver.distance = distance;
                        }

                        //get 10 nearest driver 
                        nearestdrivers = (from e in AvailableDrivers
                                          orderby e.distance ascending
                                          select e).Skip(0).Take(10).ToList();

                        foreach (var nearestdriver in nearestdrivers)
                        {          //create driverbooking entry 
                            entity.DriverBooking newdriverbookingentity = new entity.DriverBooking();
                            newdriverbookingentity.bookingId = bookingId;
                            newdriverbookingentity.driverId = nearestdriver.driverId;
                            newdriverbookingentity.status = (int)DriverBookingStatus.Pending;
                            newdriverbookingentity.created_date = DateTime.Now;
                            context.DriverBookings.Add(newdriverbookingentity);
                            context.SaveChanges();

                            //set driver notified 
                            driver_notifiedList.Add(nearestdriver.driverId);

                            //send push notification
                            Helper.PushNotificationsToDriver(nearestdriver.driverId, CustomMessage.NewDriverBooking(), bookingId);

                        }
                    }
                    else
                    {
                        //set to manual assign if all rejected on specific booking 
                        bookingobj.manual_Assign_Flag = true;
                        bookingobj.last_updated = DateTime.Now;
                        context.SaveChanges();
                    }
                    #endregion
                }

                #region //old checking
                /*
                    bool isFavAvailable = true;
                    bool isFavBooked = false;

                    //get fav driver 
                    var fav_driverEntity = from d in context.Drivers
                                    where d.driverId == fdid
                                    && (d.car_Type == bookingobj.request_Cartype || d.car_Type == null)
                                    select d;
                     
                    if (fav_driverEntity.Count() > 0 && fdid > 0)
                    {
                        int target_favdriver = fav_driverEntity.First().driverId; 

                        //get driverbookings list //push notifications 
                        var fav_driverbookingsEntity = from d in context.DriverBookings
                                                       where d.driverId == target_favdriver
                                                         && d.created_date != null
                                                         select d; 

                        //1. notifications created for fav driver and not responded 
                        if(fav_driverbookingsEntity.Count() > 0)
                        {
                            var fav_driverbookings_notavailable = from d in fav_driverbookingsEntity
                                                                  where d.bookingId == bookingId
                                                                  select d;

                            if (fav_driverbookings_notavailable.Count() > 0) isFavAvailable = false;
                        }

                        //2. fav driver started pickup with other booking 
                        var bookings_pickupEntity = context.Database.SqlQuery<BookingVM>(
                                            "Select * from Booking_trx t0 " +
                                            "where t0.assigned_driverid = {0} and t0.booking_status = {1}",
                                            fdid , (int)BookingStatus.Pickup
                                        );

                        if (bookings_pickupEntity.Count() > 0)
                        {
                            isFavAvailable = false; isFavBooked = true;
                        }
                    }
                    else
                    {
                        isFavAvailable = false; isFavBooked = true; 
                    }

                    if (isFavAvailable && !isFavBooked)
                    {
                        //create driverbooking entry 
                        entity.DriverBooking newdriverbookingentity = new entity.DriverBooking();
                        newdriverbookingentity.bookingId = bookingId;
                        newdriverbookingentity.driverId = fdid;  //use preferredID specified 
                        newdriverbookingentity.status = (int)DriverBookingStatus.Pending;
                        newdriverbookingentity.created_date = DateTime.Now;
                        context.DriverBookings.Add(newdriverbookingentity);
                        context.SaveChanges();

                        //add to driver notified list
                        driver_notifiedList.Add(fdid);

                        //send push notification
                        Helper.PushNotificationsToDriver(fdid, CustomMessage.NewDriverBooking(), bookingId);
                    }
                    else if (!isFavAvailable && !isFavBooked)
                    {
                        //no preferred driver , assign nearest non-preferred driver 
                        //check driver with no booking ongoing status associated and 
                        //driversbooking no rejected recorded  

                        //get other driver 
                        var otherdriverEntity = context.Database.SqlQuery<DriverVM>(
                                            "Select * from Driver " +
                                            "where driverId != {0} " +
                                            "and (car_Type is null or car_Type = {1})", 
                                            fdid , bookingobj.request_Cartype
                                            ).ToList();

                        List<DriverVM> AvailableDrivers = new List<DriverVM>(); 

                        if (otherdriverEntity.Count() > 0)
                        {
                            foreach(var driver in otherdriverEntity)
                            {
                                var isAddDriver = true;

                                //filter driver with driverbookings created 
                                var otherdriverEntity_driverBooking = from d in context.DriverBookings
                                                                      where d.bookingId == bookingId 
                                                                      && d.driverId == driver.driverId
                                                                      select d;
                                if (otherdriverEntity_driverBooking.Count() > 0) isAddDriver = false;

                                //filter driver with bookings matched 
                                var otherdriverEntity_bookings = context.Database.SqlQuery<BookingVM>(
                                                                "Select * from Booking_trx t0 " +
                                                                "left join DriverBookings t1 on t0.bookingId = t1.bookingId " +
                                                                "where t1.driverId = {0} and t0.booking_status = {1}",
                                                                driver.driverId, (int)BookingStatus.Pickup
                                                                    );
                                if (otherdriverEntity_bookings.Count() > 0) isAddDriver = false;

                                if (isAddDriver)
                                {
                                    AvailableDrivers.Add(driver);
                                }
                            }
                        }

                        if (AvailableDrivers.Count() > 0)
                        {
                            List<DriverVM> nearestdrivers = new List<DriverVM>();
                            double distance = 0;   //distance between current driver location and booking pickup loc

                            foreach (var driver in AvailableDrivers)
                            {
                                //calculate distance  
                                decimal driver_lat = driver.current_lat ?? default(decimal);
                                decimal driver_lng = driver.current_lng ?? default(decimal);
                                var driverCoord = new GeoCoordinate((double)driver_lat, (double)driver_lng);

                                decimal loc_lat = bookingobj.from_lat ?? default(decimal);
                                decimal loc_lng = bookingobj.from_lng ?? default(decimal);
                                var locationCoord = new GeoCoordinate((double)loc_lat, (double)loc_lng);

                                distance = driverCoord.GetDistanceTo(locationCoord);
                                driver.distance = distance;
                            }

                            //get 10 nearest driver 
                            nearestdrivers = (from e in AvailableDrivers
                                             orderby e.distance ascending
                                             select e).Skip(0).Take(10).ToList();

                            foreach(var nearestdriver in nearestdrivers)
                            {          //create driverbooking entry 
                                entity.DriverBooking newdriverbookingentity = new entity.DriverBooking();
                                newdriverbookingentity.bookingId = bookingId;
                                newdriverbookingentity.driverId = nearestdriver.driverId;
                                newdriverbookingentity.status = (int)DriverBookingStatus.Pending;
                                newdriverbookingentity.created_date = DateTime.Now;
                                context.DriverBookings.Add(newdriverbookingentity);
                                context.SaveChanges();

                                //set driver notified 
                                driver_notifiedList.Add(nearestdriver.driverId);

                                //send push notification
                                Helper.PushNotificationsToDriver(nearestdriver.driverId, CustomMessage.NewDriverBooking(), bookingId);
                   
                            }
                       }
                        else
                        {
                            //set to manual assign if all rejected on specific booking 
                            bookingobj.manual_Assign_Flag = true;
                            bookingobj.last_updated = DateTime.Now;
                            context.SaveChanges();
                        }
                    }
                    else 
                    {
                        //fav driver booked , return empty list to stop sequence and wait respond from user
                        var userid = bookingobj.userId ?? default(int);

                        //update booking status 
                        bookingobj.booking_status = (int)BookingStatus.PreferredNoRespond;
                        context.SaveChanges();

                        //notify user 
                        Helper.PushNotificationsToUser(userid, CustomMessage.FavDriverNoRespond(), bookingId);
                    }*/
                #endregion
                
            }

            return driver_notifiedList;
        }

        public EstimateFareResponse EstimatePrice(double distance, int userId, double frompx, double frompy, double topx, double topy, string from, string to, string email, int car_type = 0)
        {
            from = from == null ? "" : from ;
            to = to == null ? "" : to;
            email = email == null ? "" : email;


            if(CheckDestinationInGenting(frompx , frompy , topx , topy) && distance < 85000)
            {
                //either of destination is Genting , check rwgenting employee travelling from/to Wisma genting 

                if (CheckWismaGentingDestination(from, to, email))
                {
                    return new EstimateFareResponse() { EstimatedFare = 70, Discount = 0 };
                }
                else
                {
                    return new EstimateFareResponse() { EstimatedFare = 80, Discount = 0 };
                }
            }

            //car type : 0 , executive fare 
            double est_fare = 0; double discountvalue = 0;

            //calculate estimated price 
            var distanceinkm = distance / 1000;

            if (distanceinkm <= 1)
            {
                if (car_type == (int)Cartype.TEKS1M)
                    est_fare = 3;
                else if (car_type == (int)Cartype.RW)
                    est_fare = 1.25 * distanceinkm;
                else
                    est_fare = 6;
            }
            else if (distanceinkm > 1 && distanceinkm <= 5)
            {
                if (car_type == (int)Cartype.TEKS1M)
                    est_fare = 6;
                else if (car_type == (int)Cartype.RW)
                    est_fare = 1.25 * distanceinkm;
                else
                    est_fare = 14;
            }
            else if (distanceinkm > 5 && distanceinkm <= 10)
            {
                if (car_type == (int)Cartype.TEKS1M)
                {
                    double meter = Math.Round(((distance) - 5000) / 200);
                    est_fare = 10 + meter * 0.3;
                }
                else if (car_type == (int)Cartype.RW)
                    est_fare = 1.25 * distanceinkm;
                else
                {
                    est_fare = 24;
                }
            }
            else if (distanceinkm > 10)
            {
                if (car_type == (int)Cartype.TEKS1M)
                {
                    double meter = Math.Round(((distance) - 10000) / 200);
                    est_fare = 17.5 + meter * 0.3;
                }
                else if (car_type == (int)Cartype.RW)
                    est_fare = 1.25 * distanceinkm;
                else
                {
                    double meter = Math.Round(((distance) - 10000) / 100);  //get extra distance after 10km
                    est_fare = 24 + meter * 0.2;  //for every 100m , charge rm0.2
                }
            }

            //check user email 
            /*
            using (var context = new entity.gtaxidbEntities())
            {
                if(car_type == (int)Cartype.RW)
                {
                    var userentities = from d in context.Users
                                       where d.userId == userId
                                       select d;
                    if (userentities.Count() > 0)
                    {
                        var user = userentities.First();

                        //check email 
                        if (user.email.Contains("@rwgenting.com"))
                        {
                            //hardcode deduct rm10
                            est_fare = est_fare - 10 < 0 ? 0 : est_fare - 10;
                            discountvalue = 10;
                        }
                    }
                }

            }
             * */

            return new EstimateFareResponse() { EstimatedFare = est_fare, Discount = discountvalue };
        }

        public bool CheckWismaGentingDestination(string from, string to, string email)
        {
            if (email.Contains("@rwgenting.com"))
            {

                if ((from.ToLower().Contains("wisma genting") || to.ToLower().Contains("wisma genting")) &&
                    !(from.ToLower().Contains("wisma genting") && to.ToLower().Contains("wisma genting")))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false; 
            }
        }

        public bool CheckDestinationInGenting(double frompx, double frompy, double topx, double topy)
        {
            int GentingRadius = 4000; //10km hardcode
            bool isFromLocationInGenting = false;
            bool isToLocationInGenting = false;

            var fromcoord = new GeoCoordinate((double)frompx, (double)frompy);
            var tocoord = new GeoCoordinate((double)topx, (double)topy);

            var gentingcoord = new GeoCoordinate(3.422910, 101.793573);

            var fromdistance = fromcoord.GetDistanceTo(gentingcoord);
            var todistance = tocoord.GetDistanceTo(gentingcoord);

            if (fromdistance <= GentingRadius) isFromLocationInGenting = true;
            if (todistance <= GentingRadius) isToLocationInGenting = true;

            if ((isFromLocationInGenting || isToLocationInGenting) &&
                !(isFromLocationInGenting && isToLocationInGenting))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region ForCMSWeb
        public List<BookingDetailVM> GetAllBookings(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", bool manualAssign = false, int UserId = 0, int DriverId = 0, string User = "", string Driver = "", string BookingDateStart = "", string BookingDateTo = "", string Status = "")
        {
            List<BookingDetailVM> result = new List<BookingDetailVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Booking_trx
                          join e in context.Users on d.userId equals e.userId
                          join f in context.Drivers on d.assigned_driverId equals f.driverId into driver
                          from g in driver.DefaultIfEmpty()
                          select new
                          {
                              Booking = d,
                              User = e,
                              Driver = g == null ? null : g
                          };

                if (manualAssign == true)
                {
                    ett = ett.Where(d => d.Booking.manual_Assign_Flag == true);
                }

                //filtering
                if (UserId != 0)
                {
                    ett = ett.Where(m => m.User.userId == UserId);
                }
                if (DriverId != 0)
                {
                    ett = ett.Where(m => m.Driver.driverId == DriverId);
                }
                if (User != "")
                {
                    ett = ett.Where(m => m.User.name.ToLower().Contains(User.ToLower())).Union(ett.Where(m => m.User.email.ToLower().Contains(User.ToLower())));
                }
                if (Driver != "")
                {
                    ett = ett.Where(m => m.Driver.name.ToLower().Contains(Driver.ToLower()));
                }
                if (Status != "")
                {
                    int iStatus = int.Parse(Status);

                    if(manualAssign)
                    {
                        //manual assign case , retrieve pending and no respond status 
                        ett = ett.Where(m => m.Booking.booking_status == (int)BookingStatus.Pending ||
                           m.Booking.booking_status == (int)BookingStatus.PreferredNoRespond);
                    }
                    else
                    {
                        ett = ett.Where(m => m.Booking.booking_status == iStatus);
                    }
                }
                if (BookingDateStart != "" && BookingDateTo != "")
                {
                    DateTime DateFrom = DateTime.Parse(BookingDateStart);
                    DateTime DateTo = DateTime.Parse(BookingDateTo).AddDays(1);
                    ett = ett.Where(m => m.Booking.booking_datetime.Value >= DateFrom && m.Booking.booking_datetime.Value < DateTo);
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "UserName")
                        ett = ett.OrderBy(m => m.User.name);
                    else if (orderBy == "DriverName")
                        ett = ett.OrderBy(m => m.Driver.name);
                    else if (orderBy == "BookingDateTime")
                        ett = ett.OrderBy(m => m.Booking.booking_datetime);
                    else if (orderBy == "From")
                        ett = ett.OrderBy(m => m.Booking.from_locationname);
                    else if (orderBy == "To")
                        ett = ett.OrderBy(m => m.Booking.to_locationname);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.Booking.booking_status);
                    else
                        ett = ett.OrderBy(m => m.Booking.bookingId);
                }
                else
                {
                    if (orderBy == "UserName")
                        ett = ett.OrderByDescending(m => m.User.name);
                    else if (orderBy == "DriverName")
                        ett = ett.OrderByDescending(m => m.Driver.name);
                    else if (orderBy == "BookingDateTime")
                        ett = ett.OrderByDescending(m => m.Booking.booking_datetime);
                    else if (orderBy == "From")
                        ett = ett.OrderByDescending(m => m.Booking.from_locationname);
                    else if (orderBy == "To")
                        ett = ett.OrderByDescending(m => m.Booking.to_locationname);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.Booking.booking_status);
                    else
                        ett = ett.OrderByDescending(m => m.Booking.bookingId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    BookingDetailVM vm = new BookingDetailVM();
                    vm.BookingVM = new BookingVM();
                    vm.UserVM = new UserVM();
                    vm.DriverVM = new DriverVM();

                    vm.UserVM.name = v.User.name;
                    if (v.Driver != null)
                    {
                        vm.DriverVM.name = v.Driver.name;
                    }
                    else
                    {
                        vm.DriverVM.name = "-";
                    }
                    vm.BookingVM.booking_datetime = v.Booking.booking_datetime;
                    vm.BookingVM.from = v.Booking.from_locationname;
                    vm.BookingVM.to = v.Booking.to_locationname;
                    vm.BookingVM.booking_status = v.Booking.booking_status;
                    vm.BookingVM.bookingId = v.Booking.bookingId;
                    vm.BookingVM.created_date = v.Booking.created_date; 

                    result.Add(vm);
                }
            }

            return result;
        }

        public BookingDetailVM GetBooking(int BookingId)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Booking_trx
                          join e in context.Users on d.userId equals e.userId
                          join f in context.Drivers on d.assigned_driverId equals f.driverId into driver
                          from g in driver.DefaultIfEmpty()
                          where d.bookingId == BookingId
                          select new
                          {
                              Booking = d,
                              User = e,
                              Driver = g == null ? null : g
                          };

                if (ett.Count() > 0)
                {
                    var v = ett.First();

                    BookingDetailVM vm = new BookingDetailVM();
                    vm.BookingVM = new BookingVM();
                    vm.UserVM = new UserVM();
                    vm.DriverVM = new DriverVM();

                    //user
                    vm.UserVM.userId = v.User.userId;
                    vm.UserVM.name = v.User.name;
                    vm.UserVM.username = v.User.username;
                    vm.UserVM.email = v.User.email;
                    vm.UserVM.phone = v.User.phone;

                    //driver
                    if (v.Driver != null)
                    {
                        vm.DriverVM.driverId = v.Driver.driverId;
                        vm.DriverVM.name = v.Driver.name;
                        vm.DriverVM.gender = v.Driver.gender;
                        vm.DriverVM.car_Plate = v.Driver.car_Plate;
                        vm.DriverVM.photo_url = v.Driver.photo_url;
                    }
                    else
                    {
                        vm.DriverVM.driverId = 0;
                    }

                    //booking
                    vm.BookingVM.bookingId = v.Booking.bookingId;
                    vm.BookingVM.booking_datetime = v.Booking.booking_datetime;
                    vm.BookingVM.request_Cartype = v.Booking.request_Cartype;
                    vm.BookingVM.est_Distance = v.Booking.est_Distance;
                    vm.BookingVM.est_Fares = v.Booking.est_Fares;
                    vm.BookingVM.pickup_Datetime = v.Booking.pickup_Datetime;
                    vm.BookingVM.journey_End_Datetime = v.Booking.journey_End_Datetime;
                    vm.BookingVM.booking_status = v.Booking.booking_status;
                    vm.BookingVM.remarks = v.Booking.remarks;
                    vm.BookingVM.from_lat = v.Booking.from_lat;
                    vm.BookingVM.from_lng = v.Booking.from_lng;
                    vm.BookingVM.to_lat = v.Booking.to_Lat;
                    vm.BookingVM.to_lng = v.Booking.to_lng;
                    vm.BookingVM.from = v.Booking.from_locationname;
                    vm.BookingVM.to = v.Booking.to_locationname;
                    vm.BookingVM.manual_Assign_Flag = v.Booking.manual_Assign_Flag;

                    return vm;
                }
                else
                {
                    throw new CustomException(CustomErrorType.BookingNotFound);
                }
            }
        }

        public void AdminAssignDriver(int BookingId, int DriverId, int AdminId)
        {
            DriverService DriverService = new DriverService();
            var NADriversIds = DriverService.GetAllNotAvailableDriversIds();

            if (NADriversIds.Contains(DriverId) == true)
            {
                throw new CustomException(CustomErrorType.DriverAlreadyAssign); ////
            }
            else
            {
                using (var context = new entity.gtaxidbEntities())
                {
                    var ett = from d in context.Booking_trx
                              where d.bookingId == BookingId
                              select d;

                    if (ett.Count() > 0)
                    {
                        var Booking = ett.First();

                        if (Booking.booking_status == (int)BookingStatus.Assigned || Booking.assigned_driverId != null)
                        {
                            throw new CustomException(CustomErrorType.BookingAlreadyAssign);
                        }
                        else
                        {
                            Booking.booking_status = (int)BookingStatus.Assigned;
                            Booking.assigned_driverId = DriverId;
                            Booking.last_updated = DateTime.Now;
                            Booking.manual_Assign_AdminId = AdminId;
                            Booking.manual_Assign_Datetime = DateTime.Now;
                            context.SaveChanges();

                            //push notification here
                            Helper.PushNotificationsToUser(Booking.userId.Value, CustomMessage.UserBookingUpdated(), Booking.bookingId);
                            Helper.PushNotificationsToDriver(Booking.assigned_driverId.Value, CustomMessage.NewDriverBooking(), Booking.bookingId);
                        }
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.BookingNotFound);
                    }
                }
            }
        }

        public int GetBookingCount()
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Booking_trx
                          select d;

                return ett.Count();
            }
        }

        public List<BookingDetailVM> GetBookingWithUnusualActivities(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "")
        {
            List<BookingDetailVM> result = new List<BookingDetailVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Booking_trx
                          join e in context.Users on d.userId equals e.userId
                          join f in context.Drivers on d.assigned_driverId equals f.driverId into driver
                          from g in driver.DefaultIfEmpty()
                          select new
                          {
                              Booking = d,
                              User = e,
                              Driver = g == null ? null : g
                          };
                
                //filter by unsual activities
                var DateTime30minutesago = DateTime.Now.AddMinutes(-30);
                var DateTime6hoursago = DateTime.Now.AddHours(-6);
                var Rule1 = ett.Where(m => m.Booking.booking_status == (int)BookingStatus.Assigned && m.Booking.booking_datetime.Value <= DateTime30minutesago);
                var Rule2 = ett.Where(m => m.Booking.booking_status == (int)BookingStatus.Pickup && (m.Booking.pickup_Datetime != null ? m.Booking.pickup_Datetime.Value <= DateTime6hoursago : true));
                ett = Rule1.Union(Rule2);

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "UserName")
                        ett = ett.OrderBy(m => m.User.name);
                    else if (orderBy == "DriverName")
                        ett = ett.OrderBy(m => m.Driver.name);
                    else if (orderBy == "BookingDateTime")
                        ett = ett.OrderBy(m => m.Booking.booking_datetime);
                    else if (orderBy == "From")
                        ett = ett.OrderBy(m => m.Booking.from_locationname);
                    else if (orderBy == "To")
                        ett = ett.OrderBy(m => m.Booking.to_locationname);
                    else if (orderBy == "Status")
                        ett = ett.OrderBy(m => m.Booking.booking_status);
                    else
                        ett = ett.OrderBy(m => m.Booking.bookingId);
                }
                else
                {
                    if (orderBy == "UserName")
                        ett = ett.OrderByDescending(m => m.User.name);
                    else if (orderBy == "DriverName")
                        ett = ett.OrderByDescending(m => m.Driver.name);
                    else if (orderBy == "BookingDateTime")
                        ett = ett.OrderByDescending(m => m.Booking.booking_datetime);
                    else if (orderBy == "From")
                        ett = ett.OrderByDescending(m => m.Booking.from_locationname);
                    else if (orderBy == "To")
                        ett = ett.OrderByDescending(m => m.Booking.to_locationname);
                    else if (orderBy == "Status")
                        ett = ett.OrderByDescending(m => m.Booking.booking_status);
                    else
                        ett = ett.OrderByDescending(m => m.Booking.bookingId);
                }
                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    BookingDetailVM vm = new BookingDetailVM();
                    vm.BookingVM = new BookingVM();
                    vm.UserVM = new UserVM();
                    vm.DriverVM = new DriverVM();

                    vm.UserVM.name = v.User.name;
                    if (v.Driver != null)
                    {
                        vm.DriverVM.name = v.Driver.name;
                    }
                    else
                    {
                        vm.DriverVM.name = "-";
                    }
                    vm.BookingVM.booking_datetime = v.Booking.booking_datetime;
                    vm.BookingVM.from = v.Booking.from_locationname;
                    vm.BookingVM.to = v.Booking.to_locationname;
                    vm.BookingVM.booking_status = v.Booking.booking_status;
                    vm.BookingVM.bookingId = v.Booking.bookingId;

                    result.Add(vm);
                }
            }

            return result;
        }

        public List<DriverBookingDetailVM> GetDriverBookings(int startIdx, int length, ref int TotalCount, int BookingId = 0)
        {
            List<DriverBookingDetailVM> result = new List<DriverBookingDetailVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from c in context.DriverBookings
                          join d in context.Booking_trx on c.bookingId equals d.bookingId
                          join e in context.Users on d.userId equals e.userId
                          join f in context.Drivers on c.driverId equals f.driverId
                          where c.bookingId == BookingId
                          orderby c.driverBookingsId descending
                          select new
                          {
                              DriverBooking = c,
                              Booking = d,
                              User = e,
                              Driver = f
                          };

                TotalCount = ett.Count();

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    DriverBookingDetailVM vm = new DriverBookingDetailVM();
                    vm.DriverBookingsVM = new DriverBookingsVM();
                    vm.BookingVM = new BookingVM();
                    vm.UserVM = new UserVM();
                    vm.DriverVM = new DriverVM();

                    vm.UserVM.name = v.User.name;
                    if (v.Driver != null)
                    {
                        vm.DriverVM.name = v.Driver.name;
                    }
                    else
                    {
                        vm.DriverVM.name = "-";
                    }
                    vm.BookingVM.booking_datetime = v.Booking.booking_datetime;
                    vm.BookingVM.from = v.Booking.from_locationname;
                    vm.BookingVM.to = v.Booking.to_locationname;
                    vm.BookingVM.booking_status = v.Booking.booking_status;
                    vm.BookingVM.bookingId = v.Booking.bookingId;

                    vm.DriverBookingsVM.bookingId = v.DriverBooking.bookingId;
                    vm.DriverBookingsVM.created_date = v.DriverBooking.created_date;
                    vm.DriverBookingsVM.driverBookingsId = v.DriverBooking.driverBookingsId;
                    vm.DriverBookingsVM.driverId = v.DriverBooking.driverId;
                    vm.DriverBookingsVM.last_updated = v.DriverBooking.last_updated;
                    vm.DriverBookingsVM.response_datettime = v.DriverBooking.response_datettime;
                    vm.DriverBookingsVM.status = v.DriverBooking.status;

                    result.Add(vm);
                }
            }

            return result;
        }

        #endregion

    }

}
