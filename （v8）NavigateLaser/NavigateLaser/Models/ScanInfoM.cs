using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using NavigateLaser.DataAccess;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class ScanInfoM : NotificationObject
    {
        public int StatisticalFrameCount { get; set; }
        public int PointSequenceNum { get; set; }

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

        private int statisticalMAX;
        public int StatisticalMAX
        {
            get { return statisticalMAX; }
            set
            {
                statisticalMAX = value;
                this.RaisePropertyChanged("StatisticalMAX");
            }
        }

        private int statisticalMIN;
        public int StatisticalMIN
        {
            get { return statisticalMIN; }
            set
            {
                statisticalMIN = value;
                this.RaisePropertyChanged("StatisticalMIN");
            }
        }

        private long statisticalAVG;
        public long StatisticalAVG
        {
            get { return statisticalAVG; }
            set
            {
                statisticalAVG = value;
                this.RaisePropertyChanged("StatisticalAVG");
            }
        }

        private int validFlag;
        public int ValidFlag
        {
            get { return validFlag; }
            set
            {
                validFlag = value;
                this.RaisePropertyChanged("ValidFlag");
            }
        }

        private Single selfAngle;
        public Single SelfAngle
        {
            get { return selfAngle; }
            set
            {
                selfAngle = value;
                this.RaisePropertyChanged("SelfAngle");
            }
        }

        private int selfX;
        public int SelfX
        {
            get { return selfX; }
            set
            {
                selfX = value;
                this.RaisePropertyChanged("SelfX");
            }
        }

        private int selfY;
        public int SelfY
        {
            get { return selfY; }
            set
            {
                selfY = value;
                this.RaisePropertyChanged("SelfY");
            }
        }

        private List<MarkInfo> markInfoMenu;
        public List<MarkInfo> MarkInfoMenu
        {
            get { return markInfoMenu; }
            set
            {
                markInfoMenu = value;
                this.RaisePropertyChanged("MarkInfoMenu");
            }
        }

        private MarkInfo currentSelectItem;
        public MarkInfo CurrentSelectItem
        {
            get { return currentSelectItem; }
            set
            {
                currentSelectItem = value;
                this.RaisePropertyChanged("CurrentSelectItem");
            }
        }

        private bool btIsEnable;
        public bool BtIsEnable
        {
            get { return btIsEnable; }
            set
            {
                btIsEnable = value;
                this.RaisePropertyChanged("BtIsEnable");
            }
        }

        private int lMTotal;
        public int LMTotal
        {
            get { return lMTotal; }
            set
            {
                lMTotal = value;
                this.RaisePropertyChanged("LMTotal");
            }
        }

        private int speedX;
        public int SpeedX
        {
            get { return speedX; }
            set
            {
                speedX = value;
                this.RaisePropertyChanged("SpeedX");
            }
        }

        private int speedY;
        public int SpeedY
        {
            get { return speedY; }
            set
            {
                speedY = value;
                this.RaisePropertyChanged("SpeedY");
            }
        }

        private int speedAngle;
        public int SpeedAngle
        {
            get { return speedAngle; }
            set
            {
                speedAngle = value;
                this.RaisePropertyChanged("SpeedAngle");
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
        public System.Windows.Controls.DataGrid myDataGrid { get; set; }
    }
}
