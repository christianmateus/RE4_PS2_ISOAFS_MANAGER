using System.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1 : Form
    {
        private const uint EMPTY_SENTINEL = 0xFFFFF801;
        private const int AFS_ALIGNMENT = 0x800;
        private const string APP_VERSION = "1.3.3";

        private string? _afsPath;
        private readonly List<AfsEntry> _entries = new();
        private uint _tocOffset;
        private uint _tocSize;

        public Form1()
        {
            InitializeComponent();
            var appIconPath = Path.Combine(AppContext.BaseDirectory, "Images", "icon.ico");
            if (File.Exists(appIconPath)) Icon = new Icon(appIconPath);
            ConfigurarBatchIndexado();
            ConfigurarMenuCompactacao();
            ConfigurarIso();
            ConfigurarPreferencias();
            ConfigurarRecentes();
            ConfigurarDragDrop();
            ConfigurarVerificacaoIntegridade();
            ConfigurarAcoesV120();
            ConfigurarComparacaoAfs();
            ConfigurarDeteccaoExterna();

            txtBuscar.TextChanged += TxtBuscar_TextChanged;
            dgvArquivos.SelectionChanged += DgvArquivos_SelectionChanged;
        }

        private void BtnAbrir_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Tr("Abrir arquivo AFS", "Open AFS file"),
                Filter = Tr("Arquivos AFS (*.afs)|*.afs|Todos os arquivos (*.*)|*.*", "AFS files (*.afs)|*.afs|All files (*.*)|*.*")
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try { AbrirAfsStandalone(dialog.FileName); }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Não foi possível abrir o AFS.\n\n{ex.Message}", $"Could not open the AFS.\n\n{ex.Message}"), Tr("Erro", "Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AbrirAfs(string path)
        {
            AbrirAfsStandalone(path);
        }

        private void AbrirAfsAtual()
        {
            LimparDadosAfs();

            using Stream baseStream = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
            using BinaryReader br = new BinaryReader(baseStream);

            byte[] magic = br.ReadBytes(4);
            if (magic.Length != 4 || magic[0] != 0x41 || magic[1] != 0x46 || magic[2] != 0x53 || magic[3] != 0x00)
                throw new InvalidDataException("Assinatura AFS inválida.");

            uint fileCount = br.ReadUInt32();
            if (fileCount == 0 || fileCount > 1_000_000)
                throw new InvalidDataException($"Quantidade de entradas suspeita: {fileCount:N0}");

            long tamanhoTabela = 8L + ((long)fileCount * 8L) + 8L;
            if (tamanhoTabela > baseStream.Length)
                throw new InvalidDataException("A tabela do AFS ultrapassa o tamanho do arquivo.");

            for (int i = 0; i < fileCount; i++)
            {
                uint offset = br.ReadUInt32();
                uint size = br.ReadUInt32();

                _entries.Add(new AfsEntry
                {
                    Index = i,
                    Offset = offset,
                    StoredSize = size,
                    IsEmpty = size == EMPTY_SENTINEL
                });
            }

            _tocOffset = br.ReadUInt32();
            _tocSize = br.ReadUInt32();

            bool tocValida = _tocOffset > 0 && _tocOffset < baseStream.Length && _tocSize > 0 && ((long)_tocOffset + _tocSize) <= baseStream.Length;
            if (tocValida)
                LerFileNameToc(baseStream, br);

            CalcularEspacosFisicos(baseStream.Length);
            DetectarTipos(baseStream);

            _afsPath = _containerPath;
            lblArquivo.Text = ObterNomeAfsAtual();
            lblQuantidade.Text = fileCount.ToString("N0");
            lblTamanhoAfs.Text = FormatarBytes(baseStream.Length);
            lblTocOffset.Text = tocValida ? $"0x{_tocOffset:X8}" : "Não encontrada";
            lblTocSize.Text = tocValida ? $"{_tocSize:N0} bytes" : "-";
            AtualizarTituloJanela();

            AtualizarGrid();
            AtualizarMonitorArquivoExterno();
            toolStripStatusLabel1.Text = _isoAfsEntry != null
                ? $"ISO aberta: {_isoAfsEntry.FullPath} - {fileCount:N0} entradas"
                : $"AFS aberto: {fileCount:N0} entradas";
        }

        private void AtualizarTituloJanela()
        {
            string baseTitle = $"RE4 PS2 ISO/AFS Manager v{APP_VERSION}";
            if (string.IsNullOrWhiteSpace(_containerPath))
            {
                Text = baseTitle;
                return;
            }

            Text = _isoAfsEntry != null
                ? $"{baseTitle} - {Path.GetFileName(_containerPath)} :: {_isoAfsEntry.FullPath}"
                : $"{baseTitle} - {Path.GetFileName(_containerPath)}";
        }

        private void LerFileNameToc(Stream fs, BinaryReader br)
        {
            long quantidade = _entries.Count;
            long tamanhoEsperado48 = quantidade * 48L;
            long tamanhoEsperado32 = quantidade * 32L;

            if (_tocSize >= tamanhoEsperado48)
            {
                fs.Position = _tocOffset;

                for (int i = 0; i < _entries.Count; i++)
                {
                    byte[] nameBytes = br.ReadBytes(32);
                    if (nameBytes.Length < 32) break;

                    AfsEntry entry = _entries[i];
                    entry.FileName = DecodificarNome(nameBytes);

                    entry.TocYear = br.ReadUInt16();
                    entry.TocMonth = br.ReadUInt16();
                    entry.TocDay = br.ReadUInt16();
                    entry.TocHour = br.ReadUInt16();
                    entry.TocMinute = br.ReadUInt16();
                    entry.TocSecond = br.ReadUInt16();
                    entry.ActualSize = br.ReadUInt32();

                    entry.TocMetadata = CriarMetadataCrua(entry);
                }
                return;
            }

            if (_tocSize >= tamanhoEsperado32)
            {
                fs.Position = _tocOffset;
                for (int i = 0; i < _entries.Count; i++)
                {
                    byte[] nameBytes = br.ReadBytes(32);
                    if (nameBytes.Length < 32) break;
                    _entries[i].FileName = DecodificarNome(nameBytes);
                }
            }
        }

        private static byte[] CriarMetadataCrua(AfsEntry entry)
        {
            using MemoryStream ms = new MemoryStream(16);
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(entry.TocYear);
            bw.Write(entry.TocMonth);
            bw.Write(entry.TocDay);
            bw.Write(entry.TocHour);
            bw.Write(entry.TocMinute);
            bw.Write(entry.TocSecond);
            bw.Write(entry.ActualSize);
            return ms.ToArray();
        }

        private static string DecodificarNome(byte[] raw)
        {
            int zeroIndex = Array.IndexOf(raw, (byte)0);
            int length = zeroIndex >= 0 ? zeroIndex : raw.Length;
            if (length <= 0) return string.Empty;
            return Encoding.ASCII.GetString(raw, 0, length).Trim();
        }

        private void CalcularEspacosFisicos(long afsLength)
        {
            List<AfsEntry> validEntries = _entries
                .Where(x => !x.IsEmpty && x.Offset > 0 && x.Offset < afsLength)
                .OrderBy(x => x.Offset)
                .ToList();

            for (int i = 0; i < validEntries.Count; i++)
            {
                AfsEntry current = validEntries[i];
                long nextOffset;

                if (i + 1 < validEntries.Count)
                    nextOffset = validEntries[i + 1].Offset;
                else if (_tocOffset > current.Offset)
                    nextOffset = _tocOffset;
                else
                {
                    uint sizeBase = current.ActualSize > 0 ? current.ActualSize : current.StoredSize;
                    nextOffset = current.Offset + AlignUp(sizeBase, AFS_ALIGNMENT);
                }

                long allocated = nextOffset - current.Offset;
                if (allocated < 0) allocated = 0;

                current.AllocatedSize = allocated;

                uint tamanhoReal = current.ActualSize > 0 ? current.ActualSize : current.StoredSize;
                current.PaddingSize = allocated >= tamanhoReal ? allocated - tamanhoReal : 0;
                current.CompactSize = AlignUp(tamanhoReal, AFS_ALIGNMENT);
                current.ExcessAllocation = Math.Max(0, current.AllocatedSize - current.CompactSize);
            }
        }

        private void DetectarTipos(Stream fs)
        {
            byte[] buffer = new byte[16];

            foreach (AfsEntry entry in _entries)
            {
                if (entry.IsEmpty) { entry.FileType = "EMPTY"; continue; }
                if (entry.Offset <= 0 || entry.Offset >= fs.Length) { entry.FileType = "INVALID"; continue; }

                try
                {
                    fs.Position = entry.Offset;
                    int lidos = fs.Read(buffer, 0, buffer.Length);
                    entry.FileType = IdentificarTipo(buffer, lidos, entry.FileName);
                }
                catch { entry.FileType = "?"; }
            }
        }

        private static string IdentificarTipo(byte[] data, int length, string? fileName)
        {
            string ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(ext))
            {
                switch (ext)
                {
                    case ".snd": return "SND";
                    case ".adx": return "ADX";
                    case ".sfd": return "SFD";
                    case ".dat": return "DAT";
                    case ".bin": return "BIN";
                }

                // If the TOC already provides an extension, preserve/reuse it as
                // the displayed type instead of replacing a known extension with "?".
                return ext.TrimStart('.').ToUpperInvariant();
            }

            if (length >= 4)
            {
                if (data[0] == 0x41 && data[1] == 0x46 && data[2] == 0x53 && data[3] == 0x00) return "AFS";
                if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46) return "RIFF";
                if (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0xBA) return "MPEG/SFD";
                if (data[0] == 0x80 && data[1] == 0x00) return "ADX";
            }
            return "?";
        }

        private void AtualizarGrid()
        {
            string busca = txtBuscar.Text.Trim();
            dgvArquivos.SuspendLayout();
            dgvArquivos.Rows.Clear();

            IEnumerable<AfsEntry> lista = _entries;
            if (!string.IsNullOrWhiteSpace(busca))
            {
                lista = lista.Where(x =>
                    x.Index.ToString().Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (x.FileName ?? "").Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (x.FileType ?? "").Contains(busca, StringComparison.OrdinalIgnoreCase));
            }

            foreach (AfsEntry entry in lista)
            {
                string nome = !string.IsNullOrWhiteSpace(entry.FileName)
                    ? entry.FileName
                    : entry.IsEmpty ? "<EMPTY>" : $"File_{entry.Index:D4}";

                uint currentSize = entry.ActualSize > 0 ? entry.ActualSize : entry.StoredSize;

                int rowIndex = dgvArquivos.Rows.Add(
                    entry.Index,
                    nome,
                    entry.FileType,
                    $"0x{entry.Offset:X8}",
                    entry.IsEmpty ? "-" : currentSize.ToString("N0"),
                    entry.IsEmpty ? "-" : entry.AllocatedSize.ToString("N0"),
                    entry.IsEmpty ? "-" : entry.ExcessAllocation.ToString("N0"),
                    entry.IsEmpty ? "EMPTY" : entry.ExcessAllocation > 0 ? "WASTE" : "OK"
                );

                dgvArquivos.Rows[rowIndex].Tag = entry;
            }

            dgvArquivos.ResumeLayout();
            lblResultados.Text = $"{dgvArquivos.Rows.Count:N0} resultados";
        }

        private void TxtBuscar_TextChanged(object? sender, EventArgs e)
        {
            if (_entries.Count > 0) AtualizarGrid();
        }

        private void DgvArquivos_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvArquivos.SelectedRows.Count == 0) return;
            if (dgvArquivos.SelectedRows[0].Tag is AfsEntry entry) MostrarDetalhes(entry);
        }

        private void MostrarDetalhes(AfsEntry entry)
        {
            lblIndex.Text = entry.Index.ToString();
            lblNome.Text = string.IsNullOrWhiteSpace(entry.FileName) ? "-" : entry.FileName;
            lblTipo.Text = entry.FileType ?? "-";
            lblOffset.Text = $"0x{entry.Offset:X8}";

            if (entry.IsEmpty)
            {
                lblCurrentSize.Text = "EMPTY";
                lblAllocatedSize.Text = lblPadding.Text = lblExcess.Text = lblTimestamp.Text = lblMetadata.Text = "-";
                return;
            }

            uint currentSize = entry.ActualSize > 0 ? entry.ActualSize : entry.StoredSize;
            lblCurrentSize.Text = $"{currentSize:N0} bytes";
            lblAllocatedSize.Text = $"{entry.AllocatedSize:N0} bytes";
            lblPadding.Text = $"{entry.PaddingSize:N0} bytes";
            lblExcess.Text = $"{entry.ExcessAllocation:N0} bytes";
            lblTimestamp.Text = FormatarTimestamp(entry);
            lblMetadata.Text = entry.TocMetadata != null ? BitConverter.ToString(entry.TocMetadata).Replace("-", " ") : "-";
        }

        private void BtnExtrair_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dgvArquivos.SelectedRows.Count == 0 || dgvArquivos.SelectedRows[0].Tag is not AfsEntry entry)
            {
                MessageBox.Show(Tr("Selecione um arquivo para extrair.", "Select a file to extract."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (entry.IsEmpty)
            {
                MessageBox.Show(Tr("Esta entrada está vazia e não possui conteúdo para extrair.", "This entry is empty and has no content to extract."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            uint tamanhoReal = ObterTamanhoReal(entry);

            if (tamanhoReal == 0)
            {
                MessageBox.Show(Tr("Esta entrada possui tamanho zero.", "This entry has zero size."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string nome = ObterNomeSeguroParaExtracao(entry);

            using SaveFileDialog dialog = new SaveFileDialog
            {
                Title = Tr("Extrair arquivo", "Extract file"),
                FileName = Path.GetFileName(nome),
                Filter = "Todos os arquivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ExtrairEntrada(entry, dialog.FileName);

                toolStripStatusLabel1.Text = $"Extraído: {entry.FileName} ({tamanhoReal:N0} bytes)";

                MostrarSucesso(
                    Tr($"Arquivo extraído com sucesso.\n\nNome: {entry.FileName}\nTamanho: {tamanhoReal:N0} bytes", $"File extracted successfully.\n\nName: {entry.FileName}\nSize: {tamanhoReal:N0} bytes"),
                    Tr("Extração concluída", "Extraction complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a extração:\n\n{ex.Message}", $"Extraction error:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExtrairEntrada(AfsEntry entry, string destino)
        {
            if (_afsPath == null)
                throw new InvalidOperationException("Nenhum AFS está aberto.");

            uint tamanhoReal = ObterTamanhoReal(entry);

            if (entry.IsEmpty)
                throw new InvalidOperationException("Não é possível extrair uma entrada vazia.");

            using Stream origem = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
            using FileStream saida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);

            if ((long)entry.Offset + tamanhoReal > origem.Length)
                throw new InvalidDataException($"A entrada {entry.Index} ultrapassa os limites físicos do AFS.");

            origem.Position = entry.Offset;

            CopiarBytes(origem, saida, tamanhoReal);
        }

        private static uint ObterTamanhoReal(AfsEntry entry)
        {
            return entry.ActualSize > 0 ? entry.ActualSize : entry.StoredSize;
        }

        private void BtnExtrairTodos_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = Tr("Escolha a pasta onde os arquivos serão extraídos", "Choose the folder where files will be extracted"),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                int extraidos = 0;
                int vazios = 0;
                long totalBytes = 0;

                using Stream origem = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);

                foreach (AfsEntry entry in _entries)
                {
                    if (entry.IsEmpty || ObterTamanhoReal(entry) == 0)
                    {
                        vazios++;
                        continue;
                    }

                    string caminhoRelativo = ObterNomeSeguroParaExtracao(entry);
                    string destino = CriarCaminhoSeguro(dialog.SelectedPath, caminhoRelativo);

                    string? pasta = Path.GetDirectoryName(destino);

                    if (!string.IsNullOrEmpty(pasta))
                        Directory.CreateDirectory(pasta);

                    uint tamanhoReal = ObterTamanhoReal(entry);

                    if ((long)entry.Offset + tamanhoReal > origem.Length)
                        throw new InvalidDataException($"A entrada {entry.Index} ({entry.FileName}) ultrapassa os limites físicos do AFS.");

                    origem.Position = entry.Offset;

                    using FileStream saida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);
                    CopiarBytes(origem, saida, tamanhoReal);

                    extraidos++;
                    totalBytes += tamanhoReal;

                    toolStripStatusLabel1.Text = Tr($"Extraindo {extraidos:N0} arquivos... {entry.FileName}", $"Extracting {extraidos:N0} files... {entry.FileName}");
                    Application.DoEvents();
                }

                toolStripStatusLabel1.Text = Tr($"Extração concluída: {extraidos:N0} arquivos", $"Extraction complete: {extraidos:N0} files");

                MostrarSucesso(Tr($"Extração concluída.\n\nArquivos extraídos: {extraidos:N0}\nEntradas vazias ignoradas: {vazios:N0}\nDados extraídos: {FormatarBytes(totalBytes)}", $"Extraction complete.\n\nFiles extracted: {extraidos:N0}\nEmpty entries skipped: {vazios:N0}\nData extracted: {FormatarBytes(totalBytes)}"), Tr("Extração concluída", "Extraction complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a extração:\n\n{ex.Message}", $"Extraction error:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopiarBytes(Stream origem, Stream destino, long quantidade)
        {
            byte[] buffer = new byte[1024 * 1024]; // 1 MB
            long restante = quantidade;

            while (restante > 0)
            {
                int solicitar = (int)Math.Min(buffer.Length, restante);
                int lidos = origem.Read(buffer, 0, solicitar);

                if (lidos <= 0)
                    throw new EndOfStreamException("O AFS terminou antes do tamanho esperado para a entrada.");

                destino.Write(buffer, 0, lidos);
                restante -= lidos;
            }
        }

        private static string ObterNomeSeguroParaExtracao(AfsEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.FileName) || entry.FileName == "_")
                return $"File_{entry.Index:D4}.bin";

            string nome = entry.FileName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            while (nome.StartsWith(Path.DirectorySeparatorChar))
                nome = nome.Substring(1);

            return nome;
        }

        private static string CriarCaminhoSeguro(string pastaBase, string caminhoRelativo)
        {
            string baseCompleta = Path.GetFullPath(pastaBase);
            string destino = Path.GetFullPath(Path.Combine(baseCompleta, caminhoRelativo));

            string prefixoBase = baseCompleta.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!destino.StartsWith(prefixoBase, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Caminho inválido encontrado na TOC: {caminhoRelativo}");

            return destino;
        }

        private static string FormatarTimestamp(AfsEntry entry)
        {
            if (entry.TocYear == 0) return "-";
            try
            {
                DateTime dt = new DateTime(entry.TocYear, Math.Max((ushort)1, entry.TocMonth), Math.Max((ushort)1, entry.TocDay),
                    Math.Min((ushort)23, entry.TocHour), Math.Min((ushort)59, entry.TocMinute), Math.Min((ushort)59, entry.TocSecond));
                return dt.ToString("dd/MM/yyyy HH:mm:ss");
            }
            catch
            {
                return $"{entry.TocDay:D2}/{entry.TocMonth:D2}/{entry.TocYear:D4} {entry.TocHour:D2}:{entry.TocMinute:D2}:{entry.TocSecond:D2}";
            }
        }

        private void LimparDadosAfs()
        {
            _entries.Clear();
            dgvArquivos.Rows.Clear();
            _afsPath = null;
            _tocOffset = 0;
            _tocSize = 0;

            lblArquivo.Text = lblQuantidade.Text = lblTamanhoAfs.Text = lblTocOffset.Text = lblTocSize.Text = "-";
            lblIndex.Text = lblNome.Text = lblTipo.Text = lblOffset.Text = lblCurrentSize.Text = "-";
            lblAllocatedSize.Text = lblPadding.Text = lblExcess.Text = lblTimestamp.Text = lblMetadata.Text = "-";
            toolStripStatusLabel1.Text = "Pronto";
        }

        private void LimparTudo()
        {
            LimparDadosAfs();
            _containerPath = null;
            _afsBaseOffset = 0;
            _afsLogicalLength = 0;
            _isoAfsEntry = null;
            _isoFiles.Clear();
            if (_menuIso != null) _menuIso.Enabled = false;
        }

        private static long AlignUp(long value, int alignment)
            => ((value + alignment - 1) / alignment) * alignment;

        private static string FormatarBytes(long bytes)
        {
            const double KB = 1024;
            const double MB = KB * 1024;
            const double GB = MB * 1024;

            if (bytes >= GB) return $"{bytes / GB:N2} GB ({bytes:N0} bytes)";
            if (bytes >= MB) return $"{bytes / MB:N2} MB ({bytes:N0} bytes)";
            if (bytes >= KB) return $"{bytes / KB:N2} KB ({bytes:N0} bytes)";
            return $"{bytes:N0} bytes";
        }

    }

    public class AfsEntry
    {
        public int Index { get; set; }
        public uint Offset { get; set; }
        public uint StoredSize { get; set; }
        public uint ActualSize { get; set; }
        public long AllocatedSize { get; set; }
        public long PaddingSize { get; set; }
        public long CompactSize { get; set; }
        public long ExcessAllocation { get; set; }
        public bool IsEmpty { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public ushort TocYear { get; set; }
        public ushort TocMonth { get; set; }
        public ushort TocDay { get; set; }
        public ushort TocHour { get; set; }
        public ushort TocMinute { get; set; }
        public ushort TocSecond { get; set; }
        public byte[]? TocMetadata { get; set; }
    }
}
