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
        // 启动模式：true=ProcessStart, false=CommandLine
        public static bool UseProcessStartMode = true;

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
                    Debug.Log("[AutoOpen] 未找到外部账号文件,不执行任何操作");
                }
                else
                {
                    Debug.Log("[AutoOpen] 成功加载外部账号文件,继续执行后续操作");
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
                            Debug.Log($"[AutoOpen] 账号数量={accounts.Count},当前exe自动执行次数={count}");
                            //count没有超过账号数量则执行,超过则不执行
                            if (count <= accounts.Count)
                            {
                                string exePath = Path.Combine(Application.dataPath, "..", "战车之路.exe");

                                if (UseProcessStartMode)
                                {
                                    Debug.Log("[AutoOpen] 使用 Process.Start 方式启动");
                                    LaunchExternalExe(exePath);
                                }
                                else
                                {
                                    Debug.Log("[AutoOpen] 使用命令行方式启动");
                                    LaunchExternalExeCommandLine(exePath);
                                }

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
        /// 参考 R2ModMan 的实现方式
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
                    Debug.LogError($"[AutoOpen] 文件不存在: {fullPath}");
                    return;
                }

                // ========== 完整调试检查 ==========
                PerformPreLaunchChecks(exeDirectory);

                // 2. 构建 Doorstop 需要的绝对路径（参考 R2ModMan 的实现）
                // 优先查找 Mono Preloader，其次是通用版本
                string preloaderPath = FindPreloaderPath(exeDirectory);

                if (string.IsNullOrEmpty(preloaderPath))
                {
                    Debug.LogError("[AutoOpen] 无法找到 BepInEx Preloader DLL");
                    return;
                }

                // 3. 构建 ProcessStartInfo
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WorkingDirectory = exeDirectory;
                startInfo.Arguments = arguments;
                startInfo.FileName = fullPath;

                startInfo.UseShellExecute = false;  // 必须设置为 false 才能传递环境变量

                // 4. 设置 Doorstop 环境变量
                startInfo.EnvironmentVariables["DOORSTOP_ENABLED"] = "true";
                startInfo.EnvironmentVariables["DOORSTOP_TARGET_ASSEMBLY"] = preloaderPath;

                // 额外：设置 DOORSTOP_CONFIG_FILE 指向配置文件（如果存在）
                string configPath = Path.Combine(exeDirectory, "doorstop_config.ini");
                if (File.Exists(configPath))
                {
                    startInfo.EnvironmentVariables["DOORSTOP_CONFIG_FILE"] = configPath;
                }

                // Mono 环境不需要 DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE（那是 IL2CPP 使用的）

                // 5. 详细调试日志
                Debug.Log($"[AutoOpen] ===== 启动参数详情 ===== ");
                Debug.Log($"[AutoOpen] 启动路径: {fullPath}");
                Debug.Log($"[AutoOpen] 工作目录: {startInfo.WorkingDirectory}");
                Debug.Log($"[AutoOpen] 使用ShellExecute: {startInfo.UseShellExecute}");
                Debug.Log($"[AutoOpen] 创建新窗口: {!startInfo.CreateNoWindow}");
                Debug.Log($"[AutoOpen] 命令行参数: {arguments}");
                Debug.Log($"[AutoOpen] ===== 环境变量 ===== ");
                Debug.Log($"[AutoOpen] DOORSTOP_ENABLED: {startInfo.EnvironmentVariables["DOORSTOP_ENABLED"]}");
                Debug.Log($"[AutoOpen] DOORSTOP_TARGET_ASSEMBLY: {startInfo.EnvironmentVariables["DOORSTOP_TARGET_ASSEMBLY"]}");
                if (startInfo.EnvironmentVariables.ContainsKey("DOORSTOP_CONFIG_FILE"))
                {
                    Debug.Log($"[AutoOpen] DOORSTOP_CONFIG_FILE: {startInfo.EnvironmentVariables["DOORSTOP_CONFIG_FILE"]}");
                }

                // 6. 启动进程
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    Debug.Log($"[AutoOpen] 成功启动进程 ID: {process.Id}");
                    Debug.Log($"[AutoOpen] 进程是否正在运行: {!process.HasExited}");
                }
                else
                {
                    Debug.LogError("[AutoOpen] Process.Start 返回 null，启动失败。");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoOpen] 启动异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 使用命令行方式启动外部exe并注入BepInEx/Doorstop
        /// 通过 cmd.exe 启动，确保环境变量正确传递
        /// </summary>
        public void LaunchExternalExeCommandLine(string exePath, string arguments = "")
        {
            try
            {
                string fullPath = Path.GetFullPath(exePath);
                string exeDirectory = Path.GetDirectoryName(fullPath);

                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"[AutoOpen] 文件不存在: {fullPath}");
                    return;
                }

                PerformPreLaunchChecks(exeDirectory);

                string preloaderPath = FindPreloaderPath(exeDirectory);

                if (string.IsNullOrEmpty(preloaderPath))
                {
                    Debug.LogError("[AutoOpen] 无法找到 BepInEx Preloader DLL");
                    return;
                }

                string configPath = Path.Combine(exeDirectory, "doorstop_config.ini");

                string cmdArgs = $"/c \"set DOORSTOP_ENABLED=true && set DOORSTOP_TARGET_ASSEMBLY=BepInEx\\core\\BepInEx.Preloader.dll";

                if (File.Exists(configPath))
                {
                    cmdArgs += $" && set DOORSTOP_CONFIG_FILE=\"{configPath}\"";
                }

                cmdArgs += $" && cd /d \"{exeDirectory}\" && \"{fullPath}\" {arguments}\"";

                Debug.Log($"[AutoOpen] ===== 命令行启动参数详情 ===== ");
                Debug.Log($"[AutoOpen] 启动路径: {fullPath}");
                Debug.Log($"[AutoOpen] 工作目录: {exeDirectory}");
                Debug.Log($"[AutoOpen] 命令行参数: {cmdArgs}");
                Debug.Log($"[AutoOpen] 环境变量已设置");

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = cmdArgs;
                startInfo.WorkingDirectory = exeDirectory;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    Debug.Log($"[AutoOpen] 成功启动进程 ID: {process.Id}");
                    Debug.Log($"[AutoOpen] 进程是否正在运行: {!process.HasExited}");

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Debug.Log($"[AutoOpen] [子进程输出] {args.Data}");
                        }
                    };
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Debug.LogError($"[AutoOpen] [子进程错误] {args.Data}");
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                else
                {
                    Debug.LogError("[AutoOpen] Process.Start 返回 null，启动失败。");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoOpen] 启动异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 启动前执行完整检查
        /// </summary>
        private void PerformPreLaunchChecks(string gameDirectory)
        {
            Debug.Log("[AutoOpen] ===== 启动前检查 ===== ");

            // 检查 Doorstop 代理 DLL
            string[] doorstopDlls = { "winhttp.dll", "version.dll", "doorstop_config.dll" };
            bool foundDoorstop = false;

            foreach (string dll in doorstopDlls)
            {
                string dllPath = Path.Combine(gameDirectory, dll);
                if (File.Exists(dllPath))
                {
                    Debug.Log($"[AutoOpen] ✅ 找到 Doorstop 代理: {dllPath}");
                    foundDoorstop = true;

                    // 检查文件大小（正常的 Doorstop DLL 应该大于 100KB）
                    FileInfo fi = new FileInfo(dllPath);
                    Debug.Log($"[AutoOpen]   文件大小: {fi.Length / 1024} KB");
                }
            }

            if (!foundDoorstop)
            {
                Debug.LogWarning("[AutoOpen] ⚠️ 未找到 Doorstop 代理 DLL");
            }

            // 检查 BepInEx 目录结构
            string bepinExDir = Path.Combine(gameDirectory, "BepInEx");
            if (Directory.Exists(bepinExDir))
            {
                Debug.Log($"[AutoOpen] ✅ BepInEx 目录存在: {bepinExDir}");

                // 检查子目录
                string[] subDirs = { "core", "plugins", "config", "patchers" };
                foreach (string sub in subDirs)
                {
                    string subPath = Path.Combine(bepinExDir, sub);
                    if (Directory.Exists(subPath))
                    {
                        int fileCount = Directory.GetFiles(subPath, "*", SearchOption.AllDirectories).Length;
                        Debug.Log($"[AutoOpen]   ├─ {sub}/ ({fileCount} 个文件)");
                    }
                    else
                    {
                        Debug.Log($"[AutoOpen]   ├─ {sub}/ (不存在)");
                    }
                }
            }
            else
            {
                Debug.LogError($"[AutoOpen] ❌ BepInEx 目录不存在: {bepinExDir}");
            }

            // 检查配置文件
            string configPath = Path.Combine(gameDirectory, "doorstop_config.ini");
            if (File.Exists(configPath))
            {
                Debug.Log($"[AutoOpen] ✅ 找到配置文件: {configPath}");
                string configContent = File.ReadAllText(configPath);
                Debug.Log($"[AutoOpen] 配置内容:\n{configContent}");
            }
            else
            {
                Debug.Log($"[AutoOpen] ⚠️ 配置文件不存在: {configPath}");
            }

            Debug.Log("[AutoOpen] ===== 检查完成 ===== ");
        }

        /// <summary>
        /// 查找 BepInEx Preloader DLL 的正确路径
        /// 参考 R2ModMan 的 GameInstructionParser.bepInExPreloaderPathResolver
        /// </summary>
        private string FindPreloaderPath(string exeDirectory)
        {
            string corePath = Path.Combine(exeDirectory, "BepInEx", "core");

            if (!Directory.Exists(corePath))
            {
                Debug.LogError($"[AutoOpen] BepInEx core 目录不存在: {corePath}");
                return null;
            }

            // 按优先级查找 Preloader DLL（参考 R2ModMan 的实现）
            string[] possiblePreloaders = {
                "BepInEx.Unity.Mono.Preloader.dll",  // Mono 专用
                "BepInEx.Preloader.dll",              // 通用版本
                "BepInEx.Unity.IL2CPP.dll",           // IL2CPP 版本（备用）
                "BepInEx.IL2CPP.dll"                  // IL2CPP 通用版本（备用）
            };

            foreach (string preloader in possiblePreloaders)
            {
                string fullPath = Path.Combine(corePath, preloader);
                if (File.Exists(fullPath))
                {
                    Debug.Log($"[AutoOpen] 找到 Preloader: {fullPath}");
                    return fullPath;
                }
            }

            Debug.LogError($"[AutoOpen] 在 {corePath} 中未找到任何 Preloader DLL");
            return null;
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
                Debug.LogWarning($"[AutoOpen] ⚠️ 外部账号文件不存在: {fullPath}");
                return false;
            }
            else
            {
                Debug.Log($"[AutoOpen] ✅ 找到外部账号文件: {fullPath}");
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
                            Debug.Log($"[AutoOpen] ✅ 读取账号: {username}, 开关: true");
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

                Debug.Log($"[AutoOpen] ✅ 成功从外部文件加载 {accounts.Count} 组账号");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoOpen] ❌ 读取账号文件失败: {e.Message}");
                return false;
            }
        }
    }
}
