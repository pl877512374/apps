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
    /// Mode01ParamView.xaml 的交互逻辑
    /// </summary>
    public partial class Mode01ParamView : Window
    {
        public bool Mode01SetFlag;
        public List<byte> Mode01ParamData= new List<byte>();
        public Mode01ParamView()
        {
            Mode01SetFlag = false;
            InitializeComponent();
            this.ApplyTheme("TwilightBlue");
            this.DataContext = new Mode01ParamVM(this);
        }
    }
}
