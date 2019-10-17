using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NavigateLaser.ViewModels;
using NavigateLaser.DataAccess;
using NavigateLaser.Models;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;

namespace NavigateLaser.Views
{
    /// <summary>
    /// MenuTreeView.xaml 的交互逻辑
    /// </summary>
    public partial class MenuTreeView : UserControl
    {
        IUnityContainer _container;
        IRegionManager _regionManager;

        public MenuTreeView(IUnityContainer container, IRegionManager regionManager)
        {
            InitializeComponent();
            _container = container;
            _regionManager = regionManager;
            this.DataContext = new MenuTreeVM(_regionManager);
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                TextBlock tb = e.OriginalSource as TextBlock;
                if (tb==null)
                {
                    return;
                }
                if ((tb.Text != "设备参数") && (tb.Text != "扫描数据") && (tb.Text != "靶标数据")) 
                {
                    if (tb != null)
                    {
                        switch (tb.Text)
                        {
                            case "基本参数":
                                _regionManager.Regions["ParaWin"].Activate(_regionManager.Regions["ParaWin"].GetView("BasicParamView"));
                                break;
                            case "出产参数":
                                _regionManager.Regions["ParaWin"].Activate(_regionManager.Regions["ParaWin"].GetView("ProductionParamView"));
                                break;
                            case "功能参数":
                                _regionManager.Regions["ParaWin"].Activate(_regionManager.Regions["ParaWin"].GetView("FunctionParamView"));
                                break;
                            case "3D显示":
                                _regionManager.Regions["ParaWin"].Activate(_regionManager.Regions["ParaWin"].GetView("PCLView"));
                                break;
                            case "扫描信息":
                                _regionManager.Regions["LMWin"].Activate(_regionManager.Regions["LMWin"].GetView("ScanInfoView"));
                                break;
                            case "靶标设置":
                                _regionManager.Regions["LMWin"].Activate(_regionManager.Regions["LMWin"].GetView("LandmarkSetView"));
                                break;
                              
                        }
                    }
                    e.Handled = true;
                }
               
            }

        }

    }
}
