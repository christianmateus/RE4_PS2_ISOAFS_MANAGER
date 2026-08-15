using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FerramentaAFS
{
    public sealed class RebuildProgressInfo
    {
        public int Percent { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public sealed class RebuildProgressForm : Form
    {
        private readonly Label _stage;
        private readonly Label _detail;
        private readonly ProgressBar _progress;
        private readonly Label _percent;
        private Exception? _error;

        private RebuildProgressForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(610, 160);

            _stage = new Label { Left = 20, Top = 18, Width = 565, Height = 24, Font = new Font(Font, FontStyle.Bold), Text = "..." };
            _detail = new Label { Left = 20, Top = 48, Width = 565, Height = 38, AutoEllipsis = true, Text = "..." };
            _progress = new ProgressBar { Left = 20, Top = 96, Width = 515, Height = 24, Minimum = 0, Maximum = 100 };
            _percent = new Label { Left = 545, Top = 99, Width = 45, Height = 20, TextAlign = ContentAlignment.MiddleRight, Text = "0%" };

            Controls.AddRange(new Control[] { _stage, _detail, _progress, _percent });
        }

        public static async Task RunAsync(IWin32Window owner, string title, Func<IProgress<RebuildProgressInfo>, Task> operation)
        {
            using RebuildProgressForm dialog = new RebuildProgressForm(title);
            TaskCompletionSource<bool> completed = new TaskCompletionSource<bool>();

            dialog.Shown += async (_, _) =>
            {
                try
                {
                    Progress<RebuildProgressInfo> progress = new Progress<RebuildProgressInfo>(info =>
                    {
                        int value = Math.Max(0, Math.Min(100, info.Percent));
                        dialog._stage.Text = info.Stage;
                        dialog._detail.Text = info.Detail;
                        dialog._progress.Value = value;
                        dialog._percent.Text = value + "%";
                    });

                    await operation(progress);
                }
                catch (Exception ex)
                {
                    dialog._error = ex;
                }
                finally
                {
                    completed.TrySetResult(true);
                    dialog.Close();
                }
            };

            dialog.ShowDialog(owner);
            await completed.Task;

            if (dialog._error != null)
                throw dialog._error;
        }
    }
}
