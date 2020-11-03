using Framework.Module;
using Framework.Module.Audio;
using Framework.Module.FSM;
using Framework.Module.Resource;
using Framework.Module.Script;
using Game.Local.IL.Reginster;

namespace Game.Launch
{
    public class LoadModuleState : State<Launcher>
    {
        ScriptManager scriptManager;
        public override async void OnEnter(IFSM<Launcher> fsm)
        {
            ModuleManager.Instance.GetModule<IResourceManager>();
            ModuleManager.Instance.GetModule<IAudioManager>();

            scriptManager = ScriptManager.Instance;
            scriptManager.SetReginster(new AdaptorReginster(), new CLRBinderReginster(), new ValueTypeBinderReginster(), new DelegateConvertor());
            await scriptManager.Load("Code");
            scriptManager.InvokeMethod("Game.Hotfix.Main", "Initialize");
        }

        public override void OnUpdate(IFSM<Launcher> fsm)
        {
            //scriptManager.OnUpdate();
            //scriptManager.OnLateUpdate();
        }
    }
}