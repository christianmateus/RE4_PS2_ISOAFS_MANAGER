using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private ContextMenuStrip? _menuContextoEntrada;
        private ToolStripMenuItem? _ctxExtrair;
        private ToolStripMenuItem? _ctxImportar;
        private ToolStripMenuItem? _ctxCopiarNome;
        private ToolStripMenuItem? _menuSubstituirAfs;

        private void ConfigurarAcoesV120()
        {
            dgvArquivos.MultiSelect = true;
            dgvArquivos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvArquivos.CellMouseDown += DgvArquivos_CellMouseDown_V120;

            _menuContextoEntrada = new ContextMenuStrip();
            _menuContextoEntrada.Opening += (_, e) =>
            {
                int count = ObterEntradasSelecionadas().Count;
                if (count == 0) { e.Cancel = true; return; }
                if (_ctxExtrair != null)
                    _ctxExtrair.Text = count > 1 ? Tr($"Extrair {count} selecionados...", $"Extract {count} selected...") : Tr("Extrair selecionado...", "Extract selected...");
                if (_ctxImportar != null)
                {
                    _ctxImportar.Enabled = count == 1;
                    _ctxImportar.Text = Tr("Importar sobre selecionado...", "Import over selected...");
                }
            };

            _ctxExtrair = new ToolStripMenuItem();
            _ctxExtrair.Click += MenuExtrairSelecionadosV120_Click;
            _ctxImportar = new ToolStripMenuItem();
            _ctxImportar.Click += MenuImportarSelecionado_Click;
            _ctxCopiarNome = new ToolStripMenuItem();
            _ctxCopiarNome.Click += (_, _) =>
            {
                AfsEntry? entry = ObterEntradasSelecionadas().FirstOrDefault();
                if (entry != null)
                    Clipboard.SetText(string.IsNullOrWhiteSpace(entry.FileName) ? $"File_{entry.Index:D4}" : entry.FileName!);
            };
            _menuContextoEntrada.Items.AddRange(new ToolStripItem[] { _ctxExtrair, _ctxImportar, new ToolStripSeparator(), _ctxCopiarNome });
            dgvArquivos.ContextMenuStrip = _menuContextoEntrada;

            // Replace the old single-selection extraction handler with the multi-selection-aware flow.
            menuExtrairSelecionado.Click -= BtnExtrair_Click;
            menuExtrairSelecionado.Click += MenuExtrairSelecionadosV120_Click;

            _menuSubstituirAfs = new ToolStripMenuItem();
            _menuSubstituirAfs.Click += MenuSubstituirAfs_Click;
            menuImportar.DropDownItems.Add(new ToolStripSeparator());
            menuImportar.DropDownItems.Add(_menuSubstituirAfs);

            AtualizarTextosAcoesV120();
            AplicarTema();
        }

        private void AtualizarTextosAcoesV120()
        {
            if (_ctxExtrair != null) _ctxExtrair.Text = Tr("Extrair selecionado...", "Extract selected...");
            if (_ctxImportar != null) _ctxImportar.Text = Tr("Importar sobre selecionado...", "Import over selected...");
            if (_ctxCopiarNome != null) _ctxCopiarNome.Text = Tr("Copiar nome", "Copy name");
            if (_menuSubstituirAfs != null) _menuSubstituirAfs.Text = Tr("Substituir AFS atual...", "Replace current AFS...");
        }

        private void DgvArquivos_CellMouseDown_V120(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
            DataGridViewRow row = dgvArquivos.Rows[e.RowIndex];
            if (!row.Selected)
            {
                dgvArquivos.ClearSelection();
                row.Selected = true;
            }
            if (e.ColumnIndex >= 0)
                dgvArquivos.CurrentCell = row.Cells[e.ColumnIndex];
        }

        private List<AfsEntry> ObterEntradasSelecionadas()
        {
            return dgvArquivos.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.Tag as AfsEntry)
                .Where(e => e != null)
                .Cast<AfsEntry>()
                .OrderBy(e => e.Index)
                .ToList();
        }

        private void MenuExtrairSelecionadosV120_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<AfsEntry> selecionadas = ObterEntradasSelecionadas();
            if (selecionadas.Count == 0)
            {
                MessageBox.Show(Tr("Selecione um ou mais arquivos para extrair.", "Select one or more files to extract."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selecionadas.Count == 1)
            {
                ExtrairUmaEntradaSelecionadaV120(selecionadas[0]);
                return;
            }

            List<AfsEntry> extraiveis = selecionadas.Where(x => !x.IsEmpty && ObterTamanhoReal(x) > 0).ToList();
            if (extraiveis.Count == 0)
            {
                MessageBox.Show(Tr("As entradas selecionadas não possuem conteúdo para extrair.", "The selected entries have no content to extract."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = Tr("Escolha a pasta onde os arquivos selecionados serão extraídos", "Choose the folder where the selected files will be extracted"),
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                int extraidos = 0;
                long totalBytes = 0;
                HashSet<string> usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using Stream origem = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
                using BatchProgressForm progresso = new BatchProgressForm(Tr("Extraindo selecionados", "Extracting selected"), extraiveis.Count);
                progresso.Show(this);

                foreach (AfsEntry entry in extraiveis)
                {
                    if (progresso.CancelRequested) break;
                    string relativo = ObterNomeSeguroParaExtracao(entry);
                    string destino = CriarCaminhoSeguro(dialog.SelectedPath, relativo);
                    if (!usados.Add(destino) || File.Exists(destino))
                    {
                        string dir = Path.GetDirectoryName(destino) ?? dialog.SelectedPath;
                        string nome = Path.GetFileNameWithoutExtension(destino);
                        string ext = Path.GetExtension(destino);
                        destino = Path.Combine(dir, $"{nome}_{entry.Index:D6}{ext}");
                        usados.Add(destino);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destino) ?? dialog.SelectedPath);
                    uint tamanho = ObterTamanhoReal(entry);
                    if ((long)entry.Offset + tamanho > origem.Length)
                        throw new InvalidDataException($"A entrada {entry.Index} ({entry.FileName}) ultrapassa os limites físicos do AFS.");
                    origem.Position = entry.Offset;
                    using FileStream saida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);
                    CopiarBytes(origem, saida, tamanho);
                    extraidos++;
                    totalBytes += tamanho;
                    progresso.Report(extraidos, Tr($"Extraindo {extraidos}/{extraiveis.Count}: {entry.FileName}", $"Extracting {extraidos}/{extraiveis.Count}: {entry.FileName}"));
                }

                toolStripStatusLabel1.Text = Tr($"Extraídos {extraidos:N0} arquivos selecionados", $"Extracted {extraidos:N0} selected files");
                MostrarSucesso(Tr($"Extração concluída.\n\nArquivos: {extraidos:N0}\nDados: {FormatarBytes(totalBytes)}", $"Extraction complete.\n\nFiles: {extraidos:N0}\nData: {FormatarBytes(totalBytes)}"), Tr("Extração concluída", "Extraction complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a extração dos selecionados:\n\n{ex.Message}", $"Error while extracting selected files:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExtrairUmaEntradaSelecionadaV120(AfsEntry entry)
        {
            if (entry.IsEmpty || ObterTamanhoReal(entry) == 0)
            {
                MessageBox.Show(Tr("Esta entrada não possui conteúdo para extrair.", "This entry has no content to extract."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string nome = ObterNomeSeguroParaExtracao(entry);
            using SaveFileDialog dialog = new SaveFileDialog
            {
                Title = Tr("Extrair arquivo", "Extract file"),
                FileName = Path.GetFileName(nome),
                Filter = Tr("Todos os arquivos (*.*)|*.*", "All files (*.*)|*.*")
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            try
            {
                ExtrairEntrada(entry, dialog.FileName);
                toolStripStatusLabel1.Text = Tr($"Extraído: {entry.FileName}", $"Extracted: {entry.FileName}");
                MostrarSucesso(Tr("Arquivo extraído com sucesso.", "File extracted successfully."), Tr("Extração concluída", "Extraction complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a extração:\n\n{ex.Message}", $"Extraction error:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuSubstituirAfs_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null || _containerPath == null)
            {
                MessageBox.Show(Tr("Abra um AFS primeiro.", "Open an AFS first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Tr("Selecionar AFS substituto", "Select replacement AFS"),
                Filter = Tr("Arquivos AFS (*.afs)|*.afs|Todos os arquivos (*.*)|*.*", "AFS files (*.afs)|*.afs|All files (*.*)|*.*")
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            string origem = dialog.FileName;
            try
            {
                ValidarAfsSubstituto(origem);
                long novoTamanho = new FileInfo(origem).Length;
                string alvo = _isoAfsEntry != null ? _isoAfsEntry.FullPath : Path.GetFileName(_containerPath);
                if (_isoAfsEntry != null && novoTamanho > _isoAfsEntry.Size)
                {
                    MessageBox.Show(Tr($"O novo AFS não cabe no espaço reservado dentro da ISO.\n\nDisponível: {FormatarBytes(_isoAfsEntry.Size)}\nNovo AFS: {FormatarBytes(novoTamanho)}\n\nA substituição foi bloqueada para não deslocar os arquivos seguintes da ISO.", $"The new AFS does not fit in the space reserved inside the ISO.\n\nAvailable: {FormatarBytes(_isoAfsEntry.Size)}\nNew AFS: {FormatarBytes(novoTamanho)}\n\nReplacement was blocked to avoid moving subsequent ISO files."), Tr("AFS grande demais", "AFS too large"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = MessageBox.Show(Tr($"Substituir completamente o AFS atual?\n\nAtual: {alvo}\nNovo: {Path.GetFileName(origem)}\nTamanho novo: {FormatarBytes(novoTamanho)}\n\nEsta operação altera o arquivo atual. Recomenda-se manter um backup.", $"Completely replace the current AFS?\n\nCurrent: {alvo}\nNew: {Path.GetFileName(origem)}\nNew size: {FormatarBytes(novoTamanho)}\n\nThis operation changes the current file. Keeping a backup is recommended."), Tr("Substituir AFS atual", "Replace current AFS"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                if (_isoAfsEntry != null)
                    SubstituirAfsDentroIso(origem, novoTamanho);
                else
                    SubstituirAfsStandalone(origem);

                MostrarSucesso(Tr("AFS atual substituído com sucesso.", "Current AFS replaced successfully."), Tr("Substituição concluída", "Replacement complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível substituir o AFS.\n\n{ex.Message}", $"Could not replace the AFS.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ValidarAfsSubstituto(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 16) throw new InvalidDataException("O arquivo é pequeno demais para ser um AFS válido.");
            byte[] magic = new byte[4];
            if (fs.Read(magic, 0, 4) != 4 || magic[0] != 0x41 || magic[1] != 0x46 || magic[2] != 0x53 || magic[3] != 0x00)
                throw new InvalidDataException("O arquivo selecionado não possui a assinatura AFS válida.");
            using BinaryReader br = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
            uint count = br.ReadUInt32();
            long min = 8L + ((long)count * 8L) + 8L;
            if (count == 0 || count > 1_000_000 || min > fs.Length)
                throw new InvalidDataException("A tabela de entradas do AFS substituto é inválida ou truncada.");
        }

        private void SubstituirAfsStandalone(string origem)
        {
            MarcarAlteracaoInterna(60);
            if (_containerPath == null) throw new InvalidOperationException();
            string destino = _containerPath;
            if (string.Equals(Path.GetFullPath(origem), Path.GetFullPath(destino), StringComparison.OrdinalIgnoreCase))
            {
                AbrirAfsStandalone(destino);
                return;
            }
            string temp = destino + $".replace_{Guid.NewGuid():N}.tmp";
            try
            {
                File.Copy(origem, temp, true);
                using (FileStream check = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read)) { if (check.Length == 0) throw new IOException("Falha ao criar a cópia temporária."); }
                File.Move(temp, destino, true);
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
            AbrirAfsStandalone(destino);
        }

        private void AplicarTemaMenuContextoV120(System.Drawing.Color fundo, System.Drawing.Color texto, bool escuro)
        {
            if (_menuContextoEntrada == null) return;
            _menuContextoEntrada.BackColor = fundo;
            _menuContextoEntrada.ForeColor = texto;
            _menuContextoEntrada.Renderer = escuro ? new DarkMenuRenderer() : new ToolStripProfessionalRenderer();
            foreach (ToolStripItem item in _menuContextoEntrada.Items)
            {
                item.BackColor = fundo;
                item.ForeColor = texto;
            }
        }

        private void SubstituirAfsDentroIso(string origem, long novoTamanho)
        {
            MarcarAlteracaoInterna(60);
            if (_containerPath == null || _isoAfsEntry == null) throw new InvalidOperationException();
            string isoPath = _containerPath;
            IsoFileEntry entry = _isoAfsEntry;
            long reservado = entry.Size;
            using (FileStream iso = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (FileStream src = new FileStream(origem, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                iso.Position = entry.DataOffset;
                src.CopyTo(iso, 1024 * 1024);
                long restante = reservado - novoTamanho;
                if (restante > 0) PreencherComZeros(iso, restante);
                iso.Flush(true);
            }
            Iso9660Reader.UpdateFileSize(isoPath, entry, checked((uint)novoTamanho));
            entry.Size = checked((uint)novoTamanho);
            _afsLogicalLength = novoTamanho;
            AbrirAfsDaIso(isoPath, entry);
        }
    }
}
