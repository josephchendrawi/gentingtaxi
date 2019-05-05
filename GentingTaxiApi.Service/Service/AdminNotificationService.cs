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
    public class AdminNotificationService
    {
        public int AddAdminNotification(AdminNotificationVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                AdminNotification v = new AdminNotification();
                v.created_date = DateTime.Now;
                v.message = vm.message;

                context.AdminNotifications.Add(v);
                context.SaveChanges();

                return v.notificationId;
            }
        }

        public List<AdminNotificationVM> GetAllAdminNotifications(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterQuery = "", int AdminId = 0)
        {
            List<AdminNotificationVM> result = new List<AdminNotificationVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.AdminNotifications
                          select d;

                //filtering
                if (filterQuery != "")
                    ett = ett.Where(m => m.message.ToLower().Contains(filterQuery.ToLower()));

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "CreatedDate")
                        ett = ett.OrderBy(m => m.created_date);
                    else if (orderBy == "Message")
                        ett = ett.OrderBy(m => m.message);
                    else
                        ett = ett.OrderBy(m => m.notificationId);
                }
                else
                {
                    if (orderBy == "CreatedDate")
                        ett = ett.OrderByDescending(m => m.created_date);
                    else if (orderBy == "Message")
                        ett = ett.OrderByDescending(m => m.message);
                    else
                        ett = ett.OrderByDescending(m => m.notificationId);
                }

                ett = ett.Skip(startIdx).Take(length);

                //get read notificationId by adminId
                var ettRead = from d in context.AdminNotificationReads
                              where d.adminId == AdminId
                              select d.notificationId;

                //mapping
                foreach (var v in ett)
                {
                    AdminNotificationVM vm = new AdminNotificationVM();
                    vm.adminNotificationId = v.notificationId;
                    vm.message = v.message;
                    vm.adminId = AdminId;
                    vm.isRead = ettRead.Contains(v.notificationId);
                    result.Add(vm);
                }
            }

            return result;
        }

        public int GetUnreadAdminNotificationCount(int AdminId = 0)
        {
            List<AdminNotificationVM> result = new List<AdminNotificationVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.AdminNotifications
                          join e in
                              (from x in context.AdminNotificationReads
                               where x.adminId == AdminId
                               select x)
                          on d.notificationId equals e.notificationId into Read
                          from f in Read.DefaultIfEmpty()
                          where f == null
                          select d;

                return ett.Count();
            }
        }

        public List<AdminNotificationVM> GetLatestAdminNotification(int AdminId = 0, int Count = 5)
        {
            List<AdminNotificationVM> result = new List<AdminNotificationVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.AdminNotifications
                          join e in
                              (from x in context.AdminNotificationReads
                               where x.adminId == AdminId
                               select x)
                          on d.notificationId equals e.notificationId into Read
                          from f in Read.DefaultIfEmpty()
                          orderby d.created_date descending
                          select new
                          {
                              Notification = d,
                              isRead = f == null ? false : true
                          };

                ett = ett.Take(Count);

                //mapping
                foreach (var v in ett)
                {
                    AdminNotificationVM vm = new AdminNotificationVM();
                    vm.adminNotificationId = v.Notification.notificationId;
                    vm.message = v.Notification.message;
                    vm.adminId = AdminId;
                    vm.isRead = v.isRead;
                    result.Add(vm);
                }
            }

            return result;
        }

        public void ReadAllNotifications(int AdminId = 0)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var Admin = from d in context.Admins
                            where d.adminId == AdminId
                            select d;

                if (Admin.Count() > 0)
                {
                    var UnreadNotifications = from d in context.AdminNotifications
                                              join e in
                                                  (from x in context.AdminNotificationReads
                                                   where x.adminId == AdminId
                                                   select x)
                                              on d.notificationId equals e.notificationId into Read
                                              from f in Read.DefaultIfEmpty()
                                              where f == null
                                              select d;

                    foreach (var v in UnreadNotifications)
                    {
                        AdminNotificationRead ett = new AdminNotificationRead();
                        ett.notificationId = v.notificationId;
                        ett.adminId = AdminId;
                        ett.read_time = DateTime.Now;

                        context.AdminNotificationReads.Add(ett);
                    }
                    context.SaveChanges();
                }
            }
        }

    }
}