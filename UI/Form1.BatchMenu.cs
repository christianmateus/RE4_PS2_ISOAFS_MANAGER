namespace FerramentaAFS
{
    public partial class Form1
    {
        private void ConfigurarBatchIndexado()
        {
            // O Designer ainda liga Extrair Todos ao handler antigo.
            // Removemos esse handler e passamos a usar o fluxo indexado,
            // que cria o manifest e prefixa o índice no nome dos arquivos.
            menuExtrairTodos.Click -= BtnExtrairTodos_Click;
            menuExtrairTodos.Click += MenuExtrairTodosIndexado_Click;

            // Importar Todos já pode vir ligado ao handler indexado pelo Designer,
            // mas fazemos a religação de forma defensiva para evitar duplicidade.
            menuImportarTodos.Click -= MenuImportarTodosIndexado_Click;
            menuImportarTodos.Click += MenuImportarTodosIndexado_Click;
        }
    }
}
