using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Security;

namespace NavigateLaser.Models
{
    class LoginWindowM : NotificationObject
    {
        public string LoginID { get; set; }

        private string btLoginTXT;
        public string BtLoginTXT
        {
            get { return btLoginTXT; }
            set
            {
                btLoginTXT = value;
                this.RaisePropertyChanged("BtLoginTXT");
            }
        }

        private string tipContentTXT;
        public string TipContentTXT
        {
            get { return tipContentTXT; }
            set
            {
                tipContentTXT = value;
                this.RaisePropertyChanged("TipContentTXT");
            }
        }
    }
}
