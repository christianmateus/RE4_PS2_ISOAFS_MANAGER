using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private FileSystemWatcher? _externalWatcher;
        private System.Windows.Forms.Timer? _externalDebounce;
        private DateTime _externalEventUtc;
        private DateTime _ignoreExternalUntilUtc;
        private DateTime _watchedLastWriteUtc;
        private long _watchedLength;
        private bool _externalEventPending;
        private bool _externalPromptOpen;

        private void ConfigurarDeteccaoExterna()
        {
            _externalDebounce = new System.Windows.Forms.Timer { Interval = 350 };
            _externalDebounce.Tick += (_, _) => ProcessarAlteracaoExternaPendente();
            _externalDebounce.Start();
            FormClosed += (_, _) => DisposeExternalWatcher();
        }

        private void AtualizarMonitorArquivoExterno()
        {
            DisposeExternalWatcher();
            if (string.IsNullOrWhiteSpace(_containerPath) || !File.Exists(_containerPath)) return;
            try
            {
                FileInfo fi = new FileInfo(_containerPath);
                _watchedLastWriteUtc = fi.LastWriteTimeUtc; _watchedLength = fi.Length;
                string? dir = fi.DirectoryName; if (string.IsNullOrWhiteSpace(dir)) return;
                _externalWatcher = new FileSystemWatcher(dir, fi.Name)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                _externalWatcher.Changed += ExternalWatcher_Event;
                _externalWatcher.Deleted += ExternalWatcher_Event;
                _externalWatcher.Renamed += ExternalWatcher_Event;
            }
            catch { DisposeExternalWatcher(); }
        }

        private void DisposeExternalWatcher()
        {
            if (_externalWatcher == null) return;
            try { _externalWatcher.EnableRaisingEvents = false; _externalWatcher.Dispose(); } catch { }
            _externalWatcher = null;
        }

        private void ExternalWatcher_Event(object sender, FileSystemEventArgs e)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _externalEventPending = true;
                    _externalEventUtc = DateTime.UtcNow;
                }));
            }
            catch { }
        }

        private void MarcarAlteracaoInterna(int seconds = 30)
        {
            DateTime until = DateTime.UtcNow.AddSeconds(seconds);
            if (until > _ignoreExternalUntilUtc) _ignoreExternalUntilUtc = until;
        }

        private void AtualizarBaselineAlteracaoExterna()
        {
            if (string.IsNullOrWhiteSpace(_containerPath) || !File.Exists(_containerPath)) return;
            try
            {
                FileInfo fi = new FileInfo(_containerPath);
                _watchedLastWriteUtc = fi.LastWriteTimeUtc; _watchedLength = fi.Length;
                _externalEventPending = false;
            }
            catch { }
        }

        private void ProcessarAlteracaoExternaPendente()
        {
            if (!_externalEventPending || _externalPromptOpen || _containerPath == null) return;
            if ((DateTime.UtcNow - _externalEventUtc).TotalMilliseconds < 1200) return;

            if (DateTime.UtcNow <= _ignoreExternalUntilUtc)
            {
                AtualizarBaselineAlteracaoExterna();
                return;
            }

            if (!File.Exists(_containerPath))
            {
                _externalEventPending = false;
                _externalPromptOpen = true;
                try
                {
                    MessageBox.Show(this, Tr("O arquivo atualmente aberto foi removido ou renomeado por outro programa.", "The currently open file was removed or renamed by another program."), Tr("Arquivo alterado externamente", "File changed externally"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    toolStripStatusLabel1.Text = Tr("Arquivo aberto não está mais disponível.", "Open file is no longer available.");
                }
                finally { _externalPromptOpen = false; }
                DisposeExternalWatcher();
                return;
            }

            FileInfo fi;
            try { fi = new FileInfo(_containerPath); }
            catch { _externalEventPending = false; return; }
            if (fi.LastWriteTimeUtc == _watchedLastWriteUtc && fi.Length == _watchedLength) { _externalEventPending = false; return; }

            _externalEventPending = false; _externalPromptOpen = true;
            try
            {
                DialogResult result = MessageBox.Show(this,
                    Tr("O arquivo aberto foi modificado fora do RE4 PS2 ISO/AFS Manager.\n\nDeseja recarregar o arquivo agora? Alterações exibidas na interface serão atualizadas para refletir o conteúdo em disco.",
                       "The open file was modified outside RE4 PS2 ISO/AFS Manager.\n\nReload it now? The information shown in the interface will be refreshed to match the file on disk."),
                    Tr("Modificação externa detectada", "External modification detected"), MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes) RecarregarAposAlteracaoExterna();
                else
                {
                    AtualizarBaselineAlteracaoExterna();
                    toolStripStatusLabel1.Text = Tr("Alteração externa ignorada.", "External change ignored.");
                }
            }
            catch (Exception ex)
            {
                AtualizarBaselineAlteracaoExterna();
                MessageBox.Show(this, Tr($"Não foi possível recarregar o arquivo.\n\n{ex.Message}", $"Could not reload the file.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _externalPromptOpen = false; }
        }

        private void RecarregarAposAlteracaoExterna()
        {
            if (_containerPath == null) return;
            string path = _containerPath;
            string? internalPath = _isoAfsEntry?.FullPath;
            if (_isoAfsEntry == null)
            {
                AbrirAfsStandalone(path);
            }
            else
            {
                _isoFiles = Iso9660Reader.ReadAllFiles(path);
                IsoFileEntry? match = _isoFiles.FirstOrDefault(x => !x.IsDirectory && string.Equals(x.FullPath, internalPath, StringComparison.OrdinalIgnoreCase));
                if (match == null) throw new InvalidDataException(Tr("O AFS anteriormente selecionado não foi encontrado na ISO modificada.", "The previously selected AFS was not found in the modified ISO."));
                AbrirAfsDaIso(path, match);
            }
            toolStripStatusLabel1.Text = Tr("Arquivo recarregado após modificação externa.", "File reloaded after external modification.");
        }
    }
}
