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
            Console.WriteLine("finish run");
        }
    }
}
