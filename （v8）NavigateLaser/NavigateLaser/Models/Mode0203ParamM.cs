using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class Mode0203ParamM : NotificationObject
    {
        private ObservableCollection<string> shapeOfLandmark;
        public ObservableCollection<string> ShapeOfLandmark
        {
            get { return shapeOfLandmark; }
            set
            {
                shapeOfLandmark = value;
                this.RaisePropertyChanged("ShapeOfLandmark");
            }
        }

        private string selectedShape;
        public string SelectedShape
        {
            get { return selectedShape; }
            set
            {
                selectedShape = value;
                this.RaisePropertyChanged("SelectedShape");
            }
        }

        private int sizeOfLandmark;
        public int SizeOfLandmark
        {
            get { return sizeOfLandmark; }
            set
            {
                sizeOfLandmark = value;
                this.RaisePropertyChanged("SizeOfLandmark");
            }
        }

        private ObservableCollection<string> mappingType;
        public ObservableCollection<string> MappingType
        {
            get { return mappingType; }
            set
            {
                mappingType = value;
                this.RaisePropertyChanged("MappingType");
            }
        }

        private string selectedMappingType;
        public string SelectedMappingType
        {
            get { return selectedMappingType; }
            set
            {
                selectedMappingType = value;
                this.RaisePropertyChanged("SelectedMappingType");
            }
        }

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

        private int averageTime;
        public int AverageTime
        {
            get { return averageTime; }
            set
            {
                averageTime = value;
                this.RaisePropertyChanged("AverageTime");
            }
        }

        private int currentSelfAngle;
        public int CurrentSelfAngle
        {
            get { return currentSelfAngle; }
            set
            {
                currentSelfAngle = value;
                this.RaisePropertyChanged("CurrentSelfAngle");
            }
        }

        private int currentSelfX;
        public int CurrentSelfX
        {
            get { return currentSelfX; }
            set
            {
                currentSelfX = value;
                this.RaisePropertyChanged("CurrentSelfX");
            }
        }

        private int currentSelfY;
        public int CurrentSelfY
        {
            get { return currentSelfY; }
            set
            {
                currentSelfY = value;
                this.RaisePropertyChanged("CurrentSelfY");
            }
        }
    }
}
