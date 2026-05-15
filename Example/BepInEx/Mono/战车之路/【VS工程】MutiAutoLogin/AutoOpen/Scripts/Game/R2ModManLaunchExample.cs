//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace R2ModManLaunchExample
//{
//    /// <summary>
//    /// R2ModMan 启动参数生成器示例
//    /// 参考自 r2modmanPlus/src/r2mm/launching/instructions/instructions/loader/BepInExGameInstructions.cs
//    /// </summary>
//    public class BepInExLaunchParamsGenerator
//    {
//        // 动态路径占位符（参考 DynamicGameInstruction 枚举）
//        private const string BEPINEX_PRELOADER_PATH = "@bepInExPreloaderPath";
//        private const string PROFILE_DIRECTORY = "@profileDirectory";
//        private const string PROFILE_NAME = "@profileName";

//        // 平台检测
//        private static string GetPlatform() => 
//            Environment.OSVersion.Platform switch
//            {
//                PlatformID.Win32NT => "windows",
//                PlatformID.Unix => "linux",  // 也可能是 macOS
//                _ => "windows"
//            };

//        /// <summary>
//        /// 根据游戏类型生成 BepInEx 启动参数
//        /// </summary>
//        public static List<string> GenerateModdedLaunchParams(string gameDirectory, string profilePath, bool isMono)
//        {
//            var paramsList = new List<string>();

//            // ========== 核心 Doorstop 参数 ==========
//            // 启用 Doorstop 注入
//            paramsList.Add("--doorstop-enabled");
//            paramsList.Add("true");

//            // 指定要注入的 Preloader DLL
//            paramsList.Add("--doorstop-target-assembly");
//            paramsList.Add(BEPINEX_PRELOADER_PATH);  // 动态路径，稍后替换

//            // ========== 平台特定参数 ==========
//            if (GetPlatform() != "windows")
//            {
//                // Linux/macOS 需要传递 profile 信息给 wrapper 脚本
//                paramsList.Add("--r2profile");
//                paramsList.Add(PROFILE_NAME);
//            }

//            // ========== IL2CPP 特定参数 ==========
//            if (!isMono)
//            {
//                // IL2CPP 游戏需要指定反混淆的 corlib 路径
//                string unstrippedCorlib = Path.Combine(profilePath, "unstripped_corlib");
//                if (Directory.Exists(unstrippedCorlib))
//                {
//                    paramsList.Add("--doorstop-mono-dll-search-path-override");
//                    paramsList.Add(unstrippedCorlib);
//                }
//            }

//            return paramsList;
//        }

//        /// <summary>
//        /// 解析动态路径占位符（参考 GameInstructionParser）
//        /// </summary>
//        public static List<string> ResolveDynamicParams(List<string> rawParams, string gameDirectory, string profilePath)
//        {
//            var resolvedParams = new List<string>();
            
//            foreach (var param in rawParams)
//            {
//                string resolved = param;
                
//                // 替换动态路径占位符
//                resolved = resolved.Replace(BEPINEX_PRELOADER_PATH, 
//                    FindPreloaderPath(gameDirectory));
                
//                resolved = resolved.Replace(PROFILE_DIRECTORY, 
//                    profilePath);
                
//                resolved = resolved.Replace(PROFILE_NAME, 
//                    Path.GetFileName(profilePath));

//                resolvedParams.Add(resolved);
//            }

//            return resolvedParams;
//        }

//        /// <summary>
//        /// 查找 BepInEx Preloader DLL（参考 GameInstructionParser.bepInExPreloaderPathResolver）
//        /// </summary>
//        private static string FindPreloaderPath(string gameDirectory)
//        {
//            string corePath = Path.Combine(gameDirectory, "BepInEx", "core");
            
//            // 按优先级查找不同版本的 Preloader
//            string[] possiblePreloaders = {
//                "BepInEx.Unity.Mono.Preloader.dll",  // Mono 专用
//                "BepInEx.Preloader.dll",              // 通用版本
//                "BepInEx.Unity.IL2CPP.dll",           // IL2CPP
//                "BepInEx.IL2CPP.dll"
//            };

//            foreach (string preloader in possiblePreloaders)
//            {
//                string fullPath = Path.Combine(corePath, preloader);
//                if (File.Exists(fullPath))
//                {
//                    Console.WriteLine($"找到 Preloader: {fullPath}");
//                    return fullPath;
//                }
//            }

//            throw new FileNotFoundException("未找到 BepInEx Preloader DLL", corePath);
//        }

//        /// <summary>
//        /// 生成启动命令字符串
//        /// </summary>
//        public static string BuildLaunchCommand(string gameExePath, List<string> resolvedParams, string userArgs = "")
//        {
//            string paramsStr = string.Join(" ", resolvedParams.Select(p => 
//                p.Contains(" ") ? $"\"{p}\"" : p));
            
//            return $"\"{gameExePath}\" {paramsStr} {userArgs}".Trim();
//        }
//    }

//    // ==================== 使用示例 ====================
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            // 模拟数据
//            string gameDirectory = @"D:\Games\Risk of Rain 2";
//            string profilePath = @"C:\Users\Player\r2modman-local\Risk of Rain 2\profiles\MyModdedProfile";
//            string gameExePath = Path.Combine(gameDirectory, "Risk of Rain 2.exe");
//            bool isMonoGame = true;  // RoR2 是 Mono 游戏

//            try
//            {
//                // 1. 生成原始参数列表
//                var rawParams = BepInExLaunchParamsGenerator.GenerateModdedLaunchParams(
//                    gameDirectory, profilePath, isMonoGame);
                
//                Console.WriteLine("=== 原始参数（含占位符）===");
//                rawParams.ForEach(p => Console.WriteLine(p));

//                // 2. 解析动态路径
//                var resolvedParams = BepInExLaunchParamsGenerator.ResolveDynamicParams(
//                    rawParams, gameDirectory, profilePath);
                
//                Console.WriteLine("\n=== 解析后的参数 ===");
//                resolvedParams.ForEach(p => Console.WriteLine(p));

//                // 3. 构建最终命令
//                string launchCommand = BepInExLaunchParamsGenerator.BuildLaunchCommand(
//                    gameExePath, resolvedParams, "-screen-fullscreen 0");
                
//                Console.WriteLine("\n=== 最终启动命令 ===");
//                Console.WriteLine(launchCommand);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"错误: {ex.Message}");
//            }
//        }
//    }
//}