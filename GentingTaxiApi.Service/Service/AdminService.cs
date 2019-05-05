using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using System.Data.Entity.Validation;
using System.Net;
using System.Threading;
using GentingTaxiApi.Service.entity;

namespace GentingTaxiApi.Service
{
    public class AdminService
    {
        public int LoginAdminService(string username, string password, ref string name)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //check Admin exist 
                var entityAdmin = from d in context.Admins
                                  where d.username == username
                                  select d;

                if (entityAdmin.Count() > 0 &&
                    (Security.checkHMAC(
                        entityAdmin.First().password_salt, password) == entityAdmin.First().password
                        )
                    )
                {
                    name = entityAdmin.First().name;
                    return entityAdmin.First().adminId;
                }
                else
                {
                    throw new CustomException(CustomErrorType.AdminInvalid);
                }
            }
        }

        public static bool IsAuthenticated()
        {
            using (var context = new entity.gtaxidbEntities())
            {
                long AdminId = long.Parse(Thread.CurrentPrincipal.Identity.Name);

                var result = from d in context.Admins
                             where d.adminId == AdminId
                             select d;

                if (result.Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public List<AdminVM> GetAllAdmin(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<AdminVM> result = new List<AdminVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Admins
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.name.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Email")
                        ett = ett.Where(m => m.username.ToLower().Contains(filterQuery.ToLower()));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.name);
                    else if (orderBy == "Email")
                        ett = ett.OrderBy(m => m.username);
                    else
                        ett = ett.OrderBy(m => m.adminId);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.name);
                    else if (orderBy == "Email")
                        ett = ett.OrderByDescending(m => m.username);
                    else
                        ett = ett.OrderByDescending(m => m.adminId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    AdminVM vm = new AdminVM();
                    vm.adminId = v.adminId;
                    vm.username = v.username;
                    vm.name = v.name;
                    result.Add(vm);
                }
            }

            return result;
        }

        public AdminVM GetAdmin(int AdminId)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Admins
                          where d.adminId == AdminId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();
                    AdminVM vm = new AdminVM();
                    vm.adminId = v.adminId;
                    vm.name = v.name;

                    return vm;
                }
                else
                {
                    throw new CustomException(CustomErrorType.AdminNotFound);
                }
            }
        }

        public int AddAdmin(AdminVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Admins
                          where d.username == vm.username
                          select d;

                if (ett.Count() > 0)
                {
                    throw new CustomException(CustomErrorType.AdminAlreadyAssign);
                }
                else
                {
                    Admin v = new Admin();
                    v.name = vm.name;
                    v.username = vm.username;

                    //password
                    string key = Security.RandomString(60);
                    string pass = Security.checkHMAC(key, vm.password);

                    v.password = pass;
                    v.password_salt = key;

                    context.Admins.Add(v);
                    context.SaveChanges();

                    return v.adminId;
                }
            }
        }

        public bool EditAdmin(AdminVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Admins
                          where d.adminId == vm.adminId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();

                    if (v.username != vm.username)
                    {
                        var CheckOtherAdminUsername = from d in context.Admins
                                                      where d.username == vm.username
                                                      && d.adminId != v.adminId
                                                      select d;

                        if (CheckOtherAdminUsername.Count() > 0)
                        {
                            throw new CustomException(CustomErrorType.AdminAlreadyAssign);
                        }
                    }

                    v.name = vm.name;
                    v.username = vm.username;

                    //password
                    if (vm.password != null && vm.password != "")
                    {
                        string key = Security.RandomString(60);
                        string pass = Security.checkHMAC(key, vm.password);

                        v.password = pass;
                        v.password_salt = key;
                    }

                    context.SaveChanges();

                    return true;
                }
                else
                {
                    throw new CustomException(CustomErrorType.AdminNotFound);
                }
            }
        }

        public AdminSettingVM GetAdminSetting()
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var adminsettingentity = context.Database.SqlQuery<AdminSettingVM>(
                                            "Select * from AdminSetting "
                                        );

                if(adminsettingentity.Count() > 0)
                {
                    AdminSettingVM defaultmodel = new AdminSettingVM();
                    defaultmodel.location_range_miles = adminsettingentity.First().location_range_miles ?? default(decimal);
                    defaultmodel.searchdriverbuffer_sec = adminsettingentity.First().searchdriverbuffer_sec ?? default(long);
                    defaultmodel.driveravailabilityrange_min = adminsettingentity.First().driveravailabilityrange_min ?? default(int);
                    defaultmodel.current_driver_android_app_version = adminsettingentity.First().current_driver_android_app_version;
                    defaultmodel.current_driver_ios_app_version = adminsettingentity.First().current_driver_ios_app_version;
                    defaultmodel.current_user_android_app_version = adminsettingentity.First().current_user_android_app_version;
                    defaultmodel.current_user_ios_app_version = adminsettingentity.First().current_user_ios_app_version;

                    return adminsettingentity.First(); 
                }
                else
                {
                    AdminSettingVM emptymodel = new AdminSettingVM();
                    emptymodel.location_range_miles = 0;
                    emptymodel.searchdriverbuffer_sec = 0;
                    emptymodel.driveravailabilityrange_min = 0;

                    return emptymodel; 
                }
            }
        }

        public void UpdateAdminSetting(AdminSettingVM model)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                //clear db setting 
                context.Database.ExecuteSqlCommand("Delete from adminsetting");

                var entity = new entity.AdminSetting();
                entity.driveravailabilityrange_min = model.driveravailabilityrange_min;
                entity.searchdriverbuffer_sec = model.searchdriverbuffer_sec;
                entity.location_range_miles = model.location_range_miles;
                entity.current_driver_android_app_version = model.current_driver_android_app_version;
                entity.current_driver_ios_app_version = model.current_driver_ios_app_version;
                entity.current_user_android_app_version = model.current_user_android_app_version;
                entity.current_user_ios_app_version = model.current_user_ios_app_version;

                context.AdminSettings.Add(entity);
                context.SaveChanges(); 
            }
        }

    }
}