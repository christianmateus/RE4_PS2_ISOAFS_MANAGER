using FerramentaAFS.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public partial class Form1
    {
        private const string MANIFEST_FILE_NAME = "afs_manifest.txt";

        private void MenuExtrairTodosIndexado_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = Tr("Escolha a pasta onde o AFS será extraído", "Choose the folder where the AFS will be extracted"),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string pastaBase = dialog.SelectedPath;

            using BatchProgressForm progresso = new BatchProgressForm(Localization.Loc.English ? "Extracting files" : "Extraindo arquivos", _entries.Count);
            progresso.Show(this);

            try
            {
                List<ManifestEntry> manifest = new List<ManifestEntry>();
                int extraidos = 0;
                int vazios = 0;
                long totalBytes = 0;

                using Stream origem = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);

                int progressoAtual = 0;
                foreach (AfsEntry entry in _entries)
                {
                    if (progresso.CancelRequested) break;
                    progressoAtual++;
                    progresso.Report(progressoAtual, $"{(Localization.Loc.English ? "Extracting" : "Extraindo")} {progressoAtual:N0}/{_entries.Count:N0}: {entry.FileName}");
                    if (entry.IsEmpty || ObterTamanhoReal(entry) == 0)
                    {
                        vazios++;

                        manifest.Add(new ManifestEntry
                        {
                            Index = entry.Index,
                            OriginalName = entry.FileName ?? string.Empty,
                            ExtractedPath = string.Empty,
                            Size = 0,
                            IsEmpty = true
                        });

                        continue;
                    }

                    uint tamanhoReal = ObterTamanhoReal(entry);
                    string caminhoRelativo = CriarNomeIndexadoParaExtracao(entry);
                    string destino = CriarCaminhoSeguro(pastaBase, caminhoRelativo);

                    string? pasta = Path.GetDirectoryName(destino);

                    if (!string.IsNullOrWhiteSpace(pasta))
                        Directory.CreateDirectory(pasta);

                    if ((long)entry.Offset + tamanhoReal > origem.Length)
                        throw new InvalidDataException($"A entrada {entry.Index} ({entry.FileName}) ultrapassa os limites físicos do AFS.");

                    origem.Position = entry.Offset;

                    using FileStream saida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);
                    CopiarBytes(origem, saida, tamanhoReal);

                    manifest.Add(new ManifestEntry
                    {
                        Index = entry.Index,
                        OriginalName = entry.FileName ?? string.Empty,
                        ExtractedPath = caminhoRelativo.Replace(Path.DirectorySeparatorChar, '/'),
                        Size = tamanhoReal,
                        IsEmpty = false
                    });

                    extraidos++;
                    totalBytes += tamanhoReal;

                    toolStripStatusLabel1.Text = Tr($"Extraindo {extraidos:N0} arquivo(s): {entry.FileName}", $"Extracting {extraidos:N0} file(s): {entry.FileName}");
                    Application.DoEvents();
                }

                SalvarManifest(pastaBase, manifest);

                toolStripStatusLabel1.Text = $"Extração concluída: {extraidos:N0} arquivos";

                MostrarSucesso(Tr($"Extração concluída.\n\nArquivos extraídos: {extraidos:N0}\nEntradas vazias: {vazios:N0}\nDados extraídos: {FormatarBytes(totalBytes)}\n\nManifest criado:\n{MANIFEST_FILE_NAME}", $"Extraction complete.\n\nFiles extracted: {extraidos:N0}\nEmpty entries: {vazios:N0}\nData extracted: {FormatarBytes(totalBytes)}\n\nManifest created:\n{MANIFEST_FILE_NAME}"), Tr("Extração concluída", "Extraction complete"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a extração em lote:\n\n{ex.Message}", $"Batch extraction error:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuImportarTodosIndexado_Click(object? sender, EventArgs e)
        {
            if (_afsPath == null)
            {
                MessageBox.Show(Tr("Abra um arquivo AFS primeiro.", "Open an AFS file first."), Tr("Ferramenta AFS", "AFS Tool"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = Tr($"Escolha a pasta que contém {MANIFEST_FILE_NAME}", $"Choose the folder containing {MANIFEST_FILE_NAME}"),
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string pastaBase = dialog.SelectedPath;
            string caminhoManifest = Path.Combine(pastaBase, MANIFEST_FILE_NAME);

            if (!File.Exists(caminhoManifest))
            {
                MessageBox.Show(
                    Tr($"O arquivo {MANIFEST_FILE_NAME} não foi encontrado nesta pasta.\n\nUse primeiro 'Extrair Todos' desta versão da ferramenta.", $"The file {MANIFEST_FILE_NAME} was not found in this folder.\n\nUse 'Extract All' from this version of the tool first."),
                    Tr("Manifest não encontrado", "Manifest not found"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                List<ManifestEntry> manifest = LerManifest(caminhoManifest);
                List<ImportacaoPlanejada> plano = CriarPlanoImportacaoPorManifest(pastaBase, manifest);

                if (plano.Count == 0)
                {
                    MessageBox.Show(Tr("Nenhum arquivo válido para importar foi encontrado.", "No valid files were found to import."), Tr("Nada para importar", "Nothing to import"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                List<ImportacaoPlanejada> grandes = plano.Where(x => x.NovoTamanho > x.Entry.AllocatedSize).ToList();
                List<ImportacaoPlanejada> validos = plano.Where(x => x.NovoTamanho <= x.Entry.AllocatedSize).ToList();
                List<ImportacaoPlanejada> alterados = validos.Where(x => !ArquivoEhIdenticoAoAfs(x.Entry, x.Caminho)).ToList();
                int identicos = validos.Count - alterados.Count;

                string avisoGrandes = grandes.Count > 0 ? Tr($"\nGrandes demais: {grandes.Count:N0}", $"\nToo large: {grandes.Count:N0}") : "";

                DialogResult confirmacao = MessageBox.Show(
                    Tr($"Arquivos encontrados pelo manifest: {plano.Count:N0}\nAlterados e que serão importados: {alterados.Count:N0}\nIdênticos e que serão ignorados: {identicos:N0}{avisoGrandes}\n\nA importação continuará sendo In-Place e não moverá outras entradas.\n\nContinuar?", $"Files found by manifest: {plano.Count:N0}\nChanged files to import: {alterados.Count:N0}\nIdentical files to skip: {identicos:N0}{avisoGrandes}\n\nImport will remain In-Place and will not move other entries.\n\nContinue?"),
                    Tr("Importar Todos", "Import All"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmacao != DialogResult.Yes)
                    return;

                if (alterados.Count == 0)
                {
                    if (grandes.Count > 0)
                        MostrarArquivosGrandesDemais(grandes);

                    MessageBox.Show(Tr("Nenhum arquivo diferente do conteúdo atual precisa ser importado.", "No file differs from the current content; nothing needs to be imported."), Tr("Nada para alterar", "Nothing to change"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int importados = 0;
                long bytesImportados = 0;

                using BatchProgressForm progressoImport = new BatchProgressForm(Localization.Loc.English ? "Importing files" : "Importando arquivos", alterados.Count);
                progressoImport.Show(this);
                using Stream afs = AbrirAfsStream(FileAccess.ReadWrite, FileShare.None);

                foreach (ImportacaoPlanejada item in alterados)
                {
                    if (progressoImport.CancelRequested) break;
                    ImportarEntradaInPlace(afs, item.Entry, item.Caminho);

                    importados++;
                    bytesImportados += item.NovoTamanho;

                    toolStripStatusLabel1.Text = Tr($"Importando {importados:N0}/{alterados.Count:N0}: índice {item.Entry.Index} - {item.Entry.FileName}", $"Importing {importados:N0}/{alterados.Count:N0}: index {item.Entry.Index} - {item.Entry.FileName}");
                    progressoImport.Report(importados, toolStripStatusLabel1.Text);
                }

                afs.Flush();

                ReabrirAfsAtualPreservandoBusca(null);

                string mensagem =
                    $"Importação concluída.\n\n" +
                    $"Arquivos importados: {importados:N0}\n" +
                    $"Arquivos idênticos ignorados: {identicos:N0}\n" +
                    $"Dados escritos: {FormatarBytes(bytesImportados)}";

                if (grandes.Count > 0)
                    mensagem += $"\nArquivos grandes demais ignorados: {grandes.Count:N0}";

                MostrarSucesso(Loc.English ? mensagem.Replace("Importação concluída.", "Import complete.").Replace("Arquivos importados:", "Files imported:").Replace("Arquivos idênticos ignorados:", "Identical files skipped:").Replace("Dados escritos:", "Data written:").Replace("Arquivos grandes demais ignorados:", "Oversized files skipped:") : mensagem, Tr("Importação concluída", "Import complete"));

                if (grandes.Count > 0)
                    MostrarArquivosGrandesDemais(grandes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Tr($"Erro durante a importação em lote:\n\n{ex.Message}", $"Batch import error:\n\n{ex.Message}"), Tr("Erro", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string CriarNomeIndexadoParaExtracao(AfsEntry entry)
        {
            string nomeOriginal = entry.FileName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nomeOriginal) || nomeOriginal == "_")
                return $"{entry.Index:D6}_File_{entry.Index:D4}.bin";

            string normalizado = nomeOriginal.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            while (normalizado.StartsWith(Path.DirectorySeparatorChar))
                normalizado = normalizado.Substring(1);

            string? pasta = Path.GetDirectoryName(normalizado);
            string nome = Path.GetFileName(normalizado);

            if (string.IsNullOrWhiteSpace(nome))
                nome = $"File_{entry.Index:D4}.bin";

            string nomeIndexado = $"{entry.Index:D6}_{nome}";

            return string.IsNullOrWhiteSpace(pasta)
                ? nomeIndexado
                : Path.Combine(pasta, nomeIndexado);
        }

        private void SalvarManifest(string pastaBase, List<ManifestEntry> entries)
        {
            string caminho = Path.Combine(pastaBase, MANIFEST_FILE_NAME);

            using StreamWriter sw = new StreamWriter(caminho, false, new UTF8Encoding(false));

            sw.WriteLine("# Ferramenta AFS Manifest v1");
            sw.WriteLine("# Index|Size|Empty|OriginalName|ExtractedPath");

            foreach (ManifestEntry item in entries.OrderBy(x => x.Index))
            {
                sw.Write(item.Index.ToString(CultureInfo.InvariantCulture));
                sw.Write('|');
                sw.Write(item.Size.ToString(CultureInfo.InvariantCulture));
                sw.Write('|');
                sw.Write(item.IsEmpty ? "1" : "0");
                sw.Write('|');
                sw.Write(EscapeManifest(item.OriginalName));
                sw.Write('|');
                sw.WriteLine(EscapeManifest(item.ExtractedPath));
            }
        }

        private List<ManifestEntry> LerManifest(string caminhoManifest)
        {
            List<ManifestEntry> result = new List<ManifestEntry>();

            foreach (string rawLine in File.ReadLines(caminhoManifest, Encoding.UTF8))
            {
                string line = rawLine.TrimEnd();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = SepararLinhaManifest(line);

                if (parts.Length != 5)
                    throw new InvalidDataException($"Linha inválida no manifest:\n{line}");

                if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    throw new InvalidDataException($"Index inválido no manifest: {parts[0]}");

                if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long size))
                    throw new InvalidDataException($"Size inválido no manifest: {parts[1]}");

                result.Add(new ManifestEntry
                {
                    Index = index,
                    Size = size,
                    IsEmpty = parts[2] == "1",
                    OriginalName = UnescapeManifest(parts[3]),
                    ExtractedPath = UnescapeManifest(parts[4])
                });
            }

            return result;
        }

        private List<ImportacaoPlanejada> CriarPlanoImportacaoPorManifest(string pastaBase, List<ManifestEntry> manifest)
        {
            List<ImportacaoPlanejada> plano = new List<ImportacaoPlanejada>();

            foreach (ManifestEntry item in manifest)
            {
                if (item.IsEmpty || string.IsNullOrWhiteSpace(item.ExtractedPath))
                    continue;

                if (item.Index < 0 || item.Index >= _entries.Count)
                    throw new InvalidDataException($"O manifest possui um índice fora da faixa do AFS: {item.Index}");

                AfsEntry entry = _entries[item.Index];

                if (entry.IsEmpty)
                    continue;

                string relativo = item.ExtractedPath.Replace('/', Path.DirectorySeparatorChar);
                string caminho = CriarCaminhoSeguro(pastaBase, relativo);

                if (!File.Exists(caminho))
                    continue;

                long tamanho = new FileInfo(caminho).Length;

                plano.Add(new ImportacaoPlanejada
                {
                    Entry = entry,
                    Caminho = caminho,
                    NovoTamanho = tamanho
                });
            }

            return plano;
        }

        private bool ArquivoEhIdenticoAoAfs(AfsEntry entry, string caminhoArquivo)
        {
            if (_afsPath == null)
                return false;

            FileInfo info = new FileInfo(caminhoArquivo);
            uint tamanhoReal = ObterTamanhoReal(entry);

            if (info.Length != tamanhoReal)
                return false;

            const int BUFFER_SIZE = 1024 * 1024;
            byte[] bufferAfs = new byte[BUFFER_SIZE];
            byte[] bufferArquivo = new byte[BUFFER_SIZE];

            using Stream afs = AbrirAfsStream(FileAccess.Read, FileShare.ReadWrite);
            using FileStream arquivo = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.Read);

            afs.Position = entry.Offset;

            long restante = tamanhoReal;

            while (restante > 0)
            {
                int solicitar = (int)Math.Min(BUFFER_SIZE, restante);
                int lidosAfs = afs.Read(bufferAfs, 0, solicitar);
                int lidosArquivo = arquivo.Read(bufferArquivo, 0, solicitar);

                if (lidosAfs != solicitar || lidosArquivo != solicitar)
                    return false;

                for (int i = 0; i < solicitar; i++)
                {
                    if (bufferAfs[i] != bufferArquivo[i])
                        return false;
                }

                restante -= solicitar;
            }

            return true;
        }

        private static string EscapeManifest(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("|", "\\p")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string UnescapeManifest(string value)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    sb.Append(value[i]);
                    continue;
                }

                char next = value[++i];

                switch (next)
                {
                    case '\\':
                        sb.Append('\\');
                        break;
                    case 'p':
                        sb.Append('|');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    default:
                        sb.Append('\\');
                        sb.Append(next);
                        break;
                }
            }

            return sb.ToString();
        }

        private static string[] SepararLinhaManifest(string line)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            bool escape = false;

            foreach (char c in line)
            {
                if (escape)
                {
                    current.Append('\\');
                    current.Append(c);
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '|' && parts.Count < 4)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            if (escape)
                current.Append('\\');

            parts.Add(current.ToString());

            return parts.ToArray();
        }

        private class ManifestEntry
        {
            public int Index { get; set; }
            public string OriginalName { get; set; } = string.Empty;
            public string ExtractedPath { get; set; } = string.Empty;
            public long Size { get; set; }
            public bool IsEmpty { get; set; }
        }
    }
}
