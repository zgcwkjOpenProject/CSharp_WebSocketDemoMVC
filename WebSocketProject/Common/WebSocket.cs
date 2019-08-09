using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace WebSocketProject.Common
{
    public class WebSocket
    {
        /// <summary>
        /// 用户连接池
        /// </summary>
        private static Dictionary<string, System.Net.WebSockets.WebSocket> CONNECT_POOL = new Dictionary<string, System.Net.WebSockets.WebSocket>();

        /// <summary>
        /// 离线消息池
        /// </summary>
        private static Dictionary<string, List<MessageInfo>> MESSAGE_POOL = new Dictionary<string, List<MessageInfo>>();

        /// <summary>
        /// 监听WebSocket
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="userName">用户名称</param>
        public void Monitor(HttpContext context, string userName)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest((c) => ProcessChat(c, userName));
            }
        }

        /// <summary>
        /// 监听WebSocket
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="userName">用户名称</param>
        public void Monitor(HttpContextBase context, string userName)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest((c) => ProcessChat(c, userName));
            }
        }

        /// <summary>
        /// 进行聊天
        /// </summary>
        /// <param name="context">AspNetWebSocket上下文</param>
        /// <param name="userName">用户昵称</param>
        /// <returns></returns>
        public async Task ProcessChat(AspNetWebSocketContext context, string userName)
        {
            System.Net.WebSockets.WebSocket socket = context.WebSocket;

            try
            {
                #region 用户添加连接池

                //第一次 Open 时，添加到连接池中
                if (!CONNECT_POOL.ContainsKey(userName))
                    CONNECT_POOL.Add(userName, socket);//不存在，添加
                else
                    if (socket != CONNECT_POOL[userName])//当前对象不一致，更新
                    CONNECT_POOL[userName] = socket;

                #endregion 用户添加连接池

                #region 离线消息处理

                if (MESSAGE_POOL.ContainsKey(userName))
                {
                    List<MessageInfo> msgs = MESSAGE_POOL[userName];
                    foreach (MessageInfo item in msgs)
                    {
                        string msgTime = item.MsgTime.ToString("yyyy年MM月dd日HH:mm:ss");
                        string msgContent = Encoding.UTF8.GetString(item.MsgContent.Array);
                        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("时间：" + msgTime + "内容：" + msgContent)), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    MESSAGE_POOL.Remove(userName);//移除离线消息
                }

                #endregion 离线消息处理

                string descUser = string.Empty;//目的用户
                while (true)
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                        WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                        #region 消息处理（字符截取、消息转发）

                        try
                        {
                            #region 关闭Socket处理，删除连接池

                            if (socket.State != WebSocketState.Open)//连接关闭
                            {
                                if (CONNECT_POOL.ContainsKey(userName)) CONNECT_POOL.Remove(userName);//删除连接池
                                break;
                            }

                            #endregion 关闭Socket处理，删除连接池

                            string userMsg = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);//发送过来的消息
                            string[] msgList = userMsg.Split('|');
                            if (msgList.Length == 2)
                            {
                                if (msgList[0].Trim().Length > 0)
                                    descUser = msgList[0].Trim();//记录消息目的用户
                                buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgList[1]));

                                if (CONNECT_POOL.ContainsKey(descUser))//判断客户端是否在线
                                {
                                    System.Net.WebSockets.WebSocket destSocket = CONNECT_POOL[descUser];//目的客户端
                                    if (destSocket != null && destSocket.State == WebSocketState.Open)
                                        await destSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                                else
                                {
                                    await Task.Run(() =>
                                    {
                                        if (!MESSAGE_POOL.ContainsKey(descUser))//将用户添加至离线消息池中
                                            MESSAGE_POOL.Add(descUser, new List<MessageInfo>());
                                        MESSAGE_POOL[descUser].Add(new MessageInfo(DateTime.Now, buffer));//添加离线消息
                                    });
                                }
                            }
                            else
                            {
                                buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(userMsg));

                                foreach (KeyValuePair<string, System.Net.WebSockets.WebSocket> item in CONNECT_POOL)
                                {
                                    await item.Value.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                        }
                        catch (Exception exs)
                        {
                            //消息转发异常处理，本次消息忽略 继续监听接下来的消息
                            string message = exs.Message;
                        }

                        #endregion 消息处理（字符截取、消息转发）
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                //整体异常处理
                if (CONNECT_POOL.ContainsKey(userName)) CONNECT_POOL.Remove(userName);
            }
        }
    }
}