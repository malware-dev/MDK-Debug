using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sandbox.ModAPI;
using SpaceEngineers.Game;
using VRage.Plugins;

namespace MDK.Debug
{
    public class Plugin : IPlugin, IWin32Window
    {
        public const string Ident = "MDK-Debug";

        static int _mainThreadId;

        public static bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        public static Task SwitchToMainThread()
        {
            if (IsMainThread() || MyAPIGateway.Utilities == null)
                return Task.CompletedTask;
            var tcs = new TaskCompletionSource<object>();
            MyAPIGateway.Utilities.InvokeOnGameThread(() => tcs.SetResult(null), Plugin.Ident);
            return tcs.Task;
        }

        public static Plugin Current { get; private set; }

        readonly ProgrammableBlockExtensions _programmableBlockExtensions;

        public Plugin()
        {
            Current = this;
            _programmableBlockExtensions = new ProgrammableBlockExtensions();
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public SpaceEngineersGame Game { get; private set; }

        public void Init(object gameInstance)
        {
            Game = (SpaceEngineersGame)gameInstance;
        }

        public void Update()
        {
            if (!_programmableBlockExtensions.IsInstalled)
                _programmableBlockExtensions.Install();
        }

        public void Dispose()
        {
            Current = null;
        }

        public IntPtr Handle => ((Form)Game.GameRenderComponent?.RenderThread?.RenderWindow)?.Handle ?? IntPtr.Zero;
    }
}