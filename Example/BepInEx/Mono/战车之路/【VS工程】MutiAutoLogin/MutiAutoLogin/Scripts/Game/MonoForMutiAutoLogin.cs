using UnityEngine;

namespace MutiAutoLogin
{
    public class MonoForMutiAutoLogin : MonoBehaviour
    {
        private static MonoForMutiAutoLogin _instance;
        public static MonoForMutiAutoLogin Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = GameObject.Find("MonoForMutiAutoLogin");
                    if (obj == null) obj = new GameObject("MonoForMutiAutoLogin");
                    if (obj.GetComponent<MonoForMutiAutoLogin>() == null) _instance = obj.AddComponent<MonoForMutiAutoLogin>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }
        public static bool init;
        public AutoLogin autoLogin;

        private void Awake()
        {
            if (!init)
            {
                init = true;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
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
