using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GentingTaxiApi.Hubs
{
    public class NotificationHub : Hub
    {
        public void Send(string message)
        {
            // Call the addNotification method to update clients.
            Clients.All.addNotification(message);
        }
    }
}