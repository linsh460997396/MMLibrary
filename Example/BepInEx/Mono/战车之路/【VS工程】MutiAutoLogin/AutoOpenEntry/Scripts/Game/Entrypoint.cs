using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using MetalMaxSystem;


namespace Doorstop
{
    public class Entrypoint
    {
        public static string exePath = "E:\\Program Files\\GameCollection\\战车之路\\战车之路.exe";
        public static string debugFilePath = "E:\\Program Files\\GameCollection\\战车之路\\Debug.txt";
        public static string accountFilePath = "E:\\Program Files\\GameCollection\\战车之路\\BepInEx\\plugins\\MutiAutoLogin\\accounts.txt";
        public static string autoOpenCountPath = "E:\\Program Files\\GameCollection\\战车之路\\BepInEx\\plugins\\MutiAutoLogin\\AutoOpenCount.txt";
        public static List<AccountInfo> accounts = new List<AccountInfo>();
        public static int count = 0;

        public static void Start()
        {
            //File.WriteAllText("Debug.txt", "Hello from Unity!");

            //当前已执行次数从文本文件中读取,如果文件不存在则创建并写入0
            if (!LoadAccountsFromFile())
            {
                File.WriteAllText(autoOpenCountPath, "0");
                return;
            }
            else
            {
                count = int.Parse(File.ReadAllText(autoOpenCountPath));
            }
            count += 1;

            //汇报账号数量和当前执行次数
            MMCore.WriteLine($"[AutoOpen] 账号数量={accounts.Count},当前exe自动执行次数={count}", true);
            
            //count没有超过账号数量则执行,超过则不执行
            if (count <= accounts.Count)
            {
                string arguments = ""; // 如果需要传递命令行参数，可以在这里设置

                try
                {
                    // 1. 获取绝对路径和工作目录
                    string exeDirectory = Path.GetDirectoryName(exePath);

                    if (!File.Exists(exePath))
                    {
                        MMCore.WriteLine($"[AutoOpen] 文件不存在: {exePath}", true);
                        return;
                    }

                    // ========== 完整调试检查 ==========
                    PerformPreLaunchChecks(exeDirectory);

                    // 2. 构建 Doorstop 需要的绝对路径（参考 R2ModMan 的实现）
                    // 优先查找 Mono Preloader，其次是通用版本
                    string preloaderPath = FindPreloaderPath(exeDirectory);

                    if (string.IsNullOrEmpty(preloaderPath))
                    {
                        MMCore.WriteLine("[AutoOpen] 无法找到 BepInEx Preloader DLL", true);
                        return;
                    }

                    // 3. 构建 ProcessStartInfo
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WorkingDirectory = exeDirectory;
                    startInfo.Arguments = arguments;
                    startInfo.FileName = exePath;

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
                    MMCore.WriteLine($"[AutoOpen] ===== 启动参数详情 ===== ", true);
                    MMCore.WriteLine($"[AutoOpen] 启动路径: {exePath}", true);
                    MMCore.WriteLine($"[AutoOpen] 工作目录: {startInfo.WorkingDirectory}", true);
                    MMCore.WriteLine($"[AutoOpen] 使用ShellExecute: {startInfo.UseShellExecute}", true);
                    MMCore.WriteLine($"[AutoOpen] 创建新窗口: {!startInfo.CreateNoWindow}", true);
                    MMCore.WriteLine($"[AutoOpen] 命令行参数: {arguments}", true);
                    MMCore.WriteLine($"[AutoOpen] ===== 环境变量 ===== ", true);
                    MMCore.WriteLine($"[AutoOpen] DOORSTOP_ENABLED: {startInfo.EnvironmentVariables["DOORSTOP_ENABLED"]}", true);
                    MMCore.WriteLine($"[AutoOpen] DOORSTOP_TARGET_ASSEMBLY: {startInfo.EnvironmentVariables["DOORSTOP_TARGET_ASSEMBLY"]}", true);
                    if (startInfo.EnvironmentVariables.ContainsKey("DOORSTOP_CONFIG_FILE"))
                    {
                        MMCore.WriteLine($"[AutoOpen] DOORSTOP_CONFIG_FILE: {startInfo.EnvironmentVariables["DOORSTOP_CONFIG_FILE"]}", true);
                    }

                    // 6. 启动进程
                    Process process = Process.Start(startInfo);


                    if (process != null)
                    {
                        MMCore.WriteLine($"[AutoOpen] 成功启动进程 ID: {process.Id}", true);
                        File.WriteAllText(autoOpenCountPath, count.ToString());
                        MMCore.WriteLine($"[AutoOpen] 进程是否正在运行: {!process.HasExited}", true);
                    }
                    else
                    {
                        MMCore.WriteLine("[AutoOpen] Process.Start 返回 null，启动失败。", true);
                    }
                }
                catch (System.Exception ex)
                {
                    MMCore.WriteLine($"[AutoOpen] 启动异常: {ex.Message}\n{ex.StackTrace}", true);
                }

                MMCore.WriteLine(debugFilePath, $"[AutoOpen] count={count}", true, true);
            }
        }

        public class AccountInfo
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
        public static bool LoadAccountsFromFile()
        {
            if (!File.Exists(accountFilePath))
            {
                return false;
            }
            try
            {
                accounts.Clear();
                string[] lines = File.ReadAllLines(accountFilePath);
                string[] parts;
                string username, password, enabledStr;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    parts = line.Split('-');

                    if (parts.Length >= 3)
                    {
                        username = parts[0].Trim();
                        password = parts[1].Trim();
                        enabledStr = parts[2].Trim().ToLower();

                        if (enabledStr == "true")
                        {
                            accounts.Add(new AccountInfo(username, password, true));
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
                    return false;
                }
                return true;
            }
            catch (System.Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// 启动前执行完整检查
        /// </summary>
        public static void PerformPreLaunchChecks(string gameDirectory)
        {
            MMCore.WriteLine("[AutoOpen] ===== 启动前检查 ===== ", true);

            // 检查 Doorstop 代理 DLL
            string[] doorstopDlls = { "winhttp.dll", "version.dll", "doorstop_config.dll" };
            bool foundDoorstop = false;

            foreach (string dll in doorstopDlls)
            {
                string dllPath = Path.Combine(gameDirectory, dll);
                if (File.Exists(dllPath))
                {
                    MMCore.WriteLine($"[AutoOpen] ✅ 找到 Doorstop 代理: {dllPath}", true);
                    foundDoorstop = true;

                    // 检查文件大小（正常的 Doorstop DLL 应该大于 100KB）
                    FileInfo fi = new FileInfo(dllPath);
                    MMCore.WriteLine($"[AutoOpen]   文件大小: {fi.Length / 1024} KB", true);
                }
            }

            if (!foundDoorstop)
            {
                MMCore.WriteLine("[AutoOpen] ⚠️ 未找到 Doorstop 代理 DLL", true);
            }

            // 检查 BepInEx 目录结构
            string bepinExDir = Path.Combine(gameDirectory, "BepInEx");
            if (Directory.Exists(bepinExDir))
            {
                MMCore.WriteLine($"[AutoOpen] ✅ BepInEx 目录存在: {bepinExDir}", true);

                // 检查子目录
                string[] subDirs = { "core", "plugins", "config", "patchers" };
                foreach (string sub in subDirs)
                {
                    string subPath = Path.Combine(bepinExDir, sub);
                    if (Directory.Exists(subPath))
                    {
                        int fileCount = Directory.GetFiles(subPath, "*", SearchOption.AllDirectories).Length;
                        MMCore.WriteLine($"[AutoOpen]   ├─ {sub}/ ({fileCount} 个文件)", true);
                    }
                    else
                    {
                        MMCore.WriteLine($"[AutoOpen]   ├─ {sub}/ (不存在)", true);
                    }
                }
            }
            else
            {
                MMCore.WriteLine($"[AutoOpen] ❌ BepInEx 目录不存在: {bepinExDir}", true);
            }

            // 检查配置文件
            string configPath = Path.Combine(gameDirectory, "doorstop_config.ini");
            if (File.Exists(configPath))
            {
                MMCore.WriteLine($"[AutoOpen] ✅ 找到配置文件: {configPath}", true);
                string configContent = File.ReadAllText(configPath);
                MMCore.WriteLine($"[AutoOpen] 配置内容:\n{configContent}", true);
            }
            else
            {
                MMCore.WriteLine($"[AutoOpen] ⚠️ 配置文件不存在: {configPath}", true);
            }

            MMCore.WriteLine("[AutoOpen] ===== 检查完成 ===== ", true);
        }

        /// <summary>
        /// 查找 BepInEx Preloader DLL 的正确路径
        /// 参考 R2ModMan 的 GameInstructionParser.bepInExPreloaderPathResolver
        /// </summary>
        public static string FindPreloaderPath(string exeDirectory)
        {
            string corePath = Path.Combine(exeDirectory, "BepInEx", "core");

            if (!Directory.Exists(corePath))
            {
                MMCore.WriteLine($"[AutoOpen] BepInEx core 目录不存在: {corePath}", true);
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
                    MMCore.WriteLine($"[AutoOpen] 找到 Preloader: {fullPath}", true);
                    return fullPath;
                }
            }

            MMCore.WriteLine($"[AutoOpen] 在 {corePath} 中未找到任何 Preloader DLL", true);
            return null;
        }
    }
}
