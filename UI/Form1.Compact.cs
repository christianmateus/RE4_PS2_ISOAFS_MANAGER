using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private sealed class CompactEntryPlan
        {
            public AfsEntry Entry { get; set; } = null!;
            public uint NewOffset { get; set; }
            public uint NewStoredSize { get; set; }
        }

        private sealed class CompactPlan
        {
            public List<CompactEntryPlan> Entries { get; set; } = new List<CompactEntryPlan>();
            public uint FirstDataOffset { get; set; }
            public uint NewTocOffset { get; set; }
            public uint TocSize { get; set; }
            public long NewFileSize { get; set; }
            public long SavedBytes { get; set; }
            public int EntriesWithRecoveredSpace { get; set; }
        }

        private void MenuAnalisarCompactacao_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                CompactPlan plan = CriarPlanoCompactacao();

                MessageBox.Show(
                    Tr(
                        $"Análise concluída.\n\nTamanho atual: {FormatarBytes(ObterAfsLength())}\nTamanho estimado após rebuild: {FormatarBytes(plan.NewFileSize)}\nEspaço recuperável: {FormatarBytes(plan.SavedBytes)}\nEntradas com espaço artificial recuperável: {plan.EntriesWithRecoveredSpace:N0}\n\nO rebuild manterá alinhamento de 0x800 ({AFS_ALIGNMENT:N0} bytes), os mesmos índices e a TOC original.",
                        $"Analysis complete.\n\nCurrent size: {FormatarBytes(ObterAfsLength())}\nEstimated size after rebuild: {FormatarBytes(plan.NewFileSize)}\nRecoverable space: {FormatarBytes(plan.SavedBytes)}\nEntries with recoverable artificial space: {plan.EntriesWithRecoveredSpace:N0}\n\nThe rebuild will keep 0x800 alignment ({AFS_ALIGNMENT:N0} bytes), the same indexes, and the original TOC."),
                    Tr("Análise de Compactação", "Compaction Analysis"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível analisar a compactação:\n\n{ex.Message}", $"Could not analyze compaction:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuCompactarRebuild_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CompactPlan plan;

            try
            {
                plan = CriarPlanoCompactacao();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"O AFS não passou nas verificações necessárias para um rebuild seguro:\n\n{ex.Message}", $"The AFS did not pass the checks required for a safe rebuild:\n\n{ex.Message}"), Tr("Compactação bloqueada", "Compaction blocked"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (plan.SavedBytes <= 0)
            {
                DialogResult semEconomia = MessageBox.Show(
                    Tr("Este AFS já parece estar compacto segundo o Current Size e o alinhamento 0x800.\n\nDeseja reconstruí-lo mesmo assim?", "This AFS already appears compact according to Current Size and 0x800 alignment.\n\nRebuild it anyway?"),
                    Tr("AFS já compacto", "AFS already compact"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (semEconomia != DialogResult.Yes)
                    return;
            }

            string nomeBase = Path.GetFileNameWithoutExtension(_isoAfsEntry?.Name ?? _afsPath);
            string extensao = Path.GetExtension(_isoAfsEntry?.Name ?? _afsPath);

            using SaveFileDialog dialog = new SaveFileDialog
            {
                Title = Tr("Salvar AFS compactado", "Save compacted AFS"),
                Filter = Tr("Arquivos AFS (*.afs)|*.afs|Todos os arquivos (*.*)|*.*", "AFS files (*.afs)|*.afs|All files (*.*)|*.*"),
                FileName = nomeBase + "_compact" + extensao,
                InitialDirectory = Path.GetDirectoryName(_afsPath)
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string destino = Path.GetFullPath(dialog.FileName);
            string origem = _isoAfsEntry == null ? Path.GetFullPath(_afsPath) : string.Empty;

            if (_isoAfsEntry == null && string.Equals(destino, origem, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    Tr("Por segurança, o rebuild não pode sobrescrever diretamente o AFS que está aberto.\n\nEscolha outro nome. Depois de validar o novo arquivo, você poderá substituí-lo manualmente.", "For safety, rebuild cannot directly overwrite the currently open AFS.\n\nChoose another name. After validating the new file, you can replace it manually."),
                    Tr("Escolha outro arquivo", "Choose another file"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirmacao = MessageBox.Show(
                Tr(
                    $"Criar um novo AFS compactado?\n\nAtual: {FormatarBytes(ObterAfsLength())}\nEstimado: {FormatarBytes(plan.NewFileSize)}\nEconomia: {FormatarBytes(plan.SavedBytes)}\nEntradas beneficiadas: {plan.EntriesWithRecoveredSpace:N0}\n\nO AFS original NÃO será alterado.",
                    $"Create a new compacted AFS?\n\nCurrent: {FormatarBytes(ObterAfsLength())}\nEstimated: {FormatarBytes(plan.NewFileSize)}\nSavings: {FormatarBytes(plan.SavedBytes)}\nBenefited entries: {plan.EntriesWithRecoveredSpace:N0}\n\nThe original AFS will NOT be changed."),
                Tr("Compactar / Rebuild AFS", "Compact / Rebuild AFS"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacao != DialogResult.Yes)
                return;

            string temporario = destino + ".building";

            try
            {
                if (File.Exists(temporario))
                    File.Delete(temporario);

                ExecutarCompactacao(plan, temporario);

                toolStripStatusLabel1.Text = Tr("Validando AFS reconstruído...", "Validating rebuilt AFS...");
                Application.DoEvents();

                ValidarAfsReconstruido(temporario, plan);

                if (File.Exists(destino))
                    File.Delete(destino);

                File.Move(temporario, destino);

                toolStripStatusLabel1.Text = Tr($"Rebuild concluído: {Path.GetFileName(destino)}", $"Rebuild complete: {Path.GetFileName(destino)}");

                DialogResult abrirNovo = MessageBox.Show(
                    Tr($"Rebuild concluído e validado.\n\nArquivo: {destino}\nTamanho final: {FormatarBytes(new FileInfo(destino).Length)}\nEconomia: {FormatarBytes(plan.SavedBytes)}\n\nDeseja abrir o novo AFS na ferramenta agora?", $"Rebuild completed and validated.\n\nFile: {destino}\nFinal size: {FormatarBytes(new FileInfo(destino).Length)}\nSavings: {FormatarBytes(plan.SavedBytes)}\n\nOpen the new AFS in the tool now?"),
                    Tr("Compactação concluída", "Compaction complete"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (abrirNovo == DialogResult.Yes)
                    AbrirAfs(destino);
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(temporario))
                        File.Delete(temporario);
                }
                catch
                {
                }

                toolStripStatusLabel1.Text = Tr("Falha no rebuild", "Rebuild failed");

                MessageBox.Show(
                    Tr($"O rebuild foi interrompido e o AFS original não foi alterado.\n\n{ex.Message}", $"The rebuild was interrupted and the original AFS was not changed.\n\n{ex.Message}"),
                    Tr("Erro no Rebuild", "Rebuild Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private CompactPlan CriarPlanoCompactacao()
        {
            if (_afsPath == null)
                throw new InvalidOperationException("Nenhum AFS está aberto.");

            long afsLength = ObterAfsLength();

            if (_entries.Count == 0)
                throw new InvalidDataException("O AFS não possui entradas carregadas.");

            long expectedTocSize = (long)_entries.Count * 48L;

            if (_tocOffset == 0 || _tocSize != expectedTocSize)
            {
                throw new InvalidDataException(
                    $"A compactação desta versão exige uma TOC padrão de 48 bytes por entrada.\n" +
                    $"Esperado: {expectedTocSize:N0} bytes\nEncontrado: {_tocSize:N0} bytes.");
            }

            if ((long)_tocOffset + _tocSize > afsLength)
                throw new InvalidDataException("A TOC atual ultrapassa os limites do AFS.");

            long headerEnd = 8L + ((long)_entries.Count * 8L) + 8L;

            List<AfsEntry> fisicas = _entries
                .Where(x => !x.IsEmpty && x.Offset > 0)
                .ToList();

            if (fisicas.Count == 0)
                throw new InvalidDataException("Nenhuma entrada física válida foi encontrada.");

            uint firstDataOffset = fisicas.Min(x => x.Offset);

            if (firstDataOffset < headerEnd)
                throw new InvalidDataException($"O primeiro arquivo começa antes do fim da tabela AFS. Header end: 0x{headerEnd:X}, First data: 0x{firstDataOffset:X8}");

            uint ultimoOffset = 0;
            bool encontrouFisica = false;

            foreach (AfsEntry entry in _entries)
            {
                if (entry.IsEmpty)
                    continue;

                if (entry.Offset == 0)
                    throw new InvalidDataException($"A entrada {entry.Index} não é vazia, mas possui offset zero.");

                if (encontrouFisica && entry.Offset < ultimoOffset)
                {
                    throw new InvalidDataException(
                        $"A ordem física não acompanha a ordem dos índices na entrada {entry.Index}.\n" +
                        "Para evitar reorganizar uma variante desconhecida do AFS, o rebuild foi bloqueado.");
                }

                uint tamanhoReal = ObterTamanhoReal(entry);

                if ((long)entry.Offset + tamanhoReal > afsLength)
                    throw new InvalidDataException($"A entrada {entry.Index} ({entry.FileName}) ultrapassa o fim do AFS usando o Current Size.");

                ultimoOffset = entry.Offset;
                encontrouFisica = true;
            }

            CompactPlan plan = new CompactPlan
            {
                FirstDataOffset = firstDataOffset,
                TocSize = _tocSize
            };

            long current = firstDataOffset;

            foreach (AfsEntry entry in _entries)
            {
                if (entry.IsEmpty)
                {
                    if (current > uint.MaxValue)
                        throw new InvalidDataException("O novo offset ultrapassaria o limite uint32 do formato AFS.");

                    plan.Entries.Add(new CompactEntryPlan
                    {
                        Entry = entry,
                        NewOffset = (uint)current,
                        NewStoredSize = EMPTY_SENTINEL
                    });

                    continue;
                }

                current = AlignUp(current, AFS_ALIGNMENT);

                if (current > uint.MaxValue)
                    throw new InvalidDataException("O novo offset ultrapassaria o limite uint32 do formato AFS.");

                uint actualSize = ObterTamanhoReal(entry);

                plan.Entries.Add(new CompactEntryPlan
                {
                    Entry = entry,
                    NewOffset = (uint)current,
                    NewStoredSize = actualSize
                });

                long compactSize = AlignUp(actualSize, AFS_ALIGNMENT);

                if (entry.AllocatedSize > compactSize)
                    plan.EntriesWithRecoveredSpace++;

                current += actualSize;
                current = AlignUp(current, AFS_ALIGNMENT);
            }

            current = AlignUp(current, AFS_ALIGNMENT);

            if (current > uint.MaxValue)
                throw new InvalidDataException("O novo TOC Offset ultrapassaria o limite uint32 do formato AFS.");

            plan.NewTocOffset = (uint)current;
            plan.NewFileSize = current + _tocSize;
            plan.SavedBytes = Math.Max(0, afsLength - plan.NewFileSize);

            return plan;
        }

        private void ExecutarCompactacao(CompactPlan plan, string destinoTemporario, Action<int, string>? progress = null)
        {
            if (_afsPath == null)
                throw new InvalidOperationException("Nenhum AFS está aberto.");

            using Stream origem = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
            using FileStream destino = new FileStream(destinoTemporario, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

            using BinaryWriter bw = new BinaryWriter(destino, System.Text.Encoding.ASCII, leaveOpen: true);

            // Header
            bw.Write((byte)0x41);
            bw.Write((byte)0x46);
            bw.Write((byte)0x53);
            bw.Write((byte)0x00);
            bw.Write((uint)_entries.Count);

            foreach (CompactEntryPlan item in plan.Entries)
            {
                bw.Write(item.NewOffset);
                bw.Write(item.NewStoredSize);
            }

            bw.Write(plan.NewTocOffset);
            bw.Write(plan.TocSize);

            long headerEnd = destino.Position;

            // Preserva qualquer área auxiliar/padding existente entre a tabela e o primeiro dado.
            if (plan.FirstDataOffset > headerEnd)
            {
                origem.Position = headerEnd;
                CopiarBytes(origem, destino, plan.FirstDataOffset - headerEnd);
            }

            int processados = 0;
            int totalFisicos = plan.Entries.Count(x => !x.Entry.IsEmpty);

            foreach (CompactEntryPlan item in plan.Entries)
            {
                AfsEntry entry = item.Entry;

                if (entry.IsEmpty)
                    continue;

                EscreverPaddingAte(destino, item.NewOffset);

                uint tamanhoReal = ObterTamanhoReal(entry);

                origem.Position = entry.Offset;
                CopiarBytes(origem, destino, tamanhoReal);

                long proximoAlinhado = AlignUp(destino.Position, AFS_ALIGNMENT);
                EscreverPaddingAte(destino, proximoAlinhado);

                processados++;

                if (processados % 10 == 0 || processados == totalFisicos)
                {
                    int percent = totalFisicos == 0 ? 100 : (int)((long)processados * 100L / totalFisicos);
                    progress?.Invoke(percent, $"{processados:N0}/{totalFisicos:N0}");
                }
            }

            EscreverPaddingAte(destino, plan.NewTocOffset);

            // Copia a TOC original byte a byte: nomes, timestamps e Current Size permanecem intactos.
            origem.Position = _tocOffset;
            CopiarBytes(origem, destino, _tocSize);

            destino.SetLength(plan.NewFileSize);
            destino.Flush(true);
        }

        private static void EscreverPaddingAte(Stream stream, long offsetDestino)
        {
            if (stream.Position > offsetDestino)
                throw new InvalidDataException($"Tentativa de escrever padding para trás: atual 0x{stream.Position:X}, destino 0x{offsetDestino:X}.");

            long quantidade = offsetDestino - stream.Position;

            if (quantidade <= 0)
                return;

            byte[] zeros = new byte[1024 * 1024];

            while (quantidade > 0)
            {
                int escrever = (int)Math.Min(zeros.Length, quantidade);
                stream.Write(zeros, 0, escrever);
                quantidade -= escrever;
            }
        }

        private void ValidarAfsReconstruido(string caminho, CompactPlan plan)
        {
            using FileStream fs = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader br = new BinaryReader(fs);

            byte[] magic = br.ReadBytes(4);

            if (magic.Length != 4 || magic[0] != 0x41 || magic[1] != 0x46 || magic[2] != 0x53 || magic[3] != 0x00)
                throw new InvalidDataException("Validação falhou: assinatura AFS inválida no arquivo reconstruído.");

            uint count = br.ReadUInt32();

            if (count != _entries.Count)
                throw new InvalidDataException($"Validação falhou: quantidade de entradas mudou. Esperado {_entries.Count}, encontrado {count}.");

            for (int i = 0; i < plan.Entries.Count; i++)
            {
                uint offset = br.ReadUInt32();
                uint size = br.ReadUInt32();
                CompactEntryPlan esperado = plan.Entries[i];

                if (offset != esperado.NewOffset)
                    throw new InvalidDataException($"Validação falhou na entrada {i}: offset esperado 0x{esperado.NewOffset:X8}, encontrado 0x{offset:X8}.");

                if (size != esperado.NewStoredSize)
                    throw new InvalidDataException($"Validação falhou na entrada {i}: tamanho esperado 0x{esperado.NewStoredSize:X8}, encontrado 0x{size:X8}.");

                if (!esperado.Entry.IsEmpty)
                {
                    if ((offset & (AFS_ALIGNMENT - 1)) != 0)
                        throw new InvalidDataException($"Validação falhou na entrada {i}: offset 0x{offset:X8} não está alinhado a 0x800.");

                    uint realSize = ObterTamanhoReal(esperado.Entry);

                    if ((long)offset + realSize > fs.Length)
                        throw new InvalidDataException($"Validação falhou na entrada {i}: conteúdo ultrapassa o fim do arquivo reconstruído.");
                }
            }

            uint tocOffset = br.ReadUInt32();
            uint tocSize = br.ReadUInt32();

            if (tocOffset != plan.NewTocOffset || tocSize != plan.TocSize)
                throw new InvalidDataException("Validação falhou: ponteiro/tamanho da TOC não corresponde ao plano de rebuild.");

            if ((long)tocOffset + tocSize != fs.Length)
                throw new InvalidDataException($"Validação falhou: fim da TOC ({(long)tocOffset + tocSize:N0}) não coincide com o tamanho final ({fs.Length:N0}).");

            if ((tocOffset & (AFS_ALIGNMENT - 1)) != 0)
                throw new InvalidDataException("Validação falhou: a nova TOC não está alinhada a 0x800.");

            // Confirma que a TOC foi copiada exatamente.
            using Stream original = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);

            original.Position = _tocOffset;
            fs.Position = tocOffset;

            CompararStreams(original, fs, tocSize, "TOC");
        }

        private static void CompararStreams(Stream a, Stream b, long quantidade, string descricao)
        {
            const int BUFFER = 1024 * 1024;
            byte[] bufferA = new byte[BUFFER];
            byte[] bufferB = new byte[BUFFER];
            long restante = quantidade;

            while (restante > 0)
            {
                int solicitar = (int)Math.Min(BUFFER, restante);
                int lidosA = a.Read(bufferA, 0, solicitar);
                int lidosB = b.Read(bufferB, 0, solicitar);

                if (lidosA != solicitar || lidosB != solicitar)
                    throw new EndOfStreamException($"Validação falhou ao comparar {descricao}.");

                for (int i = 0; i < solicitar; i++)
                {
                    if (bufferA[i] != bufferB[i])
                        throw new InvalidDataException($"Validação falhou: {descricao} reconstruída difere da original.");
                }

                restante -= solicitar;
            }
        }
    }
}
