using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Web;

namespace GentingTaxiApi.Service
{
    class CustomFilterAttribute
    {
    }

    public class CustomUserSession : AuthUserSession
    {
    }

    public class CustomUserAuthenticateFilter : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            //create new session
            AuthUserSession session = (AuthUserSession)req.GetSession();
            if (string.IsNullOrEmpty(req.GetHeader("Token")))
            {
                session.IsAuthenticated = false;
            }

            var decoded = Helper.DecodeFrom64(req.GetHeader("Token"));

            AuthUserSession user = Helper.checkAuth(decoded[0], decoded[1], decoded[2]);
            
            if (user != null)
            {
                //create session if not available
                session.FullName = user.FullName;
                session.Email = user.Email;
                session.UserName = user.UserName;
                session.PhoneNumber = user.PhoneNumber;
                session.IsAuthenticated = true;

                session.RequestTokenSecret = decoded[0];    //api-key
                session.Id = decoded[0];
                session.UserAuthId = decoded[2];    //user-id

                req.SaveSession(session);
            }
            else
            {
                session.IsAuthenticated = false;
            }
        }
    }

    public class CustomDriverAuthenticateFilter : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            AuthUserSession session = (AuthUserSession)req.GetSession();
            if (string.IsNullOrEmpty(req.GetHeader("Token")))
            {
                session.IsAuthenticated = false;
            }

            var decoded = Helper.DecodeFrom64(req.GetHeader("Token"));

            AuthUserSession user = Helper.checkDriverAuth(decoded[0], decoded[1], decoded[2]);

            if (user != null)
            {
                //create session if not available
                session.FullName = user.FullName;
                session.UserName = user.UserName;
                session.IsAuthenticated = true;

                session.RequestTokenSecret = decoded[0];    //api-key
                session.Id = decoded[0];
                session.UserAuthId = decoded[2];    //user-id

                req.SaveSession(session);
            }
            else
            {
                session.IsAuthenticated = false;
            }
        }
    }
}
