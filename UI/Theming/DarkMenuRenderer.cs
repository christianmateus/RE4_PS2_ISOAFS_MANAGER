using System.Drawing;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

            if (e.Item.Selected)
            {
                using SolidBrush brush = new SolidBrush(Color.FromArgb(55, 90, 135));
                e.Graphics.FillRectangle(brush, rect);
            }
            else
            {
                using SolidBrush brush = new SolidBrush(Color.FromArgb(35, 38, 44));
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? Color.Gainsboro
                : Color.FromArgb(115, 120, 128);

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using Pen pen = new Pen(Color.FromArgb(65, 70, 78));

            int y = e.Item.Height / 2;

            e.Graphics.DrawLine(
                pen,
                8,
                y,
                e.Item.Width - 8,
                y);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using Pen pen = new Pen(Color.FromArgb(60, 65, 72));

            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                e.ToolStrip.Width - 1,
                e.ToolStrip.Height - 1);
        }
    }

    public class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground
            => Color.FromArgb(35, 38, 44);

        public override Color MenuItemSelected
            => Color.FromArgb(55, 90, 135);

        public override Color MenuItemBorder
            => Color.FromArgb(75, 105, 145);

        public override Color MenuBorder
            => Color.FromArgb(60, 65, 72);

        public override Color ImageMarginGradientBegin
            => Color.FromArgb(35, 38, 44);

        public override Color ImageMarginGradientMiddle
            => Color.FromArgb(35, 38, 44);

        public override Color ImageMarginGradientEnd
            => Color.FromArgb(35, 38, 44);

        public override Color SeparatorDark
            => Color.FromArgb(65, 70, 78);

        public override Color SeparatorLight
            => Color.FromArgb(65, 70, 78);
    }
}