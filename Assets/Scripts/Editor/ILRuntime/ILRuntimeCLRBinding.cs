#if UNITY_EDITOR
using Game.Local.IL.Reginster;
using UnityEditor;

namespace Game.Editor
{
    [System.Reflection.Obfuscation(Exclude = true)]
    public class ILRuntimeCLRBinding
    {
        [MenuItem("ILRuntime/通过自动分析热更DLL生成CLR绑定")]
        static void GenerateCLRBindingByAnalysis()
        {
            //用新的分析热更dll调用引用来生成绑定代码
            ILRuntime.Runtime.Enviorment.AppDomain domain = new ILRuntime.Runtime.Enviorment.AppDomain();
            System.IO.FileStream fs1 = new System.IO.FileStream("Assets/Sources/Code/Framework.IL.Hotfix.dll.bytes", System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.FileStream fs2 = new System.IO.FileStream("Assets/Sources/Code/Game.Hotfix.dll.bytes", System.IO.FileMode.Open, System.IO.FileAccess.Read);
            domain.LoadAssembly(fs1);
            domain.LoadAssembly(fs2);

            //Crossbind Adapter is needed to generate the correct binding code
            var adaptor = new AdaptorReginster(); 
            var clr = new CLRBinderReginster(); 
            var valueType = new ValueTypeBinderReginster(); 
            var @delegate = new DelegateConvertor();
            adaptor.Reginst(domain);
            //clr.Reginst(domain);
            valueType.Reginst(domain);
            //@delegate.Convert(domain);

            ILRuntime.Runtime.CLRBinding.BindingCodeGenerator.GenerateBindingCode(domain, "Assets/Scripts/Local/ILRuntime/Generated");

            AssetDatabase.Refresh();
        }
    }
}
#endif
