using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebSocketProject.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// WebSocket
        /// </summary>
        Common.WebSocket webSocket = new Common.WebSocket();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void WSChat(string user)
        {
            webSocket.Monitor(HttpContext, user);
        }
    }
}