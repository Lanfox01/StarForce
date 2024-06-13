//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce
{
    public class ProcedureSplash : ProcedureBase
    {
        public override bool UseNativeDialog
        {
            get
            {
                return true;
            }
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            // TODO: 这里可以播放一个 Splash 动画
            // ...

            if (GameEntry.Base.EditorResourceMode)
            {
                // 编辑器模式  主要用于开发测试
                Log.Info("Editor resource mode detected. 编辑器模式");
                ChangeState<ProcedurePreload>(procedureOwner);
            }
            else if (GameEntry.Resource.ResourceMode == ResourceMode.Package)
            {
                // 单机模式   主要用于第一次发布或者大版本重新发布；
                Log.Info("Package resource mode detected. 单机模式");
                ChangeState<ProcedureInitResources>(procedureOwner);
                /// 资源初始化之后，紧跟着 还是会调用 ProcedurePreload 流程；
            }
            else
            {
                // 可更新模式   平时小布丁更新等
                Log.Info("Updatable resource mode detected. 可更新模式");
                ChangeState<ProcedureCheckVersion>(procedureOwner); // 远程服务器地址更新，必然要验证版本号
                // 核对版本之后 ProcedureUpdateVersion 更新数据  主要是为了下载 GameFrameworkVersion.dat 这个文件 
                // 更新之后 ProcedureVerifyResources 验证资源的有效性
                // 核对Resources资源 ProcedureCheckResources
                // 预更新新 Resources资源  ProcedureUpdateResources
                // 最后 预加载 资源 ProcedurePreload
                
            }
        }
    }
}
