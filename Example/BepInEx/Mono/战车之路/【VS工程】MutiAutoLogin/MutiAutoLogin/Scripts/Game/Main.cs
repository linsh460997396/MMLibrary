using BepInEx;
using MetalMaxSystem.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MutiAutoLogin
{
    [BepInPlugin("BTCat.RoadChariot.MutiAutoLogin", "MutiAutoLogin", "0.0.5")]
    public class Main : BaseUnityPlugin
    {
        //↓入口
        private void Awake()
        {
            Debug.Log("BepInEx插件加载成功！");
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("正在等待目标场景...");
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("场景加载完成: " + scene.name);

            MainThreadDispatcher.Instance.Invoke(() =>
            {
                var obj = new GameObject("MonoGo");
                obj.AddComponent<MonoGo>();
                DontDestroyOnLoad(obj);
            });
        } 
    }
}
