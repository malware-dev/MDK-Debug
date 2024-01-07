using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDK.Debug
{
    internal class Resources
    {
        public const string ProgrammableBlockExtensions_Install_UnbindDLLButtonText = "Unbind DLL";
        public const string ProgrammableBlockExtensions_Install_BindDLLButtonText = "Bind DLL";
        public const string ProgrammableBlockExtensions_OnBindScriptDll_BindingFailed = "Binding Failed";
        public const string ProgrammableBlockExtensions_OnBindScriptDll_EnableDebugging = "Enable Programmable Block Debugging";
        public const string ProgrammableBlockExtensions_OnBindScriptDll_BindScriptDLL = "Bind Script DLL";
        public const string ProgrammableBlockExtensions_OnBindScriptDll_Filters = "Script Assembly (*.dll,*.exe)|*.dll;*.exe";
        public const string ProgrammableBlockExtensions_OnBindScriptDll_BindingError = "The plugin was unable to bind a programmable block. This is most likely caused by changes to the game.";
        public const string ProgrammableBlockExtensions_LoadScriptAssembly_InvalidAssembly = "The loaded assembly could not be recognized as a programmable block script container.";
        public const string ProgrammableBlockExtensions_LoadScriptAssembly_TooManyGridPrograms = "The loaded assembly contains too many grid programs. Only one was expected.";
        public const string Reflections_Reflections_MissingType = "The type {0} does not exist";
        public const string Reflections_GetMethodInfo_MissingMethod = "The type {0} does not have the required method {1}({2})";
        public const string Reflections_GetPropertyInfo_MissingProperty = "The type {0} does not have the required property {1}";
        public const string Reflections_GetFieldInfo_MissingField = "The type {0} does not have the required field {1}";
        public const string ProgrammableBlockExtensions_Install_NoEditButton = "Installation denied because the edit button could not be found";
        public const string ProgrammableBlockExtensions_Install_Ready = "The debug plugin is installed and ready.";
        public const string ProgrammableBlockExtensions_Unload_Unloading = "Clearing out extensions";
        public const string ProgrammableBlockExtensions_Install_AttachDebugger = "Attach Debugger";
    }
}
