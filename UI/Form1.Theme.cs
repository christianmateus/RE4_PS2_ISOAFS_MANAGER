using System;
using System.Drawing;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private bool _temaEscuro = true;

        private void MenuAlternarTema_Click(object? sender, EventArgs e)
        {
            _temaEscuro = !_temaEscuro;
            AplicarTema();
        }

        private void AplicarTema()
        {
            if (_temaEscuro)
                AplicarTemaEscuro();
            else
                AplicarTemaClaro();
        }

        private void AplicarTemaEscuro()
        {
            Color fundo = Color.FromArgb(24, 27, 32);
            Color painel = Color.FromArgb(31, 34, 40);
            Color cabecalho = Color.FromArgb(39, 43, 50);
            Color texto = Color.Gainsboro;
            Color textoForte = Color.WhiteSmoke;
            Color secundario = Color.Silver;

            BackColor = fundo;
            ForeColor = texto;

            txtBuscar.BackColor = painel;
            txtBuscar.ForeColor = textoForte;

            dgvArquivos.BackgroundColor = Color.FromArgb(26, 29, 34);
            dgvArquivos.GridColor = Color.FromArgb(52, 57, 65);
            dgvArquivos.DefaultCellStyle.BackColor = painel;
            dgvArquivos.DefaultCellStyle.ForeColor = texto;
            dgvArquivos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(44, 103, 176);
            dgvArquivos.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvArquivos.ColumnHeadersDefaultCellStyle.BackColor = cabecalho;
            dgvArquivos.ColumnHeadersDefaultCellStyle.ForeColor = textoForte;
            dgvArquivos.ColumnHeadersDefaultCellStyle.SelectionBackColor = cabecalho;
            dgvArquivos.ColumnHeadersDefaultCellStyle.SelectionForeColor = textoForte;

            groupAfs.ForeColor = texto;
            groupEntry.ForeColor = texto;

            lblMetadata.BackColor = Color.FromArgb(25, 28, 33);
            lblMetadata.ForeColor = Color.FromArgb(190, 220, 255);
            lblExcess.ForeColor = Color.FromArgb(255, 190, 90);

            menuStrip1.BackColor = Color.FromArgb(35, 38, 44);
            menuStrip1.ForeColor = Color.Gainsboro;
            AplicarTemaMenu(menuStrip1.Items, Color.FromArgb(35, 38, 44), Color.Gainsboro);

            statusStrip1.BackColor = Color.FromArgb(35, 38, 44);
            toolStripStatusLabel1.ForeColor = texto;
            menuStrip1.Renderer = new DarkMenuRenderer();
            AplicarTemaMenuContextoV120(Color.FromArgb(35, 38, 44), Color.Gainsboro, true);

            AplicarCorLabels(this, texto, secundario);

            menuAlternarTema.Text = Localization.Loc.T("LightTheme");
        }

        private void AplicarTemaClaro()
        {
            Color fundo = Color.FromArgb(245, 247, 250);
            Color painel = Color.White;
            Color cabecalho = Color.FromArgb(233, 237, 242);
            Color texto = Color.FromArgb(45, 49, 57);
            Color secundario = Color.FromArgb(90, 96, 105);

            BackColor = fundo;
            ForeColor = texto;

            txtBuscar.BackColor = painel;
            txtBuscar.ForeColor = texto;

            dgvArquivos.BackgroundColor = Color.FromArgb(235, 238, 242);
            dgvArquivos.GridColor = Color.FromArgb(220, 224, 230);
            dgvArquivos.DefaultCellStyle.BackColor = painel;
            dgvArquivos.DefaultCellStyle.ForeColor = texto;
            dgvArquivos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 115, 190);
            dgvArquivos.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvArquivos.ColumnHeadersDefaultCellStyle.BackColor = cabecalho;
            dgvArquivos.ColumnHeadersDefaultCellStyle.ForeColor = texto;
            dgvArquivos.ColumnHeadersDefaultCellStyle.SelectionBackColor = cabecalho;
            dgvArquivos.ColumnHeadersDefaultCellStyle.SelectionForeColor = texto;

            groupAfs.ForeColor = texto;
            groupEntry.ForeColor = texto;

            lblMetadata.BackColor = Color.White;
            lblMetadata.ForeColor = Color.FromArgb(35, 85, 135);
            lblExcess.ForeColor = Color.FromArgb(190, 100, 25);

            menuStrip1.BackColor = cabecalho;
            menuStrip1.ForeColor = texto;
            AplicarTemaMenu(menuStrip1.Items, Color.White, texto);

            statusStrip1.BackColor = Color.FromArgb(230, 233, 238);
            toolStripStatusLabel1.ForeColor = texto;
            menuStrip1.Renderer = new ToolStripProfessionalRenderer();
            AplicarTemaMenuContextoV120(Color.White, texto, false);

            AplicarCorLabels(this, texto, secundario);

            menuAlternarTema.Text = Localization.Loc.T("DarkTheme");
        }

        private static void AplicarTemaMenu(ToolStripItemCollection items, Color fundo, Color texto)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = item.Enabled
                    ? texto
                    : Color.FromArgb(115, 120, 128);

                item.BackColor = fundo;

                if (item is ToolStripMenuItem menu)
                {
                    menu.DropDown.BackColor = fundo;
                    menu.DropDown.ForeColor = texto;

                    AplicarTemaMenu(
                        menu.DropDownItems,
                        fundo,
                        texto);
                }
            }
        }

        private static void AplicarCorLabels(Control root, Color valor, Color titulo)
        {
            foreach (Control control in root.Controls)
            {
                if (control is Label label)
                {
                    if (label.Name.EndsWith("Titulo", StringComparison.Ordinal))
                        label.ForeColor = titulo;
                    else if (label.Name != "lblExcess" && label.Name != "lblMetadata")
                        label.ForeColor = valor;
                }

                if (control.HasChildren)
                    AplicarCorLabels(control, valor, titulo);
            }
        }
    }
}
