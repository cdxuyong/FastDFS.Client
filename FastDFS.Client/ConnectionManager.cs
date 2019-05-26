using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using FastDFS.Client.Common;
using FastDFS.Client.Config;

namespace FastDFS.Client
{
    /// <summary>
    /// 链接管理池
    /// </summary>
    public sealed class ConnectionManager
    {
        #region 私有字段

        private static List<IPEndPoint> _listTrackers = new List<IPEndPoint>(); // tracker list

        #endregion

        #region 公共静态字段
        /// <summary>
        /// trackers 
        /// </summary>
        public static Dictionary<IPEndPoint, Pool> TrackerPools = new Dictionary<IPEndPoint, Pool>();
        /// <summary>
        /// storages
        /// </summary>
        public static Dictionary<IPEndPoint, Pool> StorePools = new Dictionary<IPEndPoint, Pool>();

        #endregion

        #region 公共静态方法
        /// <summary>
        /// 初始化连接管理
        /// </summary>
        /// <param name="trackers">trackers</param>
        /// <returns></returns>
        public static bool Initialize(List<IPEndPoint> trackers)
        {
            foreach (var point in trackers)
            {
                if (!TrackerPools.ContainsKey(point))
                {
                    TrackerPools.Add(point, new Pool(point, FDFSConfig.TrackerMaxConnection, "tracker"));
                }
            }
            _listTrackers = trackers;
            return true;
        }
        /// <summary>
        /// 通过配置文件初始化
        /// </summary>
        /// <param name="config">配置信息</param>
        /// <returns></returns>
        public static bool InitializeForConfigSection(FastDfsConfig config)
        {
            if (config != null)
            {
                var trackers = new List<IPEndPoint>();

                foreach (var ipInfo in config.FastDfsServer)
                {
                    trackers.Add(new IPEndPoint(IPAddress.Parse(ipInfo.IpAddress), ipInfo.Port));
                }

                return Initialize(trackers);
            }

            return false;
        }

        /// <summary>
        /// 获取tracker连接
        /// </summary>
        /// <returns></returns>
        public static Connection GetTrackerConnection()
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => GetTrackerConnection()  end"); //log
            // 随机取一个随机数，循环获取连接池
            var index = new Random().Next(TrackerPools.Count);
            int i = TrackerPools.Count;
            while (i>0)
            {
                i--;
                var p = TrackerPools[_listTrackers[index]];
                index++;
                if (index == TrackerPools.Count) index = 0;
                if (!p.EnablePool)
                {
                    // 如果被禁用时间超过10分钟，则启用节点
                    if ((DateTime.Now - p.DisableTime).TotalMinutes > 10)
                    {
                        p.Enable();
                    }
                    else
                        continue;
                }
                var c = p.GetConnection();
                if (c != null) return c;

            }
            // 如果配置都被禁用，则强制启用所有节点
            if(_listTrackers.Count>0)
            foreach(var p in TrackerPools.Values)
            {
                p.Enable();
            }
            throw new Exception("Connection time out");
            //return null;
        }

        /// <summary>
        /// 获取存储连接
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static Connection GetStorageConnection(IPEndPoint endPoint)
        {

            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => GetStorageConnection({endPoint.Address.ToString()})  end"); //log

            lock ((StorePools as ICollection).SyncRoot)
            {
                if (!StorePools.ContainsKey(endPoint))
                {
                    StorePools.Add(endPoint, new Pool(endPoint, FDFSConfig.StorageMaxConnection,"storage"));
                }
            }

            Connection conn = StorePools[endPoint].GetConnection();
            return conn;
        }
        
        #endregion
    }
}