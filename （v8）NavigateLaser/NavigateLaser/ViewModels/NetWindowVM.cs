using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Practices.Prism.Commands;
using NavigateLaser.Models;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using NavigateLaser.DataAccess;


namespace NavigateLaser.ViewModels
{
    class NetWindowVM : NotificationObject
    {
        SendProtocolData m_SendProtocolData = new SendProtocolData();
        public DelegateCommand ConnectLaserCommand { get; set; }
        public DelegateCommand SendToLaserCommand { get; set; }
        public DelegateCommand SearchLaserUDPCommand { get; set; }
        protected IEventAggregator eventAggregatorEx;
        protected SubscriptionToken token;
        protected IEventAggregator eventAggregatorOperationComplete;
        protected SubscriptionToken tokenOperationComplete;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;
        protected IEventAggregator eventAggregatorContinuousFrame;//切换到连续获取

        protected IEventAggregator eventAggregator3DShowP;
        protected IEventAggregator eventAggregatorSendSucceedP;
        protected IEventAggregator eventAggregatorReceiveP;
        protected IEventAggregator eventAggregatorMappingRecv;
        protected IEventAggregator eventAggregatorBasicRecv;
        protected IEventAggregator eventAggregatorProductionRecv;
        protected IEventAggregator eventAggregatorMotorRecv;
        protected IEventAggregator eventAggregatorSICKRecv;
        protected IEventAggregator eventAggregatorLMRecv;
        protected IEventAggregator eventAggregatorMappingMode;
        protected IEventAggregator eventAggregatorWorkModeView;
        protected IEventAggregator eventAggregatorNaviQuery;
        protected IEventAggregator eventAggregatorDisnetInit;
        protected IEventAggregator eventAggregatorQueryHeartState;

        int CurrentWorkMode = 0;   //当前设置的工作模式 1:靶标探测  2：mapping 3：导航
        System.Threading.Timer timer;
        System.Threading.Timer timer_BasicParam;
        System.Threading.Timer timer_HeartBeat;
        int HeartBeatCount = 1;  //接收心跳统计，用于重连
        private NetWindowM m_NetWindowM;
        public NetWindowM NetWindowM
        {
            get { return m_NetWindowM; }
            set
            {
                m_NetWindowM = value;
                this.RaisePropertyChanged("NetWindowM");
            }
        }

        public NetWindowVM()
        {
            this.LoadNetWindowM();
            this.ConnectLaserCommand = new DelegateCommand(new Action(this.ConnectLaserCommandExecute));
            this.SendToLaserCommand = new DelegateCommand(new Action(this.SendToLaserCommandExecute));
            this.SearchLaserUDPCommand = new DelegateCommand(new Action(this.SearchLaserUDPCommandExecute));
            eventAggregatorEx = ServiceLocator.Current.GetInstance<IEventAggregator>();
            token = eventAggregatorEx.GetEvent<TCPSendDataEvent>().Subscribe(SendToLaserCommandEvent);
            eventAggregatorOperationComplete = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenOperationComplete = eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Subscribe(OperationCompleteCommandEvent);
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);

            this.eventAggregator3DShowP = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorSendSucceedP = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorReceiveP = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorMappingRecv = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorBasicRecv = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorProductionRecv = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorMotorRecv = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorSICKRecv = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorLMRecv = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorMappingMode = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorWorkModeView = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorNaviQuery = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorDisnetInit = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorQueryHeartState = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorContinuousFrame = ServiceLocator.Current.GetInstance<IEventAggregator>();
            timer_HeartBeat = new System.Threading.Timer(new System.Threading.TimerCallback(HeartBeatDeal), null, 0, 5000);
        }
        private void LoadNetWindowM()
        {
            this.NetWindowM = new NetWindowM();
            this.NetWindowM.LaserIP = "192.168.0.2";
            this.NetWindowM.LaserPort = "2110";
            this.NetWindowM.BtConnectTXT = "连接";
            this.NetWindowM.LaserRecData = "";
            this.NetWindowM.LaserSendData = "";
            this.NetWindowM.IsBtSearchEnable = true;
            this.NetWindowM.IsEnable = true;
            this.NetWindowM.BtConnectIsEnable = true;
        }

        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.NetWindowM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }

        #region TCP网络功能
        static public bool IsSocketConnected(Socket s)
        {
            try
            {
                return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        public Socket sckClient;            //客户端套接字
        int countcs = 0;
        int cs1 = 0;
        int cs2 = 0;
        bool f_ConnectedInfoShow = true;//连接成功是否显示标志位 
        private void ConnectLaserCommandExecute()
        {
            this.NetWindowM.BtConnectIsEnable = false;
            this.NetWindowM.IsBtSearchEnable = false;
            if (this.NetWindowM.BtConnectTXT == "连接")
            {
                //不能为空
                if (this.NetWindowM.LaserIP.Equals(""))
                {
                    ShowMsg("连接失败，激光器IP不能为空！");
                    this.NetWindowM.BtConnectIsEnable = true;
                    this.NetWindowM.IsBtSearchEnable = true;
                    return;
                }
                if (this.NetWindowM.LaserPort.Equals(""))
                {
                    ShowMsg("连接失败，激光器端口不能为空！");
                    this.NetWindowM.BtConnectIsEnable = true;
                    this.NetWindowM.IsBtSearchEnable = true;
                    return;
                }

                //动态IP 端口
                IPAddress myIp;
                IPEndPoint iep;
                try
                {
                    myIp = IPAddress.Parse(this.NetWindowM.LaserIP);
                    iep = new IPEndPoint(myIp, Convert.ToInt32(this.NetWindowM.LaserPort));
                }
                catch (System.Exception ex)
                {
                    ShowMsg("连接失败，激光器IP或者端口填写格式错误！");
                    this.NetWindowM.BtConnectIsEnable = true;
                    this.NetWindowM.IsBtSearchEnable = true;
                    return;
                }



                //创建客户端套接字
                //连接保护
                if (null == sckClient)
                {
                    try
                    {
                        sckClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    catch (System.Exception ex)
                    {
                        ShowMsg("绑定IP端口失败！");
                        this.NetWindowM.BtConnectIsEnable = true;
                        this.NetWindowM.IsBtSearchEnable = true;
                        return;
                    }
                }
                else
                {
                    if (IsSocketConnected(sckClient) == false)//服务器断线保护
                    {
                        try
                        {
                            sckClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        }
                        catch (System.Exception ex)
                        {
                            ShowMsg("绑定IP端口失败！");
                            this.NetWindowM.BtConnectIsEnable = true;
                            this.NetWindowM.IsBtSearchEnable = true;
                            return;
                        }
                    }
                    else
                    {
                        //ShowMsg("连接已经建立，请勿重复操作！");
                        this.NetWindowM.BtConnectIsEnable = true;
                        this.NetWindowM.IsBtSearchEnable = true;
                        return;
                    }

                }

                //连接服务器
                try
                {
                    sckClient.BeginConnect(iep, new AsyncCallback(ConnectCallback), sckClient);
                }
                catch (System.Exception ex)
                {
                    ShowMsg("连接激光器失败！");
                    this.NetWindowM.BtConnectIsEnable = true;
                    this.NetWindowM.IsBtSearchEnable = true;
                    return;
                }
            }
            else
            {
                f_heartTimer = false;
                n_connectLaser = 0;
                TCPConnect_Close(null, null);
            }
        }

        private void TCPConnect_Close(object sender, EventArgs e)
        {
            if (this.NetWindowM.BtConnectTXT == "断开")
            {
                try
                {
                    sckClient.Close();
                }
                catch (System.Exception ex)
                {
                    return;
                }
                this.NetWindowM.BtConnectTXT = "连接";
                DateTime dt = DateTime.Now;
                string temptime = dt.ToString("HH:mm:ss.fff" + ":");
                ShowMsg(temptime+"断开连接成功！");
                DisnetInitContent m_DisnetInitContent = new DisnetInitContent();
                this.eventAggregatorDisnetInit.GetEvent<DisnetInitEvent>().Publish(m_DisnetInitContent);
                this.NetWindowM.BtConnectIsEnable = true;
                this.NetWindowM.IsBtSearchEnable = true;
            }
        }
        int n_laserConnected = 0;//已连接上激光器次数
        private void ConnectCallback(IAsyncResult ar)
        {
            DateTime dt = DateTime.Now;
            string temptime = dt.ToString("HH:mm:ss.fff" + ":");
            Socket sckConnect = (Socket)ar.AsyncState;
            try
            {
                sckConnect.EndConnect(ar);
            }
            catch (System.Exception ex)
            {
                ShowMsg(temptime + "连接失败，请确定服务器打开，且IP端口正确！");
                this.NetWindowM.BtConnectIsEnable = true;
                this.NetWindowM.IsBtSearchEnable = true;
                return;
            }
            StateObject state = new StateObject();
            state.sckReceive = sckClient;
            try
            {
                sckClient.BeginReceive(state.ReceiveBuf, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback2), state);
            }
            catch (System.Exception ex)
            {
                this.NetWindowM.BtConnectIsEnable = true;
                return;
            }
            f_heartTimer = true;
            if (f_ConnectedInfoShow)
            {
                this.NetWindowM.BtConnectTXT = "断开";
                ShowMsg(temptime + "连接成功！");
            }
            else
            {
                f_ConnectedInfoShow = true;
            }
            if (n_laserConnected == 0)
            {
                timer_BasicParam = new System.Threading.Timer(new System.Threading.TimerCallback(SendBasicParamTimer), null, 0, 1000);
                Thread.Sleep(300);
                sckClient.Send(m_SendProtocolData.GetBasicParamData, m_SendProtocolData.GetBasicParamData.Length, 0);
                Thread.Sleep(300);
                sckClient.Send(m_SendProtocolData.GetVersionsData, m_SendProtocolData.GetVersionsData.Length, 0);
                Thread.Sleep(300);
                sckClient.Send(m_SendProtocolData.GetNetParamData, m_SendProtocolData.GetNetParamData.Length, 0);
                Thread.Sleep(300);
            }
            n_laserConnected++;
            WorkModeViewContent m_WorkModeViewContent = new WorkModeViewContent();
            m_WorkModeViewContent.WorkModeViewName = "连接后空模式";
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.eventAggregatorWorkModeView.GetEvent<WorkModeViewEvent>().Publish(m_WorkModeViewContent);
            }));
            Thread.Sleep(100);
            this.NetWindowM.IsBtSearchEnable = false;
            this.NetWindowM.BtConnectIsEnable = true;
        }
        private void SendBasicParamTimer(object O)
        {
            if (IsSocketConnected(sckClient) == true)
            {
                sckClient.Send(m_SendProtocolData.GetRunStateData, m_SendProtocolData.GetRunStateData.Length, 0);
            }
        }
        public static string heartBeatState = "";
        int n_connectLaser = 0;//重连次数
        bool f_heartTimer = true;//心跳定时器中是否执行标志位
        private void HeartBeatDeal(object O)
        {
            if (f_heartTimer)
            {
                DateTime dt = DateTime.Now;
                string temptime = dt.ToString("HH:mm:ss.fff" + ":");
                this.eventAggregatorQueryHeartState.GetEvent<QueryHeartStateEvent>().Publish("");
                if (heartBeatState == "开启")
                {
                    if (HeartBeatCount > 0)
                    {
                        HeartBeatCount = 0;
                    }
                    else
                    {
                        n_connectLaser++;
                        if (n_connectLaser >1)
                        {
                            if (n_connectLaser == 2)
                            {
                                try
                                {
                                    sckClient.Close();
                                }
                                catch (System.Exception ex)
                                {
                                    return;
                                }
                                ShowMsg(temptime + "网络已断开连接!");
                            }
                                n_runStateRespond = 0;
                                this.NetWindowM.BtConnectTXT = "连接";
                                ShowMsg(temptime + "第" + (n_connectLaser - 1).ToString() + "次网络重新连接!");
                                f_ConnectedInfoShow = false;
                                ConnectLaserCommandExecute();
                                this.NetWindowM.BtConnectTXT = "断开";
                        }
                    }
                }
            }
        }
        int n_runStateRespond=0;//运行状态回复次数
        private void ReceiveCallback2(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket sckReceive = state.sckReceive;

            if (IsSocketConnected(sckClient) == false)//服务器断线保护 
            {

                if (this.NetWindowM.BtConnectTXT == "断开")
                {
                    //TCPConnect_Close(null, null);
                }

                return;
            }

            int revLength = 0;
            try
            {
                revLength = sckReceive.EndReceive(ar);
            }
            catch (System.Exception ex)
            {
                return;
            }

            byte[] ReceiveBuf = new byte[revLength * 2];

            for (int k = 0; k < revLength; k++)
            {
                ReceiveBuf[k] = state.ReceiveBuf[k];
            }

            DateTime dt = DateTime.Now;
            string temptime = dt.ToString("HH:mm:ss.fff");
            if (revLength > 8)//长度至少8字节才进解析
            {
                int temp_length = 0;
                int temp_head, temp_tail;
                int frame_length;

                temp_length = revLength;
                temp_head = 0;
                temp_tail = temp_length - 1;

                while ((temp_length != 0) && (temp_head != temp_tail))
                {
                    if (((byte)ReceiveBuf[temp_head] == 0x02) && ((byte)ReceiveBuf[(temp_head + 1)] == 0x02) && ((byte)ReceiveBuf[(temp_head + 2)] == 0x02) && ((byte)ReceiveBuf[(temp_head + 3)] == 0x02))	//判断02 02 02 02包帧头
                    {
                        if (temp_length >= 34)
                        {
                            frame_length = (((byte)ReceiveBuf[temp_head + 4] & 0xff) << 24) + (((byte)ReceiveBuf[temp_head + 5] & 0xff) << 16) + (((byte)ReceiveBuf[temp_head + 6] & 0xff) << 8) + ((byte)ReceiveBuf[temp_head + 7]);
                            //激光分类解析（帧格式不同）
                            if ((temp_length >= frame_length + 9))
                            {
                                byte[] tempData = ReceiveBuf.Skip(temp_head).Take(frame_length + 9).ToArray();
                                byte BccBuf = Add_BCC(tempData, frame_length + 9);
                                if (BccBuf == ReceiveBuf[temp_head + frame_length + 8])
                                {
                                    if (ReceiveBuf[temp_head + 9] == 0x52 || ReceiveBuf[temp_head + 9] == 0x53)
                                    {
                                        int id = Thread.CurrentThread.ManagedThreadId;
                                        ScanDataContent m_ScanDataContent = new ScanDataContent();
                                        m_ScanDataContent.ScanContent = tempData.ToList();
                                        this.eventAggregator3DShowP.GetEvent<ScanDataShowEvent>().Publish(m_ScanDataContent);
                                        temp_head = temp_head + frame_length + 9;
                                        temp_length = temp_length - frame_length - 9;
                                    }
                                    else
                                    {
                                        temp_head = temp_head + 1;
                                        if (temp_head == temp_tail + 1)
                                        {
                                            break;
                                        }
                                        temp_length--;
                                    }
                                }
                                else
                                {
                                    temp_head = temp_head + 1;
                                    if (temp_head == temp_tail + 1)
                                    {
                                        break;
                                    }
                                    temp_length--;
                                }
                            }
                            else
                            {
                                temp_head = temp_head + 1;
                                if (temp_head == temp_tail + 1)
                                {
                                    break;
                                }
                                temp_length--;
                            }
                        }
                        else
                        {
                            temp_head = temp_head + 1;
                            if (temp_head == temp_tail + 1)
                            {
                                break;
                            }
                            temp_length--;
                        }
                    }
                    else if (((byte)ReceiveBuf[temp_head] == 0xff) && ((byte)ReceiveBuf[(temp_head + 1)] == 0xaa))	//判断FFAA包帧头
                    {
                        if (temp_length >= 34)
                        {
                            frame_length = (((byte)ReceiveBuf[temp_head + 2] & 0xff) << 8) + ((byte)ReceiveBuf[temp_head + 3]);
                            if ((temp_length >= frame_length + 4))
                            {
                                if ((byte)ReceiveBuf[temp_head + frame_length + 2] == 0xee && (byte)ReceiveBuf[temp_head + frame_length + 3] == 0xee)	//判断帧尾
                                {
                                    byte[] tempData = ReceiveBuf.Skip(temp_head).Take(frame_length + 4).ToArray();
                                    byte BccBuf = Add_BCC(tempData, frame_length);
                                    if (BccBuf == ReceiveBuf[temp_head + frame_length + 1])
                                    {
                                        if (ReceiveBuf[temp_head + 22] == 0x02 && ReceiveBuf[temp_head + 11] == 0x02)
                                        {
                                            if (ReceiveBuf[temp_head + 23] == 0x01 || (ReceiveBuf[temp_head + 23] == 0x02))
                                            {
                                                ScanDataContent m_ScanDataContent = new ScanDataContent();
                                                m_ScanDataContent.ScanContent = tempData.ToList();
                                                if (CurrentWorkMode == 4)
                                                {
                                                    if (revLength > 77)
                                                    {
                                                        if (ReceiveBuf[temp_head + 77] == 0x05)//pengl
                                                        {
                                                            this.eventAggregator3DShowP.GetEvent<ProScanDataShowEvent>().Publish(m_ScanDataContent);
                                                        }
                                                        else if (ReceiveBuf[temp_head + 77] == 0)//pl 
                                                        {
                                                            CurrentWorkMode = 1;
                                                            sckClient.Send(m_SendProtocolData.LMDetect, m_SendProtocolData.LMDetect.Length, 0);//切换到靶标探测模式
                                                            this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("靶标探测");
                                                        }
                                                        else if (ReceiveBuf[temp_head + 77] == 0x01)//pl 导航
                                                        {
                                                            //CurrentWorkMode = 3;
                                                            //int SendBuffLength = sckClient.Send(m_SendProtocolData.NavigZL, m_SendProtocolData.NavigZL.Length, 0);//切换到导航模式
                                                            //if (SendBuffLength > 0 && (IsSocketConnected(sckClient) == true))
                                                            //{
                                                            //    NaviQueryContent m_NaviQueryContent = new NaviQueryContent();
                                                            //    m_NaviQueryContent.NaviQueryName = "导航查询靶标初始化";
                                                            //    m_NaviQueryContent.LayerID = 0;
                                                            //    this.eventAggregatorNaviQuery.GetEvent<NaviQueryEvent>().Publish(m_NaviQueryContent);
                                                            //}
                                                            //this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("导航");
                                                            CurrentWorkMode = 3;
                                                            sckClient.Send(m_SendProtocolData.NavigZL, m_SendProtocolData.NavigZL.Length, 0);//切换到导航模式
                                                            this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("导航");
                                                        }
                                                    }
                                                }
                                                else if (CurrentWorkMode == 1)//靶标探测
                                                {
                                                    if (revLength > 77)
                                                    {
                                                        if (ReceiveBuf[temp_head + 77] == 0)
                                                        {
                                                            this.eventAggregator3DShowP.GetEvent<ScanDataShowEvent>().Publish(m_ScanDataContent);
                                                        }
                                                        else if (ReceiveBuf[temp_head + 77] == 0x01)
                                                        {
                                                            CurrentWorkMode = 3;
                                                            sckClient.Send(m_SendProtocolData.NavigZL, m_SendProtocolData.NavigZL.Length, 0);//切换到导航模式
                                                            this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("导航");
                                                        }
                                                        else if (ReceiveBuf[temp_head + 77] == 0x05)
                                                        {
                                                            CurrentWorkMode = 4;
                                                            sckClient.Send(m_SendProtocolData.QueryMotorPara, m_SendProtocolData.QueryMotorPara.Length, 0);//切换到距离探测模式
                                                            this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("距离探测");
                                                        }
                                                    }
                                                }
                                                else if (CurrentWorkMode == 3)
                                                {
                                                    if (revLength > 77)
                                                    {
                                                        if (ReceiveBuf[temp_head + 77] == 0x01)
                                                        {
                                                            this.eventAggregator3DShowP.GetEvent<ScanDataShowEvent>().Publish(m_ScanDataContent);
                                                        }
                                                        else if (ReceiveBuf[temp_head + 77] == 0)
                                                        {
                                                            CurrentWorkMode = 1;
                                                            sckClient.Send(m_SendProtocolData.LMDetect, m_SendProtocolData.LMDetect.Length, 0);//切换到距离探测模式
                                                            this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("靶标探测");
                                                        }
                                                        else if (ReceiveBuf[temp_head + 77] == 0x05)
                                                        {
                                                            CurrentWorkMode = 4;
                                                            sckClient.Send(m_SendProtocolData.QueryMotorPara, m_SendProtocolData.QueryMotorPara.Length, 0);//切换到距离探测模式
                                                            this.eventAggregatorNaviQuery.GetEvent<BtnColorChangeEvent>().Publish("距离探测");
                                                        }
                                                    }
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;

                                            }
                                            else
                                            {
                                                temp_head = temp_head + 1;
                                                if (temp_head == temp_tail + 1)
                                                {
                                                    break;
                                                }
                                                temp_length--;
                                            }
                                        }
                                        else if (ReceiveBuf[temp_head + 22] == 0x06 && ReceiveBuf[temp_head + 11] == 0x02)
                                        {
                                            if (ReceiveBuf[temp_head + 23] == 0x01)
                                            {
                                                BasicDataContent m_BasicDataContent = new BasicDataContent();
                                                m_BasicDataContent.ReceiveCommandName = "获得基本参数";
                                                m_BasicDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorBasicRecv.GetEvent<BasicDataShowEvent>().Publish(m_BasicDataContent);
                                                ShowMsg(temptime + ": 接收到基本参数！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x02)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 基本参数设置成功！");
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x03)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    LandmarkResponseContent m_LandmarkResponseContent = new LandmarkResponseContent();
                                                    m_LandmarkResponseContent.LandmarkResponseName = "靶标设置回复";
                                                    m_LandmarkResponseContent.LandmarkResponseData = tempData.ToList();
                                                    this.eventAggregatorLMRecv.GetEvent<LandmarkResponseEvent>().Publish(m_LandmarkResponseContent);
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x04)
                                            {

                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    WorkModeViewContent m_WorkModeViewContent = new WorkModeViewContent();
                                                    if (CurrentWorkMode == 1)
                                                    {
                                                        m_WorkModeViewContent.WorkModeViewName = "靶标探测模式";
                                                    }
                                                    else if (CurrentWorkMode == 2)
                                                    {
                                                        m_WorkModeViewContent.WorkModeViewName = "靶标获取模式";
                                                    }
                                                    else
                                                    {
                                                        m_WorkModeViewContent.WorkModeViewName = "导航模式";
                                                        NaviQueryContent m_NaviQueryContent = new NaviQueryContent();
                                                        m_NaviQueryContent.NaviQueryName = "导航查询靶标开始";
                                                        this.eventAggregatorNaviQuery.GetEvent<NaviQueryEvent>().Publish(m_NaviQueryContent);
                                                    }
                                                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                                                    {
                                                        this.eventAggregatorWorkModeView.GetEvent<WorkModeViewEvent>().Publish(m_WorkModeViewContent);
                                                    }));
                                                }



                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x05)
                                            {
                                                MappingDataContent m_MappingDataContent = new MappingDataContent();
                                                m_MappingDataContent.ReceiveCommandName = "获得Mapping数据";
                                                m_MappingDataContent.MappingContent = tempData.ToList();
                                                this.eventAggregatorMappingRecv.GetEvent<MappingDataShowEvent>().Publish(m_MappingDataContent);
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x06)
                                            {
                                                LandmarkResponseContent m_LandmarkResponseContent = new LandmarkResponseContent();
                                                m_LandmarkResponseContent.LandmarkResponseName = "靶标查询回复";
                                                m_LandmarkResponseContent.LandmarkResponseData = tempData.ToList();
                                                this.eventAggregatorLMRecv.GetEvent<LandmarkResponseEvent>().Publish(m_LandmarkResponseContent);


                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x0F)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 靶标识别阈值设置成功！");
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x10)
                                            {
                                                FunctionDataContent m_FunctionDataContent = new FunctionDataContent();
                                                m_FunctionDataContent.ReceiveCommandName = "识别阈值查询回复";
                                                m_FunctionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorLMRecv.GetEvent<FunctionDataShowEvent>().Publish(m_FunctionDataContent);
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x11)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 靶标匹配范围设置成功！");
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x12)
                                            {
                                                FunctionDataContent m_FunctionDataContent = new FunctionDataContent();
                                                m_FunctionDataContent.ReceiveCommandName = "匹配范围查询回复";
                                                m_FunctionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorLMRecv.GetEvent<FunctionDataShowEvent>().Publish(m_FunctionDataContent);
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x13)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 靶标扫描范围设置成功！");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x14)
                                            {
                                                FunctionDataContent m_FunctionDataContent = new FunctionDataContent();
                                                m_FunctionDataContent.ReceiveCommandName = "扫描范围查询回复";
                                                m_FunctionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorLMRecv.GetEvent<FunctionDataShowEvent>().Publish(m_FunctionDataContent);
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x15)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 两靶标定位功能设置成功！");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x16)
                                            {
                                                FunctionDataContent m_FunctionDataContent = new FunctionDataContent();
                                                m_FunctionDataContent.ReceiveCommandName = "2靶标定位功能查询回复";
                                                m_FunctionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorLMRecv.GetEvent<FunctionDataShowEvent>().Publish(m_FunctionDataContent);
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x18)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 一键恢复出厂设置成功！");
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x1A)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 网络参数设置成功！");
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else
                                            {
                                                temp_head = temp_head + 1;
                                                if (temp_head == temp_tail + 1)
                                                {
                                                    break;
                                                }
                                                temp_length--;
                                            }
                                        }
                                        else if (ReceiveBuf[temp_head + 22] == 0x05 && ReceiveBuf[temp_head + 11] == 0x02)
                                        {
                                            if (ReceiveBuf[temp_head + 23] == 0x01)
                                            {
                                                ProductionDataContent m_ProductionDataContent = new ProductionDataContent();
                                                m_ProductionDataContent.ReceiveCommandName = "获得状态检测";
                                                m_ProductionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorProductionRecv.GetEvent<ProductionDataShowEvent>().Publish(m_ProductionDataContent);
                                                ShowMsg(temptime + ": 接收到状态检测！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x02)
                                            {
                                                ProductionDataContent m_ProductionDataContent = new ProductionDataContent();
                                                m_ProductionDataContent.ReceiveCommandName = "获得网络参数";
                                                m_ProductionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorProductionRecv.GetEvent<ProductionDataShowEvent>().Publish(m_ProductionDataContent);
                                                ShowMsg(temptime + ": 接收到网络参数！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x03)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 网络参数设置成功！");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x04)
                                            {
                                                ProductionDataContent m_ProductionDataContent = new ProductionDataContent();
                                                m_ProductionDataContent.ReceiveCommandName = "获得APD参数";
                                                m_ProductionDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorProductionRecv.GetEvent<ProductionDataShowEvent>().Publish(m_ProductionDataContent);
                                                ShowMsg(temptime + ": 接收到APD参数！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x05)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": APD参数设置成功！");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x06)
                                            {
                                                MotorDataContent m_MotorDataContent = new MotorDataContent();
                                                m_MotorDataContent.ReceiveCommandName = "获得电机参数";
                                                m_MotorDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorMotorRecv.GetEvent<MotorDataShowEvent>().Publish(m_MotorDataContent);
                                                ShowMsg(temptime + ": 接收到电机参数！");
                                                if (PCLViewVM.f_SetWorkModeScan)
                                                {
                                                    if (m_MotorDataContent.ReceiveContent[31] == 100)//开启偏心修正
                                                    {
                                                        m_SendProtocolData.WorkModeProScan[31] = 100;

                                                    }
                                                    else//关闭偏心修正
                                                    {
                                                        m_SendProtocolData.WorkModeProScan[31] = 0;
                                                    }
                                                    int pointRatio = (m_MotorDataContent.ReceiveContent[32] << 8) + m_MotorDataContent.ReceiveContent[33];
                                                    m_SendProtocolData.WorkModeProScan[32] = (byte)(pointRatio >> 8);
                                                    m_SendProtocolData.WorkModeProScan[33] = (byte)(pointRatio & 0xff);
                                                    m_SendProtocolData.WorkModeProScan[37] = Add_BCC(m_SendProtocolData.WorkModeProScan, 36);
                                                    m_SendProtocolData.WorkModeProScan[38] = 0xEE;
                                                    m_SendProtocolData.WorkModeProScan[39] = 0xEE;
                                                    sckClient.Send(m_SendProtocolData.WorkModeProScan, m_SendProtocolData.WorkModeProScan.Length, 0);
                                                    PCLViewVM.f_SetWorkModeScan = false;
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x07)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    WorkModeViewContent m_WorkModeViewContent = new WorkModeViewContent();
                                                    if (CurrentWorkMode == 4)
                                                    {
                                                        m_WorkModeViewContent.WorkModeViewName = "生产扫描模式";
                                                        System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                                                        {
                                                            this.eventAggregatorWorkModeView.GetEvent<WorkModeViewEvent>().Publish(m_WorkModeViewContent);
                                                        }));
                                                    }
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x09)
                                            {
                                                MotorDataContent m_MotorDataContent = new MotorDataContent();
                                                m_MotorDataContent.ReceiveCommandName = "获得电机转速";
                                                m_MotorDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorMotorRecv.GetEvent<MotorDataShowEvent>().Publish(m_MotorDataContent);
                                                ShowMsg(temptime + ": 接收到电机转速！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x0E)//修正表查询回复
                                            {
                                                if (ReceiveBuf[temp_head + 26] == 0x01)//有正确修正表
                                                {
                                                    string strContent = string.Empty;
                                                    for (int i = 0; i < 20; i += 2)
                                                    {
                                                        int tempval = (Int16)(ReceiveBuf[temp_head + 29 + i] << 8) + ReceiveBuf[temp_head + 29 + i + 1];
                                                        strContent += tempval.ToString() + "  ";
                                                    }
                                                    strContent = "收到回复：有修正表，前10个数据如下：\n" + strContent;
                                                    ShowMsg(temptime + ":" + strContent);
                                                }
                                                else
                                                {
                                                    ShowMsg(temptime + ":" + "无正确修正表");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x13)//反射率表查询回复
                                            {
                                                if (ReceiveBuf[temp_head + 26] == 0x01)//有正确反射率表
                                                {
                                                    string strContent = string.Empty;
                                                    for (int i = 0; i < 20; i += 2)
                                                    {
                                                        int tempval = (Int16)(ReceiveBuf[temp_head + 29 + i] << 8) + (ReceiveBuf[temp_head + 29 + i + 1]);
                                                        strContent += tempval.ToString() + "  ";
                                                    }
                                                    strContent = "收到回复：有反射率表，前10个数据如下：\n" + strContent;
                                                    ShowMsg(temptime + ":" + strContent);
                                                }
                                                else
                                                {
                                                    ShowMsg(temptime + ":" + "无正确反射率表");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x18)//偏心表查询回复
                                            {
                                                if (ReceiveBuf[temp_head + 26] == 0x01)//有正确偏心表
                                                {
                                                    string strContent = string.Empty;
                                                    for (int i = 0; i < 10; i++)
                                                    {
                                                        int tempval = ReceiveBuf[temp_head + 29 + i];
                                                        strContent += tempval.ToString() + "  ";
                                                    }
                                                    strContent = "收到回复：有偏心表，前10个数据如下：\n" + strContent;
                                                    ShowMsg(temptime + ":" + strContent);
                                                }
                                                else
                                                {
                                                    ShowMsg(temptime + ":" + "无正确偏心表");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x10)    //心跳
                                            {
                                                HeartBeatCount++;
                                                sckClient.Send(m_SendProtocolData.HeartBeatData, m_SendProtocolData.HeartBeatData.Length, 0);
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else
                                            {
                                                temp_head = temp_head + 1;
                                                if (temp_head == temp_tail + 1)
                                                {
                                                    break;
                                                }
                                                temp_length--;
                                            }
                                        }
                                        else if (ReceiveBuf[temp_head + 22] == 0x04 && ReceiveBuf[temp_head + 11] == 0x02)
                                        {
                                            if (ReceiveBuf[temp_head + 23] == 0x01)
                                            {
                                                BasicDataContent m_BasicDataContent = new BasicDataContent();
                                                m_BasicDataContent.ReceiveCommandName = "获得版本";
                                                m_BasicDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorBasicRecv.GetEvent<BasicDataShowEvent>().Publish(m_BasicDataContent);
                                                ShowMsg(temptime + ": 接收到版本信息！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x02)
                                            {
                                                n_runStateRespond++;
                                                if(n_runStateRespond==5)//0705将3改为5
                                                {
                                                    n_runStateRespond = 0;
                                                    if (n_connectLaser > 1)
                                                    {
                                                        this.eventAggregatorContinuousFrame.GetEvent<OpenContinusFrame>().Publish("获取连续波形");
                                                        ShowMsg(temptime+":网络重新连接成功！");
                                                        n_connectLaser = 0;
                                                    }
                                                }
                                                BasicDataContent m_BasicDataContent = new BasicDataContent();
                                                m_BasicDataContent.ReceiveCommandName = "获得运行状态";
                                                m_BasicDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorBasicRecv.GetEvent<BasicDataShowEvent>().Publish(m_BasicDataContent);
                                                //ShowMsg(temptime + ": 接收到运行状态！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x03)
                                            {
                                                BasicDataContent m_BasicDataContent = new BasicDataContent();
                                                m_BasicDataContent.ReceiveCommandName = "获得复位信息";
                                                m_BasicDataContent.ReceiveContent = tempData.ToList();
                                                this.eventAggregatorBasicRecv.GetEvent<BasicDataShowEvent>().Publish(m_BasicDataContent);
                                                ShowMsg(temptime + ": 接收到复位信息！");
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x04)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 复位信息清零成功！");
                                                }
                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else if (ReceiveBuf[temp_head + 23] == 0x05)
                                            {
                                                if (ReceiveBuf[temp_head + 29] == 0x01)
                                                {
                                                    ShowMsg(temptime + ": 设备已响应重启指令，请重新连接设备！");
                                                }

                                                temp_head = temp_head + frame_length + 4;
                                                temp_length = temp_length - frame_length - 4;
                                            }
                                            else
                                            {
                                                temp_head = temp_head + 1;
                                                if (temp_head == temp_tail + 1)
                                                {
                                                    break;
                                                }
                                                temp_length--;
                                            }
                                        }
                                        else
                                        {
                                            temp_head = temp_head + 1;
                                            if (temp_head == temp_tail + 1)
                                            {
                                                break;
                                            }
                                            temp_length--;
                                        }
                                    }
                                    else
                                    {
                                        temp_head = temp_head + 1;
                                        if (temp_head == temp_tail + 1)
                                        {
                                            break;
                                        }
                                        temp_length--;
                                    }
                                }
                                else
                                {
                                    temp_head = temp_head + 1;
                                    if (temp_head == temp_tail + 1)
                                    {
                                        break;
                                    }
                                    temp_length--;
                                }
                            }
                            else
                            {
                                temp_head = temp_head + 1;
                                if (temp_head == temp_tail + 1)
                                {
                                    break;
                                }
                                temp_length--;
                            }
                        }
                        else
                        {
                            temp_head = temp_head + 1;
                            if (temp_head == temp_tail + 1)
                            {
                                break;
                            }
                            temp_length--;
                        }
                    }
                    else if (((byte)ReceiveBuf[temp_head] == 0x02) && ((byte)ReceiveBuf[(temp_head + 1)] != 0x02))	//判断单个02包帧头
                    {
                        string ReceiveData = "";
                        int tail = Array.FindIndex(ReceiveBuf, temp_head, x => x == 0x03);
                        if (tail != -1)
                        {
                            ReceiveData = Encoding.UTF8.GetString(ReceiveBuf.Skip(temp_head).Take(tail - temp_head + 1).ToArray(), 0, tail - temp_head + 1);
                            string[] ary1 = ReceiveData.Split(' ');
                            if (ary1[0] == "sAN" && ary1[1] == "mNPOSGetData")
                            {
                                ReceiveSICKContent m_ReceiveSICKContent = new ReceiveSICKContent();
                                m_ReceiveSICKContent.ReceiveContent = ary1.ToList();
                                this.eventAggregatorSICKRecv.GetEvent<ReceiveSICKEvent>().Publish(m_ReceiveSICKContent);
                            }
                        }
                        else if (tail == -1)
                        {
                            temp_head = temp_tail;
                            temp_length = 0;
                            break;
                        }

                        temp_head = Array.FindIndex(ReceiveBuf, temp_head + 1, x => x == 0x02);
                        if (temp_head == -1)
                        {
                            temp_head = temp_tail;
                            temp_length = 0;
                            break;
                        }
                        else
                        {
                            temp_length = temp_length - temp_head;
                        }

                    }
                    else
                    {
                        temp_head = temp_head + 1;
                        if (temp_head == temp_tail + 1)
                        {
                            break;
                        }
                        temp_length--;
                    }
                }
            }

            StateObject state2 = new StateObject();
            state2.sckReceive = sckReceive;
            try
            {
                sckReceive.BeginReceive(state2.ReceiveBuf, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback2), state2);
            }
            catch (System.Exception ex)
            {
                return;
            }
        }

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Display(int Type);

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdateCloudPoint(Single x, Single y, Single z);

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdatePointCloudbyName();

        int tt = 0;
        private void SendToLaserCommandExecute()
        {
            if (tt >= 4)
            {
                Random ran = new Random();
                for (Single z = -1.0f; z <= 1.0; z += 0.02f)
                {
                    for (Single x = 0.0f; x <= 360.0; x += 1.0f)
                    {
                        Single y = x * z * ran.Next(1, 10);
                        UpdateCloudPoint(x, y, z);
                    }
                }
                UpdatePointCloudbyName();
                int id = Thread.CurrentThread.ManagedThreadId;
            }
            else
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                Display(tt);
            }
            tt++;

            int SendBuffLength = 0;
            if (IsSocketConnected(sckClient) == false)//服务器断线保护
            {
                ShowMsg("发送失败，TCP连接未建立或已断开！");
                return;
            }
            string strMsg = this.m_NetWindowM.LaserSendData.Trim();
            if (strMsg == "")
            {
                ShowMsg("发送内容为空！");
                return;
            }
            string[] strTemp = strMsg.Split(' ');
            byte[] arrMsg = new byte[strTemp.Length];
            for (int i = 0; i < arrMsg.Length; i++)
            {
                arrMsg[i] = Convert.ToByte(strTemp[i], 16);
            }
            SendBuffLength = sckClient.Send(arrMsg, arrMsg.Length, 0);
            ShowMsg("成功发送 " + SendBuffLength.ToString() + " 个字节");
        }
        #endregion

        #region 设置的最终组包发送
        private int SetPackageSend(byte MainCommandNum, byte SubCommandNum, int length)
        {
            m_SendProtocolData.SendByte[22] = MainCommandNum;   //主指令号
            m_SendProtocolData.SendByte[23] = SubCommandNum;   //子命令号
            length = length + 26;
            m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
            m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
            m_SendProtocolData.SendByte[length++] = 0x00;
            m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
            length++;
            m_SendProtocolData.SendByte[length++] = 0xEE;
            m_SendProtocolData.SendByte[length++] = 0xEE;
            return sckClient.Send(m_SendProtocolData.SendByte, length, 0);
        }
        #endregion

        int SingleFrameSendFailCount = 0;
        private void SendSingleFrameTimer(object O)
        {
            if (IsSocketConnected(sckClient) == false)//服务器断线保护
            {
                SingleFrameSendFailCount++;
                if (SingleFrameSendFailCount > 3)
                {
                    timer.Dispose();
                    TCPSendSucceedName m_TCPSendSucceedName = new TCPSendSucceedName() { SendSucceedName = "停止波形指令" };
                    this.eventAggregatorSendSucceedP.GetEvent<TCPSendSucceedEvent>().Publish(m_TCPSendSucceedName);
                }
            }
            else
            {
                    SingleFrameSendFailCount = 0;
                    sckClient.Send(m_SendProtocolData.GetSingleFrame, m_SendProtocolData.GetSingleFrame.Length, 0);
            }
        }

        #region 响应订阅的发送事件
        private void SendToLaserCommandEvent(TCPSendCommandName m_TCPSendCommandName)
        {
            int SendBuffLength = 0;
            int length = 0;
            if (IsSocketConnected(sckClient) == false)//服务器断线保护
            {
                ShowMsg("发送失败，TCP连接未建立或已断开！");
                if (m_TCPSendCommandName.SendCommandName == "靶标信息查询首包")
                {
                    LandmarkResponseContent m_LandmarkResponseContent = new LandmarkResponseContent();
                    m_LandmarkResponseContent.LandmarkResponseName = "靶标查询开始发送失败";
                    this.eventAggregatorLMRecv.GetEvent<LandmarkResponseEvent>().Publish(m_LandmarkResponseContent);
                }
                else if (m_TCPSendCommandName.SendCommandName == "靶标设置指令")
                {
                    LandmarkResponseContent m_LandmarkResponseContent = new LandmarkResponseContent();
                    m_LandmarkResponseContent.LandmarkResponseName = "靶标设置发送失败";
                    this.eventAggregatorLMRecv.GetEvent<LandmarkResponseEvent>().Publish(m_LandmarkResponseContent);
                }
                return;
            }
            if (m_TCPSendCommandName.SendCommandName == "获取单帧指令")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetSingleFrame, m_SendProtocolData.GetSingleFrame.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "获取波形指令")
            {
                timer = new System.Threading.Timer(new System.Threading.TimerCallback(SendSingleFrameTimer), null, 0, 200);
                TCPSendSucceedName m_TCPSendSucceedName = new TCPSendSucceedName() { SendSucceedName = m_TCPSendCommandName.SendCommandName };
                m_TCPSendSucceedName.SendSucceedName = m_TCPSendCommandName.SendCommandName;
                this.eventAggregatorSendSucceedP.GetEvent<TCPSendSucceedEvent>().Publish(m_TCPSendSucceedName);
            }
            else if (m_TCPSendCommandName.SendCommandName == "停止波形指令")
            {
                if (timer != null)
                {
                    timer.Dispose();
                }
                TCPSendSucceedName m_TCPSendSucceedName = new TCPSendSucceedName() { SendSucceedName = m_TCPSendCommandName.SendCommandName };
                this.eventAggregatorSendSucceedP.GetEvent<TCPSendSucceedEvent>().Publish(m_TCPSendSucceedName);
            }
            else if (m_TCPSendCommandName.SendCommandName == "基本参数查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetBasicParamData, m_SendProtocolData.GetBasicParamData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "版本查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetVersionsData, m_SendProtocolData.GetVersionsData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "运行状态查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetRunStateData, m_SendProtocolData.GetRunStateData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "复位信息查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetResetInfoData, m_SendProtocolData.GetResetInfoData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "状态检测查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetStateDetectionData, m_SendProtocolData.GetStateDetectionData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "网络参数查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetNetParamData, m_SendProtocolData.GetNetParamData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "APD参数查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetAPDParamData, m_SendProtocolData.GetAPDParamData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "电机参数查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetMotorParamData, m_SendProtocolData.GetMotorParamData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "开启电机测试")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.StartMotorTestData, m_SendProtocolData.StartMotorTestData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "关闭电机测试")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.StopMotorTestData, m_SendProtocolData.StopMotorTestData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "复位清零")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.CleaResetZeroData, m_SendProtocolData.CleaResetZeroData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "重启激光器")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.RestartLaserData, m_SendProtocolData.RestartLaserData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标信息查询首包")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetLandmarkInfoData, m_SendProtocolData.GetLandmarkInfoData.Length, 0);
                if (SendBuffLength > 0 && (IsSocketConnected(sckClient) == true))
                {
                    LandmarkResponseContent m_LandmarkResponseContent = new LandmarkResponseContent();
                    m_LandmarkResponseContent.LandmarkResponseName = "靶标查询开始回复";
                    this.eventAggregatorLMRecv.GetEvent<LandmarkResponseEvent>().Publish(m_LandmarkResponseContent);
                }
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标信息查询其它包")
            {
                m_SendProtocolData.SendByte[22] = 0x06;   //主指令号
                m_SendProtocolData.SendByte[23] = 0x06;   //子命令号
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                length = length + 26;
                m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
                m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
                m_SendProtocolData.SendByte[length++] = 0x00;
                m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
                length++;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                SendBuffLength = sckClient.Send(m_SendProtocolData.SendByte, length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "Mapping过程中回复")
            {
                m_SendProtocolData.SendByte[22] = 0x06;   //主指令号
                m_SendProtocolData.SendByte[23] = 0x05;   //子命令号
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                length = length + 26;
                m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
                m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
                m_SendProtocolData.SendByte[length++] = 0x00;
                m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
                length++;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                SendBuffLength = sckClient.Send(m_SendProtocolData.SendByte, length, 0);

                DateTime dt = DateTime.Now;
                string temptime = dt.ToString("HH:mm:ss.fff");
                string temppackage = ((m_TCPSendCommandName.SendContent.ElementAt(0) << 8) + m_TCPSendCommandName.SendContent.ElementAt(1)).ToString();
                ShowMsg(temptime + ": 已完成 " + temppackage + " 次平均");
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标探测模式")
            {
                CurrentWorkMode = 1;
                m_SendProtocolData.SendByte[22] = 0x06;   //主指令号
                m_SendProtocolData.SendByte[23] = 0x04;   //子命令号
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                length = length + 26;
                m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
                m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
                m_SendProtocolData.SendByte[length++] = 0x00;
                m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
                length++;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                SendBuffLength = sckClient.Send(m_SendProtocolData.SendByte, length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标获取模式")
            {
                CurrentWorkMode = 2;
                m_SendProtocolData.SendByte[22] = 0x06;   //主指令号
                m_SendProtocolData.SendByte[23] = 0x04;   //子命令号
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                length = length + 26;
                m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
                m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
                m_SendProtocolData.SendByte[length++] = 0x00;
                m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
                length++;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                SendBuffLength = sckClient.Send(m_SendProtocolData.SendByte, length, 0);

                if (SendBuffLength > 0 && (IsSocketConnected(sckClient) == true))
                {
                    MappingWorkModeContent m_MappingWorkModeContent = new MappingWorkModeContent();
                    if (m_TCPSendCommandName.SendContent.ElementAt(1) == 0x02)
                    {
                        m_MappingWorkModeContent.MappingType = 1; //正常靶标获取
                    }
                    else
                    {
                        m_MappingWorkModeContent.MappingType = 2; //添加模式靶标获取
                    }
                    m_MappingWorkModeContent.LayerID = (m_TCPSendCommandName.SendContent.ElementAt(2) << 8) + m_TCPSendCommandName.SendContent.ElementAt(3);
                    //5.10应要求由130改为330，防止经常获取不成功   5.31由于经常出现到某一包后就结束，改回130，把+300改为+500
                    //5.31将130改为160（160测过都有回复） 
                    //0604发现160有问题，是激光器的问题出现丢包导致的，正常情况下160时间够了
                    m_MappingWorkModeContent.WaitTime = 160 * ((m_TCPSendCommandName.SendContent.ElementAt(4) << 8) + m_TCPSendCommandName.SendContent.ElementAt(5)) + 500;
                    this.eventAggregatorMappingMode.GetEvent<MappingWorkModeEvent>().Publish(m_MappingWorkModeContent);
                }
            }
            else if (m_TCPSendCommandName.SendCommandName == "导航模式")
            {
                CurrentWorkMode = 3;
                m_SendProtocolData.SendByte[22] = 0x06;   //主指令号
                m_SendProtocolData.SendByte[23] = 0x04;   //子命令号
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                length = length + 26;
                m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
                m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
                m_SendProtocolData.SendByte[length++] = 0x00;
                m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
                length++;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                SendBuffLength = sckClient.Send(m_SendProtocolData.SendByte, length, 0);

                if (SendBuffLength > 0 && (IsSocketConnected(sckClient) == true))
                {
                    NaviQueryContent m_NaviQueryContent = new NaviQueryContent();
                    m_NaviQueryContent.NaviQueryName = "导航查询靶标初始化";
                    m_NaviQueryContent.LayerID = (m_TCPSendCommandName.SendContent.ElementAt(4) << 8) + m_TCPSendCommandName.SendContent.ElementAt(5);
                    this.eventAggregatorNaviQuery.GetEvent<NaviQueryEvent>().Publish(m_NaviQueryContent);
                }
            }
            else if (m_TCPSendCommandName.SendCommandName == "生产扫描模式")
            {
                CurrentWorkMode = 4;
                SendBuffLength = sckClient.Send(m_SendProtocolData.QueryMotorPara, m_SendProtocolData.QueryMotorPara.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标设置指令")
            {
                m_SendProtocolData.SendByte[22] = 0x06;   //主指令号
                m_SendProtocolData.SendByte[23] = 0x03;   //子命令号
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                length = length + 26;
                m_SendProtocolData.SendByte[2] = (byte)((length >> 8) & 0x00ff);
                m_SendProtocolData.SendByte[3] = (byte)(length & 0x00ff);
                m_SendProtocolData.SendByte[length++] = 0x00;
                m_SendProtocolData.SendByte[length] = Add_BCC(m_SendProtocolData.SendByte, (length - 1));
                length++;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                m_SendProtocolData.SendByte[length++] = 0xEE;
                SendBuffLength = sckClient.Send(m_SendProtocolData.SendByte, length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "基本参数设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x06, 0x02, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "修正表")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x05, 0x0E, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "反射率表")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x05, 0x13, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "偏心表")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x05, 0x18, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "网络参数设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x05, 0x03, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "APD参数设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x05, 0x05, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "一键恢复出厂设置")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.ResFactorySetData, m_SendProtocolData.ResFactorySetData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "电机参数设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x05, 0x07, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "速度参数设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x06, 0x07, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标识别阈值设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x06, 0x0F, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标识别阈值查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetTHParamData, m_SendProtocolData.GetTHParamData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标匹配范围设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x06, 0x11, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标匹配范围查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetLMMatchRangeData, m_SendProtocolData.GetLMMatchRangeData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标扫描范围设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x06, 0x13, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "靶标扫描范围查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.GetLMScanRangeData, m_SendProtocolData.GetLMScanRangeData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "2靶标定位功能设置")
            {
                length = m_TCPSendCommandName.SendContent.Count;
                m_TCPSendCommandName.SendContent.ToArray().CopyTo(m_SendProtocolData.SendByte, 26);
                SendBuffLength = SetPackageSend(0x06, 0x15, length);
            }
            else if (m_TCPSendCommandName.SendCommandName == "2靶标定位功能查询")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.Get2LMlocationData, m_SendProtocolData.Get2LMlocationData.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "SICK获取单帧指令")
            {
                SendBuffLength = sckClient.Send(m_SendProtocolData.SICKGetSingleFrame, m_SendProtocolData.SICKGetSingleFrame.Length, 0);
            }
            else if (m_TCPSendCommandName.SendCommandName == "SICK速度参数设置")
            {
                SendBuffLength = sckClient.Send(m_TCPSendCommandName.SendContent.ToArray(), m_TCPSendCommandName.SendContent.Count, 0);
            }
        }
        #endregion

        #region 响应订阅显示操作完成的提示
        private void OperationCompleteCommandEvent(OperationCompleteContent m_OperationCompleteContent)
        {

            DateTime dt = DateTime.Now;
            string temptime = dt.ToString("HH:mm:ss.fff");
            ShowMsg(temptime + ": " + m_OperationCompleteContent.NoticeContent);
        }
        #endregion

        #region 广播按钮的UDP功能
        private UdpClient receiveUpdClient;
        private bool UDPSearchFlag = false;     //停止UDP功能的标志位
        private IPEndPoint remoteIpEndPoint;    // 发送目标的ip&port
        private IPEndPoint localIpEndPoint;    // 本地接收的ip&port
        private void SearchLaserUDPCommandExecute()
        {
            this.NetWindowM.IsBtSearchEnable = false;
            this.NetWindowM.IsEnable = false;
            Thread TimeThread = new Thread(SearchTimeThread);
            TimeThread.Start();

            Thread WorkThread = new Thread(SearchWorkThread);
            WorkThread.Start();
        }

        private IPAddress GetIpAdress()
        {
            IPAddress Ip = null;
            System.Net.NetworkInformation.NetworkInterface[] fNetworkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (System.Net.NetworkInformation.NetworkInterface r in fNetworkInterfaces)
            {
                if (r.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && r.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
                {
                    if (r.Description.Contains("Wireless") == false)
                    {
                        System.Net.NetworkInformation.IPInterfaceProperties t = r.GetIPProperties();
                        System.Net.NetworkInformation.IPv4InterfaceProperties f = t.GetIPv4Properties();
                        System.Net.NetworkInformation.UnicastIPAddressInformationCollection hh = t.UnicastAddresses;
                        foreach (System.Net.NetworkInformation.UnicastIPAddressInformation cc in hh)
                        {
                            if (cc.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                Ip = cc.Address;
                            }
                        }
                    }
                }
            }
            return Ip;
        }

        private void SearchWorkThread()
        {
            remoteIpEndPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 6060);
            IPAddress Ip = GetIpAdress();
            if (Ip == null)
            {
                DateTime dt = DateTime.Now;
                string temptime = dt.ToString("HH:mm:ss.fff");
                ShowMsg(temptime + " 当前无可用有线以太网IP");
                this.NetWindowM.IsBtSearchEnable = true;
                this.NetWindowM.IsEnable = true;
                return;
            }
            localIpEndPoint = new IPEndPoint(IPAddress.Any, 7000);

            if (receiveUpdClient == null)
            {
                try
                {
                    receiveUpdClient = new UdpClient(localIpEndPoint);
                }
                catch (System.Exception ex)
                {
                    this.NetWindowM.IsBtSearchEnable = true;
                    this.NetWindowM.IsEnable = true;
                    return;
                }
            }
            else
            {
                if (!receiveUpdClient.Client.LocalEndPoint.ToString().Equals(localIpEndPoint.ToString()))
                {
                    try
                    {
                        receiveUpdClient = new UdpClient(localIpEndPoint);
                    }
                    catch (System.Exception ex)
                    {
                        this.NetWindowM.IsBtSearchEnable = true;
                        this.NetWindowM.IsEnable = true;
                        return;
                    }

                }
            }

            if (UDPSearchFlag == false)
            {
                UDPSearchFlag = true;
                Thread SendThread = new Thread(SearchSendThread);
                SendThread.Start();

                receiveUpdClient.BeginReceive(new AsyncCallback(ReceiveUDPData), null);
            }
            else
            {
                return;
            }
        }

        private void ReceiveUDPData(IAsyncResult iar)
        {
            try
            {
                IPEndPoint remoteIpEndPoint2 = new IPEndPoint(IPAddress.Any, 0);
                byte[] receiveData = receiveUpdClient.EndReceive(iar, ref remoteIpEndPoint2);
                receiveUpdClient.BeginReceive(new AsyncCallback(ReceiveUDPData), null);
                int revLength = receiveData.Length;

                if (revLength > 8)
                {
                    int temp_length = 0;
                    int temp_head, temp_tail;
                    int frame_length;

                    temp_length = revLength;
                    temp_head = 0;
                    temp_tail = temp_length - 1;

                    while ((temp_length != 0) && (temp_head != temp_tail))
                    {
                        if (((byte)receiveData[temp_head] == 0xff) && ((byte)receiveData[(temp_head + 1)] == 0xaa))	//判断FFAA包帧头
                        {
                            if (temp_length >= 34)
                            {
                                frame_length = (((byte)receiveData[temp_head + 2] & 0xff) << 8) + ((byte)receiveData[temp_head + 3]);
                                if ((temp_length >= frame_length + 4))
                                {
                                    if ((byte)receiveData[temp_head + frame_length + 2] == 0xee && (byte)receiveData[temp_head + frame_length + 3] == 0xee)	//判断帧尾
                                    {
                                        byte[] tempData = receiveData.Skip(temp_head).Take(frame_length + 4).ToArray();
                                        byte BccBuf = Add_BCC(tempData, frame_length);
                                        if (BccBuf == receiveData[temp_head + frame_length + 1])
                                        {
                                            if (receiveData[temp_head + 22] == 0x04 && receiveData[temp_head + 11] == 0x02)
                                            {
                                                if (receiveData[temp_head + 23] == 0x06)
                                                {
                                                    UDPSearchFlag = false;  //广播到后退出发送广播线程，后续根据广播功能需求修改
                                                    this.NetWindowM.LaserIP = receiveData[26].ToString() + "."
                                                                                + receiveData[27].ToString() + "."
                                                                                + receiveData[28].ToString() + "."
                                                                                + receiveData[29].ToString();
                                                    this.NetWindowM.LaserPort = ((receiveData[38] << 8) + receiveData[39]).ToString();

                                                    ProductionDataContent m_ProductionDataContent = new ProductionDataContent();
                                                    m_ProductionDataContent.ReceiveCommandName = "获得网络参数";
                                                    m_ProductionDataContent.ReceiveContent = tempData.ToList();
                                                    this.eventAggregatorProductionRecv.GetEvent<ProductionDataShowEvent>().Publish(m_ProductionDataContent);
                                                    DateTime dt = DateTime.Now;
                                                    string temptime = dt.ToString("HH:mm:ss.fff");
                                                    ShowMsg(temptime + ": 成功接收到IP和端口，并已填入，请连接！");
                                                    temp_head = temp_head + frame_length + 4;
                                                    temp_length = temp_length - frame_length - 4;
                                                }
                                                else
                                                {
                                                    temp_head = temp_head + 1;
                                                    if (temp_head == temp_tail + 1)
                                                    {
                                                        break;
                                                    }
                                                    temp_length--;
                                                }
                                            }
                                            else
                                            {
                                                temp_head = temp_head + 1;
                                                if (temp_head == temp_tail + 1)
                                                {
                                                    break;
                                                }
                                                temp_length--;
                                            }
                                        }
                                        else
                                        {
                                            temp_head = temp_head + 1;
                                            if (temp_head == temp_tail + 1)
                                            {
                                                break;
                                            }
                                            temp_length--;
                                        }
                                    }
                                    else
                                    {
                                        temp_head = temp_head + 1;
                                        if (temp_head == temp_tail + 1)
                                        {
                                            break;
                                        }
                                        temp_length--;
                                    }
                                }
                                else
                                {
                                    temp_head = temp_head + 1;
                                    if (temp_head == temp_tail + 1)
                                    {
                                        break;
                                    }
                                    temp_length--;
                                }
                            }
                            else
                            {
                                temp_head = temp_head + 1;
                                if (temp_head == temp_tail + 1)
                                {
                                    break;
                                }
                                temp_length--;
                            }
                        }
                        else
                        {
                            temp_head = temp_head + 1;
                            if (temp_head == temp_tail + 1)
                            {
                                break;
                            }
                            temp_length--;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                return;
            }
        }

        private void SearchSendThread()
        {
            byte[] DataSend = new byte[34] { 0xFF, 0xAA, 0x00, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x07, 
                                             0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x06, 0x00, 0x00, 0x00, 0x00, 
                                             0x00, 0x00, 0x00, 0x1B, 0xEE, 0xEE};
            while (UDPSearchFlag)
            {
                receiveUpdClient.BeginSend(DataSend, DataSend.Length, remoteIpEndPoint, new AsyncCallback(SendUDPData), null);
                Thread.Sleep(3000);
            }
            receiveUpdClient.Close();
            receiveUpdClient = null;
            this.NetWindowM.IsBtSearchEnable = true;
            this.NetWindowM.IsEnable = true;
        }

        private void SendUDPData(IAsyncResult iar)
        {
            int sendCount = receiveUpdClient.EndSend(iar);
        }

        private void SearchTimeThread()
        {
            Thread.Sleep(10000);
            UDPSearchFlag = false;
        }
        #endregion

        public void ShowMsg(string str)
        {
            if (this.NetWindowM.LaserRecData.Split('\r').Length > 200)
            {
                this.NetWindowM.LaserRecData = "";
            }
            this.NetWindowM.LaserRecData += str + "\r\n";

        }

        //BCC校验，02的sendlen为本帧全长，FF的sendlen为本帧全长-4
        private byte Add_BCC(byte[] sendbuf, int sendlen)
        {
            int i = 0;
            byte check = 0;
            int len;
            if (sendbuf[0] == 0x02)
            {
                len = sendlen;
                for (i = 8; i < (len - 1); i++)
                {
                    check ^= sendbuf[i];
                }
            }
            else if (sendbuf[0] == 0xff)
            {
                len = sendlen - 2;
                for (i = 0; i < len; i++)
                {
                    check ^= sendbuf[i + 2];
                }
            }
            else
            {
                ;
            }
            return check;
        }
    }
}
