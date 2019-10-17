using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.Models
{
    class TopMenuM : NotificationObject
    {
        public bool LoginResult { get; set; }

        private string subMenuName;
        public string SubMenuName
        {
            get { return subMenuName; }
            set
            {
                subMenuName = value;
                this.RaisePropertyChanged("SubMenuName");
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
