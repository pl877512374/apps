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
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public bool LoginFlag;

        public LoginWindow(bool m_Flag)
        {
            LoginFlag = m_Flag;
            InitializeComponent();
            this.ApplyTheme("TwilightBlue");
            this.DataContext = new LoginWindowVM(this);
        }

    }
}
