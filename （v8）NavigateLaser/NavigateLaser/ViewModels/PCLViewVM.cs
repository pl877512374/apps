using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Practices.ServiceLocation;
using NavigateLaser.Views;
using System.Windows;

namespace NavigateLaser.ViewModels
{
    class PCLViewVM : NotificationObject
    {
        public DelegateCommand XForwardViewCommand { get; set; }
        public DelegateCommand XNegativeViewCommand { get; set; }
        public DelegateCommand YForwardViewCommand { get; set; }
        public DelegateCommand YNegativeViewCommand { get; set; }
        public DelegateCommand ZForwardViewCommand { get; set; }
        public DelegateCommand ZNegativeViewCommand { get; set; }

        public DelegateCommand EZNegativeViewCommand { get; set; }
        public DelegateCommand SetWorkMode01Command { get; set; }
        public DelegateCommand SetWorkMode0203Command { get; set; }
        public DelegateCommand SetWorkMode04Command { get; set; }
        public DelegateCommand PointDisplayCommand { get; set; }
        public DelegateCommand ScaleGridDisplayCommand { get; set; }
        public DelegateCommand RotationZCommand { get; set; }
        public DelegateCommand SetWorkModeSCCommand { get; set; }

        protected IEventAggregator eventAggregatorSetWorkMode;
        protected IEventAggregator eventAggregatorDisplayType;
        protected IEventAggregator eventAggregatorUpdatePose;
        protected SubscriptionToken tokenUpdatePose;
        protected IEventAggregator eventAggregatorCurrentState;
        protected SubscriptionToken tokenCurrentState;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;
        protected IEventAggregator eventAggregatorUpdateBtnColor;
        protected SubscriptionToken tokenUpdateBtnColor;

        int[] PoseInfoFormSet = new int[3];  //记录当前最后获取的位置信息，用于工作模式设置使用

        private PCLViewM m_PCLViewM;
        public PCLViewM PCLViewM
        {
            get { return m_PCLViewM; }
            set
            {
                m_PCLViewM = value;
                this.RaisePropertyChanged("PCLViewM");
            }
        }

        public PCLViewVM()
        {
            this.LoadPCLViewM();
            this.XForwardViewCommand = new DelegateCommand(new Action(this.XForwardViewCommandExecute));
            this.XNegativeViewCommand = new DelegateCommand(new Action(this.XNegativeViewCommandExecute));
            this.YForwardViewCommand = new DelegateCommand(new Action(this.YForwardViewCommandExecute));
            this.YNegativeViewCommand = new DelegateCommand(new Action(this.YNegativeViewCommandExecute));
            this.ZForwardViewCommand = new DelegateCommand(new Action(this.ZForwardViewCommandExecute));
            this.ZNegativeViewCommand = new DelegateCommand(new Action(this.ZNegativeViewCommandExecute));
            this.EZNegativeViewCommand = new DelegateCommand(new Action(this.EZNegativeViewCommandExecute));
            this.SetWorkMode01Command = new DelegateCommand(new Action(this.SetWorkMode01CommandExecute));
            this.SetWorkMode0203Command = new DelegateCommand(new Action(this.SetWorkMode0203CommandExecute));
            this.SetWorkMode04Command = new DelegateCommand(new Action(this.SetWorkMode04CommandExecute));
            this.PointDisplayCommand = new DelegateCommand(new Action(this.PointDisplayCommandExecute));
            this.ScaleGridDisplayCommand = new DelegateCommand(new Action(this.ScaleGridDisplayCommandExecute));
            this.RotationZCommand = new DelegateCommand(new Action(this.RotationZCommandExecute));
            this.SetWorkModeSCCommand = new DelegateCommand(new Action(this.SetWorkModeSCCommandExecute));

            this.eventAggregatorSetWorkMode = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorDisplayType = ServiceLocator.Current.GetInstance<IEventAggregator>();
            eventAggregatorUpdatePose = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenUpdatePose = eventAggregatorUpdatePose.GetEvent<UpdatePoseEvent>().Subscribe(UpdatePoseEventExecute);
            eventAggregatorCurrentState = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenCurrentState = eventAggregatorCurrentState.GetEvent<TCPSendSucceedEvent>().Subscribe(BtAuthorityChangeEvent); 
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);
            eventAggregatorUpdateBtnColor = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenUpdateBtnColor = eventAggregatorCurrentState.GetEvent<BtnColorChangeEvent>().Subscribe(UpdateBtnColorCommand);
        }

        private void LoadPCLViewM()
        {
            this.PCLViewM = new PCLViewM();
            this.PCLViewM.BtPointDisplayTXT = "线";
            this.PCLViewM.BtIsEnable = true;
            this.PCLViewM.IsEnable = true;
        }

        private void UpdatePoseEventExecute(UpdatePoseContent m_UpdatePoseContent)
        {
            PoseInfoFormSet[0] = m_UpdatePoseContent.PoseInfo.ElementAt(0);
            PoseInfoFormSet[1] = m_UpdatePoseContent.PoseInfo.ElementAt(1);
            PoseInfoFormSet[2] = m_UpdatePoseContent.PoseInfo.ElementAt(2);
        }

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void setViewTo(int axis, int sign);    //axis:0表示X轴，1表示Y轴，1表示Z轴

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void EqualProportionProjection();

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ChangePointDisplay(bool flagPointDisplay);    //flagPointDisplay：0表示不画点，1表示画点；

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ChangeLineDisplay(bool flagLineDisplay);    //flagLineDisplay：0表示不划线，1表示划线；

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DisplayScaleGrid();    //显示尺标

        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RotationViewTo();     //旋转

        private void XForwardViewCommandExecute()
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            setViewTo(0, 1);
        }

        private void XNegativeViewCommandExecute()
        {
            setViewTo(0, -1);
        }

        private void YForwardViewCommandExecute()
        {
            setViewTo(1, 1);
        }

        private void YNegativeViewCommandExecute()
        {
            setViewTo(1, -1);
        }

        private void ZForwardViewCommandExecute()
        {
            setViewTo(2, 1);  
        }

        private void ZNegativeViewCommandExecute()
        {
            setViewTo(2, -1);
        }

        private void EZNegativeViewCommandExecute()
        {
            EqualProportionProjection();
        }
        public void RotationZCommandExecute()
        {
            RotationViewTo(); 
        }

        private void PointDisplayCommandExecute()
        {
            Thread Thread = new Thread(PCLDisplaySwitchThread);
            Thread.Start();
        }

        private void PCLDisplaySwitchThread()
        {
            PCLDisplayTypeContent m_PCLDisplayTypeContent = new PCLDisplayTypeContent() { PCLDisplayTypeName = "暂停显示" };
            this.eventAggregatorDisplayType.GetEvent<PCLDisplayTypeEvent>().Publish(m_PCLDisplayTypeContent);
            Thread.Sleep(1000);
            if (this.PCLViewM.BtPointDisplayTXT == "点")
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ChangePointDisplay(true);
                    }));
                this.PCLViewM.BtPointDisplayTXT = "线";
            }
            else if (this.PCLViewM.BtPointDisplayTXT == "线")
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ChangePointDisplay(false);
                }));
                this.PCLViewM.BtPointDisplayTXT = "点";
            }
            Thread.Sleep(1000);
            m_PCLDisplayTypeContent.PCLDisplayTypeName = "恢复显示" ;
            this.eventAggregatorDisplayType.GetEvent<PCLDisplayTypeEvent>().Publish(m_PCLDisplayTypeContent);
        }

        private void ScaleGridDisplayCommandExecute()
        {
            DisplayScaleGrid();
        }

        private void SetWorkMode01CommandExecute()
        {
            Mode01ParamView Mode01ParamWin = new Mode01ParamView();

            Mode01ParamWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Mode01ParamWin.Owner = Application.Current.MainWindow;
            Mode01ParamWin.ShowDialog();

            if (Mode01ParamWin.Mode01SetFlag == true)
            {
                TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标探测模式" };
                m_TCPSendCommandName.SendContent = Mode01ParamWin.Mode01ParamData;
                this.eventAggregatorSetWorkMode.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            }
            this.PCLViewM.BtnScanbgc = "#00000000";
            this.PCLViewM.BtnDetectbgc = "Aqua";
            this.PCLViewM.BtnGetbgc = "#00000000";
            this.PCLViewM.BtnNavibgc = "#00000000";
        }

        private void SetWorkMode0203CommandExecute()
        {
            Mode0203ParamView Mode0203ParamWin = new Mode0203ParamView(PoseInfoFormSet);

            Mode0203ParamWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Mode0203ParamWin.Owner = Application.Current.MainWindow;
            Mode0203ParamWin.ShowDialog();

            if (Mode0203ParamWin.Mode0203SetFlag == true)
            {
                TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标获取模式" };
                m_TCPSendCommandName.SendContent = Mode0203ParamWin.Mode0203ParamData;
                this.eventAggregatorSetWorkMode.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            }
            this.PCLViewM.BtnScanbgc = "#00000000";
            this.PCLViewM.BtnDetectbgc = "#00000000";
            this.PCLViewM.BtnGetbgc = "Aqua";
            this.PCLViewM.BtnNavibgc = "#00000000";
        }

        private void SetWorkMode04CommandExecute()
        {
            Mode04ParamView Mode04ParamWin = new Mode04ParamView();

            Mode04ParamWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Mode04ParamWin.Owner = Application.Current.MainWindow;
            Mode04ParamWin.ShowDialog();

            if (Mode04ParamWin.Mode04SetFlag == true)
            {
                TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "导航模式" };
                m_TCPSendCommandName.SendContent = Mode04ParamWin.Mode04ParamData;
                this.eventAggregatorSetWorkMode.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            }
            this.PCLViewM.BtnScanbgc = "#00000000";
            this.PCLViewM.BtnDetectbgc = "#00000000";
            this.PCLViewM.BtnGetbgc = "#00000000";
            this.PCLViewM.BtnNavibgc = "Aqua";
        }
        public static bool f_SetWorkModeScan = false;//设置距离探测标志位
        //扫描模式，发送生产版指令，切换为出生产版距离波形
        private void SetWorkModeSCCommandExecute()
        {
            f_SetWorkModeScan = true;
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "生产扫描模式" };
            this.eventAggregatorSetWorkMode.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            this.PCLViewM.BtnScanbgc = "Aqua";
            this.PCLViewM.BtnDetectbgc = "#00000000";
            this.PCLViewM.BtnGetbgc = "#00000000";
            this.PCLViewM.BtnNavibgc = "#00000000";
        }

        private void BtAuthorityChangeEvent(TCPSendSucceedName m_TCPSendSucceedName)
        {
            if (m_TCPSendSucceedName.SendSucceedName == "获取波形指令")
            {
                this.PCLViewM.BtIsEnable = false;
            }
            else if (m_TCPSendSucceedName.SendSucceedName == "停止波形指令")
            {
                this.PCLViewM.BtIsEnable = true;
            }
        }

        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.PCLViewM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }
        private void UpdateBtnColorCommand(string m_signal)
        {
            if(m_signal=="距离探测")
            {
                this.PCLViewM.BtnScanbgc = "Aqua";
                this.PCLViewM.BtnDetectbgc = "#00000000";
                this.PCLViewM.BtnGetbgc = "#00000000";
                this.PCLViewM.BtnNavibgc = "#00000000";
            }
            else if(m_signal=="靶标探测")
            {
                this.PCLViewM.BtnScanbgc = "#00000000";
                this.PCLViewM.BtnDetectbgc = "Aqua";
                this.PCLViewM.BtnGetbgc = "#00000000";
                this.PCLViewM.BtnNavibgc = "#00000000";
            }
            else if(m_signal=="靶标获取")
            {
                this.PCLViewM.BtnScanbgc = "#00000000";
                this.PCLViewM.BtnDetectbgc = "#00000000";
                this.PCLViewM.BtnGetbgc = "Aqua";
                this.PCLViewM.BtnNavibgc = "#00000000";
            }
            else if (m_signal == "导航")
            {
                this.PCLViewM.BtnScanbgc = "#00000000";
                this.PCLViewM.BtnDetectbgc = "#00000000";
                this.PCLViewM.BtnGetbgc = "#00000000";
                this.PCLViewM.BtnNavibgc = "Aqua";
            }
        }
    }
}
