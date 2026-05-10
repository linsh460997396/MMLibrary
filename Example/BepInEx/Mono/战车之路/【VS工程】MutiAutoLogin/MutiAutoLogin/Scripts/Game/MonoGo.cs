using UnityEngine;

namespace MutiAutoLogin
{
    public class MonoGo : MonoBehaviour
    {
        public static bool firstInit;
        private static bool hasExecutedAutoLogin;

        [Header("=== 自动登录 ===")]
        public AutoLogin autoLogin;

        [Header("=== 第一人称/自由视角 ===")]
        public FirstPersonAvatar avatar;

        private void Awake()
        {
            if (!firstInit)
            {
                DontDestroyOnLoad(gameObject);
                firstInit = true;
            }
        }

        private void Start()
        {
            if (hasExecutedAutoLogin)
            {
                return;
            }
            hasExecutedAutoLogin = true;

            if (autoLogin == null)
            {
                autoLogin = new AutoLogin(this);
            }
            else
            {
                autoLogin.SetMonoBehaviour(this);
            }
            autoLogin.Execute();
        }
    }
}
