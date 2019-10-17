using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.Models
{
    class ProductionParamM : NotificationObject
    {
        private string fPGA_Address;
        public string FPGA_Address
        {
            get { return fPGA_Address; }
            set
            {
                fPGA_Address = value;
                this.RaisePropertyChanged("FPGA_Address");
            }
        }

        private string fPGA_Data;
        public string FPGA_Data
        {
            get { return fPGA_Data; }
            set
            {
                fPGA_Data = value;
                this.RaisePropertyChanged("FPGA_Data");
            }
        }

        private string fPGA_Config;
        public string FPGA_Config
        {
            get { return fPGA_Config; }
            set
            {
                fPGA_Config = value;
                this.RaisePropertyChanged("FPGA_Config");
            }
        }

        private string fRAM_Check;
        public string FRAM_Check
        {
            get { return fRAM_Check; }
            set
            {
                fRAM_Check = value;
                this.RaisePropertyChanged("FRAM_Check");
            }
        }

        private string dS18b20_Check;
        public string DS18B20_Check
        {
            get { return dS18b20_Check; }
            set
            {
                dS18b20_Check = value;
                this.RaisePropertyChanged("DS18B20_Check");
            }
        }

        private string wirelessStatus_Check;
        public string WirelessStatus_Check
        {
            get { return wirelessStatus_Check; }
            set
            {
                wirelessStatus_Check = value;
                this.RaisePropertyChanged("WirelessStatus_Check");
            }
        }

        private string deviceIP;
        public string DeviceIP
        {
            get { return deviceIP; }
            set
            {
                deviceIP = value;
                this.RaisePropertyChanged("DeviceIP");
            }
        }

        private string deviceSubnetMask;
        public string DeviceSubnetMask
        {
            get { return deviceSubnetMask; }
            set
            {
                deviceSubnetMask = value;
                this.RaisePropertyChanged("DeviceSubnetMask");
            }
        }

        private string deviceGateway;
        public string DeviceGateway
        {
            get { return deviceGateway; }
            set
            {
                deviceGateway = value;
                this.RaisePropertyChanged("DeviceGateway");
            }
        }

        private string devicePort;
        public string DevicePort
        {
            get { return devicePort; }
            set
            {
                devicePort = value;
                this.RaisePropertyChanged("DevicePort");
            }
        }

        private string deviceMAC;
        public string DeviceMAC
        {
            get { return deviceMAC; }
            set
            {
                deviceMAC = value;
                this.RaisePropertyChanged("DeviceMAC");
            }
        }

        private string aPDBreakdownVoltage;
        public string APDBreakdownVoltage
        {
            get { return aPDBreakdownVoltage; }
            set
            {
                aPDBreakdownVoltage = value;
                this.RaisePropertyChanged("APDBreakdownVoltage");
            }
        }

        private string aPDBreakdownTemp;
        public string APDBreakdownTemp
        {
            get { return aPDBreakdownTemp; }
            set
            {
                aPDBreakdownTemp = value;
                this.RaisePropertyChanged("APDBreakdownTemp");
            }
        }

        private string aPDHighVoltageCoeff;
        public string APDHighVoltageCoeff
        {
            get { return aPDHighVoltageCoeff; }
            set
            {
                aPDHighVoltageCoeff = value;
                this.RaisePropertyChanged("APDHighVoltageCoeff");
            }
        }

        private bool isEnable;
        public bool IsEnable
        {
            get { return isEnable; }
            set
            {
                isEnable = value;
                this.RaisePropertyChanged("IsEnable");
            }
        }
    }
}
