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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Forms;
using NavigateLaser.ViewModels;

namespace NavigateLaser.Views
{
    /// <summary>
    /// PCLView.xaml 的交互逻辑
    /// </summary>
    public partial class PCLView : System.Windows.Controls.UserControl
    {
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CreatPCLView(IntPtr hWnd);

        private Form1 _form1 = new Form1();
        public PCLView()
        {
            InitializeComponent();
            this.DataContext = new PCLViewVM();

            _form1.TopLevel = false;
            _form1.FormBorderStyle = FormBorderStyle.None;
            PCLShowWinHost.Child = _form1;
        }

        private void PCLShowWinHost_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

    }
}
