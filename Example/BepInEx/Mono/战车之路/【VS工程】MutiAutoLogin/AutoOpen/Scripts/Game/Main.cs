using BepInEx;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace AutoOpen
{
    [BepInPlugin("BTCat.RoadChariot.AutoOpen", "AutoOpen", "1.0.0")]
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
                if (!LoadAccountsFromFile())
                {
                    Debug.Log("AutoOpen:未找到外部账号文件,不执行任何操作");
                }
                else
                {
                    Debug.Log("AutoOpen:成功加载外部账号文件,继续执行后续操作");
                    Toggle[] toggles = canvas.GetComponentsInChildren<Toggle>();
                    int count = 0;
                    string countStr;
                    //如果有Cavas,输出所有Toggle的名称及状态
                    foreach (Toggle toggle in toggles)
                    {
                        //检查名称是否叫Toggle (1)
                        if (toggle.name == "Toggle (1)")
                        {
                            //先注销事件防止重复执行,再慢慢执行后续操作
                            SceneManager.sceneLoaded -= OnSceneLoaded;

                            //有这个开关UI说明已经进入游戏,启动外部exe前将执行总次数读取,如果文件不存在则创建并写入0
                            string countFilePath = Path.Combine(Application.dataPath, "..", "BepInEx/plugins/MutiAutoLogin/AutoOpenCount.txt");
                            //当前已执行次数从文本文件中读取,如果文件不存在则创建并写入0
                            if (!File.Exists(countFilePath))
                            {
                                File.WriteAllText(countFilePath, "0");
                            }
                            else
                            {
                                countStr = File.ReadAllText(countFilePath);
                                count = int.Parse(countStr);
                            }
                            count += 1;
                            //汇报账号数量和当前执行次数
                            Debug.Log($"AutoOpen:账号数量={accounts.Count},当前exe自动执行次数={count}");
                            //count没有超过账号数量则执行,超过则不执行
                            if (count <= accounts.Count)
                            {
                                LaunchExternalExe(
                                    Path.Combine(Application.dataPath, "..", "战车之路.exe")
                                );
                                File.WriteAllText(countFilePath, count.ToString());
                            }
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 启动外部exe并注入BepInEx/Doorstop
        /// </summary>
        public void LaunchExternalExe(string exePath, string arguments = "")
        {
            try
            {
                // 1. 获取绝对路径和工作目录
                string fullPath = Path.GetFullPath(exePath);
                string exeDirectory = Path.GetDirectoryName(fullPath);
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"文件不存在: {fullPath}");
                    return;
                }
                string preloaderPath = @"BepInEx\core\BepInEx.Preloader.dll";
                string corlibPath = @"BepInEx\core";
                // 2. 构建 ProcessStartInfo
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WorkingDirectory = exeDirectory;
                startInfo.Arguments = arguments;
                startInfo.FileName = fullPath;

                // 必须设置为 false 才能使用 EnvironmentVariables
                startInfo.UseShellExecute = false;
                // 3. 设置 Doorstop 环境变量 
                startInfo.EnvironmentVariables["DOORSTOP_ENABLED"] = "1";
                startInfo.EnvironmentVariables["DOORSTOP_TARGET_ASSEMBLY"] = preloaderPath;
                startInfo.EnvironmentVariables["DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE"] = corlibPath;
                startInfo.EnvironmentVariables["DOORSTOP_IGNORE_DISABLED_ENV"] = "0";
                startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_ENABLED"] = "0";
                startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_START_SERVER"] = "0";
                startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_ADDRESS"] = "127.0.0.1:10000";
                startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_SUSPEND"] = "0";
                startInfo.EnvironmentVariables["DOORSTOP_CLR_RUNTIME_CORECLR_PATH"] = "";
                startInfo.EnvironmentVariables["DOORSTOP_CLR_CORLIB_DIR"] = "";
                // 4. 调试日志
                Debug.Log($"[Launch] 启动路径: {fullPath}");
                Debug.Log($"[Launch] 工作目录: {startInfo.WorkingDirectory}");

                Debug.Log($"[Launch] DOORSTOP_ENABLED: {startInfo.EnvironmentVariables["DOORSTOP_ENABLED"]}");
                Debug.Log($"[Launch] DOORSTOP_TARGET_ASSEMBLY: {startInfo.EnvironmentVariables["DOORSTOP_TARGET_ASSEMBLY"]}");
                Debug.Log($"[Launch] DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE: {startInfo.EnvironmentVariables["DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE"]}");
                Debug.Log($"[Launch] DOORSTOP_IGNORE_DISABLED_ENV: {startInfo.EnvironmentVariables["DOORSTOP_IGNORE_DISABLED_ENV"]}");
                Debug.Log($"[Launch] DOORSTOP_MONO_DEBUG_ENABLED: {startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_ENABLED"]}");
                Debug.Log($"[Launch] DOORSTOP_MONO_DEBUG_START_SERVER: {startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_START_SERVER"]}");
                Debug.Log($"[Launch] DOORSTOP_MONO_DEBUG_ADDRESS: {startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_ADDRESS"]}");
                Debug.Log($"[Launch] DOORSTOP_MONO_DEBUG_SUSPEND: {startInfo.EnvironmentVariables["DOORSTOP_MONO_DEBUG_SUSPEND"]}");
                Debug.Log($"[Launch] DOORSTOP_CLR_RUNTIME_CORECLR_PATH: {startInfo.EnvironmentVariables["DOORSTOP_CLR_RUNTIME_CORECLR_PATH"]}");
                Debug.Log($"[Launch] DOORSTOP_CLR_CORLIB_DIR: {startInfo.EnvironmentVariables["DOORSTOP_CLR_CORLIB_DIR"]}");

                // 检查关键文件是否存在
                if (!File.Exists(preloaderPath))
                {
                    Debug.LogWarning(  $"[Launch] 警告: BepInEx Preloader DLL 不存在于预期路径: {preloaderPath}"  );
                }

                // 5. 启动进程
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    Debug.Log($"[Launch] 成功启动进程 ID: {process.Id}");
                }
                else
                {
                    Debug.LogError("[Launch] Process.Start 返回 null，启动失败。");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Launch] 启动异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public string accountFilePath = "BepInEx/plugins/MutiAutoLogin/accounts.txt";
        private readonly List<AccountInfo> accounts = new List<AccountInfo>();

        private class AccountInfo
        {
            public string username;
            public string password;
            public bool enabled;

            public AccountInfo(string user, string pwd, bool en)
            {
                username = user;
                password = pwd;
                enabled = en;
            }
        }

        /// <summary>
        /// 从外部文本文件加载账号信息,并存储到accounts列表中
        /// </summary>
        /// <returns>是否成功加载账号信息</returns>
        bool LoadAccountsFromFile()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", accountFilePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"外部账号文件不存在: {fullPath}");
                return false;
            }
            else
            {
                Debug.Log($"找到外部账号文件: {fullPath}");
            }

            try
            {
                accounts.Clear();
                string[] lines = File.ReadAllLines(fullPath);
                string[] parts;
                string username,
                    password,
                    enabledStr;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    parts = line.Split('-');

                    if (parts.Length >= 3)
                    {
                        username = parts[0].Trim();
                        password = parts[1].Trim();
                        enabledStr = parts[2].Trim().ToLower();

                        if (enabledStr == "true")
                        {
                            accounts.Add(new AccountInfo(username, password, true));
                            Debug.Log($"读取账号: {username}, 开关: true");
                        }
                        else if (
                            string.IsNullOrWhiteSpace(username)
                            || string.IsNullOrWhiteSpace(password)
                            || username.StartsWith("//")
                            || (enabledStr != "true" && enabledStr != "false")
                        )
                        {
                            continue;
                        }
                    }
                }

                if (accounts.Count == 0)
                {
                    Debug.LogWarning("账号为空");
                    return false;
                }

                Debug.Log($"成功从外部文件加载 {accounts.Count} 组账号");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"读取账号文件失败: {e.Message}");
                return false;
            }
        }
    }
}
