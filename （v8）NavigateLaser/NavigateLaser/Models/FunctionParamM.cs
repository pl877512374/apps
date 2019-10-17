using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class FunctionParamM : NotificationObject
    {
        private ObservableCollection<string> pointTH;
        public ObservableCollection<string> PointTH
        {
            get { return pointTH; }
            set
            {
                pointTH = value;
                this.RaisePropertyChanged("PointTH");
            }
        }

        private string selectedPointTH;
        public string SelectedPointTH
        {
            get { return selectedPointTH; }
            set
            {
                selectedPointTH = value;
                this.RaisePropertyChanged("SelectedPointTH");
            }
        }

        private ObservableCollection<string> reflectTH;
        public ObservableCollection<string> ReflectTH
        {
            get { return reflectTH; }
            set
            {
                reflectTH = value;
                this.RaisePropertyChanged("ReflectTH");
            }
        }

        private string selectedReflectTH;
        public string SelectedReflectTH
        {
            get { return selectedReflectTH; }
            set
            {
                selectedReflectTH = value;
                this.RaisePropertyChanged("SelectedReflectTH");
            }
        }

        private ObservableCollection<string> twoLMlocation;
        public ObservableCollection<string> TwoLMlocation
        {
            get { return twoLMlocation; }
            set
            {
                twoLMlocation = value;
                this.RaisePropertyChanged("TwoLMlocation");
            }
        }

        private string selectedTwoLMlocation;
        public string SelectedTwoLMlocation
        {
            get { return selectedTwoLMlocation; }
            set
            {
                selectedTwoLMlocation = value;
                this.RaisePropertyChanged("SelectedTwoLMlocation");
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

        private string matchRadiusMIN;
        public string MatchRadiusMIN
        {
            get { return matchRadiusMIN; }
            set
            {
                matchRadiusMIN = value;
                this.RaisePropertyChanged("MatchRadiusMIN");
            }
        }

        private string matchRadiusMAX;
        public string MatchRadiusMAX
        {
            get { return matchRadiusMAX; }
            set
            {
                matchRadiusMAX = value;
                this.RaisePropertyChanged("MatchRadiusMAX");
            }
        }

        private string detectionRangeMIN;
        public string DetectionRangeMIN
        {
            get { return detectionRangeMIN; }
            set
            {
                detectionRangeMIN = value;
                this.RaisePropertyChanged("DetectionRangeMIN");
            }
        }

        private string detectionRangeMAX;
        public string DetectionRangeMAX
        {
            get { return detectionRangeMAX; }
            set
            {
                detectionRangeMAX = value;
                this.RaisePropertyChanged("DetectionRangeMAX");
            }
        }

        private string scanRangeMIN;
        public string ScanRangeMIN
        {
            get { return scanRangeMIN; }
            set
            {
                scanRangeMIN = value;
                this.RaisePropertyChanged("ScanRangeMIN");
            }
        }

        private string scanRangeMAX;
        public string ScanRangeMAX
        {
            get { return scanRangeMAX; }
            set
            {
                scanRangeMAX = value;
                this.RaisePropertyChanged("ScanRangeMAX");
            }
        }
    }
}
