using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Sandbox;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;

namespace MDK.Debug
{
    public class ProgrammableBlockProxy
    {
        static Reflections _reflections;

        public static ProgrammableBlockProxy Wrap(MyProgrammableBlock programmableBlock)
        {
            if (_reflections == null)
                _reflections = new Reflections(typeof(MyProgrammableBlock));
            return new ProgrammableBlockProxy(programmableBlock);
        }

        //IMyGridProgram _instance;
        IMyIntergridCommunicationSystem _igcContextCache;

        ProgrammableBlockProxy(MyProgrammableBlock programmableBlock)
        {
            ProgrammableBlock = programmableBlock ?? throw new ArgumentNullException(nameof(programmableBlock));
        }

        public MyProgrammableBlock ProgrammableBlock { get; private set; }

        protected string StorageData
        {
            get => (string)_reflections.StorageDataField.GetValue(ProgrammableBlock);
            set => _reflections.StorageDataField.SetValue(ProgrammableBlock, value);
        }

        protected IMyGridProgram Program
        {
            get => (IMyGridProgram)_reflections.InstanceField.GetValue(ProgrammableBlock);
            set => _reflections.InstanceField.SetValue(ProgrammableBlock, value);
        }

        protected IMyGridProgramRuntimeInfo Runtime => (IMyGridProgramRuntimeInfo)_reflections.RuntimeField.GetValue(ProgrammableBlock);

        protected Assembly Assembly
        {
            get => (Assembly)_reflections.AssemblyField.GetValue(ProgrammableBlock);
            set => _reflections.AssemblyField.SetValue(ProgrammableBlock, value);
        }

        protected string TerminationReason
        {
            get => (string)_reflections.TerminationReasonField.GetValue(ProgrammableBlock);
            set => _reflections.TerminationReasonField.SetValue(ProgrammableBlock, value);
        }

        public string FileName { get; set; }

        protected void ResetRuntime()
        {
            var runtime = Runtime;
            _reflections.GetResetMethod(runtime).Invoke(runtime, null);
        }

        protected void UpdateStorage()
        {
            _reflections.UpdateStorageMethod.Invoke(ProgrammableBlock, null);
        }

        protected void Echo(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
            _reflections.EchoTextToDetailInfoMethod.Invoke(ProgrammableBlock, new object[] {text});
        }

        protected void SetDetailedInfo(string details)
        {
            _reflections.SetDetailedInfoMethod.Invoke(ProgrammableBlock, new object[] {details});
        }

        protected void EvictIgcContext()
        {
            var component = _reflections.IgcStaticProperty.GetValue(null);
            _reflections.IgcEvictContextMethod.Invoke(component, new object[] {ProgrammableBlock});
        }

        protected void CreateIgcContext()
        {
            var component = _reflections.IgcStaticProperty.GetValue(null);
            _igcContextCache = (IMyIntergridCommunicationSystem)_reflections.IgcGetOrMakeContextForMethod.Invoke(component, new object[] {ProgrammableBlock});
        }

        protected void OnProgramTermination(MyProgrammableBlock.ScriptTerminationReason reason)
        {
            _reflections.OnProgramTerminationMethod.Invoke(ProgrammableBlock, new object[] {reason});
        }

        public void UnloadProgram()
        {
            ProgrammableBlock.SendRecompile();
        }

        public void Dispose()
        {
            ProgrammableBlock = null;
            _igcContextCache = null;
        }

        public bool LoadProgram(TypeInfo type)
        {
            if (type == null)
                return true;

            UpdateStorage();
            OnProgramTermination(MyProgrammableBlock.ScriptTerminationReason.None);

            Program = FormatterServices.GetUninitializedObject(type) as IMyGridProgram;
            var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (Program == null || constructor == null)
            {
                Echo(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoValidConstructor));
                return false;
            }

            Assembly = type.Assembly;
            ResetRuntime();
            Program.Runtime = Runtime;
            Program.Storage = StorageData;
            Program.Me = ProgrammableBlock;
            Program.Echo = Echo;

            EvictIgcContext();
            CreateIgcContext();
            Program.IGC_ContextGetter = () => _igcContextCache;

            ProgrammableBlock.RunSandboxedProgramAction(p =>
            {
                constructor.Invoke(p, null);

                if (!Program.HasMainMethod)
                {
                    Echo(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain));
                    OnProgramTermination(MyProgrammableBlock.ScriptTerminationReason.NoEntryPoint);
                }
            }, out var response);
            SetDetailedInfo(response);

            return true;
        }

        class Reflections
        {
            const string StorageDataFieldName = "m_storageData";
            const string InstanceFieldName = "m_instance";
            const string AssemblyFieldName = "m_assembly";
            const string TerminationReasonFieldName = "m_terminationReason";
            const string RuntimeFieldName = "m_runtime";
            const string UpdateStorageMethodName = "UpdateStorage";
            const string EchoTextToDetailInfoMethodName = "EchoTextToDetailInfo";
            const string SetDetailedInfoMethodName = "SetDetailedInfo";
            const string ResetMethodName = "Reset";
            const string OnProgramTerminationMethodName = "OnProgramTermination";
            const string IgcSystemSessionComponentTypeName = "Sandbox.Game.SessionComponents.MyIGCSystemSessionComponent";
            const string StaticPropertName = "Static";
            const string EvictContextForMethodName = "EvictContextFor";
            const string GetOrMakeContextForMethodName = "GetOrMakeContextFor";

            static FieldInfo GetFieldInfo(Type type, string fieldName, Type fieldType)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fieldType != null && field?.FieldType != fieldType)
                    field = null;
                if (field == null)
                    throw new BindingException(string.Format(Resources.Reflections_GetFieldInfo_MissingField, type.FullName, fieldName));
                return field;
            }

            static PropertyInfo GetPropertyInfo(Type type, string propertyName, Type propertyType)
            {
                var property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (propertyType != null && property?.PropertyType != propertyType)
                    property = null;
                if (property == null)
                    throw new BindingException(string.Format(Resources.Reflections_GetPropertyInfo_MissingProperty, type.FullName, propertyName));
                return property;
            }

            static MethodInfo GetMethodInfo(Type type, string methodName, Type returnType, Type[] argumentTypes)
            {
                var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, argumentTypes, null);
                if (returnType != null && method?.ReturnType != returnType)
                    method = null;
                if (method != null) return method;

                var arguments = string.Join(", ", argumentTypes.Select(a => a.Name));
                throw new BindingException(string.Format(Resources.Reflections_GetMethodInfo_MissingMethod, type.FullName, methodName, arguments));
            }

            MethodInfo _resetMethod;

            public Reflections(Type type)
            {
                StorageDataField = GetFieldInfo(type, StorageDataFieldName, typeof(string));
                InstanceField = GetFieldInfo(type, InstanceFieldName, typeof(IMyGridProgram));
                AssemblyField = GetFieldInfo(type, AssemblyFieldName, typeof(Assembly));
                TerminationReasonField = GetFieldInfo(type, TerminationReasonFieldName, typeof(MyProgrammableBlock.ScriptTerminationReason));
                RuntimeField = GetFieldInfo(type, RuntimeFieldName, null);
                UpdateStorageMethod = GetMethodInfo(type, UpdateStorageMethodName, typeof(void), Type.EmptyTypes);
                EchoTextToDetailInfoMethod = GetMethodInfo(type, EchoTextToDetailInfoMethodName, typeof(void), new[] {typeof(string)});
                SetDetailedInfoMethod = GetMethodInfo(type, SetDetailedInfoMethodName, typeof(void), new[] {typeof(string)});
                OnProgramTerminationMethod = GetMethodInfo(type, OnProgramTerminationMethodName, typeof(void), new[] {typeof(MyProgrammableBlock.ScriptTerminationReason)});

                var gameAssembly = typeof(MySandboxGame).Assembly;
                var componentType = gameAssembly.GetType(IgcSystemSessionComponentTypeName);
                if (componentType == null)
                    throw new BindingException(string.Format(Resources.Reflections_Reflections_MissingType, IgcSystemSessionComponentTypeName));

                IgcStaticProperty = GetPropertyInfo(componentType, StaticPropertName, componentType);
                IgcEvictContextMethod = GetMethodInfo(componentType, EvictContextForMethodName, typeof(void), new[] {typeof(MyProgrammableBlock)});
                IgcGetOrMakeContextForMethod = GetMethodInfo(componentType, GetOrMakeContextForMethodName, null, new[] {typeof(MyProgrammableBlock)});
            }

            public FieldInfo StorageDataField { get; }
            public FieldInfo InstanceField { get; }
            public FieldInfo AssemblyField { get; }
            public FieldInfo TerminationReasonField { get; }
            public MethodInfo UpdateStorageMethod { get; }
            public MethodInfo EchoTextToDetailInfoMethod { get; }
            public MethodInfo SetDetailedInfoMethod { get; }
            public FieldInfo RuntimeField { get; }
            public PropertyInfo IgcStaticProperty { get; }
            public MethodInfo IgcEvictContextMethod { get; }
            public MethodInfo IgcGetOrMakeContextForMethod { get; }
            public MethodInfo OnProgramTerminationMethod { get; }

            public MethodInfo GetResetMethod(object runtime)
            {
                if (_resetMethod != null)
                    return _resetMethod;
                var runtimeType = runtime.GetType();
                _resetMethod = GetMethodInfo(runtimeType, ResetMethodName, typeof(void), Type.EmptyTypes);
                return _resetMethod;
            }
        }
    }
}