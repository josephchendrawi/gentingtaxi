using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using ServiceStack.Text;
using GentingTaxiApi.Service;
using ServiceStack.Auth;
using ServiceStack.Mvc;
using Funq;
using System.Web.Mvc;
using ServiceStack.Validation;
using ServiceStack.FluentValidation.Results;
using System.Net;

namespace GentingTaxi.Web
{
    public class AppHost : AppHostBase
    {

        public AppHost()
            : base("Genting Taxi System Web Service", typeof(UserApiService).Assembly)
        {
            //functions test
        }

        //override functions to check token 
        public override void Configure(Container container)
        {
            //register push broker instance in container 

            //Set MVC to use the same Funq IOC as ServiceStack
            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
            //ServiceStackController.CatchAllController = reqCtx => container.TryResolve<UserController>();

            /*
            base.SetConfig(new HostConfig
            {
                DebugMode = true //Show StackTraces for easier debugging (default auto inferred by Debug/Release builds)
            });*/

            //set service stack handler path
            /*
            SetConfig(new HostConfig
            {
                HandlerFactoryPath = "api",
            });*/

            //Plugins.Add(new CorsFeature()); //Enable CORS

            //JsConfig.DateHandler = DateHandler.ISO8601;

            //Set JSON web services to return idiomatic JSON camelCase properties
            //JsConfig.EmitCamelCaseNames = true;

            //Plugins.Add(new SessionFeature());
            /*
            Plugins.Add(new AuthFeature(
               () => new CustomUserSession(), //Use your own typed Custom UserSession type
               new IAuthProvider[] {    
                }));
            */
            /*
            this.PreRequestFilters.Add((req, res) =>
            {

                //add token check for all routes except login and register
                if (!req.GetAbsolutePath().ToLower().Contains("login") &&
                    !req.GetAbsolutePath().ToLower().Contains("activate") &&
                    !req.GetAbsolutePath().ToLower().Contains("register") &&
                    !req.GetAbsolutePath().ToLower().Contains("metadata") 
                    )
                {
                    //create session 
                    AuthUserSession session = (AuthUserSession)req.GetSession();
                    if (req.GetHeader("Key") == null || req.GetHeader("Key") == "")
                    {
                        throw new CustomException(CustomErrorType.Unauthenticated);
                    }

                    if (!req.GetAbsolutePath().ToLower().Contains("/d/*"))
                    {
                        //for user 
                        var decoded = Helper.DecodeFrom64(req.GetHeader("Key"));

                        AuthUserSession user = Helper.checkAuth(decoded[0], decoded[1], decoded[2]);

                        session.FullName = user.FullName;
                        session.Email = user.Email;
                        session.UserName = user.UserName;
                        session.PhoneNumber = user.PhoneNumber;

                        session.RequestTokenSecret = decoded[0];    //api-key
                        session.UserAuthId = decoded[2];    //user-id

                        req.SaveSession(session);
                    }
                    else
                    {
                        //for driver  
                    }
                }
            });

            this.GlobalResponseFilters.Add((req, res, dto) =>
            {
                req.RemoveSession();
            });

            Plugins.Add(new ValidationFeature
            {
                ErrorResponseFilter = CustomValidationError
            });
            container.RegisterValidators(typeof(AppHost).Assembly);    */
        }

        /*
        public static object CustomValidationError(
            ValidationResult validationResult, object errorDto)
        {
            var error = validationResult.Errors[0];
            var dto = new CustomErrorDto
            {
                errorcode = error.ErrorCode,
                msg = error.ErrorMessage, 
                sts = 1

            };

            //Ensure HTTP Clients recognize this as an HTTP Error
            return new HttpError(dto, HttpStatusCode.BadRequest, dto.errorcode, dto.msg);
        }*/

    }

    public class CustomErrorDto
    {
        public int sts { get; set; }
        public string msg { get; set; }
        public string errorcode { get; set; }
    }
}