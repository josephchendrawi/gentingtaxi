using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using ServiceStack.Auth;
using GentingTaxiApi.Interface.Types;

namespace GentingTaxiApi.Service
{
    public class UserCredentialsAuthProvider : CredentialsAuthProvider
    {
        //use servicestack auth
        public override bool TryAuthenticate(IServiceBase authService,
        string userName, string password)
        {
            try
            {
                using (var context = new entity.gtaxidbEntities())
                {
                    //check user exist 
                    var entityuser = from d in context.Users
                                     where d.username == userName
                                     select d;
                    if (entityuser.Count() > 0 &&
                        (Security.checkHMAC(
                            entityuser.First().password_salt, password) == entityuser.First().password
                            ))
                    {

                        //return user 
                        UserVM user = new UserVM();
                        user.username = entityuser.First().username;
                        user.email = entityuser.First().email;
                        user.phone = entityuser.First().phone;
                        user.name = entityuser.First().name;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}