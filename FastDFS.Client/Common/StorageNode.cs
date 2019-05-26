using System.Net;

namespace FastDFS.Client.Common
{
    /// <summary>
    /// storage node 
    /// </summary>
    public class StorageNode
    {
        /// <summary>
        /// group name
        /// </summary>
        public string GroupName;
        /// <summary>
        /// end point
        /// </summary>
        public IPEndPoint EndPoint;
        /// <summary>
        /// index
        /// </summary>
        public byte StorePathIndex;
    }
}
