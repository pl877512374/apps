using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Practices.Prism.Commands;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using NavigateLaser.DataAccess;
using System.Collections.ObjectModel;

namespace NavigateLaser.ViewModels
{
    class ScanInfoVM : NotificationObject
    {
        public const int DIMENSIONS_MULTIPLE = 1;   //距离数据放大倍数

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Display(int Type);
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AddCloudPointArray(Single[] x, Single[] y, Single[] z, Single[] LM, Single[] LP, bool RssiFlag, Single[] Rssi, int SPCount);
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdateCloudPoint(Single x, Single y, Single z);
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddCloudPoint(Single dist, Single angle, Single z);
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdatePointCloudbyName();
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearLMInfo();
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearSetLMInfo();
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RedLMDisplay(int ExeType, int LMNO);   //ExeType使用类型，0为下次显示更新，1为立即更新
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddScanPointArray(Single[] x, Single[] y, Single[] LP, int SPCount);

        public DelegateCommand SendSingleFrameCommand { get; set; }
        public DelegateCommand SendContinuousFrameCommand { get; set; }
        public DelegateCommand SpeedSetCommand { get; set; }
        public DelegateCommand SICKSendSingleFrameCommand { get; set; }
        public DelegateCommand SICKSpeedSetCommand { get; set; }
        public DelegateCommand<object> SelectionChangedCommand { get; set; }

        protected IEventAggregator eventAggregator;
        protected IEventAggregator eventAggregatorUpdatePose;
        protected IEventAggregator eventAggregator3DShowS;
        protected SubscriptionToken token3DShowS;
        protected IEventAggregator eventAggregator3DShowProScan;
        protected SubscriptionToken token3DShowProScan;
        protected IEventAggregator eventAggregatorSendSucceedS;
        protected SubscriptionToken tokenSendSucceedS;
        protected IEventAggregator eventAggregatorLMType;
        protected SubscriptionToken tokenLMType;
        protected IEventAggregator eventAggregatorDisplayType;
        protected SubscriptionToken tokenDisplayType;
        protected IEventAggregator eventAggregatorSICKRev;
        protected SubscriptionToken tokenSICKRev;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;
        protected IEventAggregator eventAggregatorWorkModeViewChange;
        protected SubscriptionToken tokenWorkModeViewChange;
        protected IEventAggregator eventAggregatorDisnetInit;
        protected SubscriptionToken tokenDisnetInit;
        protected IEventAggregator eventOpenContinusFrame;
        protected SubscriptionToken tokenOpenContinusFrame;

        private struct _SpliceFrame
        {
            public int FrameSequenceNum;  //帧序列号
            public int PacketTotalCount;  //一帧数据包括的包数
            public Single ResolutionAngle;//分辨角
            public int PacketNum;         //一帧数据的包序号
            public int PointNum;          //点数计数
            public bool LandMarkFlag;    //判断是否接收到靶标数据，true为接收到
            public int CurrentMode;     //当前工作模式
            public bool RSSIFlag;    //判断是否接收到扫描能量数据，true为接收到
        };
        _SpliceFrame SpliceFrame;

        private struct _Statistical
        {
            public int StatisticalFrameCount;   //统计帧数
            public int PointSequenceNum;     //统计点的序号
            public int StatisticalMAX;   //统计的最大值
            public int StatisticalMIN;    //统计的最小值
            public long StatisticalAVG;    //统计的平均值
            public long StatisticalDistSum;    //用于统计平均值
            public int CurrentStatisticalCount;   //统计计数
        };
        _Statistical Statistical;

        private ScanInfoM m_ScanInfoM;
        public ScanInfoM ScanInfoM
        {
            get
            {
                if (Statistical.StatisticalFrameCount != m_ScanInfoM.StatisticalFrameCount || (Statistical.PointSequenceNum != m_ScanInfoM.PointSequenceNum))
                {
                    if (m_ScanInfoM.StatisticalFrameCount < 10)
                    {
                        m_ScanInfoM.StatisticalFrameCount = 10;
                    }
                    Statistical.StatisticalFrameCount = m_ScanInfoM.StatisticalFrameCount;
                    if (m_ScanInfoM.PointSequenceNum < 1)
                    {
                        m_ScanInfoM.PointSequenceNum = 1;
                    }
                    else if (m_ScanInfoM.PointSequenceNum > 3600 && (SpliceFrame.ResolutionAngle == 0.1f))
                    {
                        m_ScanInfoM.PointSequenceNum = 3600;
                    }
                    else if (m_ScanInfoM.PointSequenceNum > 7200)
                    {
                        m_ScanInfoM.PointSequenceNum = 7200;
                    }
                    Statistical.PointSequenceNum = m_ScanInfoM.PointSequenceNum;
                    Statistical.StatisticalMAX = 0;   //统计的最大值
                    Statistical.StatisticalMIN = 999999;    //统计的最小值
                    Statistical.StatisticalAVG = 0;    //统计的平均值
                    Statistical.StatisticalDistSum = 0;    //统计平均值使用的和
                    Statistical.CurrentStatisticalCount = 0;   //统计计数
                }
                return m_ScanInfoM;
            }
            set
            {
                m_ScanInfoM = value;
                this.RaisePropertyChanged("ScanInfoM");
            }
        }

        public ScanInfoVM(System.Windows.Controls.DataGrid DataGridVieW)
        {
            this.LoadScanInfoM(DataGridVieW);
            this.SendSingleFrameCommand = new DelegateCommand(new Action(this.SendSingleFrameCommandExecute));
            this.SendContinuousFrameCommand = new DelegateCommand(new Action(this.SendContinuousFrameCommandExecute));
            this.SpeedSetCommand = new DelegateCommand(new Action(this.SpeedSetCommandExecute));
            this.SelectionChangedCommand = new DelegateCommand<object>(new Action<object>(this.SelectionChangedCommandExecute));
            this.eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorUpdatePose = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregator3DShowS = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.token3DShowS = eventAggregator3DShowS.GetEvent<ScanDataShowEvent>().Subscribe(ScanDataShowCommand);

            this.eventAggregator3DShowProScan = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.token3DShowProScan = eventAggregator3DShowS.GetEvent<ProScanDataShowEvent>().Subscribe(ProScanDataShowCommand);
            this.eventAggregatorSendSucceedS = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenSendSucceedS = eventAggregatorSendSucceedS.GetEvent<TCPSendSucceedEvent>().Subscribe(SendSucceedCommand);
            this.eventAggregatorLMType = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenLMType = eventAggregatorLMType.GetEvent<LandmarkTypeEvent>().Subscribe(LandmarkTypeCommand);
            this.eventAggregatorDisplayType = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenDisplayType = eventAggregatorDisplayType.GetEvent<PCLDisplayTypeEvent>().Subscribe(PCLDisplayTypeCommand);
            this.eventAggregatorSICKRev = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);
            this.eventAggregatorWorkModeViewChange = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenWorkModeViewChange = eventAggregatorWorkModeViewChange.GetEvent<WorkModeViewEvent>().Subscribe(WorkModeViewChangeCommand);
            this.eventAggregatorDisnetInit = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenDisnetInit = eventAggregatorDisnetInit.GetEvent<DisnetInitEvent>().Subscribe(DisnetInitCommand);
            this.eventOpenContinusFrame = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenOpenContinusFrame = eventOpenContinusFrame.GetEvent<OpenContinusFrame>().Subscribe(FuncOpenContinusFrame);

            for (int i = 0; i < 100; i++)
            {
                xArray[i] = new Single[7200];
                yArray[i] = new Single[7200];
                zArray[i] = new Single[7200];
                angleArray[i] = new Single[7200];
                RSSIArray[i] = new Single[7200];
            }
        }

        private void LoadScanInfoM(System.Windows.Controls.DataGrid DataGridVieW)
        {
            this.ScanInfoM = new ScanInfoM();
            this.ScanInfoM.myDataGrid = DataGridVieW;
            this.ScanInfoM.BtConnectTXT = "获取连续波形";
            this.ScanInfoM.BtIsEnable = false;
            this.ScanInfoM.StatisticalFrameCount = 100;
            this.ScanInfoM.IsEnable = true;
        }

        public void InitializeVariable()
        {
            SpliceFrame.FrameSequenceNum = -1;
            SpliceFrame.PacketTotalCount = -1;
            SpliceFrame.PacketNum = -1;
            SpliceFrame.PointNum = 0;
            SpliceFrame.ResolutionAngle = 0;
            SpliceFrame.LandMarkFlag = false;
            SpliceFrame.CurrentMode = -1;
            SpliceFrame.RSSIFlag = false;
        }
        private void FuncOpenContinusFrame(string str)
        {
            this.ScanInfoM.BtConnectTXT = str;
            SendContinuousFrameCommandExecute();
        }
        private void SendSingleFrameCommandExecute()
        {
            InitializeVariable();
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "获取单帧指令" };
            this.eventAggregator.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }
        public static bool f_ContinueWave = false;//是否获取连续波形标志位
        private void SendContinuousFrameCommandExecute()
        {
            if (this.ScanInfoM.BtConnectTXT == "获取连续波形")
            {
                f_ContinueWave = true;
                InitializeVariable();
                //ClearLMInfo();
                TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "获取波形指令" };
                this.eventAggregator.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            }
            else if (this.ScanInfoM.BtConnectTXT == "停止连续波形")
            {
                f_ContinueWave = false;
                this.ScanInfoM.BtIsEnable = false;
                Thread Thread = new Thread(SendStopCommandThread);
                Thread.Start();
            }
        }

        //发送停止指令线程
        private void SendStopCommandThread()
        {
            this.ScanInfoM.BtConnectTXT = "获取连续波形";
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "停止波形指令" };
            this.eventAggregator.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            Thread.Sleep(100);
            while (FlagDisplay == true)
            {
                Thread.Sleep(50);
            }
            this.SendSingleFrameCommandExecute();
            Thread.Sleep(1000);    //时间加长，否者按钮动画显示报错

            if (this.ScanInfoM.ValidFlag != 0)
            {
                UpdatePoseContent m_UpdatePoseContent = new UpdatePoseContent();
                m_UpdatePoseContent.PoseInfo = new List<int>();
                m_UpdatePoseContent.PoseInfo.Add(Convert.ToInt32(this.ScanInfoM.SelfAngle * 1000));
                m_UpdatePoseContent.PoseInfo.Add(this.ScanInfoM.SelfX);
                m_UpdatePoseContent.PoseInfo.Add(this.ScanInfoM.SelfY);
                this.eventAggregatorUpdatePose.GetEvent<UpdatePoseEvent>().Publish(m_UpdatePoseContent);
            }
            this.ScanInfoM.BtIsEnable = true;
        }

        private void SendSucceedCommand(TCPSendSucceedName m_TCPSendSucceedName)
        {
            if (m_TCPSendSucceedName.SendSucceedName == "获取波形指令")
            {
                this.ScanInfoM.BtConnectTXT = "停止连续波形";
            }
            else if (m_TCPSendSucceedName.SendSucceedName == "停止波形指令")
            {
                if (this.ScanInfoM.BtConnectTXT == "停止连续波形")
                {
                    this.ScanInfoM.BtConnectTXT = "获取连续波形";
                }
            }
        }

        Single[][] xArray = new Single[100][];   //100组，7200个点
        Single[][] yArray = new Single[100][];
        Single[][] zArray = new Single[100][];
        Single[][] angleArray = new Single[100][];    //100组，7200个点的角度
        Single[][] RSSIArray = new Single[100][];    //100组，7200个点的反射率
        int CurrentBuffer = 0;     //用于控制接收缓存下标位置
        int InvokeCurrentBuffer = 0;   //用于传递调用时固定下标
        int DataPointIndex = 0;   //当前类型数据接收的下标
        bool CurrentRSSIFlag = false;   //用于传递当前RSSI数据
        int TotalPointForJudge = 0;   //用于最后显示时判断是否接收完全
        Single[] LandmarkData = new Single[121]; //1 + 3*40，靶标个数+（靶标标志位+靶标位置X+靶标位置Y）
        Single[] LaserPose = new Single[4];  //有效标志位、角度、X、Y
        int[] ScanPointCount = new int[100];   //用于不同模式判断靶标信息所在字符的位置，3600点时，可以比7200少读取3600点，使用帧信息结构体传递出现0的情况
        bool FlagDisplay = false;
        int DisplayInterval = 0;
        bool DisplayPause = false;   //false: 暂停显示 ; true: 恢复显示

        private void ScanDataShowCommand(ScanDataContent m_ScanDataContent)
        {
            Single EachPointAngle = 0.00f;
            Single EachPointDist = 0.00f;
            int TempPointDist = 0;
            int TempFrameSequenceNum = m_ScanDataContent.ScanContent[81];   //帧序号，0-249
            int TempPacketNum = m_ScanDataContent.ScanContent[82];        //一帧的包号

            //新一帧数据
            if (SpliceFrame.FrameSequenceNum != TempFrameSequenceNum)
            {
                int TempPacketTotalCount = 0;
                if (m_ScanDataContent.ScanContent[78] == 0x00)
                {
                    TempPacketTotalCount = 0;    //只有第0包，使绘制判断点数时为0
                }
                else
                {
                    //0:7200点，0.05° 12包; 1:3600点，0.1° 6包
                    if (m_ScanDataContent.ScanContent[80] == 0x00)
                    {
                        TempPacketTotalCount = 12;
                        TotalPointForJudge = 7200;
                    }
                    else if (m_ScanDataContent.ScanContent[80] == 0x01)
                    {
                        TempPacketTotalCount = 6;
                        TotalPointForJudge = 3600;
                    }
                    //根据扫描数据内容类型确定总包数及总点数
                    int TempScanType = m_ScanDataContent.ScanContent[79];
                    if (TempScanType == 0)   //距离
                    {
                        //不变
                    }
                    else if (TempScanType == 1 || TempScanType == 2)  //角度 || 能量
                    {
                        TempPacketTotalCount = TempPacketTotalCount / 2;
                    }
                    else if (TempScanType == 3 || TempScanType == 4)  //距离+角度 || 距离+能量
                    {
                        TempPacketTotalCount = TempPacketTotalCount + TempPacketTotalCount / 2;
                        TotalPointForJudge = TotalPointForJudge * 2;
                    }
                    else if (TempScanType == 5)
                    {
                        //不变
                        TotalPointForJudge = TotalPointForJudge * 2;
                    }
                    else if (TempScanType == 6)
                    {
                        TempPacketTotalCount = TempPacketTotalCount * 2;
                        TotalPointForJudge = TotalPointForJudge * 3;
                    }
                }
                SpliceFrame.FrameSequenceNum = TempFrameSequenceNum;
                SpliceFrame.PacketTotalCount = TempPacketTotalCount;
                SpliceFrame.PacketNum = 0;
                SpliceFrame.PointNum = 0;
                if (m_ScanDataContent.ScanContent[80] == 0)
                {
                    SpliceFrame.ResolutionAngle = 0.05f;
                }
                else if (m_ScanDataContent.ScanContent[80] == 1)
                {
                    SpliceFrame.ResolutionAngle = 0.1f;
                }
                SpliceFrame.LandMarkFlag = false;
                SpliceFrame.RSSIFlag = false;
                SpliceFrame.CurrentMode = m_ScanDataContent.ScanContent[77];  //当前本包工作模式
            }

            //真内容数据拷贝
            if (SpliceFrame.PacketNum == TempPacketNum)
            {
                if (TempPacketNum == 0)
                {
                    int index = 0;  //靶标起始下标位置
                    List<MarkInfo> TempMarkInfoMenu = new List<MarkInfo>();   //列表写入
                    if (SpliceFrame.CurrentMode == 1)   //导航
                    {
                        if (m_ScanDataContent.ScanContent[83] == 0x01)
                        {
                            ScanInfoM.ValidFlag = m_ScanDataContent.ScanContent[85];
                            ScanInfoM.SelfAngle = Convert.ToSingle((m_ScanDataContent.ScanContent[86] << 24) + (m_ScanDataContent.ScanContent[87] << 16) + (m_ScanDataContent.ScanContent[88] << 8) + m_ScanDataContent.ScanContent[89]) / 1000;
                            ScanInfoM.SelfX = ((int)((m_ScanDataContent.ScanContent[90] << 24) + (m_ScanDataContent.ScanContent[91] << 16) + (m_ScanDataContent.ScanContent[92] << 8) + m_ScanDataContent.ScanContent[93]) * DIMENSIONS_MULTIPLE);
                            ScanInfoM.SelfY = ((int)((m_ScanDataContent.ScanContent[94] << 24) + (m_ScanDataContent.ScanContent[95] << 16) + (m_ScanDataContent.ScanContent[96] << 8) + m_ScanDataContent.ScanContent[97]) * DIMENSIONS_MULTIPLE);
                            if (m_ScanDataContent.ScanContent[85] == 0)
                            {
                                LaserPose[0] = 0;
                            }
                            else
                            {
                                LaserPose[0] = 1;
                            }

                            LaserPose[1] = ScanInfoM.SelfAngle; //角度
                            //Random ran=new Random();
                            //int n=ran.Next(-50,50);
                            LaserPose[2] = ScanInfoM.SelfX;//+n*1000; //X
                            LaserPose[3] = ScanInfoM.SelfY;// + (n-5) * 1000; //Y
                            index = 100;
                        }
                    }
                    else if (SpliceFrame.CurrentMode == 0)
                    {
                        LaserPose[0] = 1;
                        LaserPose[1] = 0; //角度
                        LaserPose[2] = 0; //X
                        LaserPose[3] = 0; //Y
                        index = 84;
                    }

                    if (m_ScanDataContent.ScanContent[index++] == 0x01)
                    {
                        ScanInfoM.LMTotal = m_ScanDataContent.ScanContent[index];  //靶标总个数
                        LandmarkData[0] = m_ScanDataContent.ScanContent[index++];  //靶标总个数
                        for (int num = 0; num < ScanInfoM.LMTotal; num++)
                        {
                            MarkInfo item = new MarkInfo();
                            item.MarkNo = m_ScanDataContent.ScanContent[index + num * 20];  //本地ID
                            item.MarkX = (m_ScanDataContent.ScanContent[index + 1 + num * 20] << 24) + (m_ScanDataContent.ScanContent[index + 2 + num * 20] << 16) +
                                            (m_ScanDataContent.ScanContent[index + 3 + num * 20] << 8) + m_ScanDataContent.ScanContent[index + 4 + num * 20];
                            item.MarkY = (m_ScanDataContent.ScanContent[index + 5 + num * 20] << 24) + (m_ScanDataContent.ScanContent[index + 6 + num * 20] << 16) +
                                            (m_ScanDataContent.ScanContent[index + 7 + num * 20] << 8) + m_ScanDataContent.ScanContent[index + 8 + num * 20];
                            TempMarkInfoMenu.Add(item);

                            LandmarkData[1 + num * 3] = m_ScanDataContent.ScanContent[index + 9 + num * 20];    //靶标标志，0未设置的新靶标；1匹配上的已设置靶标；
                            LandmarkData[1 + num * 3 + 1] = ((Single)item.MarkX) / 1000;
                            LandmarkData[1 + num * 3 + 2] = ((Single)item.MarkY) / 1000;
                        }
                    }
                    else
                    {
                        LandmarkData[0] = 0;   //靶标总个数为0
                    }

                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (SelectedNO != -1)
                        {
                            int ret = TempMarkInfoMenu.FindIndex(i => i.MarkNo == SelectedNO);
                            if (ret >= 0)
                            {
                                this.ScanInfoM.CurrentSelectItem = TempMarkInfoMenu.ElementAt(SelectedNO);
                            }
                            else
                            {
                                MarkInfo CurrentNull = new MarkInfo();
                                this.ScanInfoM.CurrentSelectItem = CurrentNull;
                            }
                        }
                        ScanInfoM.MarkInfoMenu = TempMarkInfoMenu;
                    }));
                    SpliceFrame.LandMarkFlag = true;

                    Array.Clear(xArray[CurrentBuffer], 0, 7200);
                    Array.Clear(yArray[CurrentBuffer], 0, 7200);
                    Array.Clear(angleArray[CurrentBuffer], 0, 7200);
                    Array.Clear(RSSIArray[CurrentBuffer], 0, 7200);
                    ScanPointCount[CurrentBuffer] = 0;
                    DataPointIndex = 0;
                }
                else   //解析扫描数据
                {
                    if (m_ScanDataContent.ScanContent[83] == 0x00) //距离
                    {
                        for (int num = 0; num < 600; num++)
                        {
                            EachPointAngle = SpliceFrame.PointNum * SpliceFrame.ResolutionAngle;
                            EachPointDist = (Single)((m_ScanDataContent.ScanContent[86 + num * 2] << 8) + m_ScanDataContent.ScanContent[86 + num * 2 + 1]);

                            EachPointAngle = EachPointAngle + (Single)(Math.Atan2(25, EachPointDist) / Math.PI * 180);
                            EachPointDist = EachPointDist / 1000;

                            if (ScanInfoM.ValidFlag != 0)
                            {
                                EachPointAngle = EachPointAngle + ScanInfoM.SelfAngle;
                                xArray[CurrentBuffer][SpliceFrame.PointNum] = (Single)(EachPointDist * Math.Cos((Math.PI / 180) * EachPointAngle)) + LaserPose[2] / 1000;
                                yArray[CurrentBuffer][SpliceFrame.PointNum] = (Single)(EachPointDist * Math.Sin((Math.PI / 180) * EachPointAngle)) + LaserPose[3] / 1000;
                            }
                            else
                            {
                                xArray[CurrentBuffer][SpliceFrame.PointNum] = (Single)(EachPointDist * Math.Cos((Math.PI / 180) * EachPointAngle));
                                yArray[CurrentBuffer][SpliceFrame.PointNum] = (Single)(EachPointDist * Math.Sin((Math.PI / 180) * EachPointAngle));
                            }
                            zArray[CurrentBuffer][SpliceFrame.PointNum] = 1;
                            SpliceFrame.PointNum++;
                            //统计功能
                            if (Statistical.PointSequenceNum == SpliceFrame.PointNum)  //SpliceFrame.PointNum的第1个点序号为0，Statistical.PointSequenceNum的第1个点序号为1
                            {
                                TempPointDist = Convert.ToInt32(EachPointDist * 1000);
                                Statistical.CurrentStatisticalCount++;
                                Statistical.StatisticalDistSum += TempPointDist;
                                if (Statistical.StatisticalMIN > TempPointDist)
                                {
                                    Statistical.StatisticalMIN = TempPointDist;
                                }
                                else if (Statistical.StatisticalMAX < TempPointDist)
                                {
                                    Statistical.StatisticalMAX = TempPointDist;
                                }

                                //显示统计数据
                                if (Statistical.CurrentStatisticalCount % Statistical.StatisticalFrameCount == 0)
                                {
                                    this.ScanInfoM.StatisticalMAX = Statistical.StatisticalMAX;
                                    this.ScanInfoM.StatisticalMIN = Statistical.StatisticalMIN;
                                    Statistical.StatisticalAVG = Statistical.StatisticalDistSum / Statistical.CurrentStatisticalCount;
                                    this.ScanInfoM.StatisticalAVG = Statistical.StatisticalAVG;

                                    Statistical.StatisticalMAX = 0;
                                    Statistical.StatisticalMIN = 999999;
                                    Statistical.StatisticalAVG = 0;
                                    Statistical.StatisticalDistSum = 0;
                                    Statistical.CurrentStatisticalCount = 0;
                                }
                            }
                        }
                        ScanPointCount[CurrentBuffer] = ScanPointCount[CurrentBuffer] + 600;
                    }
                    else if (m_ScanDataContent.ScanContent[83] == 0x01)  //角度
                    {
                        if ((SpliceFrame.PointNum == 3600 && SpliceFrame.ResolutionAngle == 0.1f) ||
                            (SpliceFrame.PointNum == 7200 && SpliceFrame.ResolutionAngle == 0.05f))
                        {
                            DataPointIndex = 0;
                        }

                        for (int num = 0; num < 1200; num++)
                        {
                            angleArray[CurrentBuffer][DataPointIndex] = (Single)(((SByte)m_ScanDataContent.ScanContent[86 + num]) * 10 + DataPointIndex * SpliceFrame.ResolutionAngle * 1000) / 1000;
                            DataPointIndex++;
                            SpliceFrame.PointNum++;
                        }
                    }
                    else if (m_ScanDataContent.ScanContent[83] == 0x02)  //反射率
                    {
                        if ((SpliceFrame.PointNum == 3600 && SpliceFrame.ResolutionAngle == 0.1f) ||
                            SpliceFrame.PointNum == 7200 ||
                            (SpliceFrame.PointNum == 14400 && SpliceFrame.ResolutionAngle == 0.05f))
                        {
                            DataPointIndex = 0;
                        }

                        for (int num = 0; num < 1200; num++)
                        {
                            RSSIArray[CurrentBuffer][DataPointIndex] = (Single)(m_ScanDataContent.ScanContent[86 + num]);
                            DataPointIndex++;
                            SpliceFrame.PointNum++;
                        }

                        if ((DataPointIndex == 3600 && SpliceFrame.ResolutionAngle == 0.1f) ||
                            (DataPointIndex == 7200 && SpliceFrame.ResolutionAngle == 0.05f))
                        {
                            SpliceFrame.RSSIFlag = true;
                        }
                    }
                }
                SpliceFrame.PacketNum++;
            }
            else
            {
                return;
            }

            //若靶标读取&数据读取完成
            if ((SpliceFrame.LandMarkFlag == true) && (SpliceFrame.PointNum == TotalPointForJudge))
            {
                //调用显示&初始化
                if (FlagDisplay == false && (DisplayPause == false))
                {
                    InvokeCurrentBuffer = CurrentBuffer;
                    CurrentRSSIFlag = SpliceFrame.RSSIFlag;
                    FlagDisplay = true;
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int SelectLMID = -1;
                        SelectLMID = AddCloudPointArray(xArray[InvokeCurrentBuffer], yArray[InvokeCurrentBuffer], zArray[InvokeCurrentBuffer], LandmarkData, LaserPose, CurrentRSSIFlag, RSSIArray[InvokeCurrentBuffer], ScanPointCount[InvokeCurrentBuffer]);
                        if (SelectLMID != -1)
                        {
                            this.ScanInfoM.CurrentSelectItem = this.ScanInfoM.MarkInfoMenu.ToList().Find(i => i.MarkNo == SelectLMID);
                            if (this.ScanInfoM.CurrentSelectItem != null)
                            {
                                SelectionChangedCommandExecute(this.ScanInfoM.CurrentSelectItem);
                                this.ScanInfoM.myDataGrid.ScrollIntoView(this.ScanInfoM.CurrentSelectItem);
                            }
                        }
                        FlagDisplay = false;
                    }));
                }

                InitializeVariable();
                CurrentBuffer++;
                if (CurrentBuffer >= 100)
                {
                    CurrentBuffer = CurrentBuffer % 100;
                }
            }
        }

        //生产扫描数据接收
        private void ProScanDataShowCommand(ScanDataContent m_ScanDataContent)
        {
            Single EachPointAngle = 0.00f;
            Single EachPointDist = 0.00f;
            int TempPointDist = 0;
            int TempFrameSequenceNum = m_ScanDataContent.ScanContent[81];   //帧序号，0-249
            int TempPacketNum = m_ScanDataContent.ScanContent[82];        //一帧的包号

            //新一帧数据
            if (SpliceFrame.FrameSequenceNum != TempFrameSequenceNum)
            {
                int TempPacketTotalCount = 0;
                //0:7200点，0.05° 12包; 1:3600点，0.1° 6包
                if (m_ScanDataContent.ScanContent[80] == 0x00)
                {
                    TempPacketTotalCount = 12;
                    TotalPointForJudge = 7200;
                }
                else if (m_ScanDataContent.ScanContent[80] == 0x01)
                {
                    TempPacketTotalCount = 6;
                    TotalPointForJudge = 3600;
                }

                SpliceFrame.FrameSequenceNum = TempFrameSequenceNum;
                SpliceFrame.PacketTotalCount = TempPacketTotalCount;
                SpliceFrame.PacketNum = 0;
                SpliceFrame.PointNum = 0;
                if (m_ScanDataContent.ScanContent[80] == 0)
                {
                    SpliceFrame.ResolutionAngle = 0.05f;
                }
                else if (m_ScanDataContent.ScanContent[80] == 1)
                {
                    SpliceFrame.ResolutionAngle = 0.1f;
                }
            }

            if (SpliceFrame.PacketNum == TempPacketNum)
            {
                if (TempPacketNum == 0)
                {
                    LaserPose[0] = 1;
                    LaserPose[1] = 0; //角度
                    LaserPose[2] = 0; //X
                    LaserPose[3] = 0; //Y

                    Array.Clear(xArray[CurrentBuffer], 0, 7200);
                    Array.Clear(yArray[CurrentBuffer], 0, 7200);
                    ScanPointCount[CurrentBuffer] = 0;
                    DataPointIndex = 0;
                }
                for (int num = 0; num < 600; num++)
                {
                    EachPointAngle = SpliceFrame.PointNum * SpliceFrame.ResolutionAngle;
                    EachPointDist = (Single)((m_ScanDataContent.ScanContent[86 + num * 2] << 8) + m_ScanDataContent.ScanContent[86 + num * 2 + 1]);

                    EachPointAngle = EachPointAngle + (Single)(Math.Atan2(25, EachPointDist) / Math.PI * 180);
                    EachPointDist = EachPointDist / 1000;

                    xArray[CurrentBuffer][SpliceFrame.PointNum] = (Single)(EachPointDist * Math.Cos((Math.PI / 180) * EachPointAngle));
                    yArray[CurrentBuffer][SpliceFrame.PointNum] = (Single)(EachPointDist * Math.Sin((Math.PI / 180) * EachPointAngle));

                    SpliceFrame.PointNum++;
                    //统计功能
                    if (Statistical.PointSequenceNum == SpliceFrame.PointNum)
                    {
                        TempPointDist = Convert.ToInt32(EachPointDist * 1000);
                        Statistical.CurrentStatisticalCount++;
                        Statistical.StatisticalDistSum += TempPointDist;
                        if (Statistical.StatisticalMIN > TempPointDist)
                        {
                            Statistical.StatisticalMIN = TempPointDist;
                        }
                        else if (Statistical.StatisticalMAX < TempPointDist)
                        {
                            Statistical.StatisticalMAX = TempPointDist;
                        }

                        //显示统计数据
                        if (Statistical.CurrentStatisticalCount % Statistical.StatisticalFrameCount == 0)
                        {
                            this.ScanInfoM.StatisticalMAX = Statistical.StatisticalMAX;
                            this.ScanInfoM.StatisticalMIN = Statistical.StatisticalMIN;
                            Statistical.StatisticalAVG = Statistical.StatisticalDistSum / Statistical.CurrentStatisticalCount;
                            this.ScanInfoM.StatisticalAVG = Statistical.StatisticalAVG;
                            //初始化
                            Statistical.StatisticalMAX = 0;
                            Statistical.StatisticalMIN = 999999;
                            Statistical.StatisticalAVG = 0;
                            Statistical.StatisticalDistSum = 0;
                            Statistical.CurrentStatisticalCount = 0;
                        }
                    }
                }
                ScanPointCount[CurrentBuffer] = ScanPointCount[CurrentBuffer] + 600;
                SpliceFrame.PacketNum++;
            }
            else
            {
                return;
            }

            //数据读取完成
            if ((SpliceFrame.PointNum == TotalPointForJudge))
            {
                //调用显示&初始化
                if (FlagDisplay == false && (DisplayPause == false))
                {
                    InvokeCurrentBuffer = CurrentBuffer;
                    FlagDisplay = true;
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AddScanPointArray(xArray[InvokeCurrentBuffer], yArray[InvokeCurrentBuffer], LaserPose, ScanPointCount[InvokeCurrentBuffer]);
                        FlagDisplay = false;
                    }));
                }

                InitializeVariable();
                CurrentBuffer++;
                if (CurrentBuffer >= 100)
                {
                    CurrentBuffer = CurrentBuffer % 100;
                }
            }
        }

        private void LandmarkTypeCommand(LandmarkTypeInfo m_LandmarkTypeInfo)
        {
            LandmarkData[1] = m_LandmarkTypeInfo.LandmarkTypeName;
        }

        private void PCLDisplayTypeCommand(PCLDisplayTypeContent m_PCLDisplayTypeContent)
        {
            if (m_PCLDisplayTypeContent.PCLDisplayTypeName == "恢复显示")
            {
                DisplayPause = false;
            }
            else if (m_PCLDisplayTypeContent.PCLDisplayTypeName == "暂停显示")
            {
                DisplayPause = true;
            }
        }

        private void SpeedSetCommandExecute()
        {
            byte[] SendTemp = new byte[8];
            //X方向速度
            SendTemp[0] = (byte)((this.ScanInfoM.SpeedX >> 8) & 0x00ff);
            SendTemp[1] = (byte)(this.ScanInfoM.SpeedX & 0x00ff);
            //Y方向速度
            SendTemp[2] = (byte)((this.ScanInfoM.SpeedY >> 8) & 0x00ff);
            SendTemp[3] = (byte)(this.ScanInfoM.SpeedY & 0x00ff);
            //角速度
            SendTemp[4] = (byte)((this.ScanInfoM.SpeedAngle >> 24) & 0x00ff);
            SendTemp[5] = (byte)((this.ScanInfoM.SpeedAngle >> 16) & 0x00ff);
            SendTemp[6] = (byte)((this.ScanInfoM.SpeedAngle >> 8) & 0x00ff);
            SendTemp[7] = (byte)(this.ScanInfoM.SpeedAngle & 0x00ff);
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "速度参数设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregator.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }


        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.ScanInfoM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }

        private void WorkModeViewChangeCommand(WorkModeViewContent m_WorkModeViewContent)
        {
            if (m_WorkModeViewContent.WorkModeViewName == "靶标获取模式")
            {
                this.ScanInfoM.BtIsEnable = false;
            }
            else if (m_WorkModeViewContent.WorkModeViewName == "连接后空模式")
            {

            }
            else
            {
                if (m_WorkModeViewContent.WorkModeViewName == "靶标探测模式" || (m_WorkModeViewContent.WorkModeViewName == "生产扫描模式"))
                {
                    this.ScanInfoM.ValidFlag = 0;
                    this.ScanInfoM.SelfAngle = 0;
                    this.ScanInfoM.SelfX = 0;
                    this.ScanInfoM.SelfY = 0;
                }
                this.ScanInfoM.BtIsEnable = true;
            }
        }

        int SelectedNO = -1;
        //选中一个靶标时的操作，标红显示
        private void SelectionChangedCommandExecute(object para)
        {
            MarkInfo item = (MarkInfo)para;
            int LMNO;
            if (item == null)
            {
                LMNO = -1;
            }
            else
            {
                LMNO = item.MarkNo;
            }
            if (SelectedNO == LMNO)
            {
                return;
            }
            else
            {
                SelectedNO = LMNO;
                if (this.ScanInfoM.BtConnectTXT == "停止连续波形")
                {
                        RedLMDisplay(0, LMNO);
                }
                else
                {
                        RedLMDisplay(1, LMNO);
                }
            }
        }

        private void DisnetInitCommand(DisnetInitContent m_DisnetInitContent)
        {
            this.ScanInfoM.BtIsEnable = false;
        }

    }
}
