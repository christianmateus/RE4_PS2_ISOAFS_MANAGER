using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private string? _containerPath;
        private long _afsBaseOffset;
        private long _afsLogicalLength;
        private IsoFileEntry? _isoAfsEntry;
        private List<IsoFileEntry> _isoFiles = new List<IsoFileEntry>();

        private ToolStripMenuItem? _menuAbrirIso;
        private ToolStripMenuItem? _menuIso;
        private ToolStripMenuItem? _menuEscolherAfsIso;
        private ToolStripMenuItem? _menuRebuildNaIso;
        private ToolStripMenuItem? _menuExportarAfsIso;

        private void ConfigurarIso()
        {
            _menuAbrirIso = new ToolStripMenuItem("Abrir ISO PS2...");
            _menuAbrirIso.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;
            _menuAbrirIso.Click += MenuAbrirIso_Click;
            menuArquivo.DropDownItems.Insert(1, _menuAbrirIso);

            _menuIso = new ToolStripMenuItem("ISO");
            _menuEscolherAfsIso = new ToolStripMenuItem("Escolher outro AFS da ISO...");
            _menuRebuildNaIso = new ToolStripMenuItem("Compactar / Rebuild diretamente na ISO...");
            _menuExportarAfsIso = new ToolStripMenuItem("Exportar AFS da ISO...");
            _menuEscolherAfsIso.Click += MenuEscolherAfsIso_Click;
            _menuExportarAfsIso.Click += MenuExportarAfsIso_Click;
            _menuRebuildNaIso.Click += MenuRebuildNaIso_Click;
            _menuIso.DropDownItems.Add(_menuEscolherAfsIso);
            _menuIso.DropDownItems.Add(_menuExportarAfsIso);
            _menuIso.DropDownItems.Add(new ToolStripSeparator());
            _menuIso.DropDownItems.Add(_menuRebuildNaIso);
            _menuIso.Enabled = false;
            menuStrip1.Items.Add(_menuIso);
            AplicarTema();
        }

        private Stream AbrirAfsStream(FileAccess access, FileShare share)
        {
            if (access != FileAccess.Read) MarcarAlteracaoInterna();
            if (_containerPath == null) throw new InvalidOperationException(Tr("Nenhum AFS está aberto.", "No AFS is open."));
            return new BoundedFileStream(_containerPath, _afsBaseOffset, _afsLogicalLength, access, share);
        }

        private long ObterAfsLength() => _afsLogicalLength;
        private string ObterNomeAfsAtual() => _isoAfsEntry != null ? _isoAfsEntry.FullPath : (_containerPath != null ? Path.GetFileName(_containerPath) : "-");

        private void AbrirAfsStandalone(string path)
        {
            _containerPath = path; _afsBaseOffset = 0; _afsLogicalLength = new FileInfo(path).Length; _isoAfsEntry = null; _isoFiles.Clear();
            if (_menuIso != null) _menuIso.Enabled = false;
            AbrirAfsAtual();
            AdicionarRecente(path);
        }

        private void AbrirAfsDaIso(string isoPath, IsoFileEntry entry)
        {
            _containerPath = isoPath; _afsBaseOffset = entry.DataOffset; _afsLogicalLength = entry.Size; _isoAfsEntry = entry;
            if (_menuIso != null) _menuIso.Enabled = true;
            AbrirAfsAtual();
        }

        private void MenuAbrirIso_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Tr("Abrir ISO de PlayStation 2", "Open PlayStation 2 ISO"),
                Filter = Tr("Imagens ISO (*.iso)|*.iso|Todos os arquivos (*.*)|*.*", "ISO images (*.iso)|*.iso|All files (*.*)|*.*")
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try { AbrirIso(dialog.FileName); }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível abrir a ISO.\n\n{ex.Message}", $"Could not open the ISO.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AbrirIso(string path)
        {
            toolStripStatusLabel1.Text = Tr("Lendo ISO9660...", "Reading ISO9660...");
            _isoFiles = Iso9660Reader.ReadAllFiles(path);
            List<IsoFileEntry> afs = _isoFiles.Where(x => !x.IsDirectory && x.Name.EndsWith(".AFS", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Size).ToList();
            if (afs.Count == 0)
            {
                MessageBox.Show(Tr("Nenhum arquivo .AFS foi encontrado na ISO.", "No .AFS file was found in the ISO."), "ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            IsoFileEntry? escolhido = EscolherAfs(afs);
            if (escolhido != null)
            {
                AbrirAfsDaIso(path, escolhido);
                AdicionarRecente(path);
            }
        }

        private void MenuEscolherAfsIso_Click(object? sender, EventArgs e)
        {
            if (_containerPath == null || _isoFiles.Count == 0) return;
            List<IsoFileEntry> afs = _isoFiles.Where(x => !x.IsDirectory && x.Name.EndsWith(".AFS", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Size).ToList();
            IsoFileEntry? escolhido = EscolherAfs(afs);
            if (escolhido != null) AbrirAfsDaIso(_containerPath, escolhido);
        }

        private IsoFileEntry? EscolherAfs(List<IsoFileEntry> afs)
        {
            if (afs.Count == 1) return afs[0];
            using Form form = new Form { Text = Tr("AFS encontrados na ISO", "AFS files found in ISO"), StartPosition = FormStartPosition.CenterParent, Width = 760, Height = 480, MinimizeBox = false, MaximizeBox = false };
            ListBox list = new ListBox { Dock = DockStyle.Fill };
            Button ok = new Button { Text = Tr("Abrir AFS", "Open AFS"), Dock = DockStyle.Bottom, Height = 38, DialogResult = DialogResult.OK };
            Button cancel = new Button { Text = Tr("Cancelar", "Cancel"), Dock = DockStyle.Bottom, Height = 38, DialogResult = DialogResult.Cancel };
            foreach (IsoFileEntry item in afs) list.Items.Add($"{item.FullPath}   [{FormatarBytes(item.Size)}]   LBA {item.Lba}");
            if (list.Items.Count > 0) list.SelectedIndex = 0;
            form.Controls.Add(list); form.Controls.Add(cancel); form.Controls.Add(ok); form.AcceptButton = ok; form.CancelButton = cancel;
            if (form.ShowDialog(this) != DialogResult.OK || list.SelectedIndex < 0) return null;
            return afs[list.SelectedIndex];
        }


        private async void MenuExportarAfsIso_Click(object? sender, EventArgs e)
        {
            if (_containerPath == null || _isoAfsEntry == null)
            {
                MessageBox.Show(Tr("Abra um AFS diretamente de uma ISO primeiro.", "Open an AFS directly from an ISO first."), "ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string nome = Path.GetFileName(_isoAfsEntry.FullPath);
            if (string.IsNullOrWhiteSpace(nome) || !nome.EndsWith(".afs", StringComparison.OrdinalIgnoreCase))
                nome = "exported.afs";

            using SaveFileDialog dialog = new SaveFileDialog
            {
                Title = Tr("Exportar AFS da ISO", "Export AFS from ISO"),
                Filter = Tr("Arquivos AFS (*.afs)|*.afs|Todos os arquivos (*.*)|*.*", "AFS files (*.afs)|*.afs|All files (*.*)|*.*"),
                FileName = nome,
                AddExtension = true,
                DefaultExt = "afs"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            string isoPath = _containerPath;
            long offset = _isoAfsEntry.DataOffset;
            long length = _isoAfsEntry.Size;
            string destino = dialog.FileName;

            try
            {
                await RebuildProgressForm.RunAsync(this, Tr("Exportando AFS", "Exporting AFS"), async progress =>
                {
                    await Task.Run(() =>
                    {
                        progress.Report(new RebuildProgressInfo { Percent = 2, Stage = Tr("Preparando exportação", "Preparing export"), Detail = _isoAfsEntry.FullPath });
                        using FileStream iso = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using FileStream output = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);
                        iso.Position = offset;
                        byte[] buffer = new byte[4 * 1024 * 1024];
                        long remaining = length;
                        long copied = 0;
                        while (remaining > 0)
                        {
                            int want = (int)Math.Min(buffer.Length, remaining);
                            int read = iso.Read(buffer, 0, want);
                            if (read <= 0) throw new EndOfStreamException(Tr("A ISO terminou antes do fim do AFS.", "The ISO ended before the end of the AFS."));
                            output.Write(buffer, 0, read);
                            remaining -= read; copied += read;
                            int pct = 3 + (int)(copied * 95L / Math.Max(1L, length));
                            progress.Report(new RebuildProgressInfo { Percent = Math.Min(98, pct), Stage = Tr("Exportando AFS", "Exporting AFS"), Detail = $"{FormatarBytes(copied)} / {FormatarBytes(length)}" });
                        }
                        output.Flush(true);
                        progress.Report(new RebuildProgressInfo { Percent = 100, Stage = Tr("Concluído", "Complete"), Detail = Tr("AFS exportado com sucesso.", "AFS exported successfully.") });
                    });
                });
                MostrarSucesso(Tr($"AFS exportado com sucesso.\n\n{destino}", $"AFS exported successfully.\n\n{destino}"), Tr("Exportação concluída", "Export complete"));
            }
            catch (Exception ex)
            {
                try { if (File.Exists(destino)) File.Delete(destino); } catch { }
                MessageBox.Show(this, Tr($"Não foi possível exportar o AFS.\n\n{ex.Message}", $"Could not export the AFS.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void MenuRebuildNaIso_Click(object? sender, EventArgs e)
        {
            if (_containerPath == null || _isoAfsEntry == null)
            {
                MessageBox.Show(Tr("Abra um AFS diretamente de uma ISO primeiro.", "Open an AFS directly from an ISO first."), "ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CompactPlan plan;
            try { plan = CriarPlanoCompactacao(); }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível preparar o rebuild da ISO.\n\n{ex.Message}", $"Could not prepare the ISO rebuild.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (plan.NewFileSize > _isoAfsEntry.Size)
            {
                MessageBox.Show(
                    Tr($"O AFS reconstruído ficaria maior que a área atualmente reservada na ISO.\n\nÁrea ISO: {FormatarBytes(_isoAfsEntry.Size)}\nRebuild: {FormatarBytes(plan.NewFileSize)}\n\nEsta versão bloqueia o crescimento para não deslocar outros arquivos da ISO.",
                       $"The rebuilt AFS would be larger than the area currently reserved in the ISO.\n\nISO area: {FormatarBytes(_isoAfsEntry.Size)}\nRebuild: {FormatarBytes(plan.NewFileSize)}\n\nThis version blocks growth to avoid moving other files in the ISO."),
                    Tr("Rebuild na ISO bloqueado", "ISO rebuild blocked"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                Tr($"Compactar o AFS diretamente dentro da ISO?\n\nISO: {Path.GetFileName(_containerPath)}\nAFS: {_isoAfsEntry.FullPath}\nTamanho atual: {FormatarBytes(_isoAfsEntry.Size)}\nNovo tamanho: {FormatarBytes(plan.NewFileSize)}\nEconomia: {FormatarBytes(_isoAfsEntry.Size - plan.NewFileSize)}\n\nRecomenda-se manter uma cópia de segurança da ISO.",
                   $"Compact the AFS directly inside the ISO?\n\nISO: {Path.GetFileName(_containerPath)}\nAFS: {_isoAfsEntry.FullPath}\nCurrent size: {FormatarBytes(_isoAfsEntry.Size)}\nNew size: {FormatarBytes(plan.NewFileSize)}\nSavings: {FormatarBytes(_isoAfsEntry.Size - plan.NewFileSize)}\n\nKeeping a backup copy of the ISO is recommended."),
                Tr("Rebuild direto na ISO", "Direct ISO rebuild"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            string isoPath = _containerPath;
            IsoFileEntry isoEntry = _isoAfsEntry;
            MarcarAlteracaoInterna(180);
            long oldIsoEntrySize = isoEntry.Size;
            string temp = Path.Combine(Path.GetTempPath(), $"afs_rebuild_{Guid.NewGuid():N}.afs");

            try
            {
                await RebuildProgressForm.RunAsync(this, Tr("Rebuild da ISO", "ISO Rebuild"), async progress =>
                {
                    await Task.Run(() =>
                    {
                        progress.Report(new RebuildProgressInfo { Percent = 2, Stage = Tr("Analisando estrutura", "Analyzing structure"), Detail = Tr("Preparando o plano de compactação...", "Preparing compaction plan...") });

                        progress.Report(new RebuildProgressInfo { Percent = 5, Stage = Tr("Reconstruindo AFS", "Rebuilding AFS"), Detail = Tr("Criando arquivo temporário...", "Creating temporary file...") });
                        ExecutarCompactacao(plan, temp, (pct, detail) =>
                        {
                            int mapped = 5 + (pct * 50 / 100);
                            progress.Report(new RebuildProgressInfo { Percent = mapped, Stage = Tr("Reconstruindo AFS", "Rebuilding AFS"), Detail = Tr($"Arquivos processados: {detail}", $"Files processed: {detail}") });
                        });

                        progress.Report(new RebuildProgressInfo { Percent = 58, Stage = Tr("Validando AFS", "Validating AFS"), Detail = Tr("Verificando offsets, tamanhos e TOC...", "Checking offsets, sizes, and TOC...") });
                        ValidarAfsReconstruido(temp, plan);

                        progress.Report(new RebuildProgressInfo { Percent = 65, Stage = Tr("Gravando na ISO", "Writing to ISO"), Detail = Tr("Copiando o AFS reconstruído para o LBA original...", "Copying rebuilt AFS to the original LBA...") });
                        using (FileStream iso = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        using (FileStream rebuilt = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            iso.Position = isoEntry.DataOffset;
                            byte[] buffer = new byte[1024 * 1024];
                            long copied = 0;
                            while (true)
                            {
                                int read = rebuilt.Read(buffer, 0, buffer.Length);
                                if (read <= 0) break;
                                iso.Write(buffer, 0, read);
                                copied += read;
                                int mapped = 65 + (int)(copied * 23L / Math.Max(1L, rebuilt.Length));
                                progress.Report(new RebuildProgressInfo { Percent = Math.Min(88, mapped), Stage = Tr("Gravando na ISO", "Writing to ISO"), Detail = $"{FormatarBytes(copied)} / {FormatarBytes(rebuilt.Length)}" });
                            }

                            long restante = oldIsoEntrySize - rebuilt.Length;
                            long totalRestante = restante;
                            byte[] zeros = new byte[1024 * 1024];
                            long zerado = 0;
                            while (restante > 0)
                            {
                                int escrever = (int)Math.Min(zeros.Length, restante);
                                iso.Write(zeros, 0, escrever);
                                restante -= escrever; zerado += escrever;
                                int mapped = 88 + (int)(zerado * 7L / Math.Max(1L, totalRestante));
                                progress.Report(new RebuildProgressInfo { Percent = Math.Min(95, mapped), Stage = Tr("Limpando espaço antigo", "Clearing old space"), Detail = Tr("Zerando a sobra do extent anterior...", "Zeroing the remainder of the previous extent...") });
                            }
                            iso.Flush(true);
                        }

                        progress.Report(new RebuildProgressInfo { Percent = 97, Stage = Tr("Atualizando ISO9660", "Updating ISO9660"), Detail = Tr("Atualizando o tamanho da entrada no diretório...", "Updating the directory entry size...") });
                        Iso9660Reader.UpdateFileSize(isoPath, isoEntry, checked((uint)plan.NewFileSize));
                        progress.Report(new RebuildProgressInfo { Percent = 100, Stage = Tr("Concluído", "Complete"), Detail = Tr("Rebuild finalizado com sucesso.", "Rebuild completed successfully.") });
                    });
                });

                _afsLogicalLength = plan.NewFileSize;
                AbrirAfsDaIso(isoPath, isoEntry);
                MostrarSucesso(
                    Tr("AFS reconstruído diretamente na ISO e o tamanho da entrada ISO9660 foi atualizado.\n\nO LBA inicial foi preservado; nenhum arquivo posterior foi deslocado.",
                       "AFS rebuilt directly in the ISO and the ISO9660 entry size was updated.\n\nThe initial LBA was preserved; no subsequent file was moved."),
                    Tr("Rebuild na ISO concluído", "ISO rebuild complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Falha no rebuild dentro da ISO.\n\n{ex.Message}", $"ISO rebuild failed.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }
    }
}
