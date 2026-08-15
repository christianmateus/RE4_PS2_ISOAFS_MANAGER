using System;
using System.IO;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private ToolStripMenuItem? _menuCompararAfs;

        private void ConfigurarComparacaoAfs()
        {
            if (_menuFerramentasCompact == null || _menuCompararAfs != null) return;
            _menuCompararAfs = new ToolStripMenuItem(Tr("Comparar com outro AFS...", "Compare with another AFS..."));
            _menuCompararAfs.Click += MenuCompararAfs_Click;
            _menuFerramentasCompact.DropDownItems.Insert(0, _menuCompararAfs);
            _menuFerramentasCompact.DropDownItems.Insert(1, new ToolStripSeparator());
            AplicarTema();
        }

        private void AtualizarTextoComparacaoAfs()
        {
            if (_menuCompararAfs != null) _menuCompararAfs.Text = Tr("Comparar com outro AFS...", "Compare with another AFS...");
        }

        private void MenuCompararAfs_Click(object? sender, EventArgs e)
        {
            if (_containerPath == null || _afsPath == null)
            {
                MessageBox.Show(Tr("Abra um AFS primeiro.", "Open an AFS first."), Tr("Comparar AFS", "Compare AFS"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Tr("Selecionar AFS para comparar", "Select AFS to compare"),
                Filter = Tr("Arquivos AFS (*.afs)|*.afs|Todos os arquivos (*.*)|*.*", "AFS files (*.afs)|*.afs|All files (*.*)|*.*")
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var current = new AfsComparisonSource { Path = _containerPath, BaseOffset = _afsBaseOffset, Length = _afsLogicalLength, DisplayName = ObterNomeAfsAtual() };
                var other = new AfsComparisonSource { Path = dialog.FileName, BaseOffset = 0, Length = new FileInfo(dialog.FileName).Length, DisplayName = Path.GetFileName(dialog.FileName) };
                using var form = new AfsCompareForm(current, other, FerramentaAFS.Localization.Loc.English, _temaEscuro);
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível iniciar a comparação.\n\n{ex.Message}", $"Could not start comparison.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
