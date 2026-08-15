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
        private void MenuImportarSelecionado_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dgvArquivos.SelectedRows.Count == 0 || dgvArquivos.SelectedRows[0].Tag is not AfsEntry entry)
            {
                MessageBox.Show(Tr("Selecione uma entrada para importar.", "Select an entry to import."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (entry.IsEmpty)
            {
                MessageBox.Show(Tr("Não é possível importar sobre uma entrada vazia.", "You cannot import over an empty entry."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string nomeAtual = string.IsNullOrWhiteSpace(entry.FileName) ? $"File_{entry.Index:D4}" : entry.FileName;
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Tr($"Importar sobre {nomeAtual}", $"Import over {nomeAtual}"),
                Filter = Tr("Todos os arquivos (*.*)|*.*", "All files (*.*)|*.*"),
                FileName = Path.GetFileName(nomeAtual)
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            ImportarArquivoSobreEntrada(entry, dialog.FileName);
        }

        private async void ImportarArquivoSobreEntrada(AfsEntry entry, string caminhoNovoArquivo)
        {
            if (_afsPath == null || entry.IsEmpty) return;
            string nomeAtual = string.IsNullOrWhiteSpace(entry.FileName) ? $"File_{entry.Index:D4}" : entry.FileName;
            FileInfo novoArquivo = new FileInfo(caminhoNovoArquivo);

            if (!novoArquivo.Exists)
            {
                MessageBox.Show(Tr("O arquivo selecionado não existe.", "The selected file does not exist."), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (ArquivoEhIdenticoAoAfs(entry, caminhoNovoArquivo))
            {
                MessageBox.Show(Tr("O arquivo selecionado já é idêntico ao conteúdo atual da entrada. Nenhuma alteração foi feita.", "The selected file is already identical to the current entry content. No changes were made."), Tr("Arquivo idêntico", "Identical file"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool precisaRealocar = novoArquivo.Length > entry.AllocatedSize;
            string modoPt = precisaRealocar
                ? $"O novo arquivo excede o Max Size em {(novoArquivo.Length - entry.AllocatedSize):N0} bytes. O AFS será reconstruído automaticamente e as entradas seguintes serão realocadas com alinhamento 0x800." + (_isoAfsEntry != null ? " Se o AFS encostar no próximo extent da ISO, o Manager poderá abrir espaço deslocando os dados posteriores sem alterar o LBA inicial do AFS." : "")
                : "O conteúdo será substituído no mesmo slot físico. A alocação atual será preservada.";
            string modoEn = precisaRealocar
                ? $"The new file exceeds Max Size by {(novoArquivo.Length - entry.AllocatedSize):N0} bytes. The AFS will be rebuilt automatically and subsequent entries will be relocated with 0x800 alignment." + (_isoAfsEntry != null ? " If the AFS reaches the next ISO extent, the Manager can insert space by shifting subsequent data without changing the AFS starting LBA." : "")
                : "The content will be replaced in the same physical slot. Current allocation will be preserved.";

            DialogResult confirmacao = MessageBox.Show(
                Tr($"Substituir a entrada selecionada?\n\nNome: {nomeAtual}\nCurrent Size: {ObterTamanhoReal(entry):N0} bytes\nNovo tamanho: {novoArquivo.Length:N0} bytes\nMax Size atual: {entry.AllocatedSize:N0} bytes\n\n{modoPt}", $"Replace the selected entry?\n\nName: {nomeAtual}\nCurrent Size: {ObterTamanhoReal(entry):N0} bytes\nNew size: {novoArquivo.Length:N0} bytes\nCurrent Max Size: {entry.AllocatedSize:N0} bytes\n\n{modoEn}"),
                Tr("Confirmar importação", "Confirm import"), MessageBoxButtons.YesNo, precisaRealocar ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
            if (confirmacao != DialogResult.Yes) return;

            try
            {
                if (precisaRealocar)
                {
                    if (_isoAfsEntry != null)
                        await ImportarEntradaComRealocacaoNaIsoAsync(entry, caminhoNovoArquivo);
                    else
                        ImportarEntradaComRealocacao(entry, caminhoNovoArquivo);
                }
                else
                {
                    MarcarAlteracaoInterna(60);
                    ImportarEntradaInPlace(entry, caminhoNovoArquivo);
                    ReabrirAfsAtualPreservandoBusca(entry.Index);
                }
                MostrarSucesso(Tr($"Importação concluída.\n\n{nomeAtual}\nNovo Current Size: {novoArquivo.Length:N0} bytes", $"Import complete.\n\n{nomeAtual}\nNew Current Size: {novoArquivo.Length:N0} bytes"), Tr("Importação concluída", "Import complete"));
            }
            catch (OperationCanceledException)
            {
                toolStripStatusLabel1.Text = Tr("Importação cancelada.", "Import canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a importação:\n\n{ex.Message}", $"Import error:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportarEntradaInPlace(AfsEntry entry, string caminhoNovoArquivo)
        {
            if (_afsPath == null)
                throw new InvalidOperationException("Nenhum AFS está aberto.");

            using Stream afs = AbrirAfsStream(FileAccess.ReadWrite, FileShare.None);
            ImportarEntradaInPlace(afs, entry, caminhoNovoArquivo);
            afs.Flush();
        }

        private void ImportarEntradaInPlace(Stream afs, AfsEntry entry, string caminhoNovoArquivo)
        {
            FileInfo novoArquivo = new FileInfo(caminhoNovoArquivo);

            if (!novoArquivo.Exists)
                throw new FileNotFoundException("O arquivo de importação não existe.", caminhoNovoArquivo);

            if (novoArquivo.Length > uint.MaxValue)
                throw new InvalidDataException("O arquivo é grande demais para o campo de tamanho do AFS.");

            if (novoArquivo.Length > entry.AllocatedSize)
                throw new InvalidDataException($"O arquivo '{novoArquivo.Name}' possui {novoArquivo.Length:N0} bytes, mas a entrada só permite {entry.AllocatedSize:N0} bytes.");

            if ((long)entry.Offset + entry.AllocatedSize > afs.Length)
                throw new InvalidDataException($"O slot físico da entrada {entry.Index} ultrapassa os limites do AFS.");

            afs.Position = entry.Offset;

            using (FileStream origem = new FileStream(caminhoNovoArquivo, FileMode.Open, FileAccess.Read, FileShare.Read))
                origem.CopyTo(afs, 1024 * 1024);

            long restanteSlot = entry.AllocatedSize - novoArquivo.Length;

            if (restanteSlot > 0)
                PreencherComZeros(afs, restanteSlot);

            DateTime agora = DateTime.Now;
            AtualizarTocDaEntrada(afs, entry, (uint)novoArquivo.Length, agora);

            entry.ActualSize = (uint)novoArquivo.Length;
            entry.TocYear = (ushort)agora.Year;
            entry.TocMonth = (ushort)agora.Month;
            entry.TocDay = (ushort)agora.Day;
            entry.TocHour = (ushort)agora.Hour;
            entry.TocMinute = (ushort)agora.Minute;
            entry.TocSecond = (ushort)agora.Second;
        }

        private void AtualizarTocDaEntrada(Stream afs, AfsEntry entry, uint novoTamanho, DateTime timestamp)
        {
            if (_tocOffset == 0 || _tocSize < (_entries.Count * 48L))
                throw new InvalidDataException("A TOC de 48 bytes não foi encontrada. A importação segura exige a TOC para atualizar o Current Size.");

            long metadataOffset = (long)_tocOffset + ((long)entry.Index * 48L) + 32L;

            if (metadataOffset < 0 || metadataOffset + 16 > afs.Length)
                throw new InvalidDataException("O metadata da entrada está fora dos limites físicos do AFS.");

            afs.Position = metadataOffset;

            using BinaryWriter bw = new BinaryWriter(afs, System.Text.Encoding.UTF8, leaveOpen: true);
            bw.Write((ushort)timestamp.Year);
            bw.Write((ushort)timestamp.Month);
            bw.Write((ushort)timestamp.Day);
            bw.Write((ushort)timestamp.Hour);
            bw.Write((ushort)timestamp.Minute);
            bw.Write((ushort)timestamp.Second);
            bw.Write(novoTamanho);
        }

        private static void PreencherComZeros(Stream stream, long quantidade)
        {
            byte[] zeros = new byte[1024 * 1024];
            long restante = quantidade;

            while (restante > 0)
            {
                int escrever = (int)Math.Min(zeros.Length, restante);
                stream.Write(zeros, 0, escrever);
                restante -= escrever;
            }
        }

        private static void MostrarArquivosGrandesDemais(List<ImportacaoPlanejada> grandes)
        {
            if (grandes.Count == 0)
                return;

            string texto = string.Join(
                Environment.NewLine,
                grandes.Take(25).Select(x => $"Index {x.Entry.Index:D6} | {x.Entry.FileName} | Novo: {x.NovoTamanho:N0} | Max: {x.Entry.AllocatedSize:N0}"));

            if (grandes.Count > 25)
                texto += $"{Environment.NewLine}... e mais {grandes.Count - 25:N0} arquivo(s).";

            MessageBox.Show(
                Tr("Os arquivos abaixo não foram importados porque excedem o Max Size da entrada:\n\n", "The files below were not imported because they exceed the entry Max Size:\n\n") + texto,
                Tr("Arquivos grandes demais", "Files too large"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ReabrirAfsAtualPreservandoBusca(int? selecionarIndex)
        {
            if (_afsPath == null)
                return;

            string busca = txtBuscar.Text;

            AbrirAfsAtual();
            txtBuscar.Text = busca;

            if (!selecionarIndex.HasValue)
                return;

            foreach (DataGridViewRow row in dgvArquivos.Rows)
            {
                if (row.Tag is AfsEntry item && item.Index == selecionarIndex.Value)
                {
                    row.Selected = true;
                    dgvArquivos.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        private class ImportacaoPlanejada
        {
            public AfsEntry Entry { get; set; } = null!;
            public string Caminho { get; set; } = string.Empty;
            public long NovoTamanho { get; set; }
        }
    }
}
