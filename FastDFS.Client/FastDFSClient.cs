// https://github.com/smartbooks/FastDFS.Client
/*
 * 
 * 版本变更记录
 * 2019.01.23 -- 重构连接池管理、优化多线程并发管理
 * 
 * 
 */ 


using System;
using System.Net;
using System.Threading.Tasks;
using FastDFS.Client.Common;
using FastDFS.Client.Storage;
using FastDFS.Client.Tracker;

namespace FastDFS.Client
{
    /// <summary>
    /// FastDFSClient
    /// </summary>
    public class FastDFSClient
    {
        #region 公共静态方法

        /// <summary>
        /// 获取存储节点
        /// </summary>
        /// <param name="groupName">组名，如果没有组名由服务器自动分配</param>
        /// <returns>存储节点实体类</returns>
        public static StorageNode GetStorageNode(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                using (var trackerRequest = QUERY_STORE_WITHOUT_GROUP_ONE.CreateRequest())
                {
                    var trackerResponse = new QUERY_STORE_WITHOUT_GROUP_ONE.Response(trackerRequest.GetTrackerResponse());
                    var storeEndPoint = new IPEndPoint(IPAddress.Parse(trackerResponse.IpStr), trackerResponse.Port);
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => GetStorageNode(tracker = {trackerResponse.IpStr},store = {storeEndPoint.Address})");//log
                    var result = new StorageNode
                    {
                        GroupName = trackerResponse.GroupName,
                        EndPoint = storeEndPoint,
                        StorePathIndex = trackerResponse.StorePathIndex
                    };
                    return result;
                }
            }
            else
            {
                using (var trackerRequest = QUERY_STORE_WITH_GROUP_ONE.CreateRequest(groupName))
                {
                    var trackerResponse = new QUERY_STORE_WITH_GROUP_ONE.Response(trackerRequest.GetTrackerResponse());
                    var storeEndPoint = new IPEndPoint(IPAddress.Parse(trackerResponse.IpStr), trackerResponse.Port);
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => GetStorageNode(tracker = {trackerResponse.IpStr},store = {storeEndPoint.Address})");//log
                    var result = new StorageNode
                    {
                        GroupName = trackerResponse.GroupName,
                        EndPoint = storeEndPoint,
                        StorePathIndex = trackerResponse.StorePathIndex
                    };
                    return result;
                }
            }


        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="storageNode">GetStorageNode方法返回的存储节点</param>
        /// <param name="contentByte">文件内容</param>
        /// <param name="fileExt">文件扩展名(注意:不包含".")</param>
        /// <returns>文件名</returns>
        public static string UploadFile(StorageNode storageNode, byte[] contentByte, string fileExt)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => start upload fastdfs = > {storageNode.EndPoint.Address.ToString()} => contentByte.length= {contentByte.Length}");
            using (var storageReqeust = UPLOAD_FILE.CreateRequest(
                storageNode.EndPoint,
                storageNode.StorePathIndex,
                contentByte.Length,
                fileExt,
                contentByte))
            {
                var storageResponse = new UPLOAD_FILE.Response(storageReqeust.GetStorageResponse());
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => sucess upload fastdfs = > {storageNode.EndPoint.Address.ToString()} => {storageResponse.FileName}");
                return storageResponse.FileName;
            }
        }

        /// <summary>
        /// 上传从文件
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="contentByte">文件内容</param>
        /// <param name="masterFilename">主文件名</param>
        /// <param name="prefixName">从文件后缀</param>
        /// <param name="fileExt">文件扩展名(注意:不包含".")</param>
        /// <returns>文件名</returns>
        [Obsolete]
        public static string UploadSlaveFile(string groupName, byte[] contentByte, string masterFilename, string prefixName, string fileExt)
        {
            using (var updateFile = new QUERY_UPDATE())
            {
                var trackerRequest = updateFile.GetRequest(groupName, masterFilename);
                var trackerResponse = new QUERY_UPDATE.Response(trackerRequest.GetTrackerResponse());
                var storeEndPoint = new IPEndPoint(IPAddress.Parse(trackerResponse.IpStr), trackerResponse.Port);
                var storageReqeust = UPLOAD_SLAVE_FILE.Instance.GetRequest(storeEndPoint, contentByte.Length, masterFilename, prefixName, fileExt, contentByte);
                var storageResponse = new UPLOAD_FILE.Response(storageReqeust.GetStorageResponse());
                return storageResponse.FileName;
            }

        }

        /// <summary>
        /// 获取文件名称
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="groupFileName"></param>
        /// <returns></returns>
        public static string GetFileName(string groupName,string groupFileName)
        {
            string fileName = groupFileName;
            if (groupFileName.Substring(0, groupName.Length).ToLower() == groupName.ToLower())
            {
                fileName = groupFileName.Substring(groupName.Length+1);
            }
            return fileName;
        }

        /// <summary>
        /// 上传可以Append的文件
        /// </summary>
        /// <param name="storageNode">GetStorageNode方法返回的存储节点</param>
        /// <param name="contentByte">文件内容</param>
        /// <param name="fileExt">文件扩展名(注意:不包含".")</param>
        /// <returns>文件名</returns>
        [Obsolete]
        public static string UploadAppenderFile(StorageNode storageNode, byte[] contentByte, string fileExt)
        {
            var storageReqeust = UPLOAD_APPEND_FILE.Instance.GetRequest(storageNode.EndPoint, storageNode.StorePathIndex, contentByte.Length, fileExt, contentByte);

            var storageResponse = new UPLOAD_APPEND_FILE.Response(storageReqeust.GetStorageResponse());

            return storageResponse.FileName;
        }

        /// <summary>
        /// 附加文件
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <param name="fileName">文件名</param>
        /// <param name="contentByte">文件内容</param>
        [Obsolete]
        public static void AppendFile(string groupName, string fileName, byte[] contentByte)
        {
            fileName = GetFileName(groupName, fileName);
            using (var updateFile = new QUERY_UPDATE())
            {
                var trackerRequest = updateFile.GetRequest(groupName, fileName);
                var trackerResponse = new QUERY_UPDATE.Response(trackerRequest.GetTrackerResponse());
                var storeEndPoint = new IPEndPoint(IPAddress.Parse(trackerResponse.IpStr), trackerResponse.Port);
                var storageReqeust = APPEND_FILE.Instance.GetRequest(storeEndPoint, fileName, contentByte);
                storageReqeust.GetStorageResponse();
            }

        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <param name="fileName">文件名</param>
        public static void RemoveFile(string groupName, string fileName)
        {
            fileName = GetFileName(groupName, fileName);
            using (var trackerRequest = QUERY_UPDATE.CreateRequest(groupName, fileName))
            {
                var trackerResponse = new QUERY_UPDATE.Response(trackerRequest.GetTrackerResponse());
                var storeEndPoint = new IPEndPoint(IPAddress.Parse(trackerResponse.IpStr), trackerResponse.Port);
                using (var storageReqeust = DELETE_FILE.CreateRequest(storeEndPoint, groupName, fileName))
                {
                    byte[] responseByte = storageReqeust.GetStorageResponse();
                    string result = Util.ByteToString(responseByte).TrimEnd('\0');
                }
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="storageNode">GetStorageNode方法返回的存储节点</param>
        /// <param name="fileName">文件名</param>
        /// <exception cref="System.Exception">读取网络资源异常</exception>
        /// <returns>文件内容</returns>
        public static byte[] DownloadFile(StorageNode storageNode, string fileName)
        {
            fileName = GetFileName(storageNode.GroupName, fileName);
            using (var storageReqeust = DOWNLOAD_FILE.CreateRequest(storageNode.EndPoint, 0L, 0L, storageNode.GroupName, fileName))
            {
                // 同步下载
                var storageResponse = new DOWNLOAD_FILE.Response(storageReqeust.GetStorageResponse());
                if (storageResponse.Content == null)
                    throw new System.Exception(storageReqeust.Message);
                return storageResponse.Content;
            }
        }

        /// <summary>
        /// 异步下载
        /// </summary>
        /// <param name="storageNode"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<byte[]> DownloadFileAsync(StorageNode storageNode, string fileName)
        {
            fileName = GetFileName(storageNode.GroupName, fileName);

            using (var storageReqeust = DOWNLOAD_FILE.CreateRequest(storageNode.EndPoint, 0L, 0L, storageNode.GroupName, fileName))
            {
                Task<byte[]> task = null; // storageReqeust.ReadBytesAsync();
                await task;
                return task.Result;
            }
        }

        /// <summary>
        /// 增量下载文件
        /// </summary>
        /// <param name="storageNode">GetStorageNode方法返回的存储节点</param>
        /// <param name="fileName">文件名</param>
        /// <param name="offset">从文件起始点的偏移量</param>
        /// <param name="length">要读取的字节数</param>
        /// <returns>文件内容</returns>
        public static byte[] DownloadFile(StorageNode storageNode, string fileName, long offset, long length)
        {
            fileName = GetFileName(storageNode.GroupName, fileName);
            using (var storageReqeust = DOWNLOAD_FILE.CreateRequest(storageNode.EndPoint, offset, length, storageNode.GroupName, fileName))
            {
                var storageResponse = new DOWNLOAD_FILE.Response(storageReqeust.GetStorageResponse());
                return storageResponse.Content;
            }
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="storageNode">GetStorageNode方法返回的存储节点</param>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static FDFSFileInfo GetFileInfo(StorageNode storageNode, string fileName)
        {
            fileName = GetFileName(storageNode.GroupName, fileName);
            using (var storageReqeust = QUERY_FILE_INFO.CreateRequest(storageNode.EndPoint, storageNode.GroupName, fileName))
            {
                var result = new FDFSFileInfo(storageReqeust.GetStorageResponse());
                return result;
            }
        }

        #endregion
    }
}
