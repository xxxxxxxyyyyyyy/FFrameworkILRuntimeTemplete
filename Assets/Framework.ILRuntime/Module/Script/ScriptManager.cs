using Framework.Module.Resource;
using Framework.Module.Script.Adaptor;
using Framework.Module.Script.ValueTypeBinder;
using ILRuntime.CLR.Method;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.Mono.Cecil.Pdb;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

namespace Framework.Module.Script
{
    public class ILScriptManager : ModuleBase, IScriptManager
    {
        public override int Priority => 100;
        public AppDomain appdomain { get; private set; }
        IResourceLoader resourceLoader;
        MemoryStream frameworkDllStream;
        MemoryStream frameworkPdbStream;
        MemoryStream gameDllStream;
        MemoryStream gamePdbStream;

        Dictionary<string, IType> typeCache = new Dictionary<string, IType>();
        Dictionary<Vector3Int, IMethod> methodCache = new Dictionary<Vector3Int, IMethod>();

        public override void OnInit()
        {
            base.OnInit();
            appdomain = new AppDomain();
            resourceLoader = new ResourceLoader();

            TextAsset gameAsset = resourceLoader.Get<TextAsset>("Hotfix.dll");
            MemoryStream gameDllStream = new MemoryStream(gameAsset.bytes);
#if DEBUG || UNITY_EDITOR
            TextAsset gamePdbAsset = resourceLoader.Get<TextAsset>("Hotfix.pdb");
            MemoryStream gamePdbStream = new MemoryStream(gamePdbAsset.bytes);
            appdomain.LoadAssembly(gameDllStream, gamePdbStream, new PdbReaderProvider());
#else
            appdomain.LoadAssembly(gameDllStream, null, new PdbReaderProvider());
#endif
            InitializeILRuntime();
        }

        void InitializeILRuntime()
        {
#if DEBUG && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IPHONE)
            //由于Unity的Profiler接口只允许在主线程使用，为了避免出异常，需要告诉ILRuntime主线程的线程ID才能正确将函数运行耗时报告给Profiler
            appdomain.UnityMainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            ReginsterAdpater();
            ReginsterCLRBinder();
            ReginsterValueTypeBinder();
            RegisterDelegateConvertor();

            InvokeMethod("Hotfix.Main", "Initialize");
        }

        //注册跨域继承适配器
        void ReginsterAdpater()
        {
            appdomain.RegisterCrossBindingAdaptor(new IAsyncStateMachineClassInheritanceAdaptor());
        }

        //注册CLR绑定
        void ReginsterCLRBinder()
        {
            ILRuntime.Runtime.Generated.CLRBindings.Initialize(appdomain);
        }

        //注册值类型绑定
        void ReginsterValueTypeBinder()
        {
            appdomain.RegisterValueTypeBinder(typeof(Vector3), new Vector3Binder());
            appdomain.RegisterValueTypeBinder(typeof(Quaternion), new QuaternionBinder());
            appdomain.RegisterValueTypeBinder(typeof(Vector2), new Vector2Binder());
        }
        
        //注册委托转换器
        void RegisterDelegateConvertor()
        {
            appdomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<float>>((action) =>
            {
                return new UnityEngine.Events.UnityAction<float>((a) =>
                {
                    ((Action<float>)action)(a);
                });
            });

            appdomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction>((act) =>
            {
                return new UnityEngine.Events.UnityAction(() =>
                {
                    ((Action)act)();
                });
            });
        }

        IType GetOrCacheType(string typeName)
        {
            bool get = typeCache.TryGetValue(typeName, out IType type);
            if (get)
            {
                return type;
            }

            bool getType = appdomain.LoadedTypes.TryGetValue(typeName, out type);
            if (!getType)
            {
                Debug.LogError($"热更工程中没有找到这个类型 ：{typeName} 请检查!!!");
                return null;
            }

            typeCache.Add(typeName, type);
            return type;
        }


        public IMethod GetAndCacheMethod(string typeName, string methodName, int paramCount)
        {
            int left = typeName.GetHashCode();
            int right = methodName.GetHashCode();
            Vector3Int key = new Vector3Int(left, right, paramCount);
            bool get = methodCache.TryGetValue(key, out IMethod method);
            
            if (get)
            {
                return method;
            }

            IType type = GetOrCacheType(typeName);
            if(type == null)
            {
                return null;
            }

            method = type.GetMethod(methodName, paramCount);
            if(method == null)
            {
                //Debug.LogWarning($"Type:{className} 中不包含这个方法 functionName:{methodName} paramCount:{paramCount}");
                return null;
            }
            methodCache.Add(key, method);
            return method;
        }

        public object InvokeMethod(string className, string methodName, object owner = null, params object[] args)
        {
            int paramCount = args.Length;
            IMethod method = GetAndCacheMethod(className, methodName, paramCount);
            if(method == null)
            {
                return null;
            }

            return appdomain.Invoke(method, owner, args);
        }

        List<Vector3Int> removeKeys = new List<Vector3Int>();
        public void Release(string typeName)
        {
            if (typeCache.ContainsKey(typeName))
            {
                typeCache.Remove(typeName);
            }

            removeKeys.Clear();
            foreach(var kv in methodCache)
            {
                Vector3Int key = kv.Key;
                if(key.x == typeName.GetHashCode())
                {
                    removeKeys.Add(key);
                }
            }

            foreach(var key in removeKeys)
            {
                methodCache.Remove(key);
            }
            removeKeys.Clear();
        }

        public void Release(string typeName, string methodName)
        {
            removeKeys.Clear();
            foreach (var kv in methodCache)
            {
                Vector3Int key = kv.Key;
                if (key.x == typeName.GetHashCode() && key.y == methodName.GetHashCode())
                {
                    removeKeys.Add(key);
                }
            }

            foreach (var key in removeKeys)
            {
                methodCache.Remove(key);
            }
            removeKeys.Clear();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            InvokeMethod("Hotfix.Main", "OnUpdate");
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            InvokeMethod("Hotfix.Main", "OnFixedUpdate");
        }

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();
            InvokeMethod("Hotfix.Main", "OnLateUpdate");
        }

        public override void OnTearDown()
        {
            InvokeMethod("Hotfix.Main", "OnTearDown");
            base.OnTearDown();
            typeCache.Clear();
            methodCache.Clear();
            frameworkDllStream?.Close();
            frameworkPdbStream?.Close();
            gameDllStream?.Close();
            gamePdbStream?.Close();
            frameworkDllStream = null;
            frameworkPdbStream = null;
            gameDllStream = null;
            gamePdbStream = null;
        }
    }
}