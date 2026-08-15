using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private ToolStripMenuItem? _menuVerificarAfs;

        private void ConfigurarVerificacaoIntegridade()
        {
            if (_menuFerramentasCompact == null) return;
            _menuVerificarAfs = new ToolStripMenuItem(Tr("Verificar AFS...", "Verify AFS..."));
            _menuVerificarAfs.Click += MenuVerificarAfs_Click;
            _menuFerramentasCompact.DropDownItems.Insert(0, _menuVerificarAfs);
            _menuFerramentasCompact.DropDownItems.Insert(1, new ToolStripSeparator());
            AplicarTema();
        }

        private void MenuVerificarAfs_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Verificar AFS", "Verify AFS"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                IntegrityReport report = VerificarIntegridadeAfs();
                MostrarRelatorioIntegridade(report);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível verificar o AFS.\n\n{ex.Message}", $"Could not verify the AFS.\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IntegrityReport VerificarIntegridadeAfs()
        {
            IntegrityReport report = new IntegrityReport();
            using Stream fs = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
            using BinaryReader br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
            long length = fs.Length;

            if (length < 16)
            {
                report.Error(Tr("Arquivo pequeno demais para conter um cabeçalho AFS válido.", "File is too small to contain a valid AFS header."));
                return report;
            }

            byte[] magic = br.ReadBytes(4);
            if (magic.Length != 4 || magic[0] != 0x41 || magic[1] != 0x46 || magic[2] != 0x53 || magic[3] != 0x00)
            {
                report.Error(Tr("Assinatura AFS inválida.", "Invalid AFS signature."));
                return report;
            }

            uint count = br.ReadUInt32();
            if (count == 0 || count > 1_000_000)
            {
                report.Error(Tr($"Quantidade de entradas impossível/suspeita: {count:N0}.", $"Impossible/suspicious entry count: {count:N0}."));
                return report;
            }

            long headerEnd = 8L + count * 8L + 8L;
            if (headerEnd > length)
            {
                report.Error(Tr("A tabela de entradas ultrapassa o fim do arquivo.", "The entry table extends beyond the end of the file."));
                return report;
            }

            var raw = new List<IntegrityEntry>((int)count);
            for (int i = 0; i < count; i++)
            {
                uint offset = br.ReadUInt32();
                uint stored = br.ReadUInt32();
                raw.Add(new IntegrityEntry { Index = i, Offset = offset, StoredSize = stored, IsEmpty = stored == EMPTY_SENTINEL });
            }
            uint tocOffset = br.ReadUInt32();
            uint tocSize = br.ReadUInt32();

            bool tocPointerPresent = tocOffset != 0 || tocSize != 0;
            bool tocInBounds = tocOffset > 0 && tocSize > 0 && (long)tocOffset + tocSize <= length;
            if (tocPointerPresent && !tocInBounds)
                report.Error(Tr($"TOC fora dos limites: offset 0x{tocOffset:X8}, tamanho {tocSize:N0}.", $"TOC is out of bounds: offset 0x{tocOffset:X8}, size {tocSize:N0}."));
            else if (!tocPointerPresent)
                report.Warning(Tr("TOC não encontrada (offset/tamanho zero).", "TOC not found (zero offset/size)."));

            if (tocInBounds)
            {
                if (tocOffset < headerEnd)
                    report.Error(Tr($"TOC começa dentro do cabeçalho/tabela: 0x{tocOffset:X8}.", $"TOC starts inside the header/table: 0x{tocOffset:X8}."));
                if ((tocOffset & (AFS_ALIGNMENT - 1)) != 0)
                    report.Warning(Tr($"TOC não está alinhada a 0x{AFS_ALIGNMENT:X}: 0x{tocOffset:X8}.", $"TOC is not aligned to 0x{AFS_ALIGNMENT:X}: 0x{tocOffset:X8}."));

                long toc32 = count * 32L;
                long toc48 = count * 48L;
                if (tocSize < toc32)
                    report.Error(Tr($"TOC pequena demais: {tocSize:N0} bytes; mínimo esperado para nomes: {toc32:N0}.", $"TOC is too small: {tocSize:N0} bytes; minimum expected for names: {toc32:N0}."));
                else if (tocSize < toc48)
                    report.Warning(Tr($"TOC não contém metadata de 48 bytes por entrada (tamanho: {tocSize:N0}).", $"TOC does not contain 48-byte metadata per entry (size: {tocSize:N0})."));
            }

            // Read ActualSize values when a 48-byte TOC is available.
            if (tocInBounds && tocSize >= count * 48L)
            {
                for (int i = 0; i < raw.Count; i++)
                {
                    long actualPos = (long)tocOffset + i * 48L + 44L;
                    if (actualPos + 4 > length) break;
                    fs.Position = actualPos;
                    raw[i].ActualSize = br.ReadUInt32();
                }
            }

            foreach (IntegrityEntry entry in raw)
            {
                if (entry.IsEmpty) continue;
                uint effectiveSize = entry.ActualSize > 0 ? entry.ActualSize : entry.StoredSize;

                // Resident Evil 4 AFS archives commonly contain legitimate zero-byte
                // entries. They are placeholders, not an integrity problem, so skip
                // all per-file checks for them.
                if (effectiveSize == 0) continue;

                if (entry.Offset == 0)
                {
                    report.Error(Tr($"Entrada {entry.Index}: offset zero em uma entrada não vazia.", $"Entry {entry.Index}: zero offset on a non-empty entry."));
                    continue;
                }
                if (entry.Offset < headerEnd)
                    report.Error(Tr($"Entrada {entry.Index}: offset 0x{entry.Offset:X8} invade a tabela/cabeçalho (fim em 0x{headerEnd:X}).", $"Entry {entry.Index}: offset 0x{entry.Offset:X8} overlaps the table/header (ends at 0x{headerEnd:X})."));
                if (entry.Offset >= length)
                {
                    report.Error(Tr($"Entrada {entry.Index}: offset 0x{entry.Offset:X8} está fora do arquivo.", $"Entry {entry.Index}: offset 0x{entry.Offset:X8} is outside the file."));
                    continue;
                }
                if ((entry.Offset & (AFS_ALIGNMENT - 1)) != 0)
                    report.Warning(Tr($"Entrada {entry.Index}: offset 0x{entry.Offset:X8} não está alinhado a 0x{AFS_ALIGNMENT:X}.", $"Entry {entry.Index}: offset 0x{entry.Offset:X8} is not aligned to 0x{AFS_ALIGNMENT:X}."));
                if (entry.StoredSize != 0 && (long)entry.Offset + entry.StoredSize > length)
                    report.Error(Tr($"Entrada {entry.Index}: Stored Size {entry.StoredSize:N0} é impossível para o offset 0x{entry.Offset:X8} e o tamanho do AFS.", $"Entry {entry.Index}: Stored Size {entry.StoredSize:N0} is impossible for offset 0x{entry.Offset:X8} and the AFS length."));
                if ((long)entry.Offset + effectiveSize > length)
                    report.Error(Tr($"Entrada {entry.Index}: dados ultrapassam o fim do AFS (offset 0x{entry.Offset:X8}, tamanho {effectiveSize:N0}).", $"Entry {entry.Index}: data extends beyond the AFS (offset 0x{entry.Offset:X8}, size {effectiveSize:N0})."));
                if (tocInBounds && entry.Offset < (long)tocOffset + tocSize && (long)entry.Offset + effectiveSize > tocOffset)
                    report.Error(Tr($"Entrada {entry.Index}: conteúdo sobrepõe a TOC.", $"Entry {entry.Index}: content overlaps the TOC."));
            }

            var physical = raw.Where(x => !x.IsEmpty && x.Offset > 0 && x.Offset < length)
                .Select(x => new { Entry = x, Size = (long)(x.ActualSize > 0 ? x.ActualSize : x.StoredSize) })
                .Where(x => x.Size > 0)
                .OrderBy(x => x.Entry.Offset).ToList();

            for (int i = 0; i + 1 < physical.Count; i++)
            {
                var a = physical[i];
                var b = physical[i + 1];
                long endA = (long)a.Entry.Offset + a.Size;
                if (endA > b.Entry.Offset)
                    report.Error(Tr($"Sobreposição: entrada {a.Entry.Index} termina em 0x{endA:X} e entrada {b.Entry.Index} começa em 0x{b.Entry.Offset:X8}.", $"Overlap: entry {a.Entry.Index} ends at 0x{endA:X} and entry {b.Entry.Index} starts at 0x{b.Entry.Offset:X8}."));
            }

            report.CheckedEntries = raw.Count(x => !x.IsEmpty && (x.ActualSize > 0 ? x.ActualSize : x.StoredSize) > 0);
            report.FileLength = length;
            report.TocOffset = tocOffset;
            report.TocSize = tocSize;
            return report;
        }

        private void MostrarRelatorioIntegridade(IntegrityReport report)
        {
            string status = report.Errors.Count == 0
                ? (report.Warnings.Count == 0 ? Tr("OK — nenhum problema encontrado", "OK — no problems found") : Tr("Concluído com avisos", "Completed with warnings"))
                : Tr("Problemas encontrados", "Problems found");

            var sb = new StringBuilder();
            sb.AppendLine(status);
            sb.AppendLine(new string('=', 72));
            sb.AppendLine(Tr($"Entradas verificadas: {report.CheckedEntries:N0}", $"Entries checked: {report.CheckedEntries:N0}"));
            sb.AppendLine(Tr($"Tamanho do AFS: {FormatarBytes(report.FileLength)}", $"AFS size: {FormatarBytes(report.FileLength)}"));
            if (report.TocOffset != 0 || report.TocSize != 0)
                sb.AppendLine($"TOC: 0x{report.TocOffset:X8} / {report.TocSize:N0} bytes");
            sb.AppendLine(Tr($"Erros: {report.Errors.Count:N0} | Avisos: {report.Warnings.Count:N0}", $"Errors: {report.Errors.Count:N0} | Warnings: {report.Warnings.Count:N0}"));

            if (report.Errors.Count > 0)
            {
                sb.AppendLine(); sb.AppendLine(Tr("ERROS", "ERRORS")); sb.AppendLine(new string('-', 72));
                foreach (string item in report.Errors) sb.AppendLine("• " + item);
            }
            if (report.Warnings.Count > 0)
            {
                sb.AppendLine(); sb.AppendLine(Tr("AVISOS", "WARNINGS")); sb.AppendLine(new string('-', 72));
                foreach (string item in report.Warnings) sb.AppendLine("• " + item);
            }

            using Form form = new Form
            {
                Text = Tr("Verificação de integridade do AFS", "AFS integrity verification"),
                StartPosition = FormStartPosition.CenterParent,
                Width = 850,
                Height = 600,
                MinimizeBox = false
            };
            TextBox text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new System.Drawing.Font("Consolas", 9F), Text = sb.ToString() };
            Button close = new Button { Text = Tr("Fechar", "Close"), Dock = DockStyle.Bottom, Height = 38, DialogResult = DialogResult.OK };
            form.Controls.Add(text); form.Controls.Add(close); form.AcceptButton = close; form.CancelButton = close;
            form.ShowDialog(this);
        }

        private sealed class IntegrityEntry
        {
            public int Index { get; set; }
            public uint Offset { get; set; }
            public uint StoredSize { get; set; }
            public uint ActualSize { get; set; }
            public bool IsEmpty { get; set; }
        }

        private sealed class IntegrityReport
        {
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public int CheckedEntries { get; set; }
            public long FileLength { get; set; }
            public uint TocOffset { get; set; }
            public uint TocSize { get; set; }
            public void Error(string text) => Errors.Add(text);
            public void Warning(string text) => Warnings.Add(text);
        }
    }
}
