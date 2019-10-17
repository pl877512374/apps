using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;
using NavigateLaser.DataAccess;

namespace NavigateLaser.Models
{
    class LandmarkSetM : NotificationObject
    {

        private ObservableCollection<LandmarkSetInfo> landmarkMenu;
        public ObservableCollection<LandmarkSetInfo> LandmarkMenu
        {
            get { return landmarkMenu; }
            set
            {
                landmarkMenu = value;
                this.RaisePropertyChanged("LandmarkMenu");
            }
        }

        private ObservableCollection<string> landmarkshapes;
        public ObservableCollection<string> Landmarkshapes
        {
            get { return landmarkshapes; }
            set
            {
                landmarkshapes = value;
                this.RaisePropertyChanged("Landmarkshapes");
            }
        }

        private string selectedLandmarkshape;
        public string SelectedLandmarkshape
        {
            get { return selectedLandmarkshape; }
            set
            {
                selectedLandmarkshape = value;
                this.RaisePropertyChanged("SelectedLandmarkshape");
            }
        }

        private int landmarkSize;
        public int LandmarkSize
        {
            get { return landmarkSize; }
            set
            {
                landmarkSize = value;
                this.RaisePropertyChanged("LandmarkSize");
            }
        }

        public Single MyPoseAngle { get; set; }

        public int MyPoseX { get; set; }

        public int MyPoseY { get; set; }

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

        private LandmarkSetInfo currentSelectItem;
        public LandmarkSetInfo CurrentSelectItem
        {
            get { return currentSelectItem; }
            set
            {
                currentSelectItem = value;
                this.RaisePropertyChanged("CurrentSelectItem");
            }
        }

        private List<LandmarkLayerInfo> landmarkLayerInfoMenu;
        public List<LandmarkLayerInfo> LandmarkLayerInfoMenu
        {
            get { return landmarkLayerInfoMenu; }
            set
            {
                landmarkLayerInfoMenu = value;
                this.RaisePropertyChanged("LandmarkLayerInfoMenu");
            }
        }

        private int statisticsLayerTotal;
        public int StatisticsLayerTotal
        {
            get { return statisticsLayerTotal; }
            set
            {
                statisticsLayerTotal = value;
                this.RaisePropertyChanged("StatisticsLayerTotal");
            }
        }

        private int statisticsLMTotal;
        public int StatisticsLMTotal
        {
            get { return statisticsLMTotal; }
            set
            {
                statisticsLMTotal = value;
                this.RaisePropertyChanged("StatisticsLMTotal");
            }
        }

        private bool isDGEnable;
        public bool IsDGEnable
        {
            get { return isDGEnable; }
            set
            {
                isDGEnable = value;
                this.RaisePropertyChanged("IsDGEnable");
            }
        }

        public int RotationAngle { get; set; }

        public int TranslationX { get; set; }


        public int TranslationY { get; set; }


        public int RotationX { get; set; }

        public int RotationY { get; set; }

        public System.Windows.Controls.DataGrid myDataGrid { get; set; }

    }
}
