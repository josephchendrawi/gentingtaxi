using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Web.Security;
using System.Data;
using System.Reflection;
using System.ComponentModel;

namespace GentingTaxiApi.Service
{

    public class CustomException : Exception
    {
        private CustomErrorType errorType = CustomErrorType.Unknown;
        public CustomErrorType ErrorType
        {
            get
            {
                return errorType;
            }
        }
        /// <summary>
        /// Create exception with an explicit exception type.
        /// </summary>
        /// <param name="exceptionType"></param>
        public CustomException(CustomErrorType errorType)
            : base(GetDescription(errorType))
        {
            this.errorType = errorType;
        }
        public CustomException(CustomErrorType errorType, string message)
            : base(GetDescription(errorType) + " | " + message)
        {
            this.errorType = errorType;
        }

        public CustomException(CustomErrorType errorType, string message, Exception innerException)
            : base(GetDescription(errorType) + " | " + message, innerException)
        {
            this.errorType = errorType;
        }
        public static string GetDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}
