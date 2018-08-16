using System;
using System.Windows.Forms;

namespace YukariController
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var yukariManager = new YukariManager();
            var tcpServer = new TcpServer(yukariManager.GetDispatcher());
            Console.WriteLine("finish run");
        }
    }
}
