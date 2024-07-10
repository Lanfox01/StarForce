//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce
{
    public class ProcedureCheckVersion : ProcedureBase
    {
        private bool m_CheckVersionComplete = false;
        private bool m_NeedUpdateVersion = false;
        private VersionInfo m_VersionInfo = null;

        public override bool UseNativeDialog
        {
            get
            {
                return true;
            }
        }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_CheckVersionComplete = false;
            m_NeedUpdateVersion = false;
            m_VersionInfo = null;

            GameEntry.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
            GameEntry.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

            // 向服务器请求版本信息  主要看 BuildInfo这个文件；中的CheckVersionUrl   比如 http://192.168.50.223:8080/Resources/WindowsVersion.txt
            GameEntry.WebRequest.AddWebRequest(Utility.Text.Format(GameEntry.BuiltinData.BuildInfo.CheckVersionUrl, GetPlatformPath()), this);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
            GameEntry.Event.Unsubscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_CheckVersionComplete)
            {
                return;
            }

            if (m_NeedUpdateVersion)
            {  // 如果需继续 更新资源，把服务器得到的新的版本信息内容 传输过去
                procedureOwner.SetData<VarInt32>("VersionListLength", m_VersionInfo.VersionListLength);
                procedureOwner.SetData<VarInt32>("VersionListHashCode", m_VersionInfo.VersionListHashCode);
                procedureOwner.SetData<VarInt32>("VersionListCompressedLength", m_VersionInfo.VersionListCompressedLength);
                procedureOwner.SetData<VarInt32>("VersionListCompressedHashCode", m_VersionInfo.VersionListCompressedHashCode);
                ChangeState<ProcedureUpdateVersion>(procedureOwner);
            }
            else
            {
                ChangeState<ProcedureVerifyResources>(procedureOwner); /// 不用更新下载新的资源包，这里直接验证本地资源
            }
        }

        /// <summary>
        ///  这里应该是 直接跳转到 大版本 整包下载地址？
        /// </summary>
        /// <param name="userData"></param>
        private void GotoUpdateApp(object userData)
        {
            string url = null;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            url = GameEntry.BuiltinData.BuildInfo.WindowsAppUrl;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            url = GameEntry.BuiltinData.BuildInfo.MacOSAppUrl;
#elif UNITY_IOS
            url = GameEntry.BuiltinData.BuildInfo.IOSAppUrl;
#elif UNITY_ANDROID
            url = GameEntry.BuiltinData.BuildInfo.AndroidAppUrl;
#endif
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        private void OnWebRequestSuccess(object sender, GameEventArgs e)
        {
            WebRequestSuccessEventArgs ne = (WebRequestSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            // 解析版本信息  下载后的二进制 转换为 string
            byte[] versionInfoBytes = ne.GetWebResponseBytes();
            string versionInfoString = Utility.Converter.GetString(versionInfoBytes);
            /// versionInfoString 获取到 WindowsVersion.txt 这个文件的内容   反序列化吧
            m_VersionInfo = Utility.Json.ToObject<VersionInfo>(versionInfoString);
            if (m_VersionInfo == null)
            {
                Log.Error("Parse VersionInfo failure.");
                return;
            }
            /// 为什么 要对比 两种 版本号？ 好像是 编译出包 和 资源出包的 时候设定的？
            Log.Info("Latest game version is '{0} ({1})', local game version is '{2} ({3})'.", m_VersionInfo.LatestGameVersion, m_VersionInfo.InternalGameVersion.ToString(), Version.GameVersion, Version.InternalGameVersion.ToString());

            if (m_VersionInfo.ForceUpdateGame)
            {
                // 需要强制更新游戏应用   大版本 或者 有发现大Bug 的时候
                GameEntry.UI.OpenDialog(new DialogParams
                {
                    Mode = 2,
                    Title = GameEntry.Localization.GetString("ForceUpdate.Title"),
                    Message = GameEntry.Localization.GetString("ForceUpdate.Message"),
                    ConfirmText = GameEntry.Localization.GetString("ForceUpdate.UpdateButton"),
                    OnClickConfirm = GotoUpdateApp,
                    CancelText = GameEntry.Localization.GetString("ForceUpdate.QuitButton"),
                    OnClickCancel = delegate (object userData) { UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit); },
                });

                return;
            }

            // 设置资源更新下载地址
            GameEntry.Resource.UpdatePrefixUri = Utility.Path.GetRegularPath(m_VersionInfo.UpdatePrefixUri);

            m_CheckVersionComplete = true;   /// 资源版本号对比，如果不一样需 进一步的下载
            m_NeedUpdateVersion = GameEntry.Resource.CheckVersionList(m_VersionInfo.InternalResourceVersion) == CheckVersionListResult.NeedUpdate;
        }

        private void OnWebRequestFailure(object sender, GameEventArgs e)
        {
            WebRequestFailureEventArgs ne = (WebRequestFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Warning("Check version failure, error message is '{0}'.", ne.ErrorMessage);
        }

        private string GetPlatformPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "MacOS";

                case RuntimePlatform.IPhonePlayer:
                    return "IOS";

                case RuntimePlatform.Android:
                    return "Android";

                default:
                    throw new System.NotSupportedException(Utility.Text.Format("Platform '{0}' is not supported.", Application.platform));
            }
        }
    }
}
