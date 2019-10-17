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
    class Mode01ParamVM : NotificationObject
    {
        private Mode01ParamView window { get; set; }
        public DelegateCommand ConfirmInfoCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }


        private Mode01ParamM m_Mode01ParamM;
        public Mode01ParamM Mode01ParamM
        {
            get { return m_Mode01ParamM; }
            set
            {
                m_Mode01ParamM = value;
                this.RaisePropertyChanged("Mode01ParamM");
            }
        }

        public Mode01ParamVM(object window)
        {
            this.window = (Mode01ParamView)window;
            this.LoadMode01ParamM();
            this.ConfirmInfoCommand = new DelegateCommand(new Action(this.ConfirmInfoCommandExecute));
            this.CancelCommand = new DelegateCommand(new Action(this.CancelCommandExecute));
        }

        private void LoadMode01ParamM()
        {
            this.Mode01ParamM = new Mode01ParamM();
            this.Mode01ParamM.ShapeOfLandmark = new ObservableCollection<string>()
            {
                {"圆柱"},
                {"平面"},
            };
            this.Mode01ParamM.SelectedShape = "圆柱";
            this.Mode01ParamM.SizeOfLandmark = 80;
        }

        private void ConfirmInfoCommandExecute()
        {
            this.window.Mode01SetFlag = true;
            this.window.Mode01ParamData.Add(0x00);
            this.window.Mode01ParamData.Add(0x01);
            this.window.Mode01ParamData.Add(0x01);
            this.window.Mode01ParamData.Add(0x06);
            if (this.Mode01ParamM.SelectedShape == "圆柱")
            {
                this.window.Mode01ParamData.Add(0x00);
            }
            else 
            {
                this.window.Mode01ParamData.Add(0x02);
            }
            this.window.Mode01ParamData.Add((byte)(this.Mode01ParamM.SizeOfLandmark & 0x00ff));
            this.window.Close();
        }

        private void CancelCommandExecute()
        {
            this.window.Close();
        }
    }
}
