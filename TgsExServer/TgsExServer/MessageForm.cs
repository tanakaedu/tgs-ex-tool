using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TgsExServer
{
    public partial class MessageForm : Form
    {
        public string sMes;
        public MessageForm()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            textBox1.Visible = false;
            textBox1.Text = sMes;
            textBox1.Visible = true;
        }
    }
}
