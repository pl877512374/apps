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
    class LoginWindowVM : NotificationObject
    {
        private LoginWindow window { get; set; }
        public DelegateCommand<object> AuthenticationCommand { get; set; }
        public DelegateCommand CloseWinCommand { get; set; }
        

        private LoginWindowM m_LoginWindowM;
        public LoginWindowM LoginWindowM
        {
            get { return m_LoginWindowM; }
            set
            {
                m_LoginWindowM = value;
                this.RaisePropertyChanged("LoginWindowM");
            }
        }

        public LoginWindowVM(object window)
        {
            this.window = (LoginWindow)window;
            this.LoadLoginWindowM();
            this.AuthenticationCommand = new DelegateCommand<object>(new Action<object>(this.AuthenticationCommandExecute));
            this.CloseWinCommand = new DelegateCommand(new Action(this.CloseWinCommandExecute));
        }

        private void LoadLoginWindowM()
        {
            this.LoginWindowM = new LoginWindowM();
            this.LoginWindowM.LoginID = "授权的用户";
            this.LoginWindowM.TipContentTXT = "";
            if (this.window.LoginFlag == true)
            {
                this.LoginWindowM.BtLoginTXT = "退出";
            }
            else
            {
                this.LoginWindowM.BtLoginTXT = "登录";
            }
        }

        public void WindowClose()
        {
            this.window.Close();
        }

        private void CloseWinCommandExecute()
        {
            this.WindowClose();
        }

        private void AuthenticationCommandExecute(object m_Para)
        {
            if (this.LoginWindowM.BtLoginTXT == "登录")
            {
                var passwordBox = m_Para as PasswordBox;
                var password = passwordBox.Password;
                if (this.LoginWindowM.LoginID == "授权的用户")
                {
                    if (password == "wanji")
                    {
                        this.window.LoginFlag = true;
                        this.WindowClose();
                    }
                    else
                    {
                        this.window.LoginFlag = false;
                        this.LoginWindowM.TipContentTXT = "密码错误/与用户名不匹配";
                        Thread newThread = new Thread(new ThreadStart(UpDataTextBlock));
                        newThread.Start();
                    }
                }
            }
            else if (this.LoginWindowM.BtLoginTXT == "退出")
            {
                this.window.LoginFlag = false;
                this.WindowClose();
            }            
        }

        private void UpDataTextBlock()
        {
            Thread.Sleep(5000);
            this.LoginWindowM.TipContentTXT = "";
        }
    }
}
