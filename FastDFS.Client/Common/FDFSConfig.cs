using System.Text;

namespace FastDFS.Client.Common
{
    /// <summary>
    /// fastDfs配置
    /// </summary>
    public class FDFSConfig
    {
        /// <summary>
        /// 存储连接数量
        /// </summary>
        public static int StorageMaxConnection = 5;
        /// <summary>
        /// max count of Tracker  Conn
        /// </summary>
        public static int TrackerMaxConnection = 5;
        /// <summary>
        /// 连接超时
        /// </summary>
        public static int ConnectionTimeout = 10; //Second
        /// <summary>
        /// 连接会话生命时间/秒
        /// </summary>
        public static int ConnectionLifeTime = 100; 
        /// <summary>
        /// 编码
        /// </summary>
        public static Encoding Charset = Encoding.UTF8;
    }
}