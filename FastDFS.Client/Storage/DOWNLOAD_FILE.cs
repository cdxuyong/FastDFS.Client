using System;
using System.Net;
using System.Threading.Tasks;
using FastDFS.Client.Common;

namespace FastDFS.Client.Storage
{
    /// <summary>
    ///     download/fetch file from storage server
    ///     Reqeust
    ///     Cmd: STORAGE_PROTO_CMD_DOWNLOAD_FILE 14
    ///     Body:
    ///     @ FDFS_PROTO_PKG_LEN_SIZE bytes: file offset
    ///     @ FDFS_PROTO_PKG_LEN_SIZE bytes: download file bytes
    ///     @ FDFS_GROUP_NAME_MAX_LEN bytes: group name
    ///     @ filename bytes: filename
    ///     Response
    ///     Cmd: STORAGE_PROTO_CMD_RESP
    ///     Status: 0 right other wrong
    ///     Body:
    ///     @ file content
    /// </summary>
    public class DOWNLOAD_FILE : FDFSRequest
    {
        public DOWNLOAD_FILE()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="paramList">
        ///     1,IPEndPoint    IPEndPoint-->the storage IPEndPoint
        ///     2,long offset-->file offset
        ///     3,long byteSize -->download file bytes
        ///     4,string groupName
        ///     5,string fileName
        /// </param>
        /// <returns></returns>
        public static FDFSRequest CreateRequest(params object[] paramList)
        {
            if (paramList.Length != 5)
                throw new FDFSException("param count is wrong");
            var endPoint = (IPEndPoint)paramList[0];
            var offset = (long)paramList[1];
            var byteSize = (long)paramList[2];
            var groupName = (string)paramList[3];
            var fileName = (string)paramList[4];

            var result = new DOWNLOAD_FILE {
                StorageConnection = ConnectionManager.GetStorageConnection(endPoint)
            };

            if (groupName.Length > Consts.FDFS_GROUP_NAME_MAX_LEN)
                throw new FDFSException("groupName is too long");

            long length = Consts.FDFS_PROTO_PKG_LEN_SIZE +
                          Consts.FDFS_PROTO_PKG_LEN_SIZE +
                          Consts.FDFS_GROUP_NAME_MAX_LEN +
                          fileName.Length;
            var bodyBuffer = new byte[length];
            byte[] offsetBuffer = Util.LongToBuffer(offset);
            byte[] byteSizeBuffer = Util.LongToBuffer(byteSize);
            byte[] groupNameBuffer = Util.StringToByte(groupName);
            byte[] fileNameBuffer = Util.StringToByte(fileName);
            Array.Copy(offsetBuffer, 0, bodyBuffer, 0, offsetBuffer.Length);
            Array.Copy(byteSizeBuffer, 0, bodyBuffer, Consts.FDFS_PROTO_PKG_LEN_SIZE, byteSizeBuffer.Length);
            Array.Copy(groupNameBuffer, 0, bodyBuffer, Consts.FDFS_PROTO_PKG_LEN_SIZE +
                                                       Consts.FDFS_PROTO_PKG_LEN_SIZE, groupNameBuffer.Length);
            Array.Copy(fileNameBuffer, 0, bodyBuffer, Consts.FDFS_PROTO_PKG_LEN_SIZE +
                                                      Consts.FDFS_PROTO_PKG_LEN_SIZE + Consts.FDFS_GROUP_NAME_MAX_LEN,
                fileNameBuffer.Length);

            result.Body = bodyBuffer;
            result.Header = new FDFSHeader(length, Consts.STORAGE_PROTO_CMD_DOWNLOAD_FILE, 0);
            return result;
        }

        /// <summary>
        /// 获取存储节点的响应
        /// </summary>
        /// <returns></returns>
        public override byte[] GetStorageResponse()
        {
            if (StorageConnection == null)
            {
                throw new Exception("");
            }
            this.Message = string.Empty;
            byte[] body = null;
            try
            {
                StorageConnection.OpenConnection();
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Console.WriteLine($"GetStorageResponse.OpenConnection => {ex.Message}");
                throw ex;
            }
            try
            {
                body = ReadStreamOnebyOne();
            }
            catch(Exception ex)
            {
                Message = ex.Message;
                Console.WriteLine($"GetStorageResponse.ReadStream => {ex.Message}");
            }
            return body;
        }

        // 逐个读取字节流
        private byte[] ReadStreamOnebyOne()
        {
            var stream = StorageConnection.GetStream();
            var headerBuffer = Header.ToByte();
            stream.Write(headerBuffer, 0, headerBuffer.Length);
            stream.Write(Body, 0, Body.Length);

            var header = new FDFSHeader(stream);
            if (header.Status != 0)
                throw new FDFSException(string.Format("Get Response Error,Error Code:{0}", header.Status));

            var body = new byte[header.Length];
            int i = 0;
            if (stream.CanRead)
            {
                do
                {
                    int byteData = stream.ReadByte();
                    body[i] = (byte)byteData;
                    i++;
                }
                while (stream.DataAvailable);
            }
            if (header.Length == i)
            {
                return body;
            }
            else
                return null;
        }

        public class Response
        {
            public byte[] Content;

            public Response(byte[] responseByte)
            {
                Content = responseByte;
            }
        }
    }
}