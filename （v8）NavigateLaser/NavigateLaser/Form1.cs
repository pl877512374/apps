using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;


namespace NavigateLaser
{
    public partial class Form1 : Form
    {
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CreatPCLView(IntPtr hWnd);
        [DllImport("PCLDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ChangePCLView(int Width, int Height);

        public Form1()
        {
            var wndHandle = this.Handle;
            CreatPCLView(wndHandle);
            InitializeComponent();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            Size formSize = this.Size;
            ChangePCLView(formSize.Width, formSize.Height);
        }
        
    }
}
