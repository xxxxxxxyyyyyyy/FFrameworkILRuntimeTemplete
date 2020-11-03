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
        }
    }
}