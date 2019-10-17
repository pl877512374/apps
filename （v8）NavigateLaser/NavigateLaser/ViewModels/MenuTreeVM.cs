using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Practices.Prism.Commands;
using NavigateLaser.Models;
using System.Collections.ObjectModel;
using NavigateLaser.DataAccess;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Prism.Regions;


namespace NavigateLaser.ViewModels
{
    class MenuTreeVM: NotificationObject
    {
        TreeData m_TreeData = new TreeData();
        protected IEventAggregator eventAggregatorChangeView;
        protected SubscriptionToken tokenExChangeView;
        IRegionManager _regionManager;
        protected IEventAggregator eventAggregatorWorkModeViewChange;
        protected SubscriptionToken tokenWorkModeViewChange;
        
        private ObservableCollection<TreeFirstVM> _firstTrees;
        public ObservableCollection<TreeFirstVM> FirstTrees
        {
            get { return _firstTrees; }
            set
            {
                _firstTrees = value;
                this.RaisePropertyChanged("FirstTrees");
            }
        }

        public MenuTreeVM( IRegionManager regionManager)
        {
            _regionManager = regionManager;
            eventAggregatorChangeView = ServiceLocator.Current.GetInstance<IEventAggregator>();
            tokenExChangeView = eventAggregatorChangeView.GetEvent<LoginChangeViewEvent>().Subscribe(ChangeViewCommandEvent);
            this.eventAggregatorWorkModeViewChange = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.tokenWorkModeViewChange = eventAggregatorWorkModeViewChange.GetEvent<WorkModeViewEvent>().Subscribe(WorkModeViewChangeCommand);

            TreeFirstM[] firstTrees = m_TreeData.GetFirstTreesNotLogin();
            FirstTrees = new ObservableCollection<TreeFirstVM>(
                (from firstTree in firstTrees
                 select new TreeFirstVM(firstTree,0))
                .ToList());
        }

        private void ChangeViewCommandEvent(LoginChangeViewName m_LoginChangeViewName)
        {
            if (m_LoginChangeViewName.ChangeViewName == "授权用户登录")
            {
                TreeFirstM[] firstTrees = m_TreeData.GetFirstTrees();
                FirstTrees = new ObservableCollection<TreeFirstVM>(
                    (from firstTree in firstTrees
                     select new TreeFirstVM(firstTree,1))
                    .ToList());
                _regionManager.Regions["ParaWin"].Activate(_regionManager.Regions["ParaWin"].GetView("PCLView"));
                _regionManager.Regions["LMWin"].Activate(_regionManager.Regions["LMWin"].GetView("ScanInfoView"));
            }
            else if (m_LoginChangeViewName.ChangeViewName == "退出登录")
            {
                TreeFirstM[] firstTrees = m_TreeData.GetFirstTreesNotLogin();
                FirstTrees = new ObservableCollection<TreeFirstVM>(
                    (from firstTree in firstTrees
                     select new TreeFirstVM(firstTree,0))
                    .ToList());
                _regionManager.Regions["ParaWin"].Activate(_regionManager.Regions["ParaWin"].GetView("BasicParamView"));
                _regionManager.Regions["LMWin"].Deactivate(_regionManager.Regions["LMWin"].GetView("ScanInfoView"));
                _regionManager.Regions["LMWin"].Deactivate(_regionManager.Regions["LMWin"].GetView("LandmarkSetView"));                
            }
        }

        private void WorkModeViewChangeCommand(WorkModeViewContent m_WorkModeViewContent)
        {
            if (m_WorkModeViewContent.WorkModeViewName == "靶标获取模式")
            {
                _regionManager.Regions["LMWin"].Activate(_regionManager.Regions["LMWin"].GetView("LandmarkSetView"));
            }
            else if (m_WorkModeViewContent.WorkModeViewName == "连接后空模式")
            {

            }
            else
            {
                _regionManager.Regions["LMWin"].Activate(_regionManager.Regions["LMWin"].GetView("ScanInfoView"));
            }
        }
    }
}
