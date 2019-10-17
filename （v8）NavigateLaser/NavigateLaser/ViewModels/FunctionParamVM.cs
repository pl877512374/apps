using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;

namespace NavigateLaser.ViewModels
{
    class FunctionParamVM : NotificationObject
    {
        public DelegateCommand GetTHParamCommand { get; set; }    //获取靶标识别阈值参数
        public DelegateCommand SetTHParamCommand { get; set; }    //设置靶标识别阈值参数
        public DelegateCommand Get2LMlocationCommand { get; set; }      //获取2个靶标定位功能状态
        public DelegateCommand Set2LMlocationCommand { get; set; }      //设置2个靶标定位功能状态
        public DelegateCommand GetLMMatchRangeCommand { get; set; }      //获取靶标匹配范围参数
        public DelegateCommand SetLMMatchRangeCommand { get; set; }      //设置靶标匹配范围参数
        public DelegateCommand GetLMScanRangeCommand { get; set; }      //获取靶标扫描范围参数
        public DelegateCommand SetLMScanRangeCommand { get; set; }      //设置靶标扫描范围参数

        protected IEventAggregator eventAggregatorSend;
        protected IEventAggregator eventAggregatorOperationComplete;
        protected IEventAggregator eventAggregatorReceive;
        protected SubscriptionToken tokenReceive;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;

        private FunctionParamM m_FunctionParamM;
        public FunctionParamM FunctionParamM
        {
            get { return m_FunctionParamM; }
            set
            {
                m_FunctionParamM = value;
                this.RaisePropertyChanged("FunctionParamM");
            }
        }

        public FunctionParamVM()
        {
            this.LoadFunctionParamM();
            this.GetTHParamCommand = new DelegateCommand(new Action(this.GetTHParamCommandExecute));
            this.SetTHParamCommand = new DelegateCommand(new Action(this.SetTHParamCommandExecute));
            this.Get2LMlocationCommand = new DelegateCommand(new Action(this.Get2LMlocationCommandExecute));
            this.Set2LMlocationCommand = new DelegateCommand(new Action(this.Set2LMlocationCommandExecute));
            this.GetLMMatchRangeCommand = new DelegateCommand(new Action(this.GetLMMatchRangeCommandExecute));
            this.SetLMMatchRangeCommand = new DelegateCommand(new Action(this.SetLMMatchRangeCommandExecute));
            this.GetLMScanRangeCommand = new DelegateCommand(new Action(this.GetLMScanRangeCommandExecute));
            this.SetLMScanRangeCommand = new DelegateCommand(new Action(this.SetLMScanRangeCommandExecute));
            
            this.eventAggregatorSend = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorOperationComplete = ServiceLocator.Current.GetInstance<IEventAggregator>(); 
            eventAggregatorReceive = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenReceive = eventAggregatorReceive.GetEvent<FunctionDataShowEvent>().Subscribe(ReceiveFromLaserEvent);

            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);
        }

        private void LoadFunctionParamM()
        {
            this.FunctionParamM = new FunctionParamM();
            this.FunctionParamM.IsEnable = true;
            this.FunctionParamM.PointTH = new ObservableCollection<string>()
            {
                {"50"},
                {"60"},
                {"70"},
                {"80"},
                {"90"},
                {"100"},
            };

            this.FunctionParamM.ReflectTH = new ObservableCollection<string>()
            {
                {"50"},
                {"60"},
                {"70"},
                {"80"},
                {"90"},
                {"100"},
            };

            this.FunctionParamM.TwoLMlocation = new ObservableCollection<string>()
            {
                {"关闭"},
                {"开启"},
            };
        }

        private void GetTHParamCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标识别阈值查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void SetTHParamCommandExecute()
        {
            byte[] SendTemp = new byte[4];
            int pointTH = 0;
            int reflectTH = 0;
            pointTH = Convert.ToInt32(this.FunctionParamM.SelectedPointTH);
            reflectTH = Convert.ToInt32(this.FunctionParamM.SelectedReflectTH);

            if (pointTH > 100 || (pointTH < 50) || (reflectTH > 100) || (reflectTH < 50))
            {
                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "配置参数不合理!";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                return;
            }

            SendTemp[0] = (byte)(pointTH & 0x00ff);
            SendTemp[1] = (byte)(reflectTH & 0x00ff);
            SendTemp[2] = 0x00;
            SendTemp[3] = 0x00;

            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标识别阈值设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void Get2LMlocationCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "2靶标定位功能查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void Set2LMlocationCommandExecute()
        {
            byte[] SendTemp = new byte[4];
            if (this.FunctionParamM.SelectedTwoLMlocation == "关闭")
            {
                SendTemp[0] = 0x00;
            }
            else if (this.FunctionParamM.SelectedTwoLMlocation == "开启")
            {
                SendTemp[0] = 0x01;
            }
            else
            {
                return;
            }

            SendTemp[1] = 0x00;
            SendTemp[2] = 0x00;
            SendTemp[3] = 0x00;

            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "2靶标定位功能设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetLMMatchRangeCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标匹配范围查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void SetLMMatchRangeCommandExecute()
        {
            byte[] SendTemp = new byte[12];
            int tmp = 0;
            int tmp1 = 0;
            int tmp2 = 0;
            int tmp3 = 0;
            tmp = Convert.ToInt32(this.FunctionParamM.MatchRadiusMIN);
            SendTemp[0] = (byte)((tmp >> 8) & 0x00ff);
            SendTemp[1] = (byte)(tmp & 0x00ff);

            tmp1 = Convert.ToInt32(this.FunctionParamM.MatchRadiusMAX);
            SendTemp[2] = (byte)((tmp1 >> 8) & 0x00ff);
            SendTemp[3] = (byte)(tmp1 & 0x00ff);

            tmp2 = Convert.ToInt32(this.FunctionParamM.DetectionRangeMIN);
            SendTemp[4] = (byte)((tmp2 >> 24) & 0x00ff);
            SendTemp[5] = (byte)((tmp2 >> 16) & 0x00ff);
            SendTemp[6] = (byte)((tmp2 >> 8) & 0x00ff);
            SendTemp[7] = (byte)(tmp2 & 0x00ff);

            tmp3 = Convert.ToInt32(this.FunctionParamM.DetectionRangeMAX);
            SendTemp[8] = (byte)((tmp3 >> 24) & 0x00ff);
            SendTemp[9] = (byte)((tmp3 >> 16) & 0x00ff);
            SendTemp[10] = (byte)((tmp3 >> 8) & 0x00ff);
            SendTemp[11] = (byte)(tmp3 & 0x00ff);

            if (tmp < 100 || (tmp1 > 2000) || (tmp2 < 500) || (tmp3 > 65000))
            {
                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "配置参数不合理!";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                return;
            }

            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标匹配范围设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetLMScanRangeCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标扫描范围查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void SetLMScanRangeCommandExecute()
        {
            byte[] SendTemp = new byte[8];
            int tmp = 0;
            int tmp1 = 0;

            tmp = Convert.ToInt32(this.FunctionParamM.ScanRangeMIN);
            SendTemp[0] = (byte)((tmp >> 24) & 0x00ff);
            SendTemp[1] = (byte)((tmp >> 16) & 0x00ff);
            SendTemp[2] = (byte)((tmp >> 8) & 0x00ff);
            SendTemp[3] = (byte)(tmp & 0x00ff);

            tmp1 = Convert.ToInt32(this.FunctionParamM.ScanRangeMAX);
            SendTemp[4] = (byte)((tmp1 >> 24) & 0x00ff);
            SendTemp[5] = (byte)((tmp1 >> 16) & 0x00ff);
            SendTemp[6] = (byte)((tmp1 >> 8) & 0x00ff);
            SendTemp[7] = (byte)(tmp1 & 0x00ff);

            if (tmp < 500 || (tmp1 > 65000))
            {
                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "配置参数不合理!";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
                return;
            }

            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "靶标扫描范围设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void ReceiveFromLaserEvent(FunctionDataContent m_FunctionDataContent)
        {
            if (m_FunctionDataContent.ReceiveCommandName == "识别阈值查询回复")
            {
                this.FunctionParamM.SelectedPointTH = m_FunctionDataContent.ReceiveContent[26].ToString();
                this.FunctionParamM.SelectedReflectTH = m_FunctionDataContent.ReceiveContent[27].ToString();

                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "靶标识别阈值查询成功！";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
            }
            else if (m_FunctionDataContent.ReceiveCommandName == "匹配范围查询回复")
            {
                this.FunctionParamM.MatchRadiusMIN = ((m_FunctionDataContent.ReceiveContent[26] << 8) + m_FunctionDataContent.ReceiveContent[27]).ToString();
                this.FunctionParamM.MatchRadiusMAX = ((m_FunctionDataContent.ReceiveContent[28] << 8) + m_FunctionDataContent.ReceiveContent[29]).ToString();
                this.FunctionParamM.DetectionRangeMIN = ((m_FunctionDataContent.ReceiveContent[30] << 24) + (m_FunctionDataContent.ReceiveContent[31] << 16) + 
                            (m_FunctionDataContent.ReceiveContent[32] << 8) + m_FunctionDataContent.ReceiveContent[33]).ToString();
                this.FunctionParamM.DetectionRangeMAX = ((m_FunctionDataContent.ReceiveContent[34] << 24) + (m_FunctionDataContent.ReceiveContent[35] << 16) +
                            (m_FunctionDataContent.ReceiveContent[36] << 8) + m_FunctionDataContent.ReceiveContent[37]).ToString();

                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "靶标匹配范围查询成功！";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
            }
            else if (m_FunctionDataContent.ReceiveCommandName == "扫描范围查询回复")
            {
                this.FunctionParamM.ScanRangeMIN = ((m_FunctionDataContent.ReceiveContent[26] << 24) + (m_FunctionDataContent.ReceiveContent[27] << 16) +
                            (m_FunctionDataContent.ReceiveContent[28] << 8) + m_FunctionDataContent.ReceiveContent[29]).ToString();
                this.FunctionParamM.ScanRangeMAX = ((m_FunctionDataContent.ReceiveContent[30] << 24) + (m_FunctionDataContent.ReceiveContent[31] << 16) +
                            (m_FunctionDataContent.ReceiveContent[32] << 8) + m_FunctionDataContent.ReceiveContent[33]).ToString();

                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "靶标扫描范围查询成功！";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
            }
            else if (m_FunctionDataContent.ReceiveCommandName == "2靶标定位功能查询回复")
            {
                if (m_FunctionDataContent.ReceiveContent[26] == 0x00)
                {
                    this.FunctionParamM.SelectedTwoLMlocation = "关闭";
                }
                else if (m_FunctionDataContent.ReceiveContent[26] == 0x01)
                {
                    this.FunctionParamM.SelectedTwoLMlocation = "开启";
                }

                OperationCompleteContent m_OperationCompleteContent = new OperationCompleteContent();
                m_OperationCompleteContent.NoticeContent = "两靶标定位功能查询成功";
                this.eventAggregatorOperationComplete.GetEvent<OperationCompleteEvent>().Publish(m_OperationCompleteContent);
            }
        }

        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.FunctionParamM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }

    }
}
