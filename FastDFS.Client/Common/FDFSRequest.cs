using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastDFS.Client.Common
{
    /// <summary>
    /// FastDFS请求
    /// </summary>
    public class FDFSRequest:IDisposable
    {
        #region 公共属性
        /// <summary>
        /// header
        /// </summary>
        public FDFSHeader Header { get; set; }
        /// <summary>
        /// body
        /// </summary>
        public byte[] Body { get; set; }
        /// <summary>
        /// tracker connection
        /// </summary>
        public Connection Connection { get; set; } 
        /// <summary>
        /// storage connection
        /// </summary>
        public Connection StorageConnection { get; set; }
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 最大尝试次数
        /// </summary>
        public const int MAX_TRY_COUNT = 5; 

        #endregion

        #region 公共方法
        /// <summary>
        /// 转化成Byte
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 公共虚方法
        /// <summary>
        /// 获取请求
        /// </summary>
        /// <param name="paramList"></param>
        /// <returns></returns>
        public virtual FDFSRequest GetRequest(params object[] paramList)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 读取tracker响应
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetTrackerResponse()
        {
            if (Connection == null)
            {
                Connection = ConnectionManager.GetTrackerConnection();
            }

            return GetResponse(Connection);
        }

        /// <summary>
        /// 获取存储节点响应
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetStorageResponse()
        {
            if (StorageConnection == null)
            {
                throw new Exception("");
            }
            return GetResponse(StorageConnection);
        }

        /// <summary>
        /// 异步获取存储节点信息
        /// </summary>
        /// <returns></returns>
        public virtual async Task<byte[]> GetStorageResponseAsync()
        {
            byte[] bytes = null;
            Task task = Task.Run(() =>
            {
                bytes = GetStorageResponse();
            });
            await task;
            return bytes;
        }
        /// <summary>
        /// 获取响应
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private byte[] GetResponse(Connection connection)
        {
            try
            {
                connection.OpenConnection();
                var stream = connection.GetStream();
                var headerBuffer = Header.ToByte();
                stream.Write(headerBuffer, 0, headerBuffer.Length);
                stream.Write(Body, 0, Body.Length);

                var header = new FDFSHeader(stream);
                if (header.Status != 0)
                    throw new FDFSException(string.Format("Get Response Error,Error Code:{0}", header.Status));

                var body = new byte[header.Length];
                if (header.Length != 0)
                {
                    Task<int> task = stream.ReadAsync(body, 0, (int)header.Length);
                    task.Wait();
                    if (task.Result == 0)
                    {
                        body = null;
                    }
                }
                return body;
            }
            catch (Exception ex)
            {
                //connection.Close();
                throw ex;
            }
            finally
            {
            }
        }
   

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    if (Connection != null)
                    {
                        Connection.ReleaseConnection();
                    }
                    if (StorageConnection != null)
                    {
                        StorageConnection.ReleaseConnection();
                    }
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~FDFSRequest() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            if(Connection!=null && Connection.Pool!=null)
                Console.WriteLine($"disposed tracker {Connection.SessionId} conn {Connection.Pool.IpEndPoint.ToString()}");
            if(StorageConnection!=null && StorageConnection.Pool!=null)
                Console.WriteLine($"disposed storage {StorageConnection.SessionId} conn {StorageConnection.Pool.IpEndPoint.ToString()}");
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

        #endregion


    }
}