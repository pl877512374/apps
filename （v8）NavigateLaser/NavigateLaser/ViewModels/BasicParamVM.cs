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
    class BasicParamVM : NotificationObject
    {
        public DelegateCommand SetBasicParamCommand { get; set; }    //设置基本参数
        public DelegateCommand GetBasicParamCommand { get; set; }    //获取基本参数
        public DelegateCommand GetVersionsCommand { get; set; }      //获取版本
        public DelegateCommand GetRunStateCommand { get; set; }      //获取运行状态
        public DelegateCommand GetResetInfoCommand { get; set; }      //获取复位参数
        public DelegateCommand CleaResetZeroCommand { get; set; }      //复位清零
        public DelegateCommand RestartLaserCommand { get; set; }      //重启激光器 
        public DelegateCommand TableContentQueryCommand { get; set; }  //修正表、偏心表、反射率表查询

        protected IEventAggregator eventAggregatorSend;
        protected IEventAggregator eventAggregatorReceive;
        protected SubscriptionToken tokenReceive;
        protected IEventAggregator eventAggregatorChangeView;
        protected SubscriptionToken tokenExChangeView;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;
        protected IEventAggregator eventAggregatorHeartBeat;
        protected SubscriptionToken tokenHeartBeat;
        private BasicParamM m_BasicParamM;
        public BasicParamM BasicParamM
        {
            get { return m_BasicParamM; }
            set
            {
                m_BasicParamM = value;
                this.RaisePropertyChanged("BasicParamM");
            }
        }

        public BasicParamVM()
        {
            this.LoadBasicParamM();
            this.SetBasicParamCommand = new DelegateCommand(new Action(this.SetBasicParamCommandExecute));
            this.GetBasicParamCommand = new DelegateCommand(new Action(this.GetBasicParamCommandExecute));
            this.GetVersionsCommand = new DelegateCommand(new Action(this.GetVersionsCommandExecute));
            this.GetRunStateCommand = new DelegateCommand(new Action(this.GetRunStateCommandExecute));
            this.GetResetInfoCommand = new DelegateCommand(new Action(this.GetResetInfoCommandExecute));
            this.CleaResetZeroCommand = new DelegateCommand(new Action(this.CleaResetZeroCommandExecute));
            this.RestartLaserCommand = new DelegateCommand(new Action(this.RestartLaserCommandExecute));
            this.TableContentQueryCommand=new DelegateCommand(new Action(this.TableContentQueryCommandExecute));
            this.eventAggregatorSend = ServiceLocator.Current.GetInstance<IEventAggregator>();
            eventAggregatorReceive = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenReceive = eventAggregatorReceive.GetEvent<BasicDataShowEvent>().Subscribe(ReceiveFromLaserEvent);
            eventAggregatorChangeView = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenExChangeView = eventAggregatorChangeView.GetEvent<LoginChangeViewEvent>().Subscribe(ChangeViewCommandEvent);
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);
             this.eventAggregatorHeartBeat = ServiceLocator.Current.GetInstance<IEventAggregator>();
             this.tokenHeartBeat = eventAggregatorHeartBeat.GetEvent<QueryHeartStateEvent>().Subscribe(QueryHeartState);
        }
        private void QueryHeartState(string mval)
        {
            NetWindowVM.heartBeatState= this.BasicParamM.SelectedBeat;
        }
        private void LoadBasicParamM()
        {
            this.BasicParamM = new BasicParamM();
            this.BasicParamM.IsEnable = false;
            this.BasicParamM.Resolutions = new ObservableCollection<string>()
            {
                {"0.1°"},
                {"0.05°"},
            };
            this.BasicParamM.TableTypes = new ObservableCollection<string>()
            {
                {"修正表"},
                {"反射率表"},
                {"偏心表"}
            };
            this.BasicParamM.SelectedTableType = "修正表";
            this.BasicParamM.Beats = new ObservableCollection<string>()
            {
                {"关闭"},
                {"开启"},
            };
            this.BasicParamM.AddressOffset = "0";
        }

        private void SetBasicParamCommandExecute()
        {
            byte[] SendTemp = new byte[4];
            if (this.BasicParamM.SelectedBeat == "关闭")
            {
                SendTemp[0] = 0x00;
            }
            else if (this.BasicParamM.SelectedBeat == "开启")
            {
                SendTemp[0] = 0x01;
            }
            else
            {
                return;
            }

            if (this.BasicParamM.SelectedResolution == "0.1°")
            {
                SendTemp[1] = 0x01;
            }
            else if (this.BasicParamM.SelectedResolution == "0.05°")
            {
                SendTemp[1] = 0x02;
            }
            else
            {
                return;
            }
            SendTemp[2] = 0x00;
            SendTemp[3] = 0x00;

            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "基本参数设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetBasicParamCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "基本参数查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetVersionsCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "版本查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetRunStateCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "运行状态查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetResetInfoCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "复位信息查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void CleaResetZeroCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "复位清零" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void RestartLaserCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "重启激光器" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void TableContentQueryCommandExecute()
        {
            byte[] SendTemp = new byte[4];
            if (this.BasicParamM.AddressOffset.Length >0)
            {
                int ReadAddress = Convert.ToInt32(this.BasicParamM.AddressOffset);
                SendTemp[0] = (byte)((ReadAddress >> 8) & 0x00ff);
                SendTemp[1] = (byte)(ReadAddress & 0x00ff);
                SendTemp[2] = 0x00;
                SendTemp[3] = 0x00;
                TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName=this.BasicParamM.SelectedTableType };
                m_TCPSendCommandName.SendContent = SendTemp.ToList();
                this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
            }
        }

        #region 响应订阅的回复事件
        private void ReceiveFromLaserEvent(BasicDataContent m_BasicDataContent)
        {
            if (m_BasicDataContent.ReceiveCommandName == "获得版本")
            {
                this.BasicParamM.HardwareVersion = System.Text.Encoding.Default.GetString(m_BasicDataContent.ReceiveContent.Skip(26).Take(20).ToArray());
                if (this.BasicParamM.HardwareVersion.IndexOf("\0") >= 0)
                {
                    this.BasicParamM.HardwareVersion = this.BasicParamM.HardwareVersion.Substring(0, this.BasicParamM.HardwareVersion.IndexOf("\0"));
                }
                this.BasicParamM.ProgramVersion = System.Text.Encoding.Default.GetString(m_BasicDataContent.ReceiveContent.Skip(46).Take(25).ToArray());
                if (this.BasicParamM.ProgramVersion.IndexOf("\0") >= 0)
                {
                    this.BasicParamM.ProgramVersion = this.BasicParamM.ProgramVersion.Substring(0, this.BasicParamM.ProgramVersion.IndexOf("\0"));
                }
                this.BasicParamM.FPGAVersion = System.Text.Encoding.Default.GetString(m_BasicDataContent.ReceiveContent.Skip(71).Take(25).ToArray());
                if (this.BasicParamM.FPGAVersion.IndexOf("\0") >= 0)
                {
                    this.BasicParamM.FPGAVersion = this.BasicParamM.FPGAVersion.Substring(0, this.BasicParamM.FPGAVersion.IndexOf("\0"));
                }
            }
            else if (m_BasicDataContent.ReceiveCommandName == "获得运行状态")
            {
                if (m_BasicDataContent.ReceiveContent[26] == 0x00)
                {
                    this.BasicParamM.MotorState = "未使能";
                }
                else if (m_BasicDataContent.ReceiveContent[26] == 0x01)
                {
                    this.BasicParamM.MotorState = "已使能";
                }
                if (m_BasicDataContent.ReceiveContent[27] == 0x00)
                {
                    this.BasicParamM.LaserState = "未使能";
                }
                else if (m_BasicDataContent.ReceiveContent[27] == 0x01)
                {
                    this.BasicParamM.LaserState = "已使能";
                }
                if (m_BasicDataContent.ReceiveContent[28] == 0x00)
                {
                    this.BasicParamM.BeatState = "关闭";
                }
                else if (m_BasicDataContent.ReceiveContent[28] == 0x01)
                {
                    this.BasicParamM.BeatState = "开启";
                }
                Single MotorTemperature = Math.Abs(Convert.ToSingle((Int16)((m_BasicDataContent.ReceiveContent[29] << 8) + m_BasicDataContent.ReceiveContent[30]) / 100.0));
                this.BasicParamM.MotorTemperature = (MotorTemperature).ToString();
                Single HighPressureAPD = Math.Abs(Convert.ToSingle((Int16)((m_BasicDataContent.ReceiveContent[31] << 8) + m_BasicDataContent.ReceiveContent[32]) / 100.0));
                this.BasicParamM.HighPressureAPD = (HighPressureAPD).ToString();
                Single TemperatureAPD = Math.Abs(Convert.ToSingle((Int16)((m_BasicDataContent.ReceiveContent[33] << 8) + m_BasicDataContent.ReceiveContent[34]) / 100.0));
                this.BasicParamM.TemperatureAPD = (TemperatureAPD).ToString();
                this.BasicParamM.APDTemp = TemperatureAPD;//APD温度
                this.BasicParamM.StrAPDTemp = TemperatureAPD.ToString("0.00");
                this.BasicParamM.MotorTemp = MotorTemperature;//电机温度
                this.BasicParamM.StrMotorTemp = MotorTemperature.ToString("0.00");
                this.BasicParamM.HighVol = HighPressureAPD;//APD高压
                this.BasicParamM.StrHighVol = HighPressureAPD.ToString("0.00");
                this.BasicParamM.InputVol = Math.Abs(Convert.ToSingle((Int16)((m_BasicDataContent.ReceiveContent[47] << 8) + m_BasicDataContent.ReceiveContent[48]) / 1000.0));//输入电压
                this.BasicParamM.StrInputVol = this.BasicParamM.InputVol.ToString("0.00");
                this.BasicParamM.WirelessVol =Math.Abs(Convert.ToSingle((Int16)((m_BasicDataContent.ReceiveContent[45] << 8) + m_BasicDataContent.ReceiveContent[46]) / 1000.0));//无线供电电压
                this.BasicParamM.StrWirelessVol = this.BasicParamM.WirelessVol.ToString("0.00");
                this.BasicParamM.W5300_SSR = "0x" + m_BasicDataContent.ReceiveContent[35].ToString("X2");
                this.BasicParamM.W5300_IR = "0x" + m_BasicDataContent.ReceiveContent[36].ToString("X2");
                this.BasicParamM.W5300_TXFSR = "0x" + ((m_BasicDataContent.ReceiveContent[37] << 8) + m_BasicDataContent.ReceiveContent[38]).ToString("X2");
                this.BasicParamM.W5300_RXRSR = "0x" + ((m_BasicDataContent.ReceiveContent[39] << 8) + m_BasicDataContent.ReceiveContent[40]).ToString("X2");
            }
            else if (m_BasicDataContent.ReceiveCommandName == "获得基本参数")
            {
                if (m_BasicDataContent.ReceiveContent[26] == 0x00)
                {
                    this.BasicParamM.SelectedBeat = "关闭";
                }
                else if (m_BasicDataContent.ReceiveContent[26] == 0x01)
                {
                    this.BasicParamM.SelectedBeat = "开启";
                }
                if (m_BasicDataContent.ReceiveContent[27] == 0x01)
                {
                    this.BasicParamM.SelectedResolution = "0.1°";
                }
                else if (m_BasicDataContent.ReceiveContent[27] == 0x02)
                {
                    this.BasicParamM.SelectedResolution = "0.05°";
                }
            }
            else if (m_BasicDataContent.ReceiveCommandName == "获得复位信息")
            {
                this.BasicParamM.SoftReset = ((m_BasicDataContent.ReceiveContent[26] << 8) + m_BasicDataContent.ReceiveContent[27]).ToString();
                this.BasicParamM.IWDGReset = ((m_BasicDataContent.ReceiveContent[28] << 8) + m_BasicDataContent.ReceiveContent[29]).ToString();
                this.BasicParamM.WWDGReset = ((m_BasicDataContent.ReceiveContent[30] << 8) + m_BasicDataContent.ReceiveContent[31]).ToString();
                this.BasicParamM.PORReset = ((m_BasicDataContent.ReceiveContent[32] << 8) + m_BasicDataContent.ReceiveContent[33]).ToString();
                this.BasicParamM.NRSTReset = ((m_BasicDataContent.ReceiveContent[34] << 8) + m_BasicDataContent.ReceiveContent[35]).ToString();
                this.BasicParamM.W5500Reset = ((m_BasicDataContent.ReceiveContent[36] << 8) + m_BasicDataContent.ReceiveContent[37]).ToString();
                this.BasicParamM.W5500BeatReset = ((m_BasicDataContent.ReceiveContent[38] << 8) + m_BasicDataContent.ReceiveContent[39]).ToString();
                this.BasicParamM.W5500Close = ((m_BasicDataContent.ReceiveContent[40] << 8) + m_BasicDataContent.ReceiveContent[41]).ToString();
            }
        }
        #endregion

        private void ChangeViewCommandEvent(LoginChangeViewName m_LoginChangeViewName)
        {
            if (m_LoginChangeViewName.ChangeViewName == "授权用户登录")
            {
                this.BasicParamM.IsEnable = true;
            }
            else if (m_LoginChangeViewName.ChangeViewName == "退出登录")
            {
                this.BasicParamM.IsEnable = false;
            }
        }


        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.BasicParamM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }
    }
}
