using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private void ConfigurarDragDrop()
        {
            // The form itself and all regular child controls accept AFS/ISO drops.
            // The grid keeps special handling so ordinary files can still be dropped
            // directly over an entry for in-place import.
            AllowDrop = true;
            DragEnter += Janela_DragEnter;
            DragDrop += Janela_DragDrop;
            ConfigurarDragDropGlobal(this);

            dgvArquivos.AllowDrop = true;
            dgvArquivos.DragEnter += Grid_DragEnter;
            dgvArquivos.DragOver += Grid_DragOver;
            dgvArquivos.DragDrop += Grid_DragDrop;
            dgvArquivos.Paint += DgvArquivos_PaintDragHint;
        }

        private void ConfigurarDragDropGlobal(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (ReferenceEquals(control, dgvArquivos))
                    continue;

                control.AllowDrop = true;
                control.DragEnter += Janela_DragEnter;
                control.DragDrop += Janela_DragDrop;

                if (control.HasChildren)
                    ConfigurarDragDropGlobal(control);
            }
        }

        private static string? ObterUnicoArquivo(DragEventArgs e)
        {
            if (!e.Data!.GetDataPresent(DataFormats.FileDrop)) return null;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length != 1) return null;
            return File.Exists(files[0]) ? files[0] : null;
        }

        private static bool EhAfsOuIso(string path)
        {
            string ext = Path.GetExtension(path);
            return ext.Equals(".afs", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".iso", StringComparison.OrdinalIgnoreCase);
        }

        private void Janela_DragEnter(object? sender, DragEventArgs e)
        {
            string? path = ObterUnicoArquivo(e);
            e.Effect = path != null && EhAfsOuIso(path)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void Janela_DragDrop(object? sender, DragEventArgs e)
        {
            string? path = ObterUnicoArquivo(e);
            if (path != null && EhAfsOuIso(path))
                AbrirCaminhoExterno(path);
        }

        private void Grid_DragEnter(object? sender, DragEventArgs e)
        {
            string? path = ObterUnicoArquivo(e);
            if (path == null)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            // AFS/ISO always means "open", even when dropped over a populated row.
            if (EhAfsOuIso(path))
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            e.Effect = _afsPath != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void Grid_DragOver(object? sender, DragEventArgs e)
        {
            string? path = ObterUnicoArquivo(e);
            if (path == null)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (EhAfsOuIso(path))
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            if (_afsPath == null)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            var client = dgvArquivos.PointToClient(new Point(e.X, e.Y));
            var hit = dgvArquivos.HitTest(client.X, client.Y);
            if (hit.RowIndex < 0 || dgvArquivos.Rows[hit.RowIndex].Tag is not AfsEntry entry || entry.IsEmpty)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            dgvArquivos.ClearSelection();
            dgvArquivos.Rows[hit.RowIndex].Selected = true;
            e.Effect = DragDropEffects.Copy;
        }

        private void Grid_DragDrop(object? sender, DragEventArgs e)
        {
            string? path = ObterUnicoArquivo(e);
            if (path == null) return;

            if (EhAfsOuIso(path))
            {
                AbrirCaminhoExterno(path);
                return;
            }

            if (_afsPath == null) return;

            var client = dgvArquivos.PointToClient(new Point(e.X, e.Y));
            var hit = dgvArquivos.HitTest(client.X, client.Y);
            if (hit.RowIndex < 0 || dgvArquivos.Rows[hit.RowIndex].Tag is not AfsEntry entry || entry.IsEmpty) return;
            ImportarArquivoSobreEntrada(entry, path);
        }

        private void DgvArquivos_PaintDragHint(object? sender, PaintEventArgs e)
        {
            if (dgvArquivos.Rows.Count != 0)
                return;

            string text = Tr(
                "Arraste um arquivo .AFS ou .ISO aqui para abrir",
                "Drop an .AFS or .ISO file here to open");

            Rectangle area = dgvArquivos.ClientRectangle;
            area.Y += dgvArquivos.ColumnHeadersHeight;
            area.Height -= dgvArquivos.ColumnHeadersHeight;

            Color textColor = _temaEscuro
                ? Color.FromArgb(145, 150, 160)
                : Color.FromArgb(105, 110, 120);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                dgvArquivos.Font,
                area,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak |
                TextFormatFlags.NoPrefix);
        }
    }
}
