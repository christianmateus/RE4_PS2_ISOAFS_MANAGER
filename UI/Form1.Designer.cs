namespace FerramentaAFS
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuArquivo;
        private System.Windows.Forms.ToolStripMenuItem menuAbrir;
        private System.Windows.Forms.ToolStripSeparator menuArquivoSep1;
        private System.Windows.Forms.ToolStripMenuItem menuSair;

        private System.Windows.Forms.ToolStripMenuItem menuExtrair;
        private System.Windows.Forms.ToolStripMenuItem menuExtrairSelecionado;
        private System.Windows.Forms.ToolStripMenuItem menuExtrairTodos;

        private System.Windows.Forms.ToolStripMenuItem menuImportar;
        private System.Windows.Forms.ToolStripMenuItem menuImportarSelecionado;
        private System.Windows.Forms.ToolStripMenuItem menuImportarTodos;

        private System.Windows.Forms.ToolStripMenuItem menuExibir;
        private System.Windows.Forms.ToolStripMenuItem menuAlternarTema;

        private System.Windows.Forms.TextBox txtBuscar;
        private System.Windows.Forms.DataGridView dgvArquivos;
        private System.Windows.Forms.GroupBox groupAfs;
        private System.Windows.Forms.GroupBox groupEntry;

        private System.Windows.Forms.Label lblArquivoTitulo;
        private System.Windows.Forms.Label lblArquivo;
        private System.Windows.Forms.Label lblQuantidadeTitulo;
        private System.Windows.Forms.Label lblQuantidade;
        private System.Windows.Forms.Label lblTamanhoAfsTitulo;
        private System.Windows.Forms.Label lblTamanhoAfs;
        private System.Windows.Forms.Label lblTocOffsetTitulo;
        private System.Windows.Forms.Label lblTocOffset;
        private System.Windows.Forms.Label lblTocSizeTitulo;
        private System.Windows.Forms.Label lblTocSize;
        private System.Windows.Forms.Label lblBuscaTitulo;
        private System.Windows.Forms.Label lblResultados;

        private System.Windows.Forms.Label lblIndexTitulo;
        private System.Windows.Forms.Label lblIndex;
        private System.Windows.Forms.Label lblNomeTitulo;
        private System.Windows.Forms.Label lblNome;
        private System.Windows.Forms.Label lblTipoTitulo;
        private System.Windows.Forms.Label lblTipo;
        private System.Windows.Forms.Label lblOffsetTitulo;
        private System.Windows.Forms.Label lblOffset;
        private System.Windows.Forms.Label lblCurrentSizeTitulo;
        private System.Windows.Forms.Label lblCurrentSize;
        private System.Windows.Forms.Label lblStoredSizeTitulo;
        private System.Windows.Forms.Label lblStoredSize;
        private System.Windows.Forms.Label lblAllocatedSizeTitulo;
        private System.Windows.Forms.Label lblAllocatedSize;
        private System.Windows.Forms.Label lblPaddingTitulo;
        private System.Windows.Forms.Label lblPadding;
        private System.Windows.Forms.Label lblCompactSizeTitulo;
        private System.Windows.Forms.Label lblCompactSize;
        private System.Windows.Forms.Label lblExcessTitulo;
        private System.Windows.Forms.Label lblExcess;
        private System.Windows.Forms.Label lblTimestampTitulo;
        private System.Windows.Forms.Label lblTimestamp;
        private System.Windows.Forms.Label lblMetadataTitulo;
        private System.Windows.Forms.Label lblMetadata;

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            menuArquivo = new System.Windows.Forms.ToolStripMenuItem();
            menuAbrir = new System.Windows.Forms.ToolStripMenuItem();
            menuArquivoSep1 = new System.Windows.Forms.ToolStripSeparator();
            menuSair = new System.Windows.Forms.ToolStripMenuItem();
            menuExtrair = new System.Windows.Forms.ToolStripMenuItem();
            menuExtrairSelecionado = new System.Windows.Forms.ToolStripMenuItem();
            menuExtrairTodos = new System.Windows.Forms.ToolStripMenuItem();
            menuImportar = new System.Windows.Forms.ToolStripMenuItem();
            menuImportarSelecionado = new System.Windows.Forms.ToolStripMenuItem();
            menuImportarTodos = new System.Windows.Forms.ToolStripMenuItem();
            menuExibir = new System.Windows.Forms.ToolStripMenuItem();
            menuAlternarTema = new System.Windows.Forms.ToolStripMenuItem();
            txtBuscar = new System.Windows.Forms.TextBox();
            dgvArquivos = new System.Windows.Forms.DataGridView();
            groupAfs = new System.Windows.Forms.GroupBox();
            lblArquivoTitulo = new System.Windows.Forms.Label();
            lblArquivo = new System.Windows.Forms.Label();
            lblQuantidadeTitulo = new System.Windows.Forms.Label();
            lblQuantidade = new System.Windows.Forms.Label();
            lblTamanhoAfsTitulo = new System.Windows.Forms.Label();
            lblTamanhoAfs = new System.Windows.Forms.Label();
            lblTocOffsetTitulo = new System.Windows.Forms.Label();
            lblTocOffset = new System.Windows.Forms.Label();
            lblTocSizeTitulo = new System.Windows.Forms.Label();
            lblTocSize = new System.Windows.Forms.Label();
            groupEntry = new System.Windows.Forms.GroupBox();
            lblIndexTitulo = new System.Windows.Forms.Label();
            lblIndex = new System.Windows.Forms.Label();
            lblNomeTitulo = new System.Windows.Forms.Label();
            lblNome = new System.Windows.Forms.Label();
            lblTipoTitulo = new System.Windows.Forms.Label();
            lblTipo = new System.Windows.Forms.Label();
            lblOffsetTitulo = new System.Windows.Forms.Label();
            lblOffset = new System.Windows.Forms.Label();
            lblCurrentSizeTitulo = new System.Windows.Forms.Label();
            lblCurrentSize = new System.Windows.Forms.Label();
            lblStoredSizeTitulo = new System.Windows.Forms.Label();
            lblStoredSize = new System.Windows.Forms.Label();
            lblAllocatedSizeTitulo = new System.Windows.Forms.Label();
            lblAllocatedSize = new System.Windows.Forms.Label();
            lblPaddingTitulo = new System.Windows.Forms.Label();
            lblPadding = new System.Windows.Forms.Label();
            lblCompactSizeTitulo = new System.Windows.Forms.Label();
            lblCompactSize = new System.Windows.Forms.Label();
            lblExcessTitulo = new System.Windows.Forms.Label();
            lblExcess = new System.Windows.Forms.Label();
            lblTimestampTitulo = new System.Windows.Forms.Label();
            lblTimestamp = new System.Windows.Forms.Label();
            lblMetadataTitulo = new System.Windows.Forms.Label();
            lblMetadata = new System.Windows.Forms.Label();
            lblBuscaTitulo = new System.Windows.Forms.Label();
            lblResultados = new System.Windows.Forms.Label();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            colIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colNome = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTipo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colOffset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colStored = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAllocated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCompact = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colExcess = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvArquivos).BeginInit();
            groupAfs.SuspendLayout();
            groupEntry.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = System.Drawing.Color.FromArgb(35, 38, 44);
            menuStrip1.ForeColor = System.Drawing.Color.Gainsboro;
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuArquivo, menuExtrair, menuImportar, menuExibir });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(1389, 24);
            menuStrip1.TabIndex = 7;
            // 
            // menuArquivo
            // 
            menuArquivo.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuAbrir, menuArquivoSep1, menuSair });
            menuArquivo.ForeColor = System.Drawing.Color.Gainsboro;
            menuArquivo.Name = "menuArquivo";
            menuArquivo.Size = new System.Drawing.Size(61, 20);
            menuArquivo.Text = "Arquivo";
            // 
            // menuAbrir
            // 
            menuAbrir.Name = "menuAbrir";
            menuAbrir.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O;
            menuAbrir.Size = new System.Drawing.Size(175, 22);
            menuAbrir.Text = "Abrir AFS...";
            menuAbrir.Click += MenuAbrir_Click;
            // 
            // menuArquivoSep1
            // 
            menuArquivoSep1.Name = "menuArquivoSep1";
            menuArquivoSep1.Size = new System.Drawing.Size(172, 6);
            // 
            // menuSair
            // 
            menuSair.Name = "menuSair";
            menuSair.Size = new System.Drawing.Size(175, 22);
            menuSair.Text = "Sair";
            menuSair.Click += MenuSair_Click;
            // 
            // menuExtrair
            // 
            menuExtrair.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuExtrairSelecionado, menuExtrairTodos });
            menuExtrair.ForeColor = System.Drawing.Color.Gainsboro;
            menuExtrair.Name = "menuExtrair";
            menuExtrair.Size = new System.Drawing.Size(52, 20);
            menuExtrair.Text = "Extrair";
            // 
            // menuExtrairSelecionado
            // 
            menuExtrairSelecionado.Name = "menuExtrairSelecionado";
            menuExtrairSelecionado.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E;
            menuExtrairSelecionado.Size = new System.Drawing.Size(223, 22);
            menuExtrairSelecionado.Text = "Extrair Selecionado...";
            menuExtrairSelecionado.Click += BtnExtrair_Click;
            // 
            // menuExtrairTodos
            // 
            menuExtrairTodos.Name = "menuExtrairTodos";
            menuExtrairTodos.Size = new System.Drawing.Size(223, 22);
            menuExtrairTodos.Text = "Extrair Todos...";
            menuExtrairTodos.Click += BtnExtrairTodos_Click;
            // 
            // menuImportar
            // 
            menuImportar.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuImportarSelecionado, menuImportarTodos });
            menuImportar.ForeColor = System.Drawing.Color.Gainsboro;
            menuImportar.Name = "menuImportar";
            menuImportar.Size = new System.Drawing.Size(65, 20);
            menuImportar.Text = "Importar";
            // 
            // menuImportarSelecionado
            // 
            menuImportarSelecionado.Name = "menuImportarSelecionado";
            menuImportarSelecionado.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I;
            menuImportarSelecionado.Size = new System.Drawing.Size(237, 22);
            menuImportarSelecionado.Text = "Importar Selecionado...";
            menuImportarSelecionado.Click += MenuImportarSelecionado_Click;
            // 
            // menuImportarTodos
            // 
            menuImportarTodos.Name = "menuImportarTodos";
            menuImportarTodos.Size = new System.Drawing.Size(237, 22);
            menuImportarTodos.Text = "Importar Todos de uma Pasta...";
            menuImportarTodos.Click += this.MenuImportarTodosIndexado_Click;
            // 
            // menuExibir
            // 
            menuExibir.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuAlternarTema });
            menuExibir.ForeColor = System.Drawing.Color.Gainsboro;
            menuExibir.Name = "menuExibir";
            menuExibir.Size = new System.Drawing.Size(48, 20);
            menuExibir.Text = "Exibir";
            // 
            // menuAlternarTema
            // 
            menuAlternarTema.Name = "menuAlternarTema";
            menuAlternarTema.Size = new System.Drawing.Size(133, 22);
            menuAlternarTema.Text = "Tema Claro";
            menuAlternarTema.Click += MenuAlternarTema_Click;
            // 
            // txtBuscar
            // 
            txtBuscar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtBuscar.BackColor = System.Drawing.Color.FromArgb(31, 34, 40);
            txtBuscar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtBuscar.ForeColor = System.Drawing.Color.WhiteSmoke;
            txtBuscar.Location = new System.Drawing.Point(72, 35);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.PlaceholderText = "Nome, índice ou tipo...";
            txtBuscar.Size = new System.Drawing.Size(1120, 23);
            txtBuscar.TabIndex = 5;
            // 
            // dgvArquivos
            // 
            dgvArquivos.AllowUserToAddRows = false;
            dgvArquivos.AllowUserToDeleteRows = false;
            dgvArquivos.AllowUserToResizeRows = false;
            dgvArquivos.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvArquivos.BackgroundColor = System.Drawing.Color.FromArgb(26, 29, 34);
            dgvArquivos.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dgvArquivos.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(39, 43, 50);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(39, 43, 50);
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvArquivos.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvArquivos.ColumnHeadersHeight = 32;
            dgvArquivos.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colIndex, colNome, colTipo, colOffset, colCurrent, colStored, colAllocated, colCompact, colExcess, colStatus });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(31, 34, 40);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(44, 103, 176);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvArquivos.DefaultCellStyle = dataGridViewCellStyle2;
            dgvArquivos.EnableHeadersVisualStyles = false;
            dgvArquivos.GridColor = System.Drawing.Color.FromArgb(52, 57, 65);
            dgvArquivos.Location = new System.Drawing.Point(14, 175);
            dgvArquivos.MultiSelect = false;
            dgvArquivos.Name = "dgvArquivos";
            dgvArquivos.ReadOnly = true;
            dgvArquivos.RowHeadersVisible = false;
            dgvArquivos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvArquivos.Size = new System.Drawing.Size(1015, 578);
            dgvArquivos.TabIndex = 2;
            // 
            // groupAfs
            // 
            groupAfs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupAfs.Controls.Add(lblArquivoTitulo);
            groupAfs.Controls.Add(lblArquivo);
            groupAfs.Controls.Add(lblQuantidadeTitulo);
            groupAfs.Controls.Add(lblQuantidade);
            groupAfs.Controls.Add(lblTamanhoAfsTitulo);
            groupAfs.Controls.Add(lblTamanhoAfs);
            groupAfs.Controls.Add(lblTocOffsetTitulo);
            groupAfs.Controls.Add(lblTocOffset);
            groupAfs.Controls.Add(lblTocSizeTitulo);
            groupAfs.Controls.Add(lblTocSize);
            groupAfs.ForeColor = System.Drawing.Color.Gainsboro;
            groupAfs.Location = new System.Drawing.Point(14, 68);
            groupAfs.Name = "groupAfs";
            groupAfs.Size = new System.Drawing.Size(1361, 96);
            groupAfs.TabIndex = 3;
            groupAfs.TabStop = false;
            groupAfs.Text = "Informações do AFS";
            // 
            // lblArquivoTitulo
            // 
            lblArquivoTitulo.AutoSize = true;
            lblArquivoTitulo.ForeColor = System.Drawing.Color.Silver;
            lblArquivoTitulo.Location = new System.Drawing.Point(16, 25);
            lblArquivoTitulo.Name = "lblArquivoTitulo";
            lblArquivoTitulo.Size = new System.Drawing.Size(52, 15);
            lblArquivoTitulo.TabIndex = 0;
            lblArquivoTitulo.Text = "Arquivo:";
            // 
            // lblArquivo
            // 
            lblArquivo.AutoSize = true;
            lblArquivo.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblArquivo.Location = new System.Drawing.Point(90, 25);
            lblArquivo.Name = "lblArquivo";
            lblArquivo.Size = new System.Drawing.Size(12, 15);
            lblArquivo.TabIndex = 1;
            lblArquivo.Text = "-";
            // 
            // lblQuantidadeTitulo
            // 
            lblQuantidadeTitulo.AutoSize = true;
            lblQuantidadeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblQuantidadeTitulo.Location = new System.Drawing.Point(16, 50);
            lblQuantidadeTitulo.Name = "lblQuantidadeTitulo";
            lblQuantidadeTitulo.Size = new System.Drawing.Size(55, 15);
            lblQuantidadeTitulo.TabIndex = 2;
            lblQuantidadeTitulo.Text = "Entradas:";
            // 
            // lblQuantidade
            // 
            lblQuantidade.AutoSize = true;
            lblQuantidade.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblQuantidade.Location = new System.Drawing.Point(90, 50);
            lblQuantidade.Name = "lblQuantidade";
            lblQuantidade.Size = new System.Drawing.Size(12, 15);
            lblQuantidade.TabIndex = 3;
            lblQuantidade.Text = "-";
            // 
            // lblTamanhoAfsTitulo
            // 
            lblTamanhoAfsTitulo.AutoSize = true;
            lblTamanhoAfsTitulo.ForeColor = System.Drawing.Color.Silver;
            lblTamanhoAfsTitulo.Location = new System.Drawing.Point(16, 75);
            lblTamanhoAfsTitulo.Name = "lblTamanhoAfsTitulo";
            lblTamanhoAfsTitulo.Size = new System.Drawing.Size(59, 15);
            lblTamanhoAfsTitulo.TabIndex = 4;
            lblTamanhoAfsTitulo.Text = "Tamanho:";
            // 
            // lblTamanhoAfs
            // 
            lblTamanhoAfs.AutoSize = true;
            lblTamanhoAfs.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblTamanhoAfs.Location = new System.Drawing.Point(90, 75);
            lblTamanhoAfs.Name = "lblTamanhoAfs";
            lblTamanhoAfs.Size = new System.Drawing.Size(12, 15);
            lblTamanhoAfs.TabIndex = 5;
            lblTamanhoAfs.Text = "-";
            // 
            // lblTocOffsetTitulo
            // 
            lblTocOffsetTitulo.AutoSize = true;
            lblTocOffsetTitulo.ForeColor = System.Drawing.Color.Silver;
            lblTocOffsetTitulo.Location = new System.Drawing.Point(520, 25);
            lblTocOffsetTitulo.Name = "lblTocOffsetTitulo";
            lblTocOffsetTitulo.Size = new System.Drawing.Size(67, 15);
            lblTocOffsetTitulo.TabIndex = 6;
            lblTocOffsetTitulo.Text = "TOC Offset:";
            // 
            // lblTocOffset
            // 
            lblTocOffset.AutoSize = true;
            lblTocOffset.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblTocOffset.Location = new System.Drawing.Point(612, 25);
            lblTocOffset.Name = "lblTocOffset";
            lblTocOffset.Size = new System.Drawing.Size(12, 15);
            lblTocOffset.TabIndex = 7;
            lblTocOffset.Text = "-";
            // 
            // lblTocSizeTitulo
            // 
            lblTocSizeTitulo.AutoSize = true;
            lblTocSizeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblTocSizeTitulo.Location = new System.Drawing.Point(520, 50);
            lblTocSizeTitulo.Name = "lblTocSizeTitulo";
            lblTocSizeTitulo.Size = new System.Drawing.Size(55, 15);
            lblTocSizeTitulo.TabIndex = 8;
            lblTocSizeTitulo.Text = "TOC Size:";
            // 
            // lblTocSize
            // 
            lblTocSize.AutoSize = true;
            lblTocSize.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblTocSize.Location = new System.Drawing.Point(612, 50);
            lblTocSize.Name = "lblTocSize";
            lblTocSize.Size = new System.Drawing.Size(12, 15);
            lblTocSize.TabIndex = 9;
            lblTocSize.Text = "-";
            // 
            // groupEntry
            // 
            groupEntry.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            groupEntry.Controls.Add(lblIndexTitulo);
            groupEntry.Controls.Add(lblIndex);
            groupEntry.Controls.Add(lblNomeTitulo);
            groupEntry.Controls.Add(lblNome);
            groupEntry.Controls.Add(lblTipoTitulo);
            groupEntry.Controls.Add(lblTipo);
            groupEntry.Controls.Add(lblOffsetTitulo);
            groupEntry.Controls.Add(lblOffset);
            groupEntry.Controls.Add(lblCurrentSizeTitulo);
            groupEntry.Controls.Add(lblCurrentSize);
            groupEntry.Controls.Add(lblStoredSizeTitulo);
            groupEntry.Controls.Add(lblStoredSize);
            groupEntry.Controls.Add(lblAllocatedSizeTitulo);
            groupEntry.Controls.Add(lblAllocatedSize);
            groupEntry.Controls.Add(lblPaddingTitulo);
            groupEntry.Controls.Add(lblPadding);
            groupEntry.Controls.Add(lblCompactSizeTitulo);
            groupEntry.Controls.Add(lblCompactSize);
            groupEntry.Controls.Add(lblExcessTitulo);
            groupEntry.Controls.Add(lblExcess);
            groupEntry.Controls.Add(lblTimestampTitulo);
            groupEntry.Controls.Add(lblTimestamp);
            groupEntry.Controls.Add(lblMetadataTitulo);
            groupEntry.Controls.Add(lblMetadata);
            groupEntry.ForeColor = System.Drawing.Color.Gainsboro;
            groupEntry.Location = new System.Drawing.Point(1040, 175);
            groupEntry.Name = "groupEntry";
            groupEntry.Size = new System.Drawing.Size(335, 578);
            groupEntry.TabIndex = 1;
            groupEntry.TabStop = false;
            groupEntry.Text = "Entrada selecionada";
            // 
            // lblIndexTitulo
            // 
            lblIndexTitulo.AutoSize = true;
            lblIndexTitulo.ForeColor = System.Drawing.Color.Silver;
            lblIndexTitulo.Location = new System.Drawing.Point(16, 30);
            lblIndexTitulo.Name = "lblIndexTitulo";
            lblIndexTitulo.TabIndex = 0;
            lblIndexTitulo.Text = "Index:";
            // 
            // lblIndex
            // 
            lblIndex.AutoSize = true;
            lblIndex.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblIndex.Location = new System.Drawing.Point(125, 30);
            lblIndex.Name = "lblIndex";
            lblIndex.TabIndex = 1;
            lblIndex.Text = "-";
            // 
            // 
            // lblNomeTitulo
            // 
            lblNomeTitulo.AutoSize = true;
            lblNomeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblNomeTitulo.Location = new System.Drawing.Point(16, 61);
            lblNomeTitulo.Name = "lblNomeTitulo";
            lblNomeTitulo.Size = new System.Drawing.Size(43, 15);
            lblNomeTitulo.TabIndex = 2;
            lblNomeTitulo.Text = "Nome:";
            // 
            // lblNome
            // 
            lblNome.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblNome.Location = new System.Drawing.Point(125, 61);
            lblNome.Name = "lblNome";
            lblNome.Size = new System.Drawing.Size(194, 40);
            lblNome.TabIndex = 3;
            lblNome.Text = "-";
            // 
            // lblTipoTitulo
            // 
            lblTipoTitulo.AutoSize = true;
            lblTipoTitulo.ForeColor = System.Drawing.Color.Silver;
            lblTipoTitulo.Location = new System.Drawing.Point(16, 109);
            lblTipoTitulo.Name = "lblTipoTitulo";
            lblTipoTitulo.TabIndex = 0;
            lblTipoTitulo.Text = "Tipo:";
            // 
            // lblTipo
            // 
            lblTipo.AutoSize = true;
            lblTipo.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblTipo.Location = new System.Drawing.Point(125, 109);
            lblTipo.Name = "lblTipo";
            lblTipo.TabIndex = 1;
            lblTipo.Text = "-";
            // 
            // 
            // lblOffsetTitulo
            // 
            lblOffsetTitulo.AutoSize = true;
            lblOffsetTitulo.ForeColor = System.Drawing.Color.Silver;
            lblOffsetTitulo.Location = new System.Drawing.Point(16, 140);
            lblOffsetTitulo.Name = "lblOffsetTitulo";
            lblOffsetTitulo.TabIndex = 0;
            lblOffsetTitulo.Text = "Offset:";
            // 
            // lblOffset
            // 
            lblOffset.AutoSize = true;
            lblOffset.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblOffset.Location = new System.Drawing.Point(125, 140);
            lblOffset.Name = "lblOffset";
            lblOffset.TabIndex = 1;
            lblOffset.Text = "-";
            // 
            // 
            // lblCurrentSizeTitulo
            // 
            lblCurrentSizeTitulo.AutoSize = true;
            lblCurrentSizeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblCurrentSizeTitulo.Location = new System.Drawing.Point(16, 171);
            lblCurrentSizeTitulo.Name = "lblCurrentSizeTitulo";
            lblCurrentSizeTitulo.TabIndex = 0;
            lblCurrentSizeTitulo.Text = "Current Size:";
            // 
            // lblCurrentSize
            // 
            lblCurrentSize.AutoSize = true;
            lblCurrentSize.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblCurrentSize.Location = new System.Drawing.Point(125, 171);
            lblCurrentSize.Name = "lblCurrentSize";
            lblCurrentSize.TabIndex = 1;
            lblCurrentSize.Text = "-";
            // 
            // 
            // lblStoredSizeTitulo
            // 
            lblStoredSizeTitulo.AutoSize = true;
            lblStoredSizeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblStoredSizeTitulo.Location = new System.Drawing.Point(16, 202);
            lblStoredSizeTitulo.Name = "lblStoredSizeTitulo";
            lblStoredSizeTitulo.TabIndex = 0;
            lblStoredSizeTitulo.Text = "Stored Size:";
            // 
            // lblStoredSize
            // 
            lblStoredSize.AutoSize = true;
            lblStoredSize.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblStoredSize.Location = new System.Drawing.Point(125, 202);
            lblStoredSize.Name = "lblStoredSize";
            lblStoredSize.TabIndex = 1;
            lblStoredSize.Text = "-";
            // 
            // 
            // lblAllocatedSizeTitulo
            // 
            lblAllocatedSizeTitulo.AutoSize = true;
            lblAllocatedSizeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblAllocatedSizeTitulo.Location = new System.Drawing.Point(16, 233);
            lblAllocatedSizeTitulo.Name = "lblAllocatedSizeTitulo";
            lblAllocatedSizeTitulo.TabIndex = 0;
            lblAllocatedSizeTitulo.Text = "Max Size:";
            // 
            // lblAllocatedSize
            // 
            lblAllocatedSize.AutoSize = true;
            lblAllocatedSize.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblAllocatedSize.Location = new System.Drawing.Point(125, 233);
            lblAllocatedSize.Name = "lblAllocatedSize";
            lblAllocatedSize.TabIndex = 1;
            lblAllocatedSize.Text = "-";
            // 
            // 
            // lblPaddingTitulo
            // 
            lblPaddingTitulo.AutoSize = true;
            lblPaddingTitulo.ForeColor = System.Drawing.Color.Silver;
            lblPaddingTitulo.Location = new System.Drawing.Point(16, 264);
            lblPaddingTitulo.Name = "lblPaddingTitulo";
            lblPaddingTitulo.TabIndex = 0;
            lblPaddingTitulo.Text = "Padding total:";
            // 
            // lblPadding
            // 
            lblPadding.AutoSize = true;
            lblPadding.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblPadding.Location = new System.Drawing.Point(125, 264);
            lblPadding.Name = "lblPadding";
            lblPadding.TabIndex = 1;
            lblPadding.Text = "-";
            // 
            // 
            // lblCompactSizeTitulo
            // 
            lblCompactSizeTitulo.AutoSize = true;
            lblCompactSizeTitulo.ForeColor = System.Drawing.Color.Silver;
            lblCompactSizeTitulo.Location = new System.Drawing.Point(16, 295);
            lblCompactSizeTitulo.Name = "lblCompactSizeTitulo";
            lblCompactSizeTitulo.TabIndex = 0;
            lblCompactSizeTitulo.Text = "Compact Size:";
            // 
            // lblCompactSize
            // 
            lblCompactSize.AutoSize = true;
            lblCompactSize.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblCompactSize.Location = new System.Drawing.Point(125, 295);
            lblCompactSize.Name = "lblCompactSize";
            lblCompactSize.TabIndex = 1;
            lblCompactSize.Text = "-";
            // 
            // 
            // lblExcessTitulo
            // 
            lblExcessTitulo.AutoSize = true;
            lblExcessTitulo.ForeColor = System.Drawing.Color.Silver;
            lblExcessTitulo.Location = new System.Drawing.Point(16, 326);
            lblExcessTitulo.Name = "lblExcessTitulo";
            lblExcessTitulo.TabIndex = 0;
            lblExcessTitulo.Text = "Waste real:";
            // 
            // lblExcess
            // 
            lblExcess.AutoSize = true;
            lblExcess.ForeColor = System.Drawing.Color.FromArgb(255, 190, 90);
            lblExcess.Location = new System.Drawing.Point(125, 326);
            lblExcess.Name = "lblExcess";
            lblExcess.TabIndex = 1;
            lblExcess.Text = "-";
            // 
            // 
            // lblTimestampTitulo
            // 
            lblTimestampTitulo.AutoSize = true;
            lblTimestampTitulo.ForeColor = System.Drawing.Color.Silver;
            lblTimestampTitulo.Location = new System.Drawing.Point(16, 357);
            lblTimestampTitulo.Name = "lblTimestampTitulo";
            lblTimestampTitulo.TabIndex = 0;
            lblTimestampTitulo.Text = "Timestamp:";
            // 
            // lblTimestamp
            // 
            lblTimestamp.AutoSize = true;
            lblTimestamp.ForeColor = System.Drawing.Color.WhiteSmoke;
            lblTimestamp.Location = new System.Drawing.Point(125, 357);
            lblTimestamp.Name = "lblTimestamp";
            lblTimestamp.TabIndex = 1;
            lblTimestamp.Text = "-";
            // 
            // 
            // lblMetadataTitulo
            // 
            lblMetadataTitulo.AutoSize = true;
            lblMetadataTitulo.ForeColor = System.Drawing.Color.Silver;
            lblMetadataTitulo.Location = new System.Drawing.Point(16, 395);
            lblMetadataTitulo.Name = "lblMetadataTitulo";
            lblMetadataTitulo.Size = new System.Drawing.Size(85, 15);
            lblMetadataTitulo.TabIndex = 22;
            lblMetadataTitulo.Text = "TOC Metadata:";
            // 
            // lblMetadata
            // 
            lblMetadata.BackColor = System.Drawing.Color.FromArgb(25, 28, 33);
            lblMetadata.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblMetadata.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lblMetadata.ForeColor = System.Drawing.Color.FromArgb(190, 220, 255);
            lblMetadata.Location = new System.Drawing.Point(16, 420);
            lblMetadata.Name = "lblMetadata";
            lblMetadata.Size = new System.Drawing.Size(302, 65);
            lblMetadata.TabIndex = 23;
            lblMetadata.Text = "-";
            // 
            // lblBuscaTitulo
            // 
            lblBuscaTitulo.AutoSize = true;
            lblBuscaTitulo.ForeColor = System.Drawing.Color.Silver;
            lblBuscaTitulo.Location = new System.Drawing.Point(16, 39);
            lblBuscaTitulo.Name = "lblBuscaTitulo";
            lblBuscaTitulo.Size = new System.Drawing.Size(45, 15);
            lblBuscaTitulo.TabIndex = 6;
            lblBuscaTitulo.Text = "Buscar:";
            // 
            // lblResultados
            // 
            lblResultados.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblResultados.AutoSize = true;
            lblResultados.ForeColor = System.Drawing.Color.Silver;
            lblResultados.Location = new System.Drawing.Point(1210, 39);
            lblResultados.Name = "lblResultados";
            lblResultados.Size = new System.Drawing.Size(70, 15);
            lblResultados.TabIndex = 4;
            lblResultados.Text = "0 resultados";
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = System.Drawing.Color.FromArgb(35, 38, 44);
            statusStrip1.ForeColor = System.Drawing.Color.Gainsboro;
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new System.Drawing.Point(0, 768);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1389, 22);
            statusStrip1.TabIndex = 0;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.ForeColor = System.Drawing.Color.Gainsboro;
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new System.Drawing.Size(43, 17);
            toolStripStatusLabel1.Text = "Pronto";
            // 
            // colIndex
            // 
            colIndex.HeaderText = "Index";
            colIndex.Name = "colIndex";
            colIndex.ReadOnly = true;
            colIndex.Width = 60;
            // 
            // colNome
            // 
            colNome.HeaderText = "Nome";
            colNome.Name = "colNome";
            colNome.ReadOnly = true;
            colNome.Width = 170;
            // 
            // colTipo
            // 
            colTipo.HeaderText = "Tipo";
            colTipo.Name = "colTipo";
            colTipo.ReadOnly = true;
            colTipo.Width = 72;
            // 
            // colOffset
            // 
            colOffset.HeaderText = "Offset";
            colOffset.Name = "colOffset";
            colOffset.ReadOnly = true;
            colOffset.Width = 95;
            // 
            // colCurrent
            // 
            colCurrent.HeaderText = "Current Size";
            colCurrent.Name = "colCurrent";
            colCurrent.ReadOnly = true;
            colCurrent.Width = 108;
            // 
            // colStored
            // 
            colStored.HeaderText = "Stored Size";
            colStored.Name = "colStored";
            colStored.ReadOnly = true;
            colStored.Width = 108;
            // 
            // colAllocated
            // 
            colAllocated.HeaderText = "Max Size";
            colAllocated.Name = "colAllocated";
            colAllocated.ReadOnly = true;
            colAllocated.Width = 108;
            // 
            // colCompact
            // 
            colCompact.HeaderText = "Compact Size";
            colCompact.Name = "colCompact";
            colCompact.ReadOnly = true;
            colCompact.Width = 108;
            // 
            // colExcess
            // 
            colExcess.HeaderText = "Waste";
            colExcess.Name = "colExcess";
            colExcess.ReadOnly = true;
            // 
            // colStatus
            // 
            colStatus.HeaderText = "Status";
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            colStatus.Width = 72;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(24, 27, 32);
            ClientSize = new System.Drawing.Size(1389, 790);
            Controls.Add(statusStrip1);
            Controls.Add(groupEntry);
            Controls.Add(dgvArquivos);
            Controls.Add(groupAfs);
            Controls.Add(lblResultados);
            Controls.Add(txtBuscar);
            Controls.Add(lblBuscaTitulo);
            Controls.Add(menuStrip1);
            ForeColor = System.Drawing.Color.Gainsboro;
            MainMenuStrip = menuStrip1;
            MinimumSize = new System.Drawing.Size(1180, 700);
            Name = "Form1";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Ferramenta AFS";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvArquivos).EndInit();
            groupAfs.ResumeLayout(false);
            groupAfs.PerformLayout();
            groupEntry.ResumeLayout(false);
            groupEntry.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void AddDetail(System.Windows.Forms.Label titulo, System.Windows.Forms.Label valor, string texto, int y)
        {
            titulo.AutoSize = true;
            titulo.ForeColor = System.Drawing.Color.Silver;
            titulo.Location = new System.Drawing.Point(16, y);
            titulo.Text = texto;

            valor.AutoSize = true;
            valor.ForeColor = System.Drawing.Color.WhiteSmoke;
            valor.Location = new System.Drawing.Point(125, y);
            valor.Text = "-";
        }

        private System.Windows.Forms.DataGridViewTextBoxColumn colIndex;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNome;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTipo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffset;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStored;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAllocated;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCompact;
        private System.Windows.Forms.DataGridViewTextBoxColumn colExcess;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
    }
}
