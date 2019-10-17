using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Commands;
using System.Windows;
using NavigateLaser.Views;
using System.Windows.Controls;
using System.Threading;
using System.Collections.ObjectModel;

namespace NavigateLaser.ViewModels
{
    class Mode0203ParamVM : NotificationObject
    {
        private Mode0203ParamView window { get; set; }
        public DelegateCommand ConfirmInfoCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }
        public DelegateCommand SelectionChangedCommand { get; set; }

        private Mode0203ParamM m_Mode0203ParamM;
        public Mode0203ParamM Mode0203ParamM
        {
            get { return m_Mode0203ParamM; }
            set
            {
                m_Mode0203ParamM = value;
                this.RaisePropertyChanged("Mode0203ParamM");
            }
        }

        public Mode0203ParamVM(object window)
        {
            this.window = (Mode0203ParamView)window;
            this.LoadMode0203ParamM();
            this.ConfirmInfoCommand = new DelegateCommand(new Action(this.ConfirmInfoCommandExecute));
            this.CancelCommand = new DelegateCommand(new Action(this.CancelCommandExecute));
            this.SelectionChangedCommand = new DelegateCommand(new Action(this.SelectionChangedCommandExecute));
        }

        private void LoadMode0203ParamM()
        {
            this.Mode0203ParamM = new Mode0203ParamM();
            this.Mode0203ParamM.MappingType = new ObservableCollection<string>()
            {
                {"普通模式"},
                {"添加模式"},
            };
            this.Mode0203ParamM.SelectedMappingType = "普通模式";
            this.Mode0203ParamM.ParamLayerID = 0;
            this.Mode0203ParamM.AverageTime = 200;
            this.Mode0203ParamM.CurrentSelfAngle = 0;
            this.Mode0203ParamM.CurrentSelfX = 0;
            this.Mode0203ParamM.CurrentSelfY = 0;
            this.Mode0203ParamM.ShapeOfLandmark = new ObservableCollection<string>()
            {
                {"圆柱"},
                {"平面"},
            };
            this.Mode0203ParamM.SelectedShape = "圆柱";
            this.Mode0203ParamM.SizeOfLandmark = 80;
        }

        private void ConfirmInfoCommandExecute()
        {
            this.window.Mode0203SetFlag = true;
            this.window.Mode0203ParamData.Add(0x00);
            if (this.Mode0203ParamM.SelectedMappingType == "普通模式")
            {
                this.window.Mode0203ParamData.Add(0x02);
            }
            else
            {
                this.window.Mode0203ParamData.Add(0x03);
            }
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.ParamLayerID >> 8) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)(this.Mode0203ParamM.ParamLayerID & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.AverageTime >> 8) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)(this.Mode0203ParamM.AverageTime & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfAngle >> 24) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfAngle >> 16) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfAngle >> 8) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)(this.Mode0203ParamM.CurrentSelfAngle & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfX >> 24) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfX >> 16) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfX >> 8) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)(this.Mode0203ParamM.CurrentSelfX & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfY >> 24) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfY >> 16) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)((this.Mode0203ParamM.CurrentSelfY >> 8) & 0x00ff));
            this.window.Mode0203ParamData.Add((byte)(this.Mode0203ParamM.CurrentSelfY & 0x00ff));
            if (this.Mode0203ParamM.SelectedShape == "圆柱")
            {
                this.window.Mode0203ParamData.Add(0x00);
            }
            else 
            {
                this.window.Mode0203ParamData.Add(0x02);
            }
            this.window.Mode0203ParamData.Add((byte)(this.Mode0203ParamM.SizeOfLandmark & 0x00ff));
            this.window.Close();
        }

        private void CancelCommandExecute()
        {
            this.window.Close();
        }

        private void SelectionChangedCommandExecute()
        {
            if (this.Mode0203ParamM.SelectedMappingType == "普通模式")
            {
                this.Mode0203ParamM.CurrentSelfAngle = 0;
                this.Mode0203ParamM.CurrentSelfX = 0;
                this.Mode0203ParamM.CurrentSelfY = 0;
            }
            else
            {
                this.Mode0203ParamM.CurrentSelfAngle = this.window.CurrentPoseInfo[0];
                this.Mode0203ParamM.CurrentSelfX = this.window.CurrentPoseInfo[1];
                this.Mode0203ParamM.CurrentSelfY = this.window.CurrentPoseInfo[2];
            }
        }

    }
}
