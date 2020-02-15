using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VRage.Utils;

namespace MDK.Debug
{
    public static class FileDialog
    {
        public static async Task<string> RequestFileName(string title, string filter, string fileName)
        {
            MyLog.Default.WriteLine($"Synchronization Context, before thread: {SynchronizationContext.Current != null}");
            MyLog.Default.Flush();
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

                MyLog.Default.WriteLine($"Synchronization Context, before dialog: {SynchronizationContext.Current != null}");
                MyLog.Default.Flush();
                var response = dialog.ShowDialog(Plugin.Current) == DialogResult.OK ? dialog.FileName : null;
                MyLog.Default.WriteLine($"Synchronization Context, after dialog: {SynchronizationContext.Current != null}");
                MyLog.Default.Flush();
                tcs.SetResult(response);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            MyLog.Default.WriteLine($"Before await {Thread.CurrentThread.ManagedThreadId}");
            MyLog.Default.Flush();
            var result = await tcs.Task.ConfigureAwait(false);
            MyLog.Default.WriteLine($"After await {Thread.CurrentThread.ManagedThreadId}");
            MyLog.Default.Flush();
            await Plugin.SwitchToMainThread().ConfigureAwait(false);
            MyLog.Default.WriteLine($"After thread switch {Thread.CurrentThread.ManagedThreadId}");
            MyLog.Default.Flush();
            return result;
        }
    }
}