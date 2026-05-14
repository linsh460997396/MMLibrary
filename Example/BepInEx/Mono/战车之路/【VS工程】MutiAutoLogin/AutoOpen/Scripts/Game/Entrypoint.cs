//// ReSharper disable once CheckNamespace
//using System;
//using System.IO;
//using System.Reflection;

//namespace Doorstop
//{
//    internal static class Entrypoint
//    {
//        private static readonly string SilentExceptionLog = Path.Combine(
//            AppDomain.CurrentDomain.BaseDirectory,
//            "doorstop_exception.log"
//        );
//        private static readonly string CallLog = Path.Combine(
//            AppDomain.CurrentDomain.BaseDirectory,
//            "entrypoint_call.log"
//        );

//        public static void Start()
//        {
//            // 方法1：记录调用时间戳
//            File.AppendAllText(
//                CallLog,
//                $"Entrypoint.Start() called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\r\n"
//            );

//            try
//            {
//                string preloaderPath = Path.Combine(
//                    AppDomain.CurrentDomain.BaseDirectory,
//                    "BepInEx",
//                    "core",
//                    "BepInEx.Preloader.dll"
//                );

//                // 方法2：记录预加载器路径检查
//                File.AppendAllText(
//                    CallLog,
//                    $"Preloader path: {preloaderPath}, exists: {File.Exists(preloaderPath)}\r\n"
//                );

//                if (!File.Exists(preloaderPath))
//                {
//                    var errorMsg = $"BepInEx.Preloader.dll not found at: {preloaderPath}";
//                    File.WriteAllText(SilentExceptionLog, errorMsg);
//                    File.AppendAllText(CallLog, $"ERROR: {errorMsg}\r\n");
//                    return;
//                }

//                Assembly preloaderAsm = Assembly.LoadFrom(preloaderPath);
//                File.AppendAllText(CallLog, "Preloader assembly loaded successfully\r\n");

//                Type envVarsType = preloaderAsm.GetType("BepInEx.Preloader.EnvVars");
//                if (envVarsType != null)
//                {
//                    MethodInfo loadVars = envVarsType.GetMethod(
//                        "LoadVars",
//                        BindingFlags.Public | BindingFlags.Static
//                    );
//                    loadVars?.Invoke(null, null);
//                    File.AppendAllText(CallLog, "EnvVars.LoadVars() called\r\n");
//                }

//                Type runnerType = preloaderAsm.GetType(
//                    "BepInEx.Unity.Mono.Preloader.UnityPreloaderRunner"
//                );
//                MethodInfo preMain = runnerType?.GetMethod(
//                    "PreloaderPreMain",
//                    BindingFlags.Public | BindingFlags.Static
//                );
//                preMain?.Invoke(null, null);
//                File.AppendAllText(
//                    CallLog,
//                    "UnityPreloaderRunner.PreloaderPreMain() called successfully\r\n"
//                );

//                File.AppendAllText(CallLog, "========================================\r\n");
//            }
//            catch (Exception ex)
//            {
//                File.WriteAllText(SilentExceptionLog, ex.ToString());
//                File.AppendAllText(
//                    CallLog,
//                    $"EXCEPTION: {ex.Message}\r\n{ex.StackTrace}\r\n========================================\r\n"
//                );
//            }
//        }
//    }
//}
