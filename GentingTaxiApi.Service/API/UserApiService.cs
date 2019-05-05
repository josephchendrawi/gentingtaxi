using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using ServiceStack.Caching;
using ServiceStack;
using System.Net;

namespace GentingTaxiApi.Service
{
    public class UserApiService : ServiceStack.Service
    {
        UserService userservice = new UserService();

        public UserResponse Post(RegisterUserRequest request)
        {
            try
            {
                var response = userservice.RegisterUserService(request);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        public UserResponse Post(ActivateUserRequest request)
        {
            try
            {
                var response = userservice.ActivateUserService(request);

                return response;

            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        public UserResponse Post(ResendUserCodeRequest request)
        {
            try
            {
                var response = userservice.ResendUserCodeService(request);

                return response;

            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        public UserResponse Post(LoginUserRequest request)
        {
            try
            {
                var response = userservice.LoginUserService(request);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError; 
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public UserResponse Post(UpdateUserDeviceIdRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.UpdateUserDeviceIdService(request , api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public ValidateResponse Post(ValidateUserAppRequest request)
        {
            try
            {
                var api_session = base.GetSession();

                int temp;
                int userid = int.TryParse(api_session.UserAuthId, out temp) ? temp : 0;

                var response = userservice.ValidateUserApp(userid);

                return new ValidateResponse() { isCurrentVersion = response , sts = 0 };
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new ValidateResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public UserResponse Get(GetCurrentUserRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetCurrentUserService(request, api_session);
                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        public UserResponse Post(LogoutCurrentUserRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.LogoutUserService(request, api_session);

                //clear session 
                if (api_session != null)
                {
                    using (var cache = TryResolve<ICacheClient>())
                    {
                        cache.Remove(api_session.Id);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public PreferredDriverResponse Get(GetCurrentUserPreferredDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetCurrentUserPreferredDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new PreferredDriverResponse() { sts = 1, msg = ex.Message };
            }
        }


        [CustomUserAuthenticateFilter]
        public PreferredDriverResponse Get(GetAllDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetDriversRequestByUserService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new PreferredDriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public DriverLocationResponse Get(GetDriverLocationRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetDriversLocationRequestByUserService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverLocationResponse() { sts = 1, msg = ex.Message };
            }
        }


        [CustomUserAuthenticateFilter]
        public BookingFromListResponse Get(GetCurrentUserBookingFromRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetBookingFromRequestByUserService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingFromListResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public BookingToListResponse Get(GetCurrentUserBookingToRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetBookingToRequestByUserService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingToListResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public PreferredDriverResponse Post(SetCurrentUserPreferredDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.SetCurrentUserPreferredDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new PreferredDriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public PreferredDriverResponse Delete(DeleteCurrentUserPreferredDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.DeleteCurrentUserPreferredDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new PreferredDriverResponse() { sts = 1, msg = ex.Message };
            }
        }


        [CustomUserAuthenticateFilter]
        public UserResponse Post(UpdateUserRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.UpdateCurrentUser(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public UserResponse Post(SubmitFeedbackRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.SubmitFeedbackService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public UserResponse Get(CheckUserTokenRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.CheckUserTokenService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                return new UserResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public NearByDriverResponse Post(GetNearbyDriversRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = userservice.GetNearbyDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new NearByDriverResponse() { sts = 1, msg = ex.Message };
            }
        }
    }
}
