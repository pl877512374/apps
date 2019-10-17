using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.Regions;
using NavigateLaser.Views;


namespace NavigateLaser.ModuleDefinition
{
    class ModuleShellWindow : IModule
    {
        IUnityContainer container;
        IRegionManager regionManager;

        public ModuleShellWindow(IUnityContainer container, IRegionManager regionManager)
        {
            this.container = container;
            this.regionManager = regionManager;
        }
        public void Initialize()
        {
            RegisterViews();
        }

        private void RegisterViews()
        {
            regionManager.RegisterViewWithRegion("TopMenuWin", typeof(TopMenuView));

            regionManager.Regions["ParaWin"].Add(new BasicParamView(), "BasicParamView");
            regionManager.Regions["ParaWin"].Add(new PCLView(), "PCLView");          
            regionManager.Regions["ParaWin"].Add(new ProductionParamView(), "ProductionParamView");
            regionManager.Regions["ParaWin"].Add(new FunctionParamView(), "FunctionParamView"); 

            regionManager.RegisterViewWithRegion("MenuWin", typeof(MenuTreeView));
            regionManager.RegisterViewWithRegion("NetWin", typeof(NetWindow));
            
            regionManager.Regions["LMWin"].Add(new ScanInfoView(), "ScanInfoView");
            regionManager.Regions["LMWin"].Add(new LandmarkSetView(), "LandmarkSetView");
            regionManager.Regions["LMWin"].Deactivate(regionManager.Regions["LMWin"].GetView("ScanInfoView"));   //不显示  
        }

    }
}
