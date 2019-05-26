using FastDFS.Client.Common;
using System;

namespace FastDFS.Client.Tracker
{
    /// <summary>
    /// query which storage server to store file
    /// 
    /// Reqeust 
    ///     Cmd: TRACKER_PROTO_CMD_SERVICE_QUERY_STORE_WITHOUT_GROUP_ONE 101
    ///     Body: 
    ///     
    /// Response
    ///     Cmd: TRACKER_PROTO_CMD_RESP
    ///     Status: 0 right other wrong
    ///     Body: 
    ///     @ FDFS_GROUP_NAME_MAX_LEN bytes: group name
    ///     @ IP_ADDRESS_SIZE - 1 bytes: storage server ip address
    ///     @ FDFS_PROTO_PKG_LEN_SIZE bytes: storage server port
    ///     @ 1 byte: store path index on the storage server
    /// </summary>
    public class QUERY_STORE_WITHOUT_GROUP_ONE : FDFSRequest
    {
        public static FDFSRequest CreateRequest()
        {


            var result = new QUERY_STORE_WITHOUT_GROUP_ONE();

            var body = new byte[0];
            result.Body = body;
            result.Header = new FDFSHeader(0, Consts.TRACKER_PROTO_CMD_SERVICE_QUERY_STORE_WITHOUT_GROUP_ONE, 0);
      
            return result;
        }

        public class Response
        {
            public string GroupName;
            public string IpStr;
            public int Port;
            public byte StorePathIndex;
            public Response(byte[] responseByte)
            {
                var groupNameBuffer = new byte[Consts.FDFS_GROUP_NAME_MAX_LEN];

                Array.Copy(responseByte, groupNameBuffer, Consts.FDFS_GROUP_NAME_MAX_LEN);

                GroupName = Util.ByteToString(groupNameBuffer).TrimEnd('\0');

                var ipAddressBuffer = new byte[Consts.IP_ADDRESS_SIZE - 1];

                Array.Copy(responseByte, Consts.FDFS_GROUP_NAME_MAX_LEN, ipAddressBuffer, 0, Consts.IP_ADDRESS_SIZE - 1);

                IpStr = new string(FDFSConfig.Charset.GetChars(ipAddressBuffer)).TrimEnd('\0');

                var portBuffer = new byte[Consts.FDFS_PROTO_PKG_LEN_SIZE];

                Array.Copy(responseByte, Consts.FDFS_GROUP_NAME_MAX_LEN + Consts.IP_ADDRESS_SIZE - 1, portBuffer, 0, Consts.FDFS_PROTO_PKG_LEN_SIZE);

                Port = (int)Util.BufferToLong(portBuffer, 0);

                StorePathIndex = responseByte[responseByte.Length - 1];
            }
        }
    }
}
