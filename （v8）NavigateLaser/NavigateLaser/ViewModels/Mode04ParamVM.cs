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

namespace NavigateLaser.ViewModels
{
    class Mode04ParamVM : NotificationObject
    {
        private Mode04ParamView window { get; set; }
        public DelegateCommand ConfirmInfoCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }


        private Mode04ParamM m_Mode04ParamM;
        public Mode04ParamM Mode04ParamM
        {
            get { return m_Mode04ParamM; }
            set
            {
                m_Mode04ParamM = value;
                this.RaisePropertyChanged("Mode04ParamM");
            }
        }

        public Mode04ParamVM(object window)
        {
            this.window = (Mode04ParamView)window;
            this.LoadMode04ParamM();
            this.ConfirmInfoCommand = new DelegateCommand(new Action(this.ConfirmInfoCommandExecute));
            this.CancelCommand = new DelegateCommand(new Action(this.CancelCommandExecute));
        }

        private void LoadMode04ParamM()
        {
            this.Mode04ParamM = new Mode04ParamM();
            this.Mode04ParamM.ParamLayerID = 0;
        }

        private void ConfirmInfoCommandExecute()
        {
            this.window.Mode04SetFlag = true;
            this.window.Mode04ParamData.Add(0x00);
            this.window.Mode04ParamData.Add(0x04);
            this.window.Mode04ParamData.Add(0x01);
            this.window.Mode04ParamData.Add(0x06);
            this.window.Mode04ParamData.Add((byte)((this.Mode04ParamM.ParamLayerID >> 8) & 0x00ff));
            this.window.Mode04ParamData.Add((byte)(this.Mode04ParamM.ParamLayerID & 0x00ff));
            this.window.Close();
        }

        private void CancelCommandExecute()
        {
            this.window.Close();
        }
    }
}
