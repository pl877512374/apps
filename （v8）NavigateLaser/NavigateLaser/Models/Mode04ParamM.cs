using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class Mode04ParamM : NotificationObject
    {
        private int paramLayerID;
        public int ParamLayerID
        {
            get { return paramLayerID; }
            set
            {
                paramLayerID = value;
                this.RaisePropertyChanged("ParamLayerID");
            }
        }
    }
}
