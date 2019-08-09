using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSocketProject.Common
{
    /// <summary>
    /// 离线消息
    /// </summary>
    public class MessageInfo
    {
        public MessageInfo(DateTime _MsgTime, ArraySegment<byte> _MsgContent)
        {
            MsgTime = _MsgTime;
            MsgContent = _MsgContent;
        }

        public DateTime MsgTime { get; set; }

        public ArraySegment<byte> MsgContent { get; set; }
    }
}