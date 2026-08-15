using System;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private void MenuAbrir_Click(object? sender, EventArgs e)
        {
            BtnAbrir_Click(sender, e);
        }

        private void MenuSair_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
