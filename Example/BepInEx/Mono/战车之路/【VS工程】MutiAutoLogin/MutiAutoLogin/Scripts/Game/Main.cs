using BepInEx;
using MetalMaxSystem.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MutiAutoLogin
{
    [BepInPlugin("BTCat.RoadChariot.MutiAutoLogin", "MutiAutoLogin", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public static bool init;
        //↓入口
        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("正在等待目标场景...");
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("场景加载完成: " + scene.name);
            if (!init)
            {
                init = true; //确保只执行一次
                MainThreadDispatcher.Instance.Invoke(() =>
                {
                    //如果场景没有MonoForMutiAutoLogin则创建
                    var obj = new GameObject("MonoForMutiAutoLogin");
                    obj.AddComponent<MonoForMutiAutoLogin>();
                    DontDestroyOnLoad(obj);
                });
            }
        }
    }
}
