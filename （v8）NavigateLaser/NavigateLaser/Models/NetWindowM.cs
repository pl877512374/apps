using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.Models
{
    class NetWindowM : NotificationObject
    {
        private string laserIP;
        public string LaserIP
        {
            get { return laserIP; }
            set
            {
                laserIP = value;
                this.RaisePropertyChanged("LaserIP");
            }
        }

        private string laserPort;
        public string LaserPort
        {
            get { return laserPort; }
            set
            {
                laserPort = value;
                this.RaisePropertyChanged("LaserPort");
            }
        }

        public string LaserSendData { get; set; }

        private string laserRecData;
        public string LaserRecData
        {
            get { return laserRecData; }
            set
            {
                laserRecData = value;
                this.RaisePropertyChanged("LaserRecData");
            }
        }

        private string btConnectTXT;
        public string BtConnectTXT
        {
            get { return btConnectTXT; }
            set
            {
                btConnectTXT = value;
                this.RaisePropertyChanged("BtConnectTXT");
            }
        }

        private bool isBtSearchEnable;
        public bool IsBtSearchEnable
        {
            get { return isBtSearchEnable; }
            set
            {
                isBtSearchEnable = value;
                this.RaisePropertyChanged("IsBtSearchEnable");
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

        private bool btConnectIsEnable;
        public bool BtConnectIsEnable
        {
            get { return btConnectIsEnable; }
            set
            {
                btConnectIsEnable = value;
                this.RaisePropertyChanged("BtConnectIsEnable");
            }
        }
     
    }
}
