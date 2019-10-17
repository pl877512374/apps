using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.Models
{
    class PCLViewM : NotificationObject
    {
        private string btPointDisplayTXT;
        public string BtPointDisplayTXT
        {
            get { return btPointDisplayTXT; }
            set
            {
                btPointDisplayTXT = value;
                this.RaisePropertyChanged("BtPointDisplayTXT");
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
        private string btnScanbgc;
        public string BtnScanbgc
        {
            get { return btnScanbgc; }
            set
            {
                btnScanbgc = value;
                this.RaisePropertyChanged("BtnScanbgc");
            }
        }
        private string btnDetectbgc;
        public string BtnDetectbgc
        {
            get { return btnDetectbgc; }
            set
            {
                btnDetectbgc = value;
                this.RaisePropertyChanged("BtnDetectbgc");
            }
        }
        private string btnGetbgc;
        public string BtnGetbgc
        {
            get { return btnGetbgc; }
            set
            {
                btnGetbgc = value;
                this.RaisePropertyChanged("BtnGetbgc");
            }
        }
        private string btnNavibgc;
        public string BtnNavibgc
        {
            get { return btnNavibgc; }
            set
            {
                btnNavibgc = value;
                this.RaisePropertyChanged("BtnNavibgc");
            }
        }

    }
}
