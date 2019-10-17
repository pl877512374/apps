using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Practices.Prism.Commands;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using NavigateLaser.DataAccess;

namespace NavigateLaser.ViewModels
{
    class ProductionParamVM : NotificationObject
    {
        public DelegateCommand GetStateDetectionCommand { get; set; }      //获取设备基本状态检测
        public DelegateCommand GetNetParamCommand { get; set; }      //获取设备网络参数
        public DelegateCommand SetNetParamCommand { get; set; }      //设置设备网络参数
        public DelegateCommand GetAPDParamCommand { get; set; }      //获取设备APD参数
        public DelegateCommand SetAPDParamCommand { get; set; }      //设置设备APD参数
        public DelegateCommand ResFactorySettingCommand { get; set; }   //恢复出厂设置
        
        protected IEventAggregator eventAggregatorSend;
        protected IEventAggregator eventAggregatorReceive;
        protected SubscriptionToken tokenReceive;
        protected IEventAggregator eventAggregatorChangeView;
        protected SubscriptionToken tokenExChangeView;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;

        private ProductionParamM m_ProductionParamM;
        public ProductionParamM ProductionParamM
        {
            get { return m_ProductionParamM; }
            set
            {
                m_ProductionParamM = value;
                this.RaisePropertyChanged("ProductionParamM");
            }
        }

        public ProductionParamVM()
        {
            this.LoadProductionParamM();
            this.GetStateDetectionCommand = new DelegateCommand(new Action(this.GetStateDetectionCommandExecute));
            this.GetNetParamCommand = new DelegateCommand(new Action(this.GetNetParamCommandExecute));
            this.SetNetParamCommand = new DelegateCommand(new Action(this.SetNetParamCommandExecute));
            this.GetAPDParamCommand = new DelegateCommand(new Action(this.GetAPDParamCommandExecute));
            this.SetAPDParamCommand = new DelegateCommand(new Action(this.SetAPDParamCommandExecute));
            this.ResFactorySettingCommand = new DelegateCommand(new Action(this.ResFactorySettingCommandExecute));
            this.eventAggregatorSend = ServiceLocator.Current.GetInstance<IEventAggregator>();
            eventAggregatorReceive = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenReceive = eventAggregatorReceive.GetEvent<ProductionDataShowEvent>().Subscribe(ReceiveFromLaserEvent);
            eventAggregatorChangeView = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenExChangeView = eventAggregatorChangeView.GetEvent<LoginChangeViewEvent>().Subscribe(ChangeViewCommandEvent);
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);
        }

        private void LoadProductionParamM()
        {
            this.ProductionParamM = new ProductionParamM();
            this.ProductionParamM.IsEnable = false;
        }

        private void GetStateDetectionCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "状态检测查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetNetParamCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "网络参数查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void SetNetParamCommandExecute()
        {
            byte[] SendTemp = new byte[20];
            string[] JGIPTemp = this.ProductionParamM.DeviceIP.Split('.');
            if (JGIPTemp.Length != 4)
            {
                return;
            }
            SendTemp[0] = (byte)Convert.ToInt16(JGIPTemp[0]);
            SendTemp[1] = (byte)Convert.ToInt16(JGIPTemp[1]);
            SendTemp[2] = (byte)Convert.ToInt16(JGIPTemp[2]);
            SendTemp[3] = (byte)Convert.ToInt16(JGIPTemp[3]);
            string[] YanmaTemp = this.ProductionParamM.DeviceSubnetMask.Split('.');
            if (YanmaTemp.Length != 4)
            {
                return;
            }
            SendTemp[4] = (byte)Convert.ToInt16(YanmaTemp[0]);
            SendTemp[5] = (byte)Convert.ToInt16(YanmaTemp[1]);
            SendTemp[6] = (byte)Convert.ToInt16(YanmaTemp[2]);
            SendTemp[7] = (byte)Convert.ToInt16(YanmaTemp[3]);
            string[] JGNetGateTemp = this.ProductionParamM.DeviceGateway.Split('.');
            if (JGNetGateTemp.Length != 4)
            {
                return;
            }
            SendTemp[8] = (byte)Convert.ToInt16(JGNetGateTemp[0]);
            SendTemp[9] = (byte)Convert.ToInt16(JGNetGateTemp[1]);
            SendTemp[10] = (byte)Convert.ToInt16(JGNetGateTemp[2]);
            SendTemp[11] = (byte)Convert.ToInt16(JGNetGateTemp[3]);
            SendTemp[12] = (byte)((Convert.ToInt16(this.ProductionParamM.DevicePort) >> 8) & 0x00ff);
            SendTemp[13] = (byte)(Convert.ToInt16(this.ProductionParamM.DevicePort) & 0x00ff);
            string[] MACTemp = this.ProductionParamM.DeviceMAC.Split('-');
            if (MACTemp.Length != 6)
            {
                return;
            }
            SendTemp[14] = (byte)Convert.ToByte(MACTemp[0], 16);
            SendTemp[15] = (byte)Convert.ToByte(MACTemp[1], 16);
            SendTemp[16] = (byte)Convert.ToByte(MACTemp[2], 16);
            SendTemp[17] = (byte)Convert.ToByte(MACTemp[3], 16);
            SendTemp[18] = (byte)Convert.ToByte(MACTemp[4], 16);
            SendTemp[19] = (byte)Convert.ToByte(MACTemp[5], 16);

            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "网络参数设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void GetAPDParamCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "APD参数查询" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void SetAPDParamCommandExecute()
        {
            byte[] SendTemp = new byte[6];
            int tmp=0; 
            //APD高压值
            tmp = Convert.ToInt32(Convert.ToSingle(this.ProductionParamM.APDBreakdownVoltage) * 100);
            SendTemp[0] = (byte)((tmp >> 8) & 0x00ff);
            SendTemp[1] = (byte)(tmp & 0x00ff);
            //APD温度值
            tmp = Convert.ToInt32(Convert.ToSingle(this.ProductionParamM.APDBreakdownTemp) * 100);
            SendTemp[2] = (byte)((tmp >> 8) & 0x00ff);
            SendTemp[3] = (byte)(tmp & 0x00ff);
            //APD高压系数
            tmp = Convert.ToInt32(Convert.ToSingle(this.ProductionParamM.APDHighVoltageCoeff) * 100);
            SendTemp[4] = (byte)((tmp >> 8) & 0x00ff);
            SendTemp[5] = (byte)(tmp & 0x00ff);
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "APD参数设置" };
            m_TCPSendCommandName.SendContent = SendTemp.ToList();
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        private void ResFactorySettingCommandExecute()
        {
            TCPSendCommandName m_TCPSendCommandName = new TCPSendCommandName() { SendCommandName = "一键恢复出厂设置" };
            this.eventAggregatorSend.GetEvent<TCPSendDataEvent>().Publish(m_TCPSendCommandName);
        }

        #region 响应订阅的回复事件
        private void ReceiveFromLaserEvent(ProductionDataContent m_ProductionDataContent)
        {
            if (m_ProductionDataContent.ReceiveCommandName == "获得状态检测")
            {
                this.ProductionParamM.FPGA_Address = "0x" + m_ProductionDataContent.ReceiveContent[26].ToString("X2") + m_ProductionDataContent.ReceiveContent[27].ToString("X2");
                this.ProductionParamM.FPGA_Data = "0x" + m_ProductionDataContent.ReceiveContent[28].ToString("X2") + m_ProductionDataContent.ReceiveContent[29].ToString("X2");
                this.ProductionParamM.FPGA_Config = "0x" + m_ProductionDataContent.ReceiveContent[30].ToString("X2") + m_ProductionDataContent.ReceiveContent[31].ToString("X2");
                this.ProductionParamM.FRAM_Check = "0x" + m_ProductionDataContent.ReceiveContent[32].ToString("X2");
                this.ProductionParamM.DS18B20_Check = "0x" + m_ProductionDataContent.ReceiveContent[33].ToString("X2");
                this.ProductionParamM.WirelessStatus_Check = "0x" + m_ProductionDataContent.ReceiveContent[34].ToString("X2");
            }
            else if (m_ProductionDataContent.ReceiveCommandName == "获得网络参数")
            {
                this.ProductionParamM.DeviceIP = m_ProductionDataContent.ReceiveContent[26].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[27].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[28].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[29].ToString();
                this.ProductionParamM.DeviceSubnetMask = m_ProductionDataContent.ReceiveContent[30].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[31].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[32].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[33].ToString();
                this.ProductionParamM.DeviceGateway = m_ProductionDataContent.ReceiveContent[34].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[35].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[36].ToString() + "."
                    + m_ProductionDataContent.ReceiveContent[37].ToString();
                this.ProductionParamM.DevicePort = ((m_ProductionDataContent.ReceiveContent[38] << 8) + m_ProductionDataContent.ReceiveContent[39]).ToString();
                this.ProductionParamM.DeviceMAC = m_ProductionDataContent.ReceiveContent[40].ToString("X2") + "-"
                    + m_ProductionDataContent.ReceiveContent[41].ToString("X2") + "-"
                    + m_ProductionDataContent.ReceiveContent[42].ToString("X2") + "-"
                    + m_ProductionDataContent.ReceiveContent[43].ToString("X2") + "-"
                    + m_ProductionDataContent.ReceiveContent[44].ToString("X2") + "-"
                    + m_ProductionDataContent.ReceiveContent[45].ToString("X2");
            }
            else if (m_ProductionDataContent.ReceiveCommandName == "获得APD参数")
            {
                this.ProductionParamM.APDBreakdownVoltage = ((Single)((m_ProductionDataContent.ReceiveContent[26] << 8) + m_ProductionDataContent.ReceiveContent[27]) / 100).ToString();
                this.ProductionParamM.APDBreakdownTemp = ((Single)((m_ProductionDataContent.ReceiveContent[28] << 8) + m_ProductionDataContent.ReceiveContent[29]) / 100).ToString();
                this.ProductionParamM.APDHighVoltageCoeff = ((Single)((m_ProductionDataContent.ReceiveContent[30] << 8) + m_ProductionDataContent.ReceiveContent[31]) / 100).ToString();
            }
        }
        #endregion

        private void ChangeViewCommandEvent(LoginChangeViewName m_LoginChangeViewName)
        {
            if (m_LoginChangeViewName.ChangeViewName == "授权用户登录")
            {
                this.ProductionParamM.IsEnable = true;
            }
            else if (m_LoginChangeViewName.ChangeViewName == "退出登录")
            {
                this.ProductionParamM.IsEnable = false;
            }
        }

        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.ProductionParamM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }
    }
}
