using System;
using System.Net.Sockets;

namespace FastDFS.Client.Common
{
    /// <summary>
    /// tcp client connection
    /// </summary>
    public class Connection : TcpClient
    {
        /// <summary>
        /// conncetion
        /// </summary>
        public Connection()
        {
            CreateTime = DateTime.Now;
            LastUseTime = DateTime.Now;
            InUse = false;
            SessionId = Guid.NewGuid().ToString("N");
        }
        /// <summary>
        /// id
        /// </summary>
        public string SessionId { get; set; }
        /// <summary>
        /// <seealso cref="Pool"/>
        /// </summary>
        public Pool Pool { get; set; }
        /// <summary>
        /// create time
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// last use time of connection
        /// </summary>
        public DateTime LastUseTime { get; set; }
        /// <summary>
        /// is used
        /// </summary>
        public bool InUse { get; set; }
        /// <summary>
        /// count of used
        /// </summary>
        private int CountOfUsed = 0;

        /// <summary>
        /// 打开连接
        /// <url>https://docs.microsoft.com/zh-cn/dotnet/api/system.net.sockets.socket.connected?view=netframework-4.7.2</url>
        /// </summary>
        public void OpenConnection()
        {
            #region 调试代码，用于捕获是否有这种情况的异常
            if (Connected == false )
            {
                Console.WriteLine($"{SessionId} Connection is disConnected:activie={Active} , remove this connect({Pool.IpEndPoint}) ");
                throw new Exception("connect is disconnected when occuring an exception");
            }
            #endregion
            InUse = true;
            LastUseTime = DateTime.Now;
            CountOfUsed++;
        }
        /// <summary>
        /// is connect of tcpclient
        /// “调用非阻止性、 零字节发送”确认连接是否有效
        /// </summary>
        /// <url>https://docs.microsoft.com/zh-cn/dotnet/api/system.net.sockets.socket.connected?view=netframework-4.7.2</url>
        /// <returns></returns>
        public bool IsConnect()
        {
            bool blockingState = Client.Blocking;
            try
            {
                byte[] tmp = new byte[1];

                Client.Blocking = false;
                Client.Send(tmp, 0, 0);
                Console.WriteLine("Connected!");
                return true;
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                {
                    Console.WriteLine("Still Connected, but the Send would block");
                    return true;
                }
                else
                {
                    Console.WriteLine("Disconnected: error code {0}!", e.NativeErrorCode);
                    return false;
                }
            }
            finally
            {
                Client.Blocking = blockingState;
            }
        }
        /// <summary>
        /// close conn
        /// </summary>
        public void CloseConnection()
        {
            if (this.Connected)
            {
                // 断开连接
                try
                {
                    var header = new FDFSHeader(0, Consts.FDFS_PROTO_CMD_QUIT, 0);
                    var buffer = header.ToByte();
                    this.GetStream().Write(buffer, 0, buffer.Length);
                    this.GetStream().Close();
                }
                catch
                {
                }
                // 关闭连接
                try
                {
                    this.Close();
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// release conn
        /// </summary>
        public void ReleaseConnection()
        {
            //Pool.ReleaseConnection(this);
            //Dispose(true);
            InUse = false;
        }
    }
}