using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace FastDFS.Client.Common
{
    /// <summary>
    /// connect pool
    /// </summary>
    public class Pool
    {
        private const int MAX_CONNECTION_TIME = 1000 * 60;//连接超时设置
        private readonly List<Connection> _inUse;
        private Stack<Connection> _idle;
        private readonly AutoResetEvent _autoEvent;
        private readonly IPEndPoint _endPoint;
        private readonly int _maxConnection;
        private object lockInstance = new object();
        private List<Connection> _connections;//连接集合
        private DateTime lastRefreshTime = DateTime.Now;
        private string poolName;//连接池名称 tracker or storage

        /// <summary>
        /// enable
        /// </summary>
        public bool EnablePool { get; private set; } = false;
        private int ConnectionFailCount = 0;//连接失败次数

        /// <summary>
        /// disable time
        /// </summary>
        public DateTime DisableTime { get; private set; } = DateTime.MinValue;
        /// <summary>
        /// ip of end point
        /// </summary>
        public IPEndPoint IpEndPoint
        {
            get { return _endPoint; }
        }

        /// <summary>
        /// 连接池
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="maxConnection"></param>
        /// <param name="name"></param>
        public Pool(IPEndPoint endPoint, int maxConnection,string name)
        {
            _autoEvent = new AutoResetEvent(false);
            _inUse = new List<Connection>(maxConnection);
            _idle = new Stack<Connection>(maxConnection);
            _maxConnection = maxConnection;
            _connections = new List<Connection>(maxConnection);
            _endPoint = endPoint;
            EnablePool = true;
            poolName = name;
        }

        /// <summary>
        /// 禁用连接池
        /// </summary>
        public void Disable()
        {
            EnablePool = false;
            DisableTime = DateTime.Now;
        }

        /// <summary>
        /// 启用连接池
        /// </summary>
        public void Enable()
        {
            EnablePool = true;
            DisableTime = DateTime.MinValue;
        }

        /// <summary>
        /// get conn
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        private Connection GetPooldConncetion()
        {
            Connection result = null;
            lock ((_idle as ICollection).SyncRoot) //这部分代码无效
            {
                //Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => FastDFS.GetPooldConncetion({this.IpEndPoint.Address.ToString()}) => idle = {_idle.Count} "); //log
                if (_idle.Count > 0)
                    result = _idle.Pop();
                if (result != null && (int)(DateTime.Now - result.LastUseTime).TotalSeconds > FDFSConfig.ConnectionLifeTime)
                {
                    foreach (var conn in _idle)
                    {
                        conn.CloseConnection();
                    }
                    _idle = new Stack<Connection>(_maxConnection);
                    result = null;
                }
            }
            // 连接池管理每次都需要创建新的连接
            // 可以考虑共享连接池，但需要考虑服务器主动断开连接的情况
            lock ((_inUse as ICollection).SyncRoot)
            {
                if (_inUse.Count == _maxConnection)
                    return null;
                if (result == null)
                {
                    try
                    {
                        result = new Connection();
                        result.Connect(_endPoint);
                        result.Pool = this;
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => FastDFS.GetPooldConncetion({this.IpEndPoint.Address.ToString()}) add connection "); //log
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => FastDFS.GetPooldConncetion({this.IpEndPoint.Address.ToString()}) {ex.Message}"); //log
                        this.Disable();
                        return null;
                    }
                }
                _inUse.Add(result);
            }
            return result;
        }

        /// <summary>
        /// get conn
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public Connection GetConnection2()
        {
            var timeOut = FDFSConfig.ConnectionTimeout * 1000;
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => FastDFS.GetConnection({this.IpEndPoint.Address.ToString()}) ..."); //log

            var watch = Stopwatch.StartNew();
            while (timeOut > 0)
            {
                var result = GetPooldConncetion();
                if (result != null)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => FastDFS.GetConnection({this.IpEndPoint.Address.ToString()})  from pool"); //log
                    return result;
                }

                if (!_autoEvent.WaitOne(timeOut, false))
                    break;

                watch.Stop();

                timeOut = timeOut - (int)watch.ElapsedMilliseconds;
            }
            return null; 
            //throw new FDFSException("Connection Time Out");
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        /// <returns></returns>
        public Connection GetConnection()
        {
            Connection conn = null;
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => {_endPoint}-{poolName} connect {_connections.Count} client.");

            #region 连接超时检查
            if ((DateTime.Now - lastRefreshTime).TotalSeconds > FDFSConfig.ConnectionLifeTime)
            {
                lock (lockInstance) //清理超时连接
                {
                    List<Connection> removes = null;
                    foreach (Connection c in _connections)
                    {
                        if (c.Connected == false || (DateTime.Now - c.LastUseTime).TotalSeconds > FDFSConfig.ConnectionLifeTime)
                        {
                            if (removes == null) removes = new List<Connection>();
                            removes.Add(c);
                        }
                    }
                    if (removes != null)
                    {
                        for (int i = 0; i < removes.Count; i++)
                        {
                            Connection c = removes[i];
                            c.CloseConnection();//关闭连接
                            _connections.Remove(c);
                            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} =>{c.SessionId} remove from pool when connect out of time ");
                        }
                    }
                    lastRefreshTime = DateTime.Now;
                }
            }
            #endregion

            // 连接池中获取连接
            int time = MAX_CONNECTION_TIME;
            while (time > 0)
            {
                lock (lockInstance)
                {
                    conn = GetConnectionInPool();
                    // 如果连接池中无可用连接，则创建新的连接
                    if(conn==null)
                    if (_connections.Count < _maxConnection)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => add new conn {_endPoint} when no used conn");
                        conn = new Connection();
                        conn.Pool = this;
                        try
                        {
                            conn.Connect(this.IpEndPoint);
                            ConnectionFailCount = 0;
                            _connections.Add(conn);
                        }
                        catch (Exception ex)
                        {
                            ConnectionFailCount++;
                            conn = null;
                            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => FastDFS.GetPooldConncetion({this.IpEndPoint.Address.ToString()}) {ex.Message}"); //log
                        }
                        //conn.OpenConnection();
                    }
                    // 标记连接使用中
                    if (conn != null)
                    {
                        if (conn.Connected)
                        {
                            conn.OpenConnection();
                            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => cost {MAX_CONNECTION_TIME - time} when getting conn");
                            break;
                        }
                        else
                        {
                            _connections.Remove(conn);
                            conn.CloseConnection();
                            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} =>{conn.SessionId} remove from pool when connect out of time ");
                        }
                    }
                    if (ConnectionFailCount > 3)
                    {
                        this.Disable();
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => disable this conn({this.IpEndPoint.Address.ToString()}) after 3th try to connect");
                        break;
                    }
                }

                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} => cost {MAX_CONNECTION_TIME - time} when out of time");
                Thread.Sleep(1000);
                time = time - 1000;
            }
            return conn;
        }

        /// <summary>
        /// 随机获取空闲连接
        /// </summary>
        /// <returns></returns>
        private Connection GetConnectionInPool()
        {
            int i = 0;
            Connection conn = null;
            while (i < _connections.Count)
            {
                if (!_connections[i].InUse)
                {
                    conn = _connections[i];
                    if (conn.IsConnect())
                    {
                        break;
                    }
                    else
                    {
                        _connections.Remove(conn);
                        conn.CloseConnection();
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:fff")} =>{conn.SessionId} remove from pool when unabled to connect ");
                        i--;
                    }
                }
                i++;
            }
            return conn;
        }

        /// <summary>
        /// release conn
        /// </summary>
        /// <param name="conn"></param>
        [Obsolete]
        public void ReleaseConnection(Connection conn)
        {
            if (!conn.InUse)
            {
                var header = new FDFSHeader(0, Consts.FDFS_PROTO_CMD_QUIT, 0);
                var buffer = header.ToByte();
                conn.GetStream().Write(buffer, 0, buffer.Length);
                conn.GetStream().Close();
            }

            conn.Close();

            lock ((_inUse as ICollection).SyncRoot)
            {
                _inUse.Remove(conn);
            }
            _autoEvent.Set();
        }
        /// <summary>
        /// close conn
        /// </summary>
        /// <param name="conn"></param>
        [Obsolete]
        public void CloseConnection(Connection conn)
        {
            conn.InUse = false;
            lock ((_inUse as ICollection).SyncRoot)
            {
                _inUse.Remove(conn);
            }
            lock ((_idle as ICollection).SyncRoot)
            {
                _idle.Push(conn);
            }
            _autoEvent.Set();
        }
    }
}