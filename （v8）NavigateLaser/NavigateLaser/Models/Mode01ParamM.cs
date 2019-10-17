using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class Mode01ParamM : NotificationObject
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
    }
}
