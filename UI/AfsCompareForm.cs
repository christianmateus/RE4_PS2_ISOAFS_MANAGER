using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FerramentaAFS
{
    internal sealed class AfsComparisonSource
    {
        public string Path { get; init; } = string.Empty;
        public long BaseOffset { get; init; }
        public long Length { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }

    internal sealed class AfsCompareForm : Form
    {
        private readonly AfsComparisonSource _left;
        private readonly AfsComparisonSource _right;
        private readonly bool _english;
        private readonly bool _dark;
        private readonly DataGridView _grid = new DataGridView();
        private readonly ComboBox _filter = new ComboBox();
        private readonly TextBox _search = new TextBox();
        private readonly Label _summary = new Label();
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly Label _status = new Label();
        private List<CompareRow> _rows = new List<CompareRow>();
        private readonly CancellationTokenSource _comparisonCts = new CancellationTokenSource();
        private bool _comparisonStarted;

        public AfsCompareForm(AfsComparisonSource left, AfsComparisonSource right, bool english, bool dark)
        {
            var appIconPath = Path.Combine(AppContext.BaseDirectory, "Images", "icon.ico");
            if (File.Exists(appIconPath)) Icon = new Icon(appIconPath);
            _left = left; _right = right; _english = english; _dark = dark;
            Text = T("Comparar AFS", "Compare AFS");
            StartPosition = FormStartPosition.CenterParent;
            Width = 1120; Height = 720; MinimumSize = new Size(900, 560);
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            Shown += async (_, _) =>
            {
                if (_comparisonStarted) return;
                _comparisonStarted = true;
                await RunComparisonAsync();
            };
            FormClosing += (_, _) => _comparisonCts.Cancel();
            FormClosed += (_, _) => _comparisonCts.Dispose();
        }

        private string T(string pt, string en) => _english ? en : pt;

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(16) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            var hero = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 8) };
            var title = new Label { Text = T("Comparação de AFS", "AFS Comparison"), AutoSize = true, Font = new Font("Segoe UI Semibold", 18F), Location = new Point(0, 0) };
            var subtitle = new Label { Text = $"{_left.DisplayName}  ↔  {_right.DisplayName}", AutoEllipsis = true, Location = new Point(2, 40), Width = 1000, Height = 24, Font = new Font("Segoe UI", 9.5F) };
            _summary.Location = new Point(2, 64); _summary.Width = 1000; _summary.Height = 20;
            hero.Controls.Add(title); hero.Controls.Add(subtitle); hero.Controls.Add(_summary);

            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0, 6, 0, 6) };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            _filter.Dock = DockStyle.Fill; _filter.DropDownStyle = ComboBoxStyle.DropDownList;
            _filter.Items.AddRange(new object[] { T("Todos", "All"), T("Modificados", "Modified"), T("Iguais", "Identical"), T("Só no atual", "Only current"), T("Só no comparado", "Only compared") });
            _filter.SelectedIndex = 0; _filter.SelectedIndexChanged += (_, _) => ApplyFilter();
            _search.Dock = DockStyle.Fill; _search.PlaceholderText = T("Buscar por nome ou índice...", "Search by name or index..."); _search.TextChanged += (_, _) => ApplyFilter();
            var export = new Button { Text = T("Copiar resumo", "Copy summary"), Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
            export.Click += (_, _) => Clipboard.SetText(BuildSummaryText());
            var close = new Button { Text = T("Fechar", "Close"), Dock = DockStyle.Fill, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
            filters.Controls.Add(_filter, 0, 0); filters.Controls.Add(_search, 1, 0); filters.Controls.Add(export, 2, 0); filters.Controls.Add(close, 3, 0);

            _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false; _grid.RowHeadersVisible = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = true; _grid.AutoGenerateColumns = false; _grid.BorderStyle = BorderStyle.FixedSingle; _grid.BackgroundColor = SystemColors.Window;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = T("Estado", "Status"), Width = 115 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = T("Índice", "Index"), Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = T("Nome", "Name"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 210 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LeftSize", HeaderText = T("Tamanho atual", "Current size"), Width = 125 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RightSize", HeaderText = T("Tamanho comparado", "Compared size"), Width = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Detail", HeaderText = T("Diferença", "Difference"), Width = 180 });

            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            _status.Dock = DockStyle.Fill; _status.TextAlign = ContentAlignment.MiddleLeft;
            _progress.Dock = DockStyle.Fill; _progress.Style = ProgressBarStyle.Marquee; _progress.MarqueeAnimationSpeed = 25;
            footer.Controls.Add(_status, 0, 0); footer.Controls.Add(_progress, 1, 0);

            root.Controls.Add(hero, 0, 0); root.Controls.Add(filters, 0, 1); root.Controls.Add(_grid, 0, 2); root.Controls.Add(footer, 0, 3);
            Controls.Add(root); AcceptButton = close;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (!_dark) return;
            Color bg = Color.FromArgb(30, 30, 30), panel = Color.FromArgb(38, 38, 38), fg = Color.Gainsboro, gridBg = Color.FromArgb(32, 32, 32), header = Color.FromArgb(48, 48, 48);
            BackColor = bg; ForeColor = fg;
            foreach (Control c in EnumerateControls(this)) { c.ForeColor = fg; if (c is Panel || c is TableLayoutPanel) c.BackColor = bg; }
            _search.BackColor = panel; _search.ForeColor = fg; _filter.BackColor = panel; _filter.ForeColor = fg;
            _grid.BackgroundColor = gridBg; _grid.DefaultCellStyle.BackColor = gridBg; _grid.DefaultCellStyle.ForeColor = fg; _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(62, 92, 130);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = header; _grid.ColumnHeadersDefaultCellStyle.ForeColor = fg; _grid.EnableHeadersVisualStyles = false; _grid.GridColor = Color.FromArgb(58, 58, 58);
        }

        private static IEnumerable<Control> EnumerateControls(Control root)
        {
            foreach (Control c in root.Controls) { yield return c; foreach (var x in EnumerateControls(c)) yield return x; }
        }

        private async Task RunComparisonAsync()
        {
            CancellationToken token = _comparisonCts.Token;
            try
            {
                if (IsDisposed || Disposing || token.IsCancellationRequested) return;
                _status.Text = T("Lendo e comparando os dois AFS...", "Reading and comparing both AFS files...");
                _progress.Visible = true;

                List<CompareRow> rows = await Task.Run(() => Compare(_left, _right, token), token);
                token.ThrowIfCancellationRequested();
                if (IsDisposed || Disposing || !IsHandleCreated) return;

                _rows = rows;
                ApplyFilter();
                int same = _rows.Count(x => x.Kind == CompareKind.Same), mod = _rows.Count(x => x.Kind == CompareKind.Modified), onlyL = _rows.Count(x => x.Kind == CompareKind.OnlyLeft), onlyR = _rows.Count(x => x.Kind == CompareKind.OnlyRight);
                _summary.Text = T($"Iguais: {same:N0}   •   Modificados: {mod:N0}   •   Só no atual: {onlyL:N0}   •   Só no comparado: {onlyR:N0}", $"Identical: {same:N0}   •   Modified: {mod:N0}   •   Only current: {onlyL:N0}   •   Only compared: {onlyR:N0}");
                _status.Text = T($"Comparação concluída: {_rows.Count:N0} entradas analisadas.", $"Comparison complete: {_rows.Count:N0} entries analyzed.");
            }
            catch (OperationCanceledException)
            {
                // Closing the comparison window intentionally cancels the worker.
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested || IsDisposed || Disposing) return;
                _status.Text = T("Falha na comparação.", "Comparison failed.");
                MessageBox.Show(this, T($"Não foi possível comparar os AFS.\n\n{ex.Message}", $"Could not compare the AFS files.\n\n{ex.Message}"), T("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed && !Disposing && IsHandleCreated) _progress.Visible = false;
            }
        }

        private void ApplyFilter()
        {
            if (_rows == null) return;
            string q = _search.Text.Trim(); int fi = _filter.SelectedIndex;
            IEnumerable<CompareRow> rows = _rows;
            if (fi == 1) rows = rows.Where(x => x.Kind == CompareKind.Modified);
            else if (fi == 2) rows = rows.Where(x => x.Kind == CompareKind.Same);
            else if (fi == 3) rows = rows.Where(x => x.Kind == CompareKind.OnlyLeft);
            else if (fi == 4) rows = rows.Where(x => x.Kind == CompareKind.OnlyRight);
            if (q.Length > 0) rows = rows.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Index.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));
            _grid.Rows.Clear();
            foreach (CompareRow r in rows)
            {
                int idx = _grid.Rows.Add(StatusText(r.Kind), r.Index, r.Name, SizeText(r.LeftSize), SizeText(r.RightSize), DetailText(r.Detail));
                DataGridViewRow gr = _grid.Rows[idx];
                if (!_dark)
                {
                    if (r.Kind == CompareKind.Modified) gr.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                    else if (r.Kind == CompareKind.OnlyLeft || r.Kind == CompareKind.OnlyRight) gr.DefaultCellStyle.BackColor = Color.FromArgb(235, 244, 255);
                }
            }
        }

        private string DetailText(string d) => d == "Content" ? T("Conteúdo", "Content") : d == "Name" ? T("Nome", "Name") : d;
        private string StatusText(CompareKind k) => k switch { CompareKind.Same => T("Igual", "Identical"), CompareKind.Modified => T("Modificado", "Modified"), CompareKind.OnlyLeft => T("Só no atual", "Only current"), _ => T("Só no comparado", "Only compared") };
        private static string SizeText(long? v) => v.HasValue ? FormatBytes(v.Value) : "—";
        private static string FormatBytes(long b) { if (b >= 1024 * 1024) return $"{b / 1048576d:N2} MB"; if (b >= 1024) return $"{b / 1024d:N2} KB"; return $"{b:N0} B"; }

        private string BuildSummaryText()
        {
            int same = _rows.Count(x => x.Kind == CompareKind.Same), mod = _rows.Count(x => x.Kind == CompareKind.Modified), l = _rows.Count(x => x.Kind == CompareKind.OnlyLeft), r = _rows.Count(x => x.Kind == CompareKind.OnlyRight);
            return $"{T("Comparação de AFS", "AFS Comparison")}\r\n{_left.DisplayName} <-> {_right.DisplayName}\r\n{T("Iguais", "Identical")}: {same}\r\n{T("Modificados", "Modified")}: {mod}\r\n{T("Só no atual", "Only current")}: {l}\r\n{T("Só no comparado", "Only compared")}: {r}";
        }

        private static List<CompareRow> Compare(AfsComparisonSource left, AfsComparisonSource right, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            List<SnapshotEntry> a = ReadSnapshot(left, token);
            token.ThrowIfCancellationRequested();
            List<SnapshotEntry> b = ReadSnapshot(right, token);
            int max = Math.Max(a.Count, b.Count); var result = new List<CompareRow>(max);
            for (int i = 0; i < max; i++)
            {
                token.ThrowIfCancellationRequested();
                SnapshotEntry? x = i < a.Count ? a[i] : null, y = i < b.Count ? b[i] : null;
                if (x == null) { result.Add(new CompareRow(i, y!.Name, null, y.Size, CompareKind.OnlyRight, "—")); continue; }
                if (y == null) { result.Add(new CompareRow(i, x.Name, x.Size, null, CompareKind.OnlyLeft, "—")); continue; }
                string name = string.IsNullOrWhiteSpace(x.Name) ? y.Name : x.Name;
                bool sameName = string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                if (x.Empty && y.Empty) result.Add(new CompareRow(i, name, 0, 0, sameName ? CompareKind.Same : CompareKind.Modified, sameName ? "—" : "Name"));
                else if (x.Size != y.Size) result.Add(new CompareRow(i, name, x.Size, y.Size, CompareKind.Modified, $"{(y.Size - x.Size):+#,0;-#,0;0} B"));
                else
                {
                    bool sameHash = string.Equals(x.Hash, y.Hash, StringComparison.Ordinal);
                    CompareKind kind = sameHash && sameName ? CompareKind.Same : CompareKind.Modified;
                    string detail = !sameHash ? "Content" : !sameName ? "Name" : "—";
                    result.Add(new CompareRow(i, name, x.Size, y.Size, kind, detail));
                }
            }
            return result;
        }

        private static List<SnapshotEntry> ReadSnapshot(AfsComparisonSource s, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using Stream fs = new BoundedFileStream(s.Path, s.BaseOffset, s.Length, FileAccess.Read, FileShare.ReadWrite);
            using BinaryReader br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
            if (fs.Length < 16 || br.ReadByte() != 0x41 || br.ReadByte() != 0x46 || br.ReadByte() != 0x53 || br.ReadByte() != 0) throw new InvalidDataException("Invalid AFS signature.");
            uint count = br.ReadUInt32(); if (count == 0 || count > 1_000_000 || 8L + count * 8L + 8L > fs.Length) throw new InvalidDataException("Invalid AFS entry table.");
            var list = new List<SnapshotEntry>((int)count);
            for (int i = 0; i < count; i++) { token.ThrowIfCancellationRequested(); uint off = br.ReadUInt32(), stored = br.ReadUInt32(); list.Add(new SnapshotEntry { Offset = off, Stored = stored, Empty = stored == 0xFFFFF801 }); }
            uint tocOff = br.ReadUInt32(), tocSize = br.ReadUInt32();
            bool toc48 = tocOff > 0 && tocSize >= count * 48L && (long)tocOff + tocSize <= fs.Length;
            bool toc32 = tocOff > 0 && tocSize >= count * 32L && (long)tocOff + tocSize <= fs.Length;
            if (toc32)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    fs.Position = (long)tocOff + i * (toc48 ? 48L : 32L);
                    byte[] name = br.ReadBytes(32); int z = Array.IndexOf(name, (byte)0); int n = z >= 0 ? z : name.Length; list[i].Name = n > 0 ? Encoding.ASCII.GetString(name, 0, n).Trim() : string.Empty;
                    if (toc48) { fs.Position = (long)tocOff + i * 48L + 44L; list[i].Actual = br.ReadUInt32(); }
                }
            }
            byte[] buffer = new byte[1024 * 1024];
            for (int i = 0; i < list.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                SnapshotEntry e = list[i]; e.Name = string.IsNullOrWhiteSpace(e.Name) ? $"{i:D6}" : e.Name;
                if (e.Empty) { e.Size = 0; e.Hash = "EMPTY"; continue; }
                e.Size = e.Actual > 0 ? e.Actual : e.Stored;
                if (e.Size == 0) { e.Hash = "ZERO"; continue; }
                if (e.Offset <= 0 || (long)e.Offset + e.Size > fs.Length) { e.Hash = "INVALID"; continue; }
                fs.Position = e.Offset; long remain = e.Size;
                using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                while (remain > 0) { token.ThrowIfCancellationRequested(); int read = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, remain)); if (read <= 0) break; hash.AppendData(buffer, 0, read); remain -= read; }
                e.Hash = Convert.ToHexString(hash.GetHashAndReset());
            }
            return list;
        }

        private enum CompareKind { Same, Modified, OnlyLeft, OnlyRight }
        private sealed class SnapshotEntry { public uint Offset, Stored, Actual; public bool Empty; public long Size; public string Name = string.Empty, Hash = string.Empty; }
        private sealed record CompareRow(int Index, string Name, long? LeftSize, long? RightSize, CompareKind Kind, string Detail);
    }
}
