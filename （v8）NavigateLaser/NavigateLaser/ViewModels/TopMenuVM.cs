using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Practices.Prism.Commands;
using NavigateLaser.Views;
using NavigateLaser.Models;
using System.Windows;
using WPF.Themes;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;

namespace NavigateLaser.ViewModels
{
    class TopMenuVM : NotificationObject
    {
        public DelegateCommand LoginCommand { get; set; }
        public DelegateCommand<string> ChangeThemeCommand { get; set; }
        protected IEventAggregator eventAggregatorChangeView;
        protected IEventAggregator eventAggregatorViewIsEnable;
        protected SubscriptionToken tokenViewIsEnable;

        private TopMenuM m_TopMenuM;
        public TopMenuM TopMenuM
        {
            get { return m_TopMenuM; }
            set
            {
                m_TopMenuM = value;
                this.RaisePropertyChanged("TopMenuM");
            }
        }

        public TopMenuVM()
        {
            this.LoadTopMenuM();
            this.LoginCommand = new DelegateCommand(new Action(this.LoginCommandExecute));
            this.ChangeThemeCommand = new DelegateCommand<string>(new Action<string>(this.ChangeThemeCommandExecute));
            this.eventAggregatorChangeView = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.eventAggregatorViewIsEnable = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenViewIsEnable = eventAggregatorViewIsEnable.GetEvent<ViewIsEnableEvent>().Subscribe(ViewIsEnableCommand);    
        }

        private void LoadTopMenuM()
        {
            this.TopMenuM = new TopMenuM();
            this.TopMenuM.LoginResult = false;
            this.TopMenuM.SubMenuName = "用户登录";
            this.TopMenuM.IsEnable = true;
        }

        private void LoginCommandExecute()
        {
            LoginWindow LoginWin = new LoginWindow(this.TopMenuM.LoginResult);
            LoginChangeViewName m_LoginChangeViewName = new LoginChangeViewName();
            if (this.TopMenuM.SubMenuName == "用户登录")
            {
                LoginWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                LoginWin.Owner = Application.Current.MainWindow;
                LoginWin.ShowDialog();
                
                this.TopMenuM.LoginResult = LoginWin.LoginFlag;
                if (this.TopMenuM.LoginResult == true)
                {
                    this.TopMenuM.SubMenuName = "退出登录";
                    m_LoginChangeViewName.ChangeViewName = "授权用户登录";
                    this.eventAggregatorChangeView.GetEvent<LoginChangeViewEvent>().Publish(m_LoginChangeViewName);
                }
            }
            else if (this.TopMenuM.SubMenuName == "退出登录")
            {
                this.TopMenuM.LoginResult = false;
                this.TopMenuM.SubMenuName = "用户登录";
                m_LoginChangeViewName.ChangeViewName = "退出登录";
                this.eventAggregatorChangeView.GetEvent<LoginChangeViewEvent>().Publish(m_LoginChangeViewName);
            }
            
        }

        private void ChangeThemeCommandExecute(string para)
        {
            Window mainWindow = Application.Current.MainWindow;
            mainWindow.ApplyTheme(para);
        }

        private void ViewIsEnableCommand(ViewIsEnableContent m_ViewIsEnableContent)
        {
            this.TopMenuM.IsEnable = m_ViewIsEnableContent.IsEnable;
        }
    }
}
