using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.DataAccess
{
    class LandmarkSetInfo : NotificationObject
    {
        public LandmarkSetInfo()
        {
            flagBlueColor = 0;
        }
        private int no;
        public int No
        {
            get { return no; }
            set
            {
                no = value;
                this.RaisePropertyChanged("No");
            }
        }

        private int landmarkX;
        public int LandmarkX
        {
            get { return landmarkX; }
            set
            {
                landmarkX = value;
                this.RaisePropertyChanged("LandmarkX");
            }
        }

        private int landmarkY;
        public int LandmarkY
        {
            get { return landmarkY; }
            set
            {
                landmarkY = value;
                this.RaisePropertyChanged("LandmarkY");
            }
        }

        public int LayerID { get; set; }

        public bool IsCyclinder { get; set; }

        public int LandmarkShapeSize { get; set; }

        public bool IsSelected { get; set; }


        private bool isBtEnable;
        public bool IsBtEnable
        {
            get { return isBtEnable; }
            set
            {
                isBtEnable = value;
                this.RaisePropertyChanged("IsBtEnable");
            }
        }

        private bool isCylinderCheckEnable;
        public bool IsCylinderCheckEnable
        {
            get { return isCylinderCheckEnable; }
            set
            {
                isCylinderCheckEnable = value;
                this.RaisePropertyChanged("IsCylinderCheckEnable");
            }
        }
        private bool isSelectedCheckEnable;
        public bool IsSelectedCheckEnable
        {
            get { return isSelectedCheckEnable; }
            set
            {
                isSelectedCheckEnable = value;
                this.RaisePropertyChanged("IsSelectedCheckEnable");
            }
        }
        public bool isChangeColor { get; set; }
        public int  flagBlueColor { get; set; }
    }

    class LandmarkLayerInfo : NotificationObject
    {
        public int ID { get; set; }
        public int LandmarkTotal { get; set; }
    }
}
