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
using System.Windows.Shapes;
using NavigateLaser.ViewModels;
using WPF.Themes;

namespace NavigateLaser.Views
{
    /// <summary>
    /// Mode0203ParamView.xaml 的交互逻辑
    /// </summary>
    public partial class Mode0203ParamView : Window
    {
        public bool Mode0203SetFlag;
        public int[] CurrentPoseInfo = new int[3];
        public List<byte> Mode0203ParamData= new List<byte>();
        public Mode0203ParamView(int[] TempPoseInfo)
        {
            Mode0203SetFlag = false;
            CurrentPoseInfo = TempPoseInfo;
            InitializeComponent();
            this.ApplyTheme("TwilightBlue");
            this.DataContext = new Mode0203ParamVM(this);
        }
    }
}
