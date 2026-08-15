using System;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private ToolStripMenuItem? _menuFerramentasCompact;
        private ToolStripMenuItem? _menuAnalisarCompact;
        private ToolStripMenuItem? _menuRebuildCompact;

        private void ConfigurarMenuCompactacao()
        {
            if (_menuFerramentasCompact != null)
                return;

            _menuFerramentasCompact = new ToolStripMenuItem("Ferramentas");
            _menuAnalisarCompact = new ToolStripMenuItem("Analisar Espaço Recuperável...");
            _menuRebuildCompact = new ToolStripMenuItem("Compactar / Rebuild AFS...");

            _menuAnalisarCompact.Click += MenuAnalisarCompactacao_Click;
            _menuRebuildCompact.Click += MenuCompactarRebuild_Click;

            _menuFerramentasCompact.DropDownItems.Add(_menuAnalisarCompact);
            _menuFerramentasCompact.DropDownItems.Add(new ToolStripSeparator());
            _menuFerramentasCompact.DropDownItems.Add(_menuRebuildCompact);

            menuStrip1.Items.Add(_menuFerramentasCompact);

            // Mantém o menu coerente com o tema já ativo.
            AplicarTema();
        }
    }
}
