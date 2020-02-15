using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Entity;
using VRage.Utils;

namespace MDK.Debug
{
    public class ProgrammableBlockExtensions
    {
        readonly Dictionary<MyProgrammableBlock, ProgrammableBlockProxy> _proxyCache = new Dictionary<MyProgrammableBlock, ProgrammableBlockProxy>();

        public bool IsInstalled { get; private set; }

        public bool IsFaulted { get; private set; }

        public bool HasLoadedProgram { get; private set; }

        public void Install()
        {
            if (IsInstalled || IsFaulted)
                return;

            // Custom terminal control needs to be initialized after game controls otherwise game fails it initialize it's own
            if (!MyTerminalControlFactory.AreControlsCreated<MyProgrammableBlock>())
                return;

            if (MyTerminalControlFactory.GetControls(typeof(MyProgrammableBlock)).Any(x => x.Id == "MDK-Debug-BindScriptDLL"))
                return;

            IsInstalled = true;

            var editIndex = FindEditButtonIndex();
            if (editIndex < 0)
            {
                MyLog.Default.WriteLine($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_Install_NoEditButton}");
                MyAPIGateway.Utilities.ShowMessage(Plugin.Ident, Resources.ProgrammableBlockExtensions_Install_NoEditButton);
                IsInstalled = true;
                IsFaulted = true;
                return;
            }

            var button = new MyTerminalControlButton<MyProgrammableBlock>("MDK-Debug-UnbindScriptDll", MyStringId.GetOrCompute($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_Install_UnbindDLLButtonText}"), MyStringId.NullOrEmpty, OnUnbindScriptDll)
            {
                Visible = IsUnbindButtonVisible,
                Enabled = IsUnbindButtonEnabled
            };
            MyTerminalControlFactory.AddControl(editIndex, button);

            button = new MyTerminalControlButton<MyProgrammableBlock>("MDK-Debug-BindScriptDLL", MyStringId.GetOrCompute($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_Install_BindDLLButtonText}"), MyStringId.NullOrEmpty, OnBindScriptDll)
            {
                Visible = IsBindButtonVisible,
                Enabled = IsBindButtonEnabled,
            };
            MyTerminalControlFactory.AddControl(editIndex, button);

            button = new MyTerminalControlButton<MyProgrammableBlock>("MDK-Debug-AttachDebugger", MyStringId.GetOrCompute($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_Install_AttachDebugger}"), MyStringId.NullOrEmpty, OnAttachDebugger)
            {
                Visible = IsAttachDebuggerButtonVisible,
                Enabled = IsAttachDebuggerButtonEnabled
            };
            MyTerminalControlFactory.AddControl(editIndex, button);

            MySession.OnUnloading += Unload;

            MyLog.Default.WriteLine($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_Install_Ready}");
            MyAPIGateway.Utilities.ShowMessage(Plugin.Ident, Resources.ProgrammableBlockExtensions_Install_Ready);
        }

        int FindEditButtonIndex()
        {
            var controls = MyTerminalControlFactory.GetControls(typeof(MyProgrammableBlock));
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls.ItemAt(i).Id == "Edit")
                    return i;
            }

            return -1;
        }

        bool IsWorkable() => MyFakes.ENABLE_PROGRAMMABLE_BLOCK && MySession.Static.EnableIngameScripts && !IsFaulted;

        bool IsAttachDebuggerButtonEnabled(MyProgrammableBlock programmableBlock)
        {
            return IsWorkable() && !Debugger.IsAttached;
        }

        bool IsAttachDebuggerButtonVisible(MyProgrammableBlock programmableBlock)
        {
            return IsWorkable();
        }

        void OnAttachDebugger(MyProgrammableBlock programmableBlock)
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
        }

        bool IsUnbindButtonVisible(MyProgrammableBlock programmableBlock)
        {
            return IsWorkable();
        }

        bool IsUnbindButtonEnabled(MyProgrammableBlock programmableBlock)
        {
            return IsWorkable() && _proxyCache.TryGetValue(programmableBlock, out var proxy) && proxy.HasLoadedProgram;
        }

        bool IsBindButtonVisible(MyProgrammableBlock programmableBlock)
        {
            return IsWorkable();
        }

        bool IsBindButtonEnabled(MyProgrammableBlock programmableBlock)
        {
            return IsWorkable();
        }

        async void OnBindScriptDll(MyProgrammableBlock programmableBlock)
        {
            if (IsFaulted)
                return;

            if (!_proxyCache.TryGetValue(programmableBlock, out var proxy))
            {
                try
                {
                    proxy = ProgrammableBlockProxy.Wrap(programmableBlock);
                    _proxyCache[programmableBlock] = proxy;
                    proxy.ProgrammableBlock.OnClose += OnProgrammableBlockClosed;
                }
                catch (BindingException e)
                {
                    MyLog.Default.Error($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindingFailed}: {e}");
                    MyLog.Default.Flush();
                    MyAPIGateway.Utilities.ShowMissionScreen(
                        $"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindingFailed}",
                        currentObjective: Resources.ProgrammableBlockExtensions_OnBindScriptDll_EnableDebugging,
                        screenDescription: Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindingError);
                    IsFaulted = true;
                    return;
                }
            }

            proxy.UnloadProgram();

            var fileName = await FileDialog.RequestFileName(
                Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindScriptDLL,
                Resources.ProgrammableBlockExtensions_OnBindScriptDll_Filters,
                proxy.FileName).ConfigureAwait(false);

            MyLog.Default.WriteLine($"After RequestFileName {Thread.CurrentThread.ManagedThreadId}");
            MyLog.Default.Flush();

            if (fileName != null)
            {
                MyLog.Default.WriteLine($"Found {fileName} {Thread.CurrentThread.ManagedThreadId}");
                MyLog.Default.Flush();
                LoadScriptAssembly(fileName, proxy);
                MyLog.Default.WriteLine($"Loaded {fileName} {Thread.CurrentThread.ManagedThreadId}");
                MyLog.Default.Flush();
            }

            proxy.ProgrammableBlock?.RaisePropertiesChanged();
        }

        void LoadScriptAssembly(string fileName, ProgrammableBlockProxy proxy)
        {
            var assembly = LoadAssembly(fileName);
            if (assembly == null)
            {
                MyAPIGateway.Utilities.ShowMissionScreen(
                    $"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindingFailed}",
                    currentObjective: Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindScriptDLL,
                    screenDescription: Resources.ProgrammableBlockExtensions_LoadScriptAssembly_InvalidAssembly);
                return;
            }

            var programTypes = assembly.DefinedTypes.Where(type => !type.IsAbstract && typeof(MyGridProgram).IsAssignableFrom(type)).ToList();
            if (programTypes.Count == 0)
            {
                MyAPIGateway.Utilities.ShowMissionScreen(
                    $"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindingFailed}",
                    currentObjective: Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindScriptDLL,
                    screenDescription: Resources.ProgrammableBlockExtensions_LoadScriptAssembly_InvalidAssembly);
                return;
            }

            if (programTypes.Count > 1)
            {
                MyAPIGateway.Utilities.ShowMissionScreen(
                    $"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindingFailed}",
                    currentObjective: Resources.ProgrammableBlockExtensions_OnBindScriptDll_BindScriptDLL,
                    screenDescription: Resources.ProgrammableBlockExtensions_LoadScriptAssembly_TooManyGridPrograms);
                return;
            }

            if (proxy.LoadProgram(programTypes[0]))
            {
                HasLoadedProgram = true;

            }
        }

        Assembly LoadAssembly(string fileName)
        {
            try
            {
                var rawAssembly = File.ReadAllBytes(fileName);
                return Assembly.Load(rawAssembly);
            }
            catch
            {
                return null;
            }
        }

        void OnProgrammableBlockClosed(MyEntity entity)
        {
            var programmableBlock = (MyProgrammableBlock)entity;
            if (!_proxyCache.TryGetValue(programmableBlock, out var proxy))
                return;
            _proxyCache.Remove(programmableBlock);
            proxy.Dispose();
        }

        void OnUnbindScriptDll(MyProgrammableBlock programmableBlock)
        {
            if (IsFaulted)
                return;

            if (_proxyCache.TryGetValue(programmableBlock, out var proxy))
                proxy.UnloadProgram();
        }

        void Unload()
        {
            MyLog.Default.WriteLine($"{Plugin.Ident}: {Resources.ProgrammableBlockExtensions_Unload_Unloading}");
            foreach (var proxy in _proxyCache.Values)
                proxy.Dispose();
            _proxyCache.Clear();
            IsInstalled = false;
        }
    }
}