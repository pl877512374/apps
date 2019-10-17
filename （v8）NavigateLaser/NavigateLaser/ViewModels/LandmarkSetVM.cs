using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Commands;
using System.Collections.ObjectModel;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using NavigateLaser.DataAccess;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;
using System.Threading;
using System.Windows;
using System.Collections;
using System.Windows.Media;
using System.Windows.Threading;

namespace NavigateLaser.ViewModels
{
    class LandmarkSetVM : NotificationObject
    {
        public DelegateCommand<object> SelectMenuItemCommand { get; set; }    //增加靶标
        public DelegateCommand SendLandmarkCommand { get; set; }      //发送靶标设置
        public DelegateCommand GetLandmarkInfoCommand { get; set; }   //发送获取靶标信息
        public DelegateCommand<object> DeleteCommand { get; set; }   //删除靶标
        public DelegateCommand ImportLandmarkCommand { get; set; }      //从文件导入靶标信息
        public DelegateCommand ExportLandmarkCommand { get; set; }   //导出靶标信息到文件
        public DelegateCommand<object> SelectionChangedCommand { get; set; }  //选中一个现实标记一个
        public DelegateCommand<object> BeginningEditCommand { get; set; }  //编辑前记录当前行信息
        public DelegateCommand<object> CellEditEndingCommand { get; set; }  //编辑后查看变更信息
        public DelegateCommand<object> LayerSelectionChangedCommand { get; set; }  //选中层信息
        public DelegateCommand RotationLMCommand { get; set; }  //靶标旋转
        public DelegateCommand TranslationLMCommand { get; set; }  //靶标平移
        public DelegateCommand<object> ShapeChangeCommand { get; set; }    //形状变化后，改变坐标

        protected IEventAggregator eventAggregatorSend;
        protected IEventAggregator eventAggregatorLMType;
        protected IEventAggregator eventAggregatorOperationComplete;
        protected IEventAggregator eventAggregatorMappingMode;
        protected IEventAggregator eventAggregatorViewIsEnable;

        protected IEventAggregator eventAggregatorMapping;
        protected SubscriptionToken tokenMapping;
        protected IEventAggregator eventAggregatorLMSetView;
        protected SubscriptionToken tokenLMSetView;
        protected IEventAggregator eventAggregatorLMSQRecv;
        protected SubscriptionToken tokenLMSQRecv;
        protected IEventAggregator eventAggregatorMappingModeState;
        protected SubscriptionToken tokenMappingModeState;
        protected IEventAggregator eventAggregatorRecViewIsEnable;
        protected SubscriptionToken tokenRecViewIsEnable;
        protected IEventAggregator eventAggregatorNaviQuery;
        protected SubscriptionToken tokenNaviQuery;
        protected IEventAggregator eventAggregatorWorkModeViewChange;
        protected SubscriptionToken tokenWorkModeViewChange;

        public List<LandmarkSetInfo> LandmarkInfoList = new List<LandmarkSetInfo>();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DelSetSelectedRun(int x, int y);
        bool SelectedChangedShow = true;//单击靶标列表是是否触动靶标变红色事件标志位


        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DealClickedSetLM(DelSetSelectedRun selectedCallBack);

        public void GetSetSelectedLMCallBack(int x, int y)//pl
        {
            int LMID = -1;
            double minDis = 100000;
            int Size = this.LandmarkSetM.LandmarkMenu.Count;
            if (Size > 1 &&!ScanInfoVM.f_ContinueWave)//f_ContinueWave为true表示在获取连续波形绘图，此时就不需要进行以下操作，否则会使绘制波形卡死
            {
                for (int i = 0; i < Size; i++)
                {
                    int x_temp = this.LandmarkSetM.LandmarkMenu.ElementAt(i).LandmarkX;
                    int y_temp = this.LandmarkSetM.LandmarkMenu.ElementAt(i).LandmarkY;
                    if (x >= x_temp - 600 && x <= x_temp + 600 && y >= y_temp - 600 && y <= y_temp + 600)
                    {
                        double dis_temp = Math.Sqrt(((x_temp - x) * (x_temp - x) + (y_temp - y) * (y_temp - y)));
                        if (dis_temp < minDis)
                        {
                            minDis = dis_temp;
                            LMID = this.LandmarkSetM.LandmarkMenu.ElementAt(i).No;
                        }
                    }
                }
            }
            if (LMID != -1)
            {
                SelectedChangedShow = false;
                this.LandmarkSetM.CurrentSelectItem = this.LandmarkSetM.LandmarkMenu.ToList().Find(i => i.No == LMID);
                if (this.LandmarkSetM.CurrentSelectItem != null)
                {
                    this.LandmarkSetM.myDataGrid.Dispatcher.Invoke(new Action(delegate
                            {
                                if (oldSelectedItem != null)
                                {
                                    oldSelectedItem.IsBtEnable = false;
                                    oldSelectedItem.IsCylinderCheckEnable = false;
                                }
                                oldSelectedItem = this.LandmarkSetM.CurrentSelectItem;
                                CurrentSelectedItem = this.LandmarkSetM.CurrentSelectItem.No;//这样的话删除判断的时候可以通过
                                this.LandmarkSetM.CurrentSelectItem.IsBtEnable = true;//使删除按钮可以操作
                                this.LandmarkSetM.CurrentSelectItem.IsCylinderCheckEnable = true;//使圆柱复选框可以操作
                                this.LandmarkSetM.myDataGrid.ScrollIntoView(this.LandmarkSetM.CurrentSelectItem);//使表格中选中行出现在视野
                                RedSetLMDisplay(LMID);
                            }
                            )
                        );
                }
            }
            else
            {
                //this.LandmarkSetM.CurrentSelectItem = null;
            }
        }
        private void FuncGetSelLM()
        {
            while (true)
            {
                DealClickedSetLM(GetSetSelectedLMCallBack);
                Thread.Sleep(100);
            }
        }
        private struct _LMStatistical
        {
            public int ReceiveNum;   //接收次数
            public int LMNum;   //靶标个数
            public Single AngleResolution;   //角度分辨率
            public List<int> StatisticalCount;   //统计次数
            public List<Single> StatisticalAngle;   //统计的角度
            public List<Single> StatisticalDist;    //统计的距离
        };
        _LMStatistical LMStatistical;

        public void InitializeLMStatistical()
        {
            LMStatistical.ReceiveNum = 0;
            LMStatistical.LMNum = 0;
            LMStatistical.AngleResolution = 0;
            LMStatistical.StatisticalCount.Clear();
            LMStatistical.StatisticalAngle.Clear();
            LMStatistical.StatisticalDist.Clear();
        }

        private LandmarkSetM m_LandmarkSetM;
        public LandmarkSetM LandmarkSetM
        {
            get { return m_LandmarkSetM; }
            set
            {
                m_LandmarkSetM = value;
                this.RaisePropertyChanged("LandmarkSetM");
            }
        }

        public LandmarkSetVM(System.Windows.Controls.DataGrid DataGridVieW)
        {
            LMStatistical.StatisticalCount = new List<int>();   //统计次数
            LMStatistical.StatisticalAngle = new List<Single>();   //统计的角度
            LMStatistical.StatisticalDist = new List<Single>();    //统计的距离

            this.LoadLandmarkSetM(DataGridVieW);
            this.SelectMenuItemCommand = new DelegateCommand<object>(new Action<object>(this.SelectMenuItemCommandExecute));
            this.SendLandmarkCommand = new DelegateCommand(new Action(this.SendLandmarkCommandExecute));
            this.GetLandmarkInfoCommand = new DelegateCommand(new Action(this.GetLandmarkInfoCommandExecute));
            this.DeleteCommand = new DelegateCommand<object>(new Action<object>(this.DeleteCommandExecute));
            this.ImportLandmarkCommand = new DelegateCommand(new Action(this.ImportLandmarkCommandExecute));
            this.ExportLandmarkCommand = new DelegateCommand(new Action(this.ExportLandmarkCommandExecute));
            this.SelectionChangedCommand = new DelegateCommand<object>(new Action<object>(this.SelectionChangedCommandExecute));
            this.BeginningEditCommand = new DelegateCommand<object>(new Action<object>(this.BeginningEditCommandExecute));
            this.CellEditEndingCommand = new DelegateCommand<object>(new Action<object>(this.CellEditEndingCommandExecute));
            this.LayerSelectionChangedCommand = new DelegateCommand<object>(new Action<object>(this.LayerSelectionChangedCommandExecute));
            this.RotationLMCommand = new DelegateCommand(new Action(this.RotationLMCommandExecute));
            this.TranslationLMCommand = new DelegateCommand(new Action(this.TranslationLMCommandExecute));
            this.ShapeChangeCommand = new DelegateCommand<object>(new Action<object>(this.ShapeChangeCommandExecute));

            this.eventAggregatorSend = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorLMType = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorOperationComplete = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorMappingMode = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            eventAggregatorMapping = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenMapping = eventAggregatorMapping.GetEvent<MappingDataShowEvent>().Subscribe(MappingLMInfoEvent);
            eventAggregatorLMSetView = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenLMSetView = eventAggregatorLMSetView.GetEvent<TCPSendSucceedEvent>().Subscribe(ViewAuthorityChangeEvent);

            eventAggregatorLMSQRecv = ServiceLocator.Current.GetInstance<IEventAggregator>(); ;           //接收靶标设置和查询的回复
            tokenLMSQRecv = eventAggregatorLMSQRecv.GetEvent<LandmarkResponseEvent>().Subscribe(LandmarkSQResponseEvent);      //订阅事件实例

            eventAggregatorMappingModeState = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenMappingModeState = eventAggregatorMappingModeState.GetEvent<MappingWorkModeEvent>().Subscribe(MappingModeStateEvent);
            this.eventAggregatorRecViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenRecViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);
            this.eventAggregatorNaviQuery = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenNaviQuery = eventAggregatorNaviQuery.GetEvent<NaviQueryEvent>().Subscribe(NaviQueryCommand);
            this.eventAggregatorWorkModeViewChange = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenWorkModeViewChange = eventAggregatorWorkModeViewChange.GetEvent<WorkModeViewEvent>().Subscribe(WorkModeViewChangeCommand);

            Thread th_GetSelectedLM = new Thread(FuncGetSelLM);
            th_GetSelectedLM.IsBackground = true;
            th_GetSelectedLM.Start();
        }

        private void LoadLandmarkSetM(System.Windows.Controls.DataGrid DataGridVieW)
        {
            this.LandmarkSetM = new LandmarkSetM();

            this.LandmarkSetM.myDataGrid = DataGridVieW;

            this.LandmarkSetM.LandmarkMenu = new ObservableCollection<LandmarkSetInfo>();

            AddRowAtMenuEnd();

            this.LandmarkSetM.Landmarkshapes = new ObservableCollection<string>()
            {
                {"圆柱体"},
                //{"正方体"},
                {"面"},
            };

            this.LandmarkSetM.SelectedLandmarkshape = "圆柱体";
            this.LandmarkSetM.LandmarkSize = 80;
            this.LandmarkSetM.IsEnable = true;
            this.LandmarkSetM.IsDGEnable = true;

            this.LandmarkSetM.StatisticsLayerTotal = 0;
            this.LandmarkSetM.StatisticsLMTotal = 0;
            this.LandmarkSetM.RotationAngle = 0;
            this.LandmarkSetM.RotationX = 0;
            this.LandmarkSetM.RotationY = 0;
            this.LandmarkSetM.TranslationX = 0;
            this.LandmarkSetM.TranslationY = 0;
        }

        private void dataGridDisplayChanged()//pl0808注释
        {
            //for (int i = 0; i < this.LandmarkSetM.LandmarkMenu.Count; i++)
            //{
            //    if (this.LandmarkSetM.LandmarkMenu.ElementAt(i).isChangeColor == true)
            //    {
            //        //改变颜色
            //        var row = this.LandmarkSetM.myDataGrid.ItemContainerGenerator.ContainerFromItem(this.LandmarkSetM.myDataGrid.Items[i]) as DataGridRow;
            //        if (row == null) return;
            //        //row.Background = new SolidColorBrush(Colors.Orange);
            //    }
            //    else
            //    {
            //        //改变颜色
            //        var row = this.LandmarkSetM.myDataGrid.ItemContainerGenerator.ContainerFromItem(this.LandmarkSetM.myDataGrid.Items[i]) as DataGridRow;
            //        if (row == null) return;
            //        row.Background = new SolidColorBrush(Colors.White);
            //    }
            //}
        }

        LandmarkSetInfo LastNewRowLMInfo = new LandmarkSetInfo();    //记录最后一个未选中的新行
        //在列表末尾显示一个未选中的空行
        private void AddRowAtMenuEnd()
        {
            List<LandmarkSetInfo> TempMenu = LandmarkInfoList.OrderBy(u => u.No).ToList();
            int i = 0;
            for (; i < TempMenu.Count; i++)
            {
                if (i != TempMenu.ElementAt(i).No)
                {
                    break;
                }
            }
            LandmarkSetInfo item = new LandmarkSetInfo();
            item.No = i;
            item.LayerID = 0;
            item.IsCyclinder = true;
            item.LandmarkShapeSize = 80;
            item.IsBtEnable = false;
            item.IsCylinderCheckEnable = false;
            item.IsSelectedCheckEnable = false;
            item.isChangeColor = false;   //无需颜色
            LastNewRowLMInfo = item;
            CurrentSelectedItem = item.No;
            this.LandmarkSetM.LandmarkMenu.Add(item);
        }

        int cdcdcd = 0;
        private void DeleteCommandExecute(object para)
        {
            if (CurrentSelectedItem == (int)para)
            {
                this.LandmarkSetM.IsDGEnable = false;

                LandmarkInfoList.Remove(LandmarkInfoList.Find(i => i.No == (int)para));
                DeleteOneSetLMDisplay((int)para);
                this.LandmarkSetM.LandmarkMenu.Remove(this.LandmarkSetM.LandmarkMenu.ToList().Find(i => i.No == (int)para));
                dataGridDisplayChanged();


                //LMLayerInfoStatistics(this.LandmarkSetM.LandmarkMenu.Where(i => i.IsSelected == true).ToList());
                LMLayerInfoStatistics();
                this.LandmarkSetM.IsDGEnable = true;
            }
            CurrentSelectedItem = -1;
        }

        private void SelectMenuItemCommandExecute(object para)
        {
            LandmarkSetInfo SelectedItem = (LandmarkSetInfo)para;
            if (LastNewRowLMInfo.No == SelectedItem.No)
            {
                SelectedItem.IsBtEnable = true;
                SelectedItem.IsCylinderCheckEnable = true;
                SelectedItem.IsSelectedCheckEnable = true;
                LandmarkInfoList.Add(SelectedItem);
                AddOneSetLMDisplay(SelectedItem.LandmarkX, SelectedItem.LandmarkY, SelectedItem.No);
                AddRowAtMenuEnd();
            }

            LMLayerInfoStatistics();
        }

        private void ShapeChangeCommandExecute(object para)    //形状变化后，变化坐标
        {
            LandmarkSetInfo SelectedItem = (LandmarkSetInfo)para;
            if (CurrentSelectedItem == SelectedItem.No)
            {
                this.LandmarkSetM.IsDGEnable = false;
                Double TempAngle = Math.Atan2(SelectedItem.LandmarkY, SelectedItem.LandmarkX);
                Double SizeChange = 0;
                if (SelectedItem.IsCyclinder == true)     //由平面变圆形
                {
                    SizeChange = SelectedItem.LandmarkShapeSize / 2.00;
                    SelectedItem.LandmarkX = Convert.ToInt32(SizeChange * Math.Cos(TempAngle)) + SelectedItem.LandmarkX;
                    SelectedItem.LandmarkY = Convert.ToInt32(SizeChange * Math.Sin(TempAngle)) + SelectedItem.LandmarkY;
                    AddOneSetLMDisplay(SelectedItem.LandmarkX, SelectedItem.LandmarkY, SelectedItem.No);
                }
                else
                {
                    SizeChange = -SelectedItem.LandmarkShapeSize / 2.00;
                    SelectedItem.LandmarkX = Convert.ToInt32(SizeChange * Math.Cos(TempAngle)) + SelectedItem.LandmarkX;
                    SelectedItem.LandmarkY = Convert.ToInt32(SizeChange * Math.Sin(TempAngle)) + SelectedItem.LandmarkY;
                    AddOneSetLMDisplay(SelectedItem.LandmarkX, SelectedItem.LandmarkY, SelectedItem.No);
                }
                this.LandmarkSetM.IsDGEnable = true;
            }
        }

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DisplayLMSetArray(int[] x, int[] y, Single[] Angle, int[] LaserPose, int LMCount, int LMType);

        //显示当前设置或者要设置的靶标，传入的数据要求按照ID升序排序
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLMArrayDisplay(int[] x, int[] y, int[] LMID, int[] BlueColorFlag, int LMCount, int SetLMKind, bool f_LayerChange = false);

        //添加or修改，显示当前设置或者要设置的靶标
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddOneSetLMDisplay(int x, int y, int LMID);

        //删除，显示当前设置或者要设置的靶标
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteOneSetLMDisplay(int LMID);

        //标红，显示当前设置或者要设置的靶标
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RedSetLMDisplay(int LMID);

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearLMInfo();
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearSetLMInfo();
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void EqualProportionProjectionByXY(double x, double y);

        EventWaitHandle EventLMSend = new AutoResetEvent(false);
        int CurrentPackageNum = 0;
        int TotalPackage = 0;

        private void SendLandmarkCommandExecute()
        {
            ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = false };
            this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
            Thread th = new Thread(new ThreadStart(ThreadSendLandmark));
            th.Start();
        }

        int ResendCount = 0;
        private void ThreadSendLandmark()
        {
            int temp = 0;
            int tempData = 0;
            int LMListIndex = 0;
            int CurrentPackageLMCount = 0;
            ResendCount = 0;

            List<LandmarkSetInfo> ListLandmarkInfo = this.LandmarkSetM.LandmarkMenu.Where(i => i.IsSelected == true).ToList();
            ListLandmarkInfo = ListLandmarkInfo.OrderBy(u => u.LayerID).ThenBy(u => u.No).ToList();
            byte[] SendTemp = new byte[1402];
            if (ListLandmarkInfo.Count() % 100 == 0)
            {
                if (ListLandmarkInfo.Count() == 0)
                {
                    TotalPackage = 2;
                }
                else
                {
                    TotalPackage = ListLandmarkInfo.Count() / 100 + 1;
                }
            }
            else
            {
                TotalPackage = ListLandmarkInfo.Count() / 100 + 2;
            }

            CurrentPackageNum = 0;
            //第一包当前包号
            SendTemp[temp++] = 0x00;
            SendTemp[temp++] = 0x00;
            //第一包靶标总个数
            SendTemp[temp++] = (byte)((ListLandmarkInfo.Count() >> 8) & 0x00ff);
            SendTemp[temp++] = (byte)(ListLandmarkInfo.Count() & 0x00ff);
            //第一包中靶标总包数
            SendTemp[temp++] = (byte)((TotalPackage >> 8) & 0x00ff);
            SendTemp[temp++] = (byte)(TotalPackage & 0x00ff);
            //第一包发送，包括重发
            while (ResendCount < 5)
            {
                TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标设置指令" };
                m_TCPSendCommandName.SendContent = SendTemp.Take(temp).ToList();
                this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
                if (EventLMSend.WaitOne(2000, false))
                {
                    break;
                }
                else
                {
                    ResendCount++;
                }
            }
            if (ResendCount == 5)
            {
                ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "靶标设置失败";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                return;
            }
            //其他包发送
            for (CurrentPackageNum = 1; CurrentPackageNum < TotalPackage; CurrentPackageNum++)
            {
                temp = 0;
                SendTemp[temp++] = (byte)((CurrentPackageNum >> 8) & 0x00ff);
                SendTemp[temp++] = (byte)(CurrentPackageNum & 0x00ff);

                if (CurrentPackageNum == (TotalPackage - 1))
                {
                    CurrentPackageLMCount = ListLandmarkInfo.Count() - 100 * (CurrentPackageNum - 1);
                }
                else
                {
                    CurrentPackageLMCount = 100;
                }


                for (int j = 0; j < CurrentPackageLMCount; j++)
                {
                    //ID号
                    tempData = ListLandmarkInfo.ElementAt(LMListIndex + j).No;
                    SendTemp[temp++] = (byte)((tempData >> 8) & 0x00ff);
                    SendTemp[temp++] = (byte)(tempData & 0x00ff);
                    //X坐标
                    tempData = ListLandmarkInfo.ElementAt(LMListIndex + j).LandmarkX;
                    SendTemp[temp++] = (byte)((tempData >> 24) & 0x00ff);
                    SendTemp[temp++] = (byte)((tempData >> 16) & 0x00ff);
                    SendTemp[temp++] = (byte)((tempData >> 8) & 0x00ff);
                    SendTemp[temp++] = (byte)(tempData & 0x00ff);
                    //Y坐标
                    tempData = ListLandmarkInfo.ElementAt(LMListIndex + j).LandmarkY;
                    SendTemp[temp++] = (byte)((tempData >> 24) & 0x00ff);
                    SendTemp[temp++] = (byte)((tempData >> 16) & 0x00ff);
                    SendTemp[temp++] = (byte)((tempData >> 8) & 0x00ff);
                    SendTemp[temp++] = (byte)(tempData & 0x00ff);
                    //层号
                    tempData = ListLandmarkInfo.ElementAt(LMListIndex + j).LayerID;
                    SendTemp[temp++] = (byte)((tempData >> 8) & 0x00ff);
                    SendTemp[temp++] = (byte)(tempData & 0x00ff);
                    //形状
                    if (ListLandmarkInfo.ElementAt(LMListIndex + j).IsCyclinder == true)
                    {
                        tempData = 0;
                    }
                    else
                    {
                        tempData = 2;
                    }
                    SendTemp[temp++] = (byte)(tempData & 0x00ff);
                    //尺寸
                    tempData = ListLandmarkInfo.ElementAt(LMListIndex + j).LandmarkShapeSize;
                    SendTemp[temp++] = (byte)(tempData & 0x00ff);
                }
                //对于无靶标的情况，需要把内容位用0填满
                if (CurrentPackageLMCount == 0)
                {
                    SendTemp[temp++] = 0x00;
                    SendTemp[temp++] = 0x00;
                }

                ResendCount = 0;
                LMListIndex = LMListIndex + CurrentPackageLMCount;
                while (ResendCount < 5)
                {
                    TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标设置指令" };
                    m_TCPSendCommandName.SendContent = SendTemp.Take(temp).ToList();
                    this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
                    if (EventLMSend.WaitOne(2000, false))
                    {
                        break;
                    }
                    else
                    {
                        ResendCount++;
                    }
                }
                if (ResendCount == 5)
                {
                    ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                    this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                    OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                    m_OperationCompleteContent.NoticeContent = "靶标设置失败";
                    this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                    return;
                }
            }
        }

        List<LandmarkSetInfo> QueryLandmarkRecvMenu = new List<LandmarkSetInfo>();   //用于记录当前查询到的靶标信息
        int LMQueryTotal = 0;   //靶标总个数
        private void LandmarkSQResponseEvent(LandmarkResponseContent m_LandmarkResponseContent)
        {
            byte[] SendTemp = new byte[4];
            int index = 0;
            int temp = 0;
            int tempCurrent = 0;
            if (m_LandmarkResponseContent.LandmarkResponseName == "靶标设置回复")
            {
                if (m_LandmarkResponseContent.LandmarkResponseData[29] == 0x01)
                {
                    temp = (m_LandmarkResponseContent.LandmarkResponseData[26] << 8) + m_LandmarkResponseContent.LandmarkResponseData[27];
                    if (CurrentPackageNum == 0)
                    {
                        tempCurrent = TotalPackage;
                    }
                    else
                    {
                        tempCurrent = CurrentPackageNum;
                    }
                    if (temp == tempCurrent)
                    {
                        EventLMSend.Set();
                        if (temp == (TotalPackage - 1))
                        {

                            for (int i = 0; i < LandmarkInfoList.Count; i++)
                            {
                                if (LandmarkInfoList.ElementAt(i).isChangeColor == true)
                                {
                                    LandmarkInfoList.ElementAt(i).isChangeColor = false;
                                }
                            }
                            for (int i = 0; i < this.LandmarkSetM.LandmarkMenu.Count; i++)
                            {
                                if (this.LandmarkSetM.LandmarkMenu.ElementAt(i).isChangeColor == true)
                                {
                                    this.LandmarkSetM.LandmarkMenu.ElementAt(i).isChangeColor = false;
                                }
                            }

                            //发布Mapping开始改变可操作属性事件
                            ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                            this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                            OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                            m_OperationCompleteContent.NoticeContent = "靶标设置成功";
                            this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                        }
                        else
                        {
                            OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                            m_OperationCompleteContent.NoticeContent = "靶标信息设置中，完成第 " + CurrentPackageNum + " 包数据，请稍等！";
                            this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                        }
                    }
                }
                else
                {
                    //设置失败，等待重发
                }
            }
            else if (m_LandmarkResponseContent.LandmarkResponseName == "靶标查询开始发送失败")
            {
                if (NaviQueryFalg == false)
                {
                    RecvPackageWaitFlag = false;
                }
            }
            else if (m_LandmarkResponseContent.LandmarkResponseName == "靶标设置发送失败")
            {
                ResendCount = 4;
            }
            else if (m_LandmarkResponseContent.LandmarkResponseName == "靶标查询开始回复")
            {
                if (NaviQueryFalg == false)
                {
                    ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = false };
                    this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                    WaitTimeCount = 0;
                    Thread th = new Thread(new ThreadStart(ThreadRecvPackageWait));
                    th.Start();
                }
            }
            else if (m_LandmarkResponseContent.LandmarkResponseName == "靶标查询回复")
            {
                if (RecvPackageWaitFlag == true)
                {
                    temp = (m_LandmarkResponseContent.LandmarkResponseData[26] << 8) + m_LandmarkResponseContent.LandmarkResponseData[27];
                    if (temp == 0)
                    {
                        QueryLandmarkRecvMenu.Clear();
                        CurrentPackageNum = 1;
                        LMQueryTotal = (m_LandmarkResponseContent.LandmarkResponseData[28] << 8) + m_LandmarkResponseContent.LandmarkResponseData[29];
                        TotalPackage = (m_LandmarkResponseContent.LandmarkResponseData[30] << 8) + m_LandmarkResponseContent.LandmarkResponseData[31];
                        //发送接收成功回复
                        SendTemp[index++] = (byte)((TotalPackage >> 8) & 0x00ff);
                        SendTemp[index++] = (byte)(TotalPackage & 0x00ff);
                        SendTemp[index++] = 0x00;
                        SendTemp[index++] = 0x01;

                        TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标信息查询其它包" };
                        m_TCPSendCommandName.SendContent = SendTemp.Take(index).ToList();
                        this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
                        OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                        m_OperationCompleteContent.NoticeContent = "靶标信息查询中，请稍等！";
                        this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);

                        lock (Locker)
                        {
                            WaitTimeCount = 0;
                        }
                    }
                    else if (temp == CurrentPackageNum)
                    {
                        int CurrentPackageLMTotal = 0;    //当前包中的靶标总数
                        if (temp == (TotalPackage - 1))
                        {
                            CurrentPackageLMTotal = LMQueryTotal - (temp - 1) * 100;
                        }
                        else
                        {
                            CurrentPackageLMTotal = 100;
                        }
                        //存数据，并将当前包号+1
                        for (int i = 0; i < CurrentPackageLMTotal; i++)
                        {
                            LandmarkSetInfo item = new LandmarkSetInfo();
                            item.No = (m_LandmarkResponseContent.LandmarkResponseData[28 + i * 14] << 8) + m_LandmarkResponseContent.LandmarkResponseData[29 + i * 14];
                            item.LandmarkX = (int)((m_LandmarkResponseContent.LandmarkResponseData[30 + i * 14] << 24) + (m_LandmarkResponseContent.LandmarkResponseData[31 + i * 14] << 16) +
                                                  (m_LandmarkResponseContent.LandmarkResponseData[32 + i * 14] << 8) + m_LandmarkResponseContent.LandmarkResponseData[33 + i * 14]);
                            item.LandmarkY = (int)((m_LandmarkResponseContent.LandmarkResponseData[34 + i * 14] << 24) + (m_LandmarkResponseContent.LandmarkResponseData[35 + i * 14] << 16) +
                                                  (m_LandmarkResponseContent.LandmarkResponseData[36 + i * 14] << 8) + m_LandmarkResponseContent.LandmarkResponseData[37 + i * 14]);
                            item.LayerID = (m_LandmarkResponseContent.LandmarkResponseData[38 + i * 14] << 8) + m_LandmarkResponseContent.LandmarkResponseData[39 + i * 14];
                            if (m_LandmarkResponseContent.LandmarkResponseData[40 + i * 14] == 0x02)
                            {
                                item.IsCyclinder = false;
                            }
                            else
                            {
                                item.IsCyclinder = true;
                            }
                            item.LandmarkShapeSize = m_LandmarkResponseContent.LandmarkResponseData[41 + i * 14];
                            item.IsSelected = true;
                            //item.IsBtEnable = true;
                            item.isChangeColor = false;
                            QueryLandmarkRecvMenu.Add(item);
                        }
                        //若是最后一包，则进行显示
                        if (temp == (TotalPackage - 1))
                        {
                            RecvPackageWaitFlag = false;
                            if (QueryLandmarkRecvMenu.Count == 0)
                            {
                                LandmarkInfoList.Clear();
                                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    ClearSetLMInfo();
                                    ClearLMInfo();
                                    this.LandmarkSetM.LandmarkMenu.Clear();
                                    LMLayerInfoStatistics();
                                    AddRowAtMenuEnd();
                                }));
                            }
                            else
                            {
                                ObservableCollection<LandmarkSetInfo> TempLandmarkMenu = new ObservableCollection<LandmarkSetInfo>();
                                QueryLandmarkRecvMenu.OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
                                LandmarkInfoList = TempLandmarkMenu.ToList();
                                if (NaviQueryFalg == true)
                                {
                                    TempLandmarkMenu.Clear();
                                    LandmarkInfoList.Where(i => i.LayerID == UserLayer).OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
                                }

                                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    LMLayerInfoStatistics();
                                    SetLMArrayDisplay(TempLandmarkMenu.Select(i => i.LandmarkX).ToArray(), TempLandmarkMenu.Select(i => i.LandmarkY).ToArray(), TempLandmarkMenu.Select(i => i.No).ToArray(), TempLandmarkMenu.Select(i => i.flagBlueColor).ToArray(), TempLandmarkMenu.Count, 1);
                                    this.LandmarkSetM.LandmarkMenu = TempLandmarkMenu;
                                    AddRowAtMenuEnd();
                                    dataGridDisplayChanged();
                                }));
                            }
                        }

                        //发送接收成功回复
                        SendTemp[index++] = (byte)((temp >> 8) & 0x00ff);
                        SendTemp[index++] = (byte)(temp & 0x00ff);
                        //第一包靶标总个数
                        SendTemp[index++] = 0x00;
                        SendTemp[index++] = 0x01;

                        TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标信息查询其它包" };
                        m_TCPSendCommandName.SendContent = SendTemp.Take(index).ToList();
                        this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
                        OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                        if (temp == (TotalPackage - 1))
                        {
                            if (NaviQueryFalg == true)
                            {
                                m_OperationCompleteContent.NoticeContent = "导航工作模式设置成功！";
                                NaviQueryFalg = false;
                            }
                            else
                            {
                                m_OperationCompleteContent.NoticeContent = "靶标信息查询成功！";
                            }
                            ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                            this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                        }
                        else
                        {
                            if (NaviQueryFalg == true)
                            {
                                m_OperationCompleteContent.NoticeContent = "导航前的靶标信息查询显示中，请稍等！";
                            }
                            else
                            {
                                m_OperationCompleteContent.NoticeContent = "靶标信息查询中，请稍等！";
                            }
                        }
                        this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);

                        lock (Locker)
                        {
                            WaitTimeCount = 0;
                        }
                        CurrentPackageNum++;
                    }
                    else
                    {
                        //发送接收错误
                        SendTemp[index++] = (byte)((temp >> 8) & 0x00ff);
                        SendTemp[index++] = (byte)(temp & 0x00ff);
                        //第一包靶标总个数
                        SendTemp[index++] = 0x00;
                        SendTemp[index++] = 0x00;
                        TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标信息查询其它包" };
                        m_TCPSendCommandName.SendContent = SendTemp.Take(index).ToList();
                        this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
                    }
                }
            }
        }

        int WaitTimeCount = 0;  //等待时间计数，每包每次等待1秒
        bool RecvPackageWaitFlag = false;
        private static readonly object Locker = new object();  //加锁
        #region 靶标信息查询 发送首包和每包计时
        private void GetLandmarkInfoCommandExecute()
        {
            if (NaviQueryFalg == false)
            {
                RecvPackageWaitFlag = true;
            }
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标信息查询首包" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        int UserLayer = -1;
        bool NaviQueryFalg = false;   //导航前查询靶标信息标志
        private void NaviQueryCommand(NaviQueryContent m_NaviQueryContent)
        {
            if (m_NaviQueryContent.NaviQueryName == "导航查询靶标初始化")
            {
                NaviQueryFalg = true;
                UserLayer = m_NaviQueryContent.LayerID;
                ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = false };
                this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);

                RecvPackageWaitFlag = true;
                WaitTimeCount = 0;
                Thread th = new Thread(new ThreadStart(ThreadRecvPackageWait));
                th.Start();
            }
            else if (m_NaviQueryContent.NaviQueryName == "导航查询靶标开始")
            {
                lock (Locker)
                {
                    WaitTimeCount = 0;
                }
                GetLandmarkInfoCommandExecute();
            }
        }

        private void ThreadRecvPackageWait()
        {
            while (RecvPackageWaitFlag)
            {
                lock (Locker)
                {
                    if (WaitTimeCount > 3)
                    {
                        RecvPackageWaitFlag = false;
                        OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                        if (NaviQueryFalg == true)
                        {
                            m_OperationCompleteContent.NoticeContent = "导航前的靶标查询失败，建议先尝试手动查询靶标！";
                            NaviQueryFalg = false;
                        }
                        else
                        {
                            m_OperationCompleteContent.NoticeContent = "靶标信息查询失败，请重新查询！";
                        }
                        this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                        ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                        this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                    }
                    else
                    {
                        WaitTimeCount++;
                    }
                }
                Thread.Sleep(2000);
            }
        }
        #endregion

        //去重
        public static int IsRepeatHashSet(int[] array)
        {
            HashSet<int> hs = new HashSet<int>();
            for (int i = 0; i < array.Length; i++)
            {
                if (hs.Contains(array[i]))
                {
                    return array[i];
                }
                else
                {
                    hs.Add(array[i]);
                }
            }
            return -1;
        }


        #region  靶标导入导出功能
        private void ImportLandmarkCommandExecute()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "文本文件(*.lmk)|*.lmk|(*.txt)|*.txt";
            ArrayList List = new ArrayList();    //用于读取ID号，判断是否重复
            if (openFile.ShowDialog() == true)
            {
                using (StreamReader sr = new StreamReader(openFile.FileName, Encoding.Default))
                {
                    ObservableCollection<LandmarkSetInfo> TempLandmarkMenu = new ObservableCollection<LandmarkSetInfo>();
                    bool ReadFlag = false;    //开始读取数据
                    int ReadLineCount = 0;    //记录读取的靶标数
                    int ReadTotalCount = 0;    //读取记录的靶标总数
                    LandmarkInfoList.Clear();  //清空当前记录的靶标信息
                    while (sr.Peek() > 0)
                    {
                        string temp = sr.ReadLine();
                        string[] TempInfo = temp.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (ReadFlag == false)
                        {
                            if (TempInfo[0] == "#LayerID")
                            {
                                temp = sr.ReadLine();
                                TempInfo = temp.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                ReadTotalCount = ReadTotalCount + Convert.ToInt32(TempInfo[2]);
                                continue;
                            }
                            else if (TempInfo[0] == "globID")
                            {
                                ReadFlag = true;
                                continue;
                            }
                        }
                        else
                        {
                            //排除数据中有空行，或数据不足行
                            if (TempInfo.Count() < 7)
                            {
                                continue;
                            }
                            LandmarkSetInfo item = new LandmarkSetInfo();
                            item.No = Convert.ToInt32(TempInfo[0]);  //ReadLineCount + 1;
                            List.Add(Convert.ToInt32(TempInfo[0]));
                            item.LandmarkX = Convert.ToInt32(TempInfo[1]);
                            item.LandmarkY = Convert.ToInt32(TempInfo[2]);
                            if (Convert.ToInt32(TempInfo[4]) == 1)
                            {
                                item.IsCyclinder = false;
                            }
                            else if (Convert.ToInt32(TempInfo[4]) == 2)
                            {
                                item.IsCyclinder = true;
                            }
                            item.LandmarkShapeSize = Convert.ToInt32(TempInfo[5]);
                            item.LayerID = Convert.ToInt32(TempInfo[6]);
                            item.IsSelected = true;
                            //item.IsBtEnable = true;
                            item.isChangeColor = false;
                            LandmarkInfoList.Add(item);
                            ReadLineCount++;
                        }
                    }

                    if (ReadLineCount == 0)
                    {
                        //当无靶标数据时，信息界面还原初始状态
                        AddRowAtMenuEnd();
                        this.LandmarkSetM.LandmarkLayerInfoMenu.Clear();
                        this.LandmarkSetM.StatisticsLayerTotal = 0;
                        this.LandmarkSetM.StatisticsLMTotal = 0;
                    }
                    else
                    {
                        Int32[] values = new Int32[List.Count];
                        List.CopyTo(values);
                        int RepeatNum = IsRepeatHashSet(values);
                        if (RepeatNum != -1)
                        {
                            AddRowAtMenuEnd();
                            this.LandmarkSetM.LandmarkLayerInfoMenu.Clear();
                            this.LandmarkSetM.StatisticsLayerTotal = 0;
                            this.LandmarkSetM.StatisticsLMTotal = 0;
                            OperationCompleteContent m_OperationCompleteContentTemp = new OperationCompleteContent();
                            m_OperationCompleteContentTemp.NoticeContent = "靶标导入错误，ID: " + RepeatNum.ToString() + " 重复";
                            this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContentTemp);
                            return;
                        }

                        LMLayerInfoStatistics();
                        LandmarkInfoList.OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
                        SetLMArrayDisplay(TempLandmarkMenu.Select(i => i.LandmarkX).ToArray(), TempLandmarkMenu.Select(i => i.LandmarkY).ToArray(), TempLandmarkMenu.Select(i => i.No).ToArray(), TempLandmarkMenu.Select(i => i.flagBlueColor).ToArray(), TempLandmarkMenu.Count, 1);
                        this.LandmarkSetM.LandmarkMenu = TempLandmarkMenu;
                        AddRowAtMenuEnd();
                        dataGridDisplayChanged();
                    }
                    OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                    m_OperationCompleteContent.NoticeContent = "靶标导入完成,共导入" + ReadLineCount.ToString() + "个靶标";
                    this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                }

            }
        }

        private void ExportLandmarkCommandExecute()
        {
            string MsgLine = "";
            SaveFileDialog sf = new SaveFileDialog();
            sf.Title = "Save Files";
            sf.DefaultExt = "lmk";
            sf.Filter = "lmk files (*.lmk)|*.lmk|txt files (*.txt)|*.txt|All files (*.*)|*.*";
            sf.FilterIndex = 1;
            string DefaultName = "WANJILayout";
            DefaultName = DefaultName + DateTime.Now.ToString("yyyyMMdd");
            sf.FileName = DefaultName;
            sf.RestoreDirectory = true;
            if ((bool)sf.ShowDialog())
            {
                //读取数据，并排序
                List<LandmarkSetInfo> ListLandmarkInfo = LandmarkInfoList.Where(i => i.IsSelected == true).ToList();
                ListLandmarkInfo = ListLandmarkInfo.OrderBy(u => u.No).ToList();
                var TempLayerInfo = ListLandmarkInfo.Select(i => i.LayerID).OrderBy(i => i).GroupBy(i => i);

                using (FileStream fs = new FileStream(sf.FileName, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                    {
                        //参照SICK NAV350的文件开头信息
                        MsgLine = "#WANJI TECHNOLOGY";
                        sw.WriteLine(MsgLine);
                        MsgLine = "#NAV Layout data";
                        sw.WriteLine(MsgLine);
                        MsgLine = "#FileFormat: 1.0";
                        sw.WriteLine(MsgLine);
                        MsgLine = "#WLR-712";
                        sw.WriteLine(MsgLine);
                        MsgLine = "#Used Layers:";
                        sw.WriteLine(MsgLine);
                        MsgLine = "#LayerID #landmarks";
                        sw.WriteLine(MsgLine);
                        foreach (var item in TempLayerInfo)
                        {
                            MsgLine = "#  " + String.Format("{0:D3}", item.Key) + "    " + String.Format("{0,5}", item.Count());
                            sw.WriteLine(MsgLine);
                        }
                        MsgLine = "#";
                        sw.WriteLine(MsgLine);

                        //靶标数据
                        MsgLine = "globID     x[mm]     y[mm] type subtype size[mm] layer1 layer2 layer3";
                        sw.WriteLine(MsgLine);
                        string ShapeType = "";   //形状类型

                        for (int i = 0; i < ListLandmarkInfo.Count; i++)
                        {
                            MsgLine = String.Format("{0:D6}", ListLandmarkInfo.ElementAt(i).No) + String.Format("{0,10}", ListLandmarkInfo.ElementAt(i).LandmarkX) + String.Format("{0,10}", ListLandmarkInfo.ElementAt(i).LandmarkY);
                            if (ListLandmarkInfo.ElementAt(i).IsCyclinder == true)         //圆柱体
                            {
                                ShapeType = "    1       2";
                            }
                            else   //面
                            {
                                ShapeType = "    1       1";
                            }
                            //靶标尺寸
                            MsgLine = MsgLine + ShapeType + String.Format("{0,9}", ListLandmarkInfo.ElementAt(i).LandmarkShapeSize) + String.Format("{0,7}", ListLandmarkInfo.ElementAt(i).LayerID);
                            sw.WriteLine(MsgLine);
                        }
                        sw.Flush();
                        sw.Dispose();

                        OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                        m_OperationCompleteContent.NoticeContent = "靶标导出完成,共导出" + ListLandmarkInfo.Count().ToString() + "个靶标";
                        this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                    }
                }
            }
        }
        #endregion

        //靶标层信息统计，传入的值是所有的靶标
        private void LMLayerInfoStatistics()
        {
            List<LandmarkLayerInfo> TempLandmarkMenu = new List<LandmarkLayerInfo>();
            var TempLayerInfo = LandmarkInfoList.Select(i => i.LayerID).OrderBy(i => i).GroupBy(i => i);
            foreach (var item in TempLayerInfo)
            {
                LandmarkLayerInfo LayerItem = new LandmarkLayerInfo();
                LayerItem.ID = item.Key;
                LayerItem.LandmarkTotal = item.Count();
                TempLandmarkMenu.Add(LayerItem);
            }
            this.LandmarkSetM.StatisticsLayerTotal = TempLandmarkMenu.Count;
            this.LandmarkSetM.StatisticsLMTotal = LandmarkInfoList.Count;
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LandmarkSetM.LandmarkLayerInfoMenu = TempLandmarkMenu;
            }));
        }

        private void MappingLMInfoEvent(MappingDataContent m_MappingDataContent)
        {
            if (m_MappingDataContent.ReceiveCommandName == "获得Mapping数据")
            {
                if (MappingRunning == true)
                {
                    int TempTotalPackage = (m_MappingDataContent.MappingContent[26] << 8) + m_MappingDataContent.MappingContent[27];
                    int TempCurrentPackage = (m_MappingDataContent.MappingContent[28] << 8) + m_MappingDataContent.MappingContent[29];
                    if (TempTotalPackage == TempCurrentPackage)
                    {
                        MappingRunning = false;
                        ObservableCollection<LandmarkSetInfo> TempLandmarkMenu = new ObservableCollection<LandmarkSetInfo>();
                        List<LandmarkSetInfo> TempLandmarkInfoList = new List<LandmarkSetInfo>();
                        int index = 30;
                        int TempLandmarkTotal = 0;
                        if (m_MappingDataContent.MappingContent[index++] == 0x01)
                        {
                            TempLandmarkTotal = m_MappingDataContent.MappingContent[index++];
                            for (int i = 0; i < TempLandmarkTotal; i++)
                            {
                                LandmarkSetInfo item = new LandmarkSetInfo();
                                item.No = i;
                                item.LandmarkX = (m_MappingDataContent.MappingContent[33 + i * 20] << 24) + (m_MappingDataContent.MappingContent[34 + i * 20] << 16) +
                                                 (m_MappingDataContent.MappingContent[35 + i * 20] << 8) + m_MappingDataContent.MappingContent[36 + i * 20];
                                item.LandmarkY = (m_MappingDataContent.MappingContent[37 + i * 20] << 24) + (m_MappingDataContent.MappingContent[38 + i * 20] << 16) +
                                                 (m_MappingDataContent.MappingContent[39 + i * 20] << 8) + m_MappingDataContent.MappingContent[40 + i * 20];
                                item.LayerID = CurrentLayerID;
                                if (m_MappingDataContent.MappingContent[42 + i * 20] == 0x00)
                                {
                                    item.IsCyclinder = true;
                                }
                                else
                                {
                                    item.IsCyclinder = false;
                                }
                                item.IsSelected = true;
                                //item.IsBtEnable = true;
                               // item.isChangeColor = true;
                                item.LandmarkShapeSize = m_MappingDataContent.MappingContent[43 + i * 20];
                                TempLandmarkInfoList.Add(item);
                            }
                            //根据当前靶标获取模式，显示获取的靶标
                            if (CurrentMappingType == 1)
                            {
                                //正常靶标
                                LandmarkInfoList.Clear();
                                LandmarkInfoList = TempLandmarkInfoList;
                            }
                            else
                            {
                                if (TempLandmarkInfoList.Count != 0)
                                {
                                    //添加靶标
                                    for (int i = 0; i < LandmarkInfoList.Count; i++)
                                    {
                                        LandmarkInfoList.ElementAt(i).flagBlueColor = 0;
                                    }
                                    List<int> TempID = LandmarkInfoList.Select(i => i.No).OrderBy(i => i).ToList();
                                    int TempNo = 0;
                                    int j = 0;
                                    for (int i = 0; i < TempLandmarkInfoList.Count; i++)
                                    {
                                        for (; j < TempID.Count; TempNo++, j++)
                                        {
                                            if (TempNo == TempID.ElementAt(j))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        TempLandmarkInfoList.ElementAt(i).No = TempNo;
                                        TempLandmarkInfoList.ElementAt(i).flagBlueColor = 1;
                                        TempLandmarkInfoList.ElementAt(i).isChangeColor = true;
                                        TempNo++;
                                    }
                                    LandmarkInfoList.AddRange(TempLandmarkInfoList);
                                }
                            }
                            LMLayerInfoStatistics();
                            LandmarkInfoList.OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
                            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                SetLMArrayDisplay(TempLandmarkMenu.Select(i => i.LandmarkX).ToArray(), TempLandmarkMenu.Select(i => i.LandmarkY).ToArray(), TempLandmarkMenu.Select(i => i.No).ToArray(), TempLandmarkMenu.Select(i => i.flagBlueColor).ToArray(), TempLandmarkMenu.Count, CurrentMappingType);
                                this.LandmarkSetM.LandmarkMenu = TempLandmarkMenu;
                                AddRowAtMenuEnd();
                                dataGridDisplayChanged();
                            }));
                        }
                        else
                        {
                            if (CurrentMappingType == 1)
                            {
                                LandmarkInfoList.Clear();
                                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    ClearSetLMInfo();
                                    ClearLMInfo();
                                    LMLayerInfoStatistics();
                                    AddRowAtMenuEnd();
                                }));
                            }
                        }
                        if (Thread != null)
                        {
                            Thread.Abort();//如果得到了最后一包数据就退出靶标获取等待线程
                        }
                        ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                        this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                        OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                        m_OperationCompleteContent.NoticeContent = "已完成靶标获取模式，本次共得到 " + TempLandmarkInfoList.Count.ToString() + " 个靶标";
                        this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                    }
                    else
                    {
                        TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName();
                        m_TCPSendCommandName.SendCommandName = "Mapping过程中回复";
                        m_TCPSendCommandName.SendContent = new List<byte>();
                        m_TCPSendCommandName.SendContent.Add(m_MappingDataContent.MappingContent[28]);
                        m_TCPSendCommandName.SendContent.Add(m_MappingDataContent.MappingContent[29]);
                        m_TCPSendCommandName.SendContent.Add(0x00);
                        m_TCPSendCommandName.SendContent.Add(0x01);
                        this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
                    }
                }
            }
        }

        private void ViewAuthorityChangeEvent(TCPSendSucceedName m_TCPSendSucceedName)
        {
            if (m_TCPSendSucceedName.SendSucceedName == "获取波形指令")
            {
                this.LandmarkSetM.IsEnable = false;
            }
            else if (m_TCPSendSucceedName.SendSucceedName == "停止波形指令")
            {
                this.LandmarkSetM.IsEnable = true;
            }
        }

        //选中一个靶标时的操作，标红显示
        int CurrentSelectedItem = -1;
        LandmarkSetInfo oldSelectedItem;//上一次选中的item
        private void SelectionChangedCommandExecute(object para)
        {
            if (SelectedChangedShow)
            {
                this.LandmarkSetM.IsDGEnable = false;
                int LMID;
                LandmarkSetInfo item = null;
                if (para == null)
                {
                    LMID = -1;
                }
                else
                {
                    item = (LandmarkSetInfo)para;
                    item.IsBtEnable = true;
                    item.IsCylinderCheckEnable = true;
                    item.IsSelectedCheckEnable = true;
                    if (oldSelectedItem != null)
                    {
                        oldSelectedItem.IsBtEnable = false;
                        oldSelectedItem.IsCylinderCheckEnable = false;
                        oldSelectedItem.IsSelectedCheckEnable = false;
                    }
                    oldSelectedItem = item;
                    int ret = LandmarkInfoList.FindIndex(i => i.No == item.No);
                    if (ret == -1)
                    {
                        LMID = -1;
                    }
                    else
                    {
                        LMID = item.No;
                    }
                }

                if (CurrentSelectedItem != LMID)//&& SelectedChangedShow
                {
                    CurrentSelectedItem = LMID;
                    RedSetLMDisplay(LMID);
                    if (item != null)
                    {
                        //EqualProportionProjectionByXY((double)(item.LandmarkX / 1000.0), (double)(item.LandmarkY / 1000.0));
                    }
                }
                this.LandmarkSetM.IsDGEnable = true;
            }
            else
            {
                SelectedChangedShow = true;
            }
        }

        int BeforeEditLMLayer = 0;  //记录变更前的靶标信息
        int BeforeEditLMX = 0;
        int BeforeEditLMY = 0;
        int BeforeEditLMSize = 0;
        bool BeforeEditLMShape = false;

        private void BeginningEditCommandExecute(object para)
        {
            LandmarkSetInfo item = (LandmarkSetInfo)para;
            BeforeEditLMLayer = item.LayerID;
            BeforeEditLMX = item.LandmarkX;
            BeforeEditLMY = item.LandmarkY;
            BeforeEditLMSize = item.LandmarkShapeSize;
            BeforeEditLMShape = item.IsCyclinder;
        }

        private void CellEditEndingCommandExecute(object para)
        {
            LandmarkSetInfo item = (LandmarkSetInfo)para;
            if (BeforeEditLMLayer != item.LayerID)
            {
                LMLayerInfoStatistics();
            }
            else if (BeforeEditLMX != item.LandmarkX || (BeforeEditLMY != item.LandmarkY))   //坐标修改
            {
                AddOneSetLMDisplay(item.LandmarkX, item.LandmarkY, item.No);
            }
            else if (BeforeEditLMSize != item.LandmarkShapeSize && (item.IsCyclinder == true))
            {
                Double TempAngle = Math.Atan2(BeforeEditLMY, BeforeEditLMX);
                Double SizeChange = (item.LandmarkShapeSize - BeforeEditLMSize) / 2.0;
                item.LandmarkX = Convert.ToInt32(SizeChange * Math.Cos(TempAngle)) + item.LandmarkX;
                item.LandmarkY = Convert.ToInt32(SizeChange * Math.Sin(TempAngle)) + item.LandmarkY;
                AddOneSetLMDisplay(item.LandmarkX, item.LandmarkY, item.No);
            }
        }

        //层信息选中后的变更靶标信息显示
        private void LayerSelectionChangedCommandExecute(object para)
        {
            System.Collections.IList items = (System.Collections.IList)para;
            var collection = items.Cast<LandmarkLayerInfo>();
            var someModelList = collection.ToList();

            if (CurrentWorkMode == 3)
            {
                if (someModelList.FindIndex(i => i.ID == UserLayer) < 0)
                {
                    someModelList.Add(this.LandmarkSetM.LandmarkLayerInfoMenu.Find(i => i.ID == UserLayer));
                }
            }

            if (someModelList.Count == 0)
            {
                return;
            }
            else
            {
                ObservableCollection<LandmarkSetInfo> TempLandmarkMenu = new ObservableCollection<LandmarkSetInfo>();
                List<LandmarkSetInfo> TempLandmarkMenuList = new List<LandmarkSetInfo>();

                foreach (var item in someModelList)
                {
                    LandmarkLayerInfo TempItem = item;
                    TempLandmarkMenuList.AddRange(LandmarkInfoList.Where(i => i.LayerID == TempItem.ID).ToList());
                }
                TempLandmarkMenuList.OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
                SetLMArrayDisplay(TempLandmarkMenu.Select(i => i.LandmarkX).ToArray(), TempLandmarkMenu.Select(i => i.LandmarkY).ToArray(), TempLandmarkMenu.Select(i => i.No).ToArray(),TempLandmarkMenu.Select(i => i.flagBlueColor).ToArray(), TempLandmarkMenu.Count,2,true);
                TempLandmarkMenu.Add(LastNewRowLMInfo);
                this.LandmarkSetM.LandmarkMenu = TempLandmarkMenu;
                dataGridDisplayChanged();
            }
        }

        int CurrentMappingType = 0;   //1为正常；2为添加
        int CurrentLayerID = 0;   //层ID号
        bool MappingRunning = false;
        Thread Thread;
        private void MappingModeStateEvent(MappingWorkModeContent m_MappingWorkModeContent)
        {
            ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = false };
            this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);

            MappingRunning = true;
            CurrentMappingType = m_MappingWorkModeContent.MappingType;
            CurrentLayerID = m_MappingWorkModeContent.LayerID;

            Thread = new Thread(new ParameterizedThreadStart(MappingWaitThread));
            int WaitTime = m_MappingWorkModeContent.WaitTime;
            Thread.Start(WaitTime);
        }

        private void MappingWaitThread(object WaitTime)
        {
            int RealTime = 0;
            if ((int)WaitTime < 4000)
            {
                RealTime = 4000;
            }
            else
            {
                RealTime = (int)WaitTime;
            }
            Thread.Sleep(RealTime);
            //判断Mapping是否结束
            if (MappingRunning == true)
            {
                MappingRunning = false;
                ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = true };
                this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
                return;
            }
            else
            {
                return;
            }
        }

        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.LandmarkSetM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }

        int CurrentWorkMode = 0;   //当前设置的工作模式 1:靶标探测  2：mapping 3：导航
        private void WorkModeViewChangeCommand(WorkModeViewContent m_WorkModeViewContent)
        {
            if (m_WorkModeViewContent.WorkModeViewName == "导航模式")
            {
                CurrentWorkMode = 3;
                this.LandmarkSetM.IsDGEnable = false;
            }
            else if (m_WorkModeViewContent.WorkModeViewName == "连接后空模式")
            {
                this.LandmarkSetM.IsDGEnable = true;
            }
            else if (m_WorkModeViewContent.WorkModeViewName == "生产扫描模式")
            {
                CurrentWorkMode = 4;
                this.LandmarkSetM.IsDGEnable = true;
                Thread th = new Thread(new ThreadStart(LandmarkClearWork));
                th.Start();
            }
            else
            {
                CurrentWorkMode = 1;
                this.LandmarkSetM.IsDGEnable = true;
                if (m_WorkModeViewContent.WorkModeViewName == "靶标探测模式")
                {
                    Thread th = new Thread(new ThreadStart(LandmarkClearWork));
                    th.Start();
                }
            }
        }

        private void LandmarkClearWork()
        {
            Thread.Sleep(100);
            ViewIsEnableContent m_ViewIsEnableContent = new ViewIsEnableContent() { IsEnable = false };
            this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
            LandmarkInfoList.Clear();
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LandmarkSetM.LandmarkMenu.Clear();
                ClearSetLMInfo();
                ClearLMInfo();
                LMLayerInfoStatistics();
                AddRowAtMenuEnd();
            }));
            Thread.Sleep(500);
            m_ViewIsEnableContent.IsEnable = true;
            this.eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Publish(m_ViewIsEnableContent);
            OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
            if (CurrentWorkMode == 4)
            {
                m_OperationCompleteContent.NoticeContent = "距离探测设置成功！";
            }
            else
            {
                m_OperationCompleteContent.NoticeContent = "靶标探测工作模式设置成功！";
            }
            this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
        }

        //将当前显示的靶标进行旋转
        private void RotationLMCommandExecute()
        {
            ObservableCollection<LandmarkSetInfo> TempLandmarkMenu = new ObservableCollection<LandmarkSetInfo>();
            OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
            int beforeRoTransX = 0;
            int beforeRoTransY = 0;
            int afterRoTransX = 0;
            int afterRoTransY = 0;
            int RoAxisX = this.LandmarkSetM.RotationX;
            int RoAxisY = this.LandmarkSetM.RotationY;
            Single roAngle = 0.000f;
            if (LandmarkInfoList.Count == 0)
            {
                m_OperationCompleteContent.NoticeContent = "当前无靶标";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                return;
            }

            roAngle = Convert.ToSingle(this.LandmarkSetM.RotationAngle) / 1000;
            for (int i = 0; i < LandmarkInfoList.Count; i++)
            {
                beforeRoTransX = LandmarkInfoList.ElementAt(i).LandmarkX;
                beforeRoTransY = LandmarkInfoList.ElementAt(i).LandmarkY;
                //以旋转轴为原点的旋转         
                afterRoTransX = Convert.ToInt32((beforeRoTransX - RoAxisX) * Math.Cos((Math.PI / 180) * roAngle) - (beforeRoTransY - RoAxisY) * Math.Sin((Math.PI / 180) * roAngle));
                afterRoTransY = Convert.ToInt32((beforeRoTransX - RoAxisX) * Math.Sin((Math.PI / 180) * roAngle) + (beforeRoTransY - RoAxisY) * Math.Cos((Math.PI / 180) * roAngle));
                //平移到原坐标系原点
                LandmarkInfoList.ElementAt(i).LandmarkX = afterRoTransX + RoAxisX;
                LandmarkInfoList.ElementAt(i).LandmarkY = afterRoTransY + RoAxisY;
            }

            LandmarkInfoList.OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
            SetLMArrayDisplay(TempLandmarkMenu.Select(i => i.LandmarkX).ToArray(), TempLandmarkMenu.Select(i => i.LandmarkY).ToArray(), TempLandmarkMenu.Select(i => i.No).ToArray(),TempLandmarkMenu.Select(i => i.flagBlueColor).ToArray(), TempLandmarkMenu.Count, 1);
            this.LandmarkSetM.LandmarkMenu = TempLandmarkMenu;
            AddRowAtMenuEnd();
            m_OperationCompleteContent.NoticeContent = "靶标旋转完成";
            this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);

        }

        //将当前显示的靶标进行平移
        private void TranslationLMCommandExecute()
        {
            ObservableCollection<LandmarkSetInfo> TempLandmarkMenu = new ObservableCollection<LandmarkSetInfo>();
            OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();

            if (LandmarkInfoList.Count == 0)
            {
                m_OperationCompleteContent.NoticeContent = "当前无靶标";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                return;
            }

            for (int i = 0; i < LandmarkInfoList.Count; i++)
            {
                //平移
                LandmarkInfoList.ElementAt(i).LandmarkX = LandmarkInfoList.ElementAt(i).LandmarkX + this.LandmarkSetM.TranslationX;
                LandmarkInfoList.ElementAt(i).LandmarkY = LandmarkInfoList.ElementAt(i).LandmarkY + this.LandmarkSetM.TranslationY;
            }

            LandmarkInfoList.OrderBy(i => i.No).ToList().ForEach(x => TempLandmarkMenu.Add(x));
            SetLMArrayDisplay(TempLandmarkMenu.Select(i => i.LandmarkX).ToArray(), TempLandmarkMenu.Select(i => i.LandmarkY).ToArray(), TempLandmarkMenu.Select(i => i.No).ToArray(),TempLandmarkMenu.Select(i => i.flagBlueColor).ToArray(),TempLandmarkMenu.Count, 1);
            this.LandmarkSetM.LandmarkMenu = TempLandmarkMenu;
            AddRowAtMenuEnd();

            m_OperationCompleteContent.NoticeContent = "靶标平移完成";
            this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
        }


    }
}
