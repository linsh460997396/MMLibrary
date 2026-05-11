using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AudioStop
{
    [BepInPlugin("BTCat.RoadChariot.AudioStop", "AudioStop", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public static bool init;

        //↓入口
        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Toggle[] toggles = canvas.GetComponentsInChildren<Toggle>();
                //如果有Cavas,输出所有Toggle的名称及状态
                foreach (Toggle toggle in toggles)
                {
                    //Debug.Log($"找到Toggle: {toggle.name}, 状态: {toggle.isOn}");

                    //如果状态是false,检查名称是否叫Toggle (1),如果是则设置状态为true
                    if (!toggle.isOn && toggle.name == "Toggle (1)")
                    {
                        toggle.isOn = true;
                        Debug.Log($"已屏蔽声音!");

                        //注销事件
                        SceneManager.sceneLoaded -= OnSceneLoaded;
                        return;
                    }
                }
            }
        }
    }
}
