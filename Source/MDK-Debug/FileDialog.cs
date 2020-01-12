using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MDK.Debug
{
    public static class FileDialog
    {
        public static async Task<string> RequestFileName(string title, string filter, string fileName)
        {
            var tcs = new TaskCompletionSource<string>();
            var thread = new Thread(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = title,
                    Filter = filter,
                    FileName = fileName,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    ShowReadOnly = false,
                    AutoUpgradeEnabled = true
                };

                tcs.SetResult(dialog.ShowDialog(Plugin.Current) == DialogResult.OK ? dialog.FileName : null);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            var result = await tcs.Task;
            await Plugin.SwitchToMainThread();
            return result;
        }
    }
}