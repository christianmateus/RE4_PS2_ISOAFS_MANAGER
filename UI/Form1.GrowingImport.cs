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
        /// <summary>
        /// Imports a file that no longer fits in the entry's current physical slot.
        /// A new AFS is built with the same indexes/TOC and 0x800 alignment, then it
        /// replaces the standalone AFS or the AFS extent inside the ISO.
        /// </summary>
        private void ImportarEntradaComRealocacao(AfsEntry entry, string caminhoNovoArquivo)
        {
            if (_afsPath == null || _containerPath == null)
                throw new InvalidOperationException("Nenhum AFS está aberto.");

            FileInfo novoArquivo = new FileInfo(caminhoNovoArquivo);
            if (!novoArquivo.Exists)
                throw new FileNotFoundException("O arquivo de importação não existe.", caminhoNovoArquivo);
            if (novoArquivo.Length > uint.MaxValue)
                throw new InvalidDataException("O arquivo é grande demais para o campo de tamanho do AFS.");

            CompactPlan plan = CriarPlanoImportacaoExpandida(entry, checked((uint)novoArquivo.Length));

            string temp = Path.Combine(Path.GetTempPath(), $"afs_grow_{Guid.NewGuid():N}.afs");
            try
            {
                ConstruirAfsComEntradaSubstituida(plan, entry, caminhoNovoArquivo, temp);
                ValidarAfsComEntradaSubstituida(temp, plan, entry, caminhoNovoArquivo);

                if (_isoAfsEntry != null)
                    GarantirEspacoParaAfsExpandidoNaIso(plan.NewFileSize);

                MarcarAlteracaoInterna(900);

                if (_isoAfsEntry == null)
                {
                    string atual = _containerPath;
                    SubstituirAfsStandalone(temp);
                    SelecionarEntradaPorIndice(entry.Index);
                    toolStripStatusLabel1.Text = Tr(
                        $"Importado com realocação: {entry.FileName} - AFS reconstruído para {FormatarBytes(new FileInfo(atual).Length)}",
                        $"Imported with relocation: {entry.FileName} - AFS rebuilt to {FormatarBytes(new FileInfo(atual).Length)}");
                }
                else
                {
                    // SubstituirAfsDentroIso normally regards the current ISO9660 size as
                    // the reservation. Growth is safe here because we already calculated
                    // the physical gap up to the next extent, so write it directly.
                    AplicarAfsExpandidoNaIso(temp, plan.NewFileSize);
                    SelecionarEntradaPorIndice(entry.Index);
                    toolStripStatusLabel1.Text = Tr(
                        $"Importado com realocação dentro da ISO: {entry.FileName}",
                        $"Imported with relocation inside ISO: {entry.FileName}");
                }
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }


        private async Task ImportarEntradaComRealocacaoNaIsoAsync(AfsEntry entry, string caminhoNovoArquivo)
        {
            if (_containerPath == null || _isoAfsEntry == null)
                throw new InvalidOperationException(Tr("Nenhum AFS de ISO está aberto.", "No ISO AFS is open."));

            FileInfo novoArquivo = new FileInfo(caminhoNovoArquivo);
            if (!novoArquivo.Exists)
                throw new FileNotFoundException(Tr("O arquivo de importação não existe.", "The import file does not exist."), caminhoNovoArquivo);
            if (novoArquivo.Length > uint.MaxValue)
                throw new InvalidDataException(Tr("O arquivo é grande demais para o campo de tamanho do AFS.", "The file is too large for the AFS size field."));

            CompactPlan plan = CriarPlanoImportacaoExpandida(entry, checked((uint)novoArquivo.Length));
            string isoPath = _containerPath;
            string afsPath = _isoAfsEntry.FullPath;
            long capacidadeAtual = ObterCapacidadeFisicaAfsNaIso();
            long inserir = 0;
            long inicioDeslocamento = 0;

            if (plan.NewFileSize > capacidadeAtual)
            {
                long faltam = plan.NewFileSize - capacidadeAtual;
                inserir = AlignUp(faltam, AFS_ALIGNMENT);
                inicioDeslocamento = _isoAfsEntry.DataOffset + capacidadeAtual;
                DialogResult result = MessageBox.Show(this,
                    Tr(
                        $"O AFS reconstruído precisa de {FormatarBytes(faltam)} além do espaço contíguo atual.\n\nPara concluir a importação, o Manager precisa abrir {FormatarBytes(inserir)} na ISO deslocando os dados posteriores. Essa etapa pode levar algum tempo, mas será exibida em uma janela de progresso.\n\nISO: {Path.GetFileName(isoPath)}\nAFS: {afsPath}\n\nContinuar?\n\nRecomenda-se manter um backup da ISO.",
                        $"The rebuilt AFS needs {FormatarBytes(faltam)} beyond the current contiguous space.\n\nTo finish the import, the Manager must insert {FormatarBytes(inserir)} into the ISO by shifting subsequent data. This step may take some time and will be shown in a progress window.\n\nISO: {Path.GetFileName(isoPath)}\nAFS: {afsPath}\n\nContinue?\n\nKeeping a backup of the ISO is recommended."),
                    Tr("Expandir espaço do AFS na ISO", "Expand AFS space in ISO"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    throw new OperationCanceledException();
            }

            long inserirFinal = inserir;
            long inicioFinal = inicioDeslocamento;
            string temp = Path.Combine(Path.GetTempPath(), $"afs_grow_{Guid.NewGuid():N}.afs");
            List<IsoFileEntry>? arquivosAtualizados = null;
            IsoFileEntry? entradaAtualizada = null;

            try
            {
                MarcarAlteracaoInterna(900);
                await RebuildProgressForm.RunAsync(this, Tr("Importando arquivo maior", "Importing larger file"), async progress =>
                {
                    await Task.Run(() =>
                    {
                        progress.Report(new RebuildProgressInfo { Percent = 2, Stage = Tr("Preparando importação", "Preparing import"), Detail = Tr("Calculando o novo layout do AFS...", "Calculating the new AFS layout...") });

                        progress.Report(new RebuildProgressInfo { Percent = 5, Stage = Tr("Reconstruindo AFS", "Rebuilding AFS"), Detail = Tr("Realocando as entradas...", "Relocating entries...") });
                        ConstruirAfsComEntradaSubstituida(plan, entry, caminhoNovoArquivo, temp, (done, total, name) =>
                        {
                            int pct = 5 + (int)(done * 38L / Math.Max(1, total));
                            progress.Report(new RebuildProgressInfo { Percent = Math.Min(43, pct), Stage = Tr("Reconstruindo AFS", "Rebuilding AFS"), Detail = $"{done:N0}/{total:N0} - {name}" });
                        });

                        progress.Report(new RebuildProgressInfo { Percent = 46, Stage = Tr("Validando AFS", "Validating AFS"), Detail = Tr("Verificando o arquivo reconstruído...", "Checking the rebuilt file...") });
                        ValidarAfsComEntradaSubstituida(temp, plan, entry, caminhoNovoArquivo);

                        if (inserirFinal > 0)
                        {
                            progress.Report(new RebuildProgressInfo { Percent = 50, Stage = Tr("Expandindo ISO", "Expanding ISO"), Detail = Tr("Deslocando os dados posteriores para abrir espaço...", "Shifting subsequent data to make room...") });
                            Iso9660Reader.InsertSpaceBeforeExtent(isoPath, inicioFinal, inserirFinal, (moved, total) =>
                            {
                                int pct = 50 + (int)(moved * 27L / Math.Max(1L, total));
                                progress.Report(new RebuildProgressInfo { Percent = Math.Min(77, pct), Stage = Tr("Expandindo ISO", "Expanding ISO"), Detail = $"{FormatarBytes(moved)} / {FormatarBytes(total)}" });
                            });
                            arquivosAtualizados = Iso9660Reader.ReadAllFiles(isoPath);
                            entradaAtualizada = arquivosAtualizados.FirstOrDefault(x => !x.IsDirectory && string.Equals(x.FullPath, afsPath, StringComparison.OrdinalIgnoreCase));
                            if (entradaAtualizada == null)
                                throw new InvalidDataException(Tr("A ISO foi expandida, mas o AFS não pôde ser reencontrado.", "The ISO was expanded, but the AFS could not be found again."));
                        }
                        else
                        {
                            arquivosAtualizados = new List<IsoFileEntry>(_isoFiles);
                            entradaAtualizada = arquivosAtualizados.FirstOrDefault(x => !x.IsDirectory && string.Equals(x.FullPath, afsPath, StringComparison.OrdinalIgnoreCase)) ?? _isoAfsEntry;
                        }

                        IsoFileEntry activeEntry = entradaAtualizada ?? throw new InvalidDataException(Tr("O AFS selecionado não está mais disponível na ISO.", "The selected AFS is no longer available in the ISO."));
                        List<IsoFileEntry> activeFiles = arquivosAtualizados ?? throw new InvalidDataException(Tr("A lista de arquivos da ISO não pôde ser atualizada.", "The ISO file list could not be refreshed."));
                        long capacidade = activeFiles
                            .Where(x => x.DataOffset > activeEntry.DataOffset)
                            .Select(x => x.DataOffset)
                            .DefaultIfEmpty(new FileInfo(isoPath).Length)
                            .Min() - activeEntry.DataOffset;
                        if (plan.NewFileSize > capacidade)
                            throw new InvalidDataException(Tr("Mesmo após a expansão, não há espaço físico suficiente para o AFS reconstruído.", "Even after expansion, there is not enough physical space for the rebuilt AFS."));

                        progress.Report(new RebuildProgressInfo { Percent = 80, Stage = Tr("Gravando AFS na ISO", "Writing AFS to ISO"), Detail = Tr("Copiando o AFS reconstruído...", "Copying the rebuilt AFS...") });
                        using (FileStream iso = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        using (FileStream src = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            iso.Position = activeEntry.DataOffset;
                            byte[] buffer = new byte[4 * 1024 * 1024];
                            long copied = 0;
                            while (true)
                            {
                                int read = src.Read(buffer, 0, buffer.Length);
                                if (read <= 0) break;
                                iso.Write(buffer, 0, read);
                                copied += read;
                                int pct = 80 + (int)(copied * 16L / Math.Max(1L, src.Length));
                                progress.Report(new RebuildProgressInfo { Percent = Math.Min(96, pct), Stage = Tr("Gravando AFS na ISO", "Writing AFS to ISO"), Detail = $"{FormatarBytes(copied)} / {FormatarBytes(src.Length)}" });
                            }
                            if (plan.NewFileSize < activeEntry.Size)
                                PreencherComZeros(iso, activeEntry.Size - plan.NewFileSize);
                            iso.Flush(true);
                        }

                        progress.Report(new RebuildProgressInfo { Percent = 98, Stage = Tr("Atualizando ISO9660", "Updating ISO9660"), Detail = Tr("Atualizando o tamanho do AFS...", "Updating AFS size...") });
                        Iso9660Reader.UpdateFileSize(isoPath, activeEntry, checked((uint)plan.NewFileSize));
                        activeEntry.Size = checked((uint)plan.NewFileSize);
                        entradaAtualizada = activeEntry;
                        progress.Report(new RebuildProgressInfo { Percent = 100, Stage = Tr("Concluído", "Complete"), Detail = Tr("Importação concluída com sucesso.", "Import completed successfully.") });
                    });
                });

                _isoFiles = arquivosAtualizados ?? Iso9660Reader.ReadAllFiles(isoPath);
                IsoFileEntry finalEntry = entradaAtualizada ?? _isoFiles.First(x => string.Equals(x.FullPath, afsPath, StringComparison.OrdinalIgnoreCase));
                _isoAfsEntry = finalEntry;
                _afsBaseOffset = finalEntry.DataOffset;
                _afsLogicalLength = plan.NewFileSize;
                AbrirAfsDaIso(isoPath, finalEntry);
                SelecionarEntradaPorIndice(entry.Index);
                toolStripStatusLabel1.Text = Tr($"Importado com realocação dentro da ISO: {entry.FileName}", $"Imported with relocation inside ISO: {entry.FileName}");
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private CompactPlan CriarPlanoImportacaoExpandida(AfsEntry alvo, uint novoTamanho)
        {
            long afsLength = ObterAfsLength();
            if (_entries.Count == 0)
                throw new InvalidDataException("O AFS não possui entradas carregadas.");

            long expectedTocSize = (long)_entries.Count * 48L;
            if (_tocOffset == 0 || _tocSize != expectedTocSize)
                throw new InvalidDataException(Tr(
                    $"A importação de arquivos maiores exige uma TOC padrão de 48 bytes por entrada.\nEsperado: {expectedTocSize:N0} bytes\nEncontrado: {_tocSize:N0} bytes.",
                    $"Importing oversized files requires a standard 48-byte-per-entry TOC.\nExpected: {expectedTocSize:N0} bytes\nFound: {_tocSize:N0} bytes."));
            if ((long)_tocOffset + _tocSize > afsLength)
                throw new InvalidDataException("A TOC atual ultrapassa os limites do AFS.");

            long headerEnd = 8L + ((long)_entries.Count * 8L) + 8L;
            var fisicas = _entries.Where(x => !x.IsEmpty && x.Offset > 0).ToList();
            if (fisicas.Count == 0)
                throw new InvalidDataException("Nenhuma entrada física válida foi encontrada.");

            uint firstDataOffset = fisicas.Min(x => x.Offset);
            if (firstDataOffset < headerEnd)
                throw new InvalidDataException("O primeiro arquivo começa antes do fim da tabela AFS.");

            uint ultimoOffset = 0;
            bool encontrouFisica = false;
            foreach (AfsEntry e in _entries)
            {
                if (e.IsEmpty) continue;
                if (e.Offset == 0)
                    throw new InvalidDataException($"A entrada {e.Index} não é vazia, mas possui offset zero.");
                if (encontrouFisica && e.Offset < ultimoOffset)
                    throw new InvalidDataException(Tr(
                        $"A ordem física não acompanha a ordem dos índices na entrada {e.Index}. A realocação foi bloqueada para esta variante do AFS.",
                        $"Physical order does not follow index order at entry {e.Index}. Relocation was blocked for this AFS variant."));

                // Existing entries must be readable from the source. The replacement
                // entry is intentionally exempt from fitting its old physical layout.
                if (e.Index != alvo.Index)
                {
                    uint tamanho = ObterTamanhoReal(e);
                    if ((long)e.Offset + tamanho > afsLength)
                        throw new InvalidDataException($"A entrada {e.Index} ultrapassa o fim do AFS atual.");
                }

                ultimoOffset = e.Offset;
                encontrouFisica = true;
            }

            CompactPlan plan = new CompactPlan { FirstDataOffset = firstDataOffset, TocSize = _tocSize };
            long current = firstDataOffset;

            foreach (AfsEntry e in _entries)
            {
                if (e.IsEmpty)
                {
                    if (current > uint.MaxValue)
                        throw new InvalidDataException("O novo offset ultrapassaria o limite uint32 do formato AFS.");
                    plan.Entries.Add(new CompactEntryPlan { Entry = e, NewOffset = (uint)current, NewStoredSize = EMPTY_SENTINEL });
                    continue;
                }

                current = AlignUp(current, AFS_ALIGNMENT);
                if (current > uint.MaxValue)
                    throw new InvalidDataException("O novo offset ultrapassaria o limite uint32 do formato AFS.");

                uint tamanho = e.Index == alvo.Index ? novoTamanho : ObterTamanhoReal(e);
                plan.Entries.Add(new CompactEntryPlan { Entry = e, NewOffset = (uint)current, NewStoredSize = tamanho });
                current += tamanho;
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

        private void GarantirEspacoParaAfsExpandidoNaIso(long novoTamanho)
        {
            if (_containerPath == null || _isoAfsEntry == null)
                return;

            long capacidade = ObterCapacidadeFisicaAfsNaIso();
            if (novoTamanho <= capacidade)
                return;

            long faltam = novoTamanho - capacidade;
            long inserir = AlignUp(faltam, AFS_ALIGNMENT);
            long inicioDeslocamento = _isoAfsEntry.DataOffset + capacidade;
            string caminhoAfs = _isoAfsEntry.FullPath;

            DialogResult result = MessageBox.Show(
                this,
                Tr(
                    $"O AFS reconstruído precisa de {FormatarBytes(faltam)} além do espaço contíguo atual.\n\nPara concluir a importação, o Manager pode abrir {FormatarBytes(inserir)} na ISO deslocando os dados posteriores para frente. O LBA inicial do AFS será preservado e os registros ISO9660 dos arquivos deslocados serão atualizados automaticamente.\n\nISO: {Path.GetFileName(_containerPath)}\nAFS: {caminhoAfs}\n\nContinuar?\n\nRecomenda-se manter um backup da ISO.",
                    $"The rebuilt AFS needs {FormatarBytes(faltam)} beyond the currently contiguous space.\n\nTo finish the import, the Manager can insert {FormatarBytes(inserir)} into the ISO by shifting subsequent data forward. The AFS starting LBA will be preserved and the ISO9660 records of shifted files will be updated automatically.\n\nISO: {Path.GetFileName(_containerPath)}\nAFS: {caminhoAfs}\n\nContinue?\n\nKeeping a backup of the ISO is recommended."),
                Tr("Expandir espaço do AFS na ISO", "Expand AFS space in ISO"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                throw new OperationCanceledException(Tr("Importação cancelada pelo usuário.", "Import canceled by the user."));

            MarcarAlteracaoInterna(900);
            Iso9660Reader.InsertSpaceBeforeExtent(_containerPath, inicioDeslocamento, inserir);

            // Re-read the filesystem because all extents after the insertion point received new LBAs.
            _isoFiles = Iso9660Reader.ReadAllFiles(_containerPath);
            IsoFileEntry? atualizado = _isoFiles.FirstOrDefault(x =>
                !x.IsDirectory && string.Equals(x.FullPath, caminhoAfs, StringComparison.OrdinalIgnoreCase));
            if (atualizado == null)
                throw new InvalidDataException(Tr(
                    "A ISO foi expandida, mas o AFS selecionado não pôde ser reencontrado no diretório ISO9660.",
                    "The ISO was expanded, but the selected AFS could not be found again in the ISO9660 directory."));

            _isoAfsEntry = atualizado;
            _afsBaseOffset = atualizado.DataOffset;
            _afsLogicalLength = atualizado.Size;

            long novaCapacidade = ObterCapacidadeFisicaAfsNaIso();
            if (novoTamanho > novaCapacidade)
                throw new InvalidDataException(Tr(
                    $"A expansão da ISO não criou espaço suficiente. Necessário: {FormatarBytes(novoTamanho)}; disponível: {FormatarBytes(novaCapacidade)}.",
                    $"ISO expansion did not create enough space. Required: {FormatarBytes(novoTamanho)}; available: {FormatarBytes(novaCapacidade)}."));
        }

        private long ObterCapacidadeFisicaAfsNaIso()
        {
            if (_isoAfsEntry == null)
                return long.MaxValue;

            long inicio = _isoAfsEntry.DataOffset;
            long proximoExtent = _isoFiles
                .Where(x => x.DataOffset > inicio)
                .Select(x => x.DataOffset)
                .DefaultIfEmpty(new FileInfo(_containerPath!).Length)
                .Min();

            if (proximoExtent <= inicio)
                return _isoAfsEntry.Size;

            return proximoExtent - inicio;
        }

        private void ConstruirAfsComEntradaSubstituida(CompactPlan plan, AfsEntry alvo, string substituto, string destinoPath, Action<int, int, string>? progress = null)
        {
            using Stream origem = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
            using FileStream destino = new FileStream(destinoPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(destino, System.Text.Encoding.ASCII, leaveOpen: true);

            bw.Write((byte)0x41); bw.Write((byte)0x46); bw.Write((byte)0x53); bw.Write((byte)0x00);
            bw.Write((uint)_entries.Count);
            foreach (CompactEntryPlan item in plan.Entries)
            {
                bw.Write(item.NewOffset);
                bw.Write(item.NewStoredSize);
            }
            bw.Write(plan.NewTocOffset);
            bw.Write(plan.TocSize);

            long headerEnd = destino.Position;
            if (plan.FirstDataOffset > headerEnd)
            {
                origem.Position = headerEnd;
                CopiarBytes(origem, destino, plan.FirstDataOffset - headerEnd);
            }

            foreach (CompactEntryPlan item in plan.Entries)
            {
                if (item.Entry.IsEmpty)
                    continue;

                EscreverPaddingAte(destino, item.NewOffset);

                if (item.Entry.Index == alvo.Index)
                {
                    using FileStream novo = new FileStream(substituto, FileMode.Open, FileAccess.Read, FileShare.Read);
                    novo.CopyTo(destino, 1024 * 1024);
                }
                else
                {
                    uint tamanhoReal = ObterTamanhoReal(item.Entry);
                    origem.Position = item.Entry.Offset;
                    CopiarBytes(origem, destino, tamanhoReal);
                }

                EscreverPaddingAte(destino, AlignUp(destino.Position, AFS_ALIGNMENT));
                progress?.Invoke(item.Entry.Index + 1, _entries.Count, item.Entry.FileName ?? $"File_{item.Entry.Index:D4}");
            }

            EscreverPaddingAte(destino, plan.NewTocOffset);
            origem.Position = _tocOffset;
            CopiarBytes(origem, destino, _tocSize);

            DateTime agora = DateTime.Now;
            long metadataOffset = (long)plan.NewTocOffset + ((long)alvo.Index * 48L) + 32L;
            destino.Position = metadataOffset;
            bw.Write((ushort)agora.Year);
            bw.Write((ushort)agora.Month);
            bw.Write((ushort)agora.Day);
            bw.Write((ushort)agora.Hour);
            bw.Write((ushort)agora.Minute);
            bw.Write((ushort)agora.Second);
            bw.Write(checked((uint)new FileInfo(substituto).Length));

            destino.SetLength(plan.NewFileSize);
            destino.Flush(true);
        }

        private void ValidarAfsComEntradaSubstituida(string path, CompactPlan plan, AfsEntry alvo, string substituto)
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader br = new BinaryReader(fs, System.Text.Encoding.ASCII, leaveOpen: true);

            byte[] magic = br.ReadBytes(4);
            if (magic.Length != 4 || magic[0] != 0x41 || magic[1] != 0x46 || magic[2] != 0x53 || magic[3] != 0x00)
                throw new InvalidDataException("Validação do rebuild falhou: assinatura AFS inválida.");
            if (br.ReadUInt32() != _entries.Count)
                throw new InvalidDataException("Validação do rebuild falhou: quantidade de entradas mudou.");

            foreach (CompactEntryPlan item in plan.Entries)
            {
                uint offset = br.ReadUInt32();
                uint size = br.ReadUInt32();
                if (offset != item.NewOffset || size != item.NewStoredSize)
                    throw new InvalidDataException($"Validação do rebuild falhou na entrada {item.Entry.Index}.");
            }

            uint tocOffset = br.ReadUInt32();
            uint tocSize = br.ReadUInt32();
            if (tocOffset != plan.NewTocOffset || tocSize != plan.TocSize || (long)tocOffset + tocSize != fs.Length)
                throw new InvalidDataException("Validação do rebuild falhou na TOC.");

            CompactEntryPlan alvoPlan = plan.Entries.First(x => x.Entry.Index == alvo.Index);
            using FileStream novo = new FileStream(substituto, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Position = alvoPlan.NewOffset;
            CompararStreams(novo, fs, novo.Length, "arquivo importado");

            fs.Position = (long)plan.NewTocOffset + ((long)alvo.Index * 48L) + 44L;
            uint currentSize = br.ReadUInt32();
            if (currentSize != novo.Length)
                throw new InvalidDataException("Validação do rebuild falhou: Current Size da TOC não foi atualizado.");
        }

        private void AplicarAfsExpandidoNaIso(string origem, long novoTamanho)
        {
            if (_containerPath == null || _isoAfsEntry == null)
                throw new InvalidOperationException();

            string isoPath = _containerPath;
            IsoFileEntry entry = _isoAfsEntry;
            long capacidade = ObterCapacidadeFisicaAfsNaIso();
            if (novoTamanho > capacidade)
                throw new InvalidDataException("O AFS reconstruído excede o espaço físico seguro disponível na ISO.");

            using (FileStream iso = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (FileStream src = new FileStream(origem, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                iso.Position = entry.DataOffset;
                src.CopyTo(iso, 1024 * 1024);

                // Zero only up to the end of the old logical AFS when shrinking. When
                // growing, bytes beyond the new end belong to ISO padding/next extent.
                if (novoTamanho < entry.Size)
                    PreencherComZeros(iso, entry.Size - novoTamanho);

                iso.Flush(true);
            }

            Iso9660Reader.UpdateFileSize(isoPath, entry, checked((uint)novoTamanho));
            entry.Size = checked((uint)novoTamanho);
            _afsLogicalLength = novoTamanho;
            AbrirAfsDaIso(isoPath, entry);
        }

        private void SelecionarEntradaPorIndice(int index)
        {
            foreach (System.Windows.Forms.DataGridViewRow row in dgvArquivos.Rows)
            {
                if (row.Tag is AfsEntry e && e.Index == index)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0)
                        dgvArquivos.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }
    }
}
