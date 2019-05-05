using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;

namespace GentingTaxiApi.Service
{
    public class LocalizedEnumAttribute : DescriptionAttribute
    {
        private PropertyInfo _nameProperty;
        private Type _resourceType;

        public LocalizedEnumAttribute(string displayNameKey)
            : base(displayNameKey)
        {

        }

        public Type NameResourceType
        {
            get
            {
                return _resourceType;
            }
            set
            {
                _resourceType = value;

                _nameProperty = _resourceType.GetProperty(this.Description, BindingFlags.Static | BindingFlags.Public);
            }
        }

        public override string Description
        {
            get
            {
                //check if nameProperty is null and return original display name value
                if (_nameProperty == null)
                {
                    return base.Description;
                }

                return (string)_nameProperty.GetValue(_nameProperty.DeclaringType, null);
            }
        }
    }

    public static class EnumExtender
    {
        public static string GetLocalizedDescription(this Enum @enum)
        {
            if (@enum == null)
                return null;

            string description = @enum.ToString();

            FieldInfo fieldInfo = @enum.GetType().GetField(description);
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Any())
                return attributes[0].Description;

            return description;
        }
    }

    public enum CustomErrorType
    {
        [Description("Unknown error.")]
        Unknown = 0,
        [Description("Database or entity error.")]
        Data = 1,
        [Description("Unauthorized access.")]
        Unauthorized = 2,
        /// <summary>
        /// User is not authenticated thus not allowed to perform operation.
        /// </summary>
        [Description("Unauthenticated access.")]
        Unauthenticated = 3,
        /// <summary>
        /// Inconsistency in objects due to earlier operation errors. 
        /// Inconsistent error causes business logic error and thus will have limitation in future operations too.
        /// These errors should be logged as serious exception and should be reported to be fixed by administrators.
        /// </summary>
        [Description("ObjectInconsistency.")]
        ObjectInconsistency = 4,
        [Description("Invalid argument(s).")]
        InvalidArguments = 5,
        [Description("Missing required property(s).")]
        MissingRequiredProperty = 6,
        [Description("User not activated .")]
        NotActivated = 7,
        [Description("Token not found .")]
        TokenNotFound = 8,

        [Description("User service error.")]
        UserUnknown = 1000,
        [Description("User not found.")]
        UserNotFound = 1001,
        [Description("User already assign.")]
        UserAlreadyAssign = 1002,
        [Description("User failed.")]
        UserFailed = 1003,
        [Description("User failed delete.")]
        UserFailedDelete = 1004,
        [Description("Invalid Email or Password.")]
        UserInvalid = 1005,
        [Description("Password don't match .")]
        UserPasswordMatchfailed = 1006,

        [Description("Booking service error.")]
        BookingUnknown = 2000,
        [Description("Booking not found.")]
        BookingNotFound = 2001,
        [Description("Booking already assign.")]
        BookingAlreadyAssign = 2002,
        [Description("Booking failed.")]
        BookingFailed = 2003,
        [Description("Booking failed delete.")]
        BookingFailedDelete = 2004,
        [Description("Invalid booking ID for this driver .")]
        BookingInvalidForDriver = 2005,
        [Description("Invalid booking status update .")]
        BookingInvalidStatusUpdate = 2006,
        [Description("Booking already responded or expired .")]
        BookingResponded = 2007,

        [Description("Document type service error.")]
        DocumentTypeUnknown = 3000,
        [Description("Document type token invalid.")]
        DocumentTypeNotFound = 3001,
        [Description("Document type already assign.")]
        DocumentTypeAlreadyAssign = 3002,
        [Description("Document type failed.")]
        DocumentTypeFailed = 3003,
        [Description("Document type failed delete.")]
        DocumentTypeFailedDelete = 3004,

        [Description("Location service error.")]
        LocationUnknown = 4000,
        [Description("Location token invalid.")]
        LocationNotFound = 4001,
        [Description("Location already assign.")]
        LocationAlreadyAssign = 4002,
        [Description("Location failed.")]
        LocationFailed = 4003,
        [Description("Location failed delete.")]
        LocationFailedDelete = 4004,

        [Description("Driver service error.")]
        DriverUnknown = 5000,
        [Description("Driver not found.")]
        DriverNotFound = 5001,
        [Description("Driver already assign.")]
        DriverAlreadyAssign = 5002,
        [Description("Driver failed.")]
        DriverFailed = 5003,
        [Description("Driver failed delete.")]
        DriverFailedDelete = 5004,
        [Description("Invalid Email or Password.")]
        DriverInvalid = 5005,
        
        [Description("Admin service error.")]
        AdminUnknown = 6000,
        [Description("Admin not found.")]
        AdminNotFound = 6001,
        [Description("Admin already assign.")]
        AdminAlreadyAssign = 6002,
        [Description("Admin failed.")]
        AdminFailed = 6003,
        [Description("Admin failed delete.")]
        AdminFailedDelete = 6004,
        [Description("Invalid Email or Password.")]
        AdminInvalid = 6005,

        [Description("Invalid Phone Number.")]
        InvalidPhoneNumber = 7000,
        [Description("Unable send activation code , user already activated.")]
        UnableSentCodeActivated = 7001,
        [Description("Unable send activation code , please wait 5 minutes before requesting again.")]
        UnableSentCodeWithinTime = 7002, 
        
        [Description("Recommended locations not found .")]
        Hotspotnotfound = 8000,
        [Description("Hotspot token invalid.")]
        HotspotNotFound = 8001,
        [Description("Hotspot already assign.")]
        HotspotAlreadyAssign = 8002,
        [Description("Hotspot failed.")]
        HotspotFailed = 8003,
        [Description("Hotspot failed delete.")]
        HotspotFailedDelete = 8004,
        [Description("Hotspot Pricing already assign.")]
        HotspotPricingAlreadyAssign = 8006,
        [Description("Hotspot service error.")]
        HotspotUnknown = 8007,
    }

    public static class CustomMessage
    {
        public static string NewDriverBooking(string dest = "" , string datetimestr = "")
        {
            return "New Booking !";
        }
        public static string UserBookingUpdated(string dest = "", string datetimestr = "")
        {
            return "Your booking status is updated " ;
        }
        public static string FavDriverNoRespond(string dest = "")
        {
            return "Favourite driver not available " ;
        }

        public static string BookingSetManual(string dest = "")
        {
            return "No driver found currently, please wait momentarily .";
        }

        public static string NoDriverRespondInPeriod()
        {
            return "We are currently unable to get you any drivers, you will be contacted soon.";
        }
    }

}
