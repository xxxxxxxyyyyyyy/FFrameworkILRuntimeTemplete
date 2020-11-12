using Framework.IL.Hotfix.Module.UI;
using UnityEngine;

namespace Game.Hotfix
{
    public static class Main
    {
        public static void Initialize()
        {
            Debug.Log($"aaaaa{Layer.POPUP}");
            Debug.Log("hello ilruntime");
            Debug.Log("你在干啥呢");
            Debug.Log(Get().Item1);
            Debug.Log(Get().Item2);
            Debug.Log(Get().Item3);
        }

        static (int, string, bool) Get()
        {
            return (1, "2", true);
        }
    }
}