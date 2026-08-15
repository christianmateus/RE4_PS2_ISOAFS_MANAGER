using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private const int MAX_RECENT_FILES = 10;
        private ToolStripMenuItem? _menuRecentes;
        private ToolStripMenuItem? _menuLimparRecentes;

        private void ConfigurarRecentes()
        {
            _menuRecentes = new ToolStripMenuItem();
            _menuLimparRecentes = new ToolStripMenuItem();
            _menuLimparRecentes.Click += (_, _) =>
            {
                _settings.RecentFiles.Clear();
                _settings.Save();
                AtualizarMenuRecentes();
            };

            int separatorIndex = menuArquivo.DropDownItems.IndexOf(menuArquivoSep1);
            int insertAt = separatorIndex >= 0 ? separatorIndex : menuArquivo.DropDownItems.Count;
            menuArquivo.DropDownItems.Insert(insertAt, _menuRecentes);
            AtualizarMenuRecentes();
        }

        private void AdicionarRecente(string path)
        {
            string fullPath;
            try { fullPath = Path.GetFullPath(path); }
            catch { return; }

            _settings.RecentFiles.RemoveAll(x => string.Equals(x, fullPath, StringComparison.OrdinalIgnoreCase));
            _settings.RecentFiles.Insert(0, fullPath);
            if (_settings.RecentFiles.Count > MAX_RECENT_FILES)
                _settings.RecentFiles.RemoveRange(MAX_RECENT_FILES, _settings.RecentFiles.Count - MAX_RECENT_FILES);
            _settings.Save();
            AtualizarMenuRecentes();
        }

        private void AtualizarMenuRecentes()
        {
            if (_menuRecentes == null || _menuLimparRecentes == null) return;

            _menuRecentes.Text = Tr("Arquivos recentes", "Recent files");
            _menuLimparRecentes.Text = Tr("Limpar lista", "Clear list");
            _menuRecentes.DropDownItems.Clear();

            var existentes = _settings.RecentFiles.Where(File.Exists).ToList();
            if (existentes.Count != _settings.RecentFiles.Count)
            {
                _settings.RecentFiles = existentes;
                _settings.Save();
            }

            if (existentes.Count == 0)
            {
                _menuRecentes.DropDownItems.Add(new ToolStripMenuItem(Tr("(nenhum)", "(none)")) { Enabled = false });
            }
            else
            {
                for (int i = 0; i < existentes.Count; i++)
                {
                    string path = existentes[i];
                    string kind = Path.GetExtension(path).Equals(".iso", StringComparison.OrdinalIgnoreCase) ? "ISO" : "AFS";
                    var item = new ToolStripMenuItem($"{i + 1}. [{kind}] {Path.GetFileName(path)}") { ToolTipText = path, Tag = path };
                    item.Click += MenuRecente_Click;
                    _menuRecentes.DropDownItems.Add(item);
                }
                _menuRecentes.DropDownItems.Add(new ToolStripSeparator());
            }
            _menuRecentes.DropDownItems.Add(_menuLimparRecentes);
            AplicarTema();
        }

        private void MenuRecente_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string path) return;
            AbrirCaminhoExterno(path);
        }

        private void AbrirCaminhoExterno(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(Tr("O arquivo não existe mais e será removido dos recentes.", "The file no longer exists and will be removed from recent files."), Tr("Arquivo não encontrado", "File not found"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _settings.RecentFiles.RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
                _settings.Save();
                AtualizarMenuRecentes();
                return;
            }

            try
            {
                if (Path.GetExtension(path).Equals(".iso", StringComparison.OrdinalIgnoreCase))
                    AbrirIso(path);
                else
                {
                    AbrirAfsStandalone(path);
                    AdicionarRecente(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível abrir o arquivo.\n\n{ex.Message}", $"Could not open the file.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
