using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace MutiAutoLogin
{
    [System.Serializable]
    public class AutoLogin
    {
        [Header("=== 自动登录设置 ===")]
        public bool enableAutoLogin = true;
        /// <summary>
        /// 自动登录按钮点击延迟时间（秒）,在填充完登录信息后等待一段时间再点击登录按钮,可以根据实际情况调整这个值以确保登录流程的稳定性,过短可能导致登录按钮未完全就绪而点击失败,过长则会增加登录等待时间
        /// </summary>
        public float autoLoginDelay = 1f;

        [Header("=== 外部账号文件设置 ===")]
        public string accountFilePath = "BepInEx/plugins/MutiAutoLogin/accounts.txt";
        /// <summary>
        /// MonoBehaviour实例,用于启动协程执行自动登录流程,需要在构造函数中传入或通过SetMonoBehaviour方法设置,否则无法执行自动登录流程
        /// </summary>
        private MonoBehaviour _monoBehaviour;
        /// <summary>
        /// 账号信息列表,存储从外部文件加载的账号信息,每个账号信息包含用户名、密码和启用状态等字段,用于在自动登录流程中根据当前输入框用户名找到下一组可用的账号进行登录
        /// </summary>
        private readonly List<AccountInfo> accounts = new List<AccountInfo>();

        /// <summary>
        /// 账号信息类,包含用户名、密码和启用状态等字段,用于存储从外部文件加载的账号信息
        /// </summary>
        [System.Serializable]
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
        /// 构造函数,可以选择传入一个MonoBehaviour实例用于启动协程,如果不传入则需要后续调用SetMonoBehaviour方法设置MonoBehaviour实例
        /// </summary>
        public AutoLogin() { }

        public AutoLogin(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        /// <summary>
        /// 设置MonoBehaviour实例,用于启动协程执行自动登录流程,如果在构造函数中已经传入了MonoBehaviour实例则不需要调用此方法
        /// </summary>
        /// <param name="monoBehaviour">用于启动协程的MonoBehaviour实例</param>
        public void SetMonoBehaviour(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        /// <summary>
        /// 执行自动登录流程:加载账号信息,等待登录界面就绪,根据当前输入框用户名找到下一组账号,填充登录信息并点击登录按钮
        /// </summary>
        public void Execute()
        {
            if (_monoBehaviour != null)
            {
                _monoBehaviour.StartCoroutine(ExecuteCoroutine());
            }
            else
            {
                Debug.LogError("MonoBehaviour 未设置,无法启动协程");
            }
        }

        /// <summary>
        /// 自动登录流程的协程实现,包含加载账号信息,等待登录界面就绪,根据当前输入框用户名找到下一组账号,填充登录信息并点击登录按钮等步骤
        /// </summary>
        /// <returns>协程的枚举器</returns>
        System.Collections.IEnumerator ExecuteCoroutine()
        {
            if (!enableAutoLogin)
            {
                Debug.Log("自动登录已禁用");
                yield break;
            }
            Debug.Log("开始执行自动登录流程...");
            if (!LoadAccountsFromFile())
            {
                Debug.Log("未找到外部账号文件,不执行任何操作");
                yield break;
            }
            float waitTime = 0f;
            float maxWaitTime = 10f;
            while (!CheckUsernameInputFieldExists() && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }
            // 如果等待超时仍未找到用户名输入框
            if (waitTime >= maxWaitTime)
            {
                Debug.LogWarning($"等待输入框超时({maxWaitTime}s),跳过自动登录");
                yield break;
            }
            Debug.Log($"输入框已就绪,耗时: {waitTime}s");

            string currentInputUsername = GetCurrentInputUsername();
            AccountInfo targetAccount = FindNextAccount(currentInputUsername);
            if (targetAccount == null)
            {
                Debug.Log("没有找到可用的下一组账号");
                yield break;
            }
            Debug.Log($"当前输入框: {(string.IsNullOrEmpty(currentInputUsername) ? "空" : currentInputUsername)}");
            Debug.Log($"准备登录账号: {targetAccount.username}");
            PerformLogin(targetAccount.username, targetAccount.password, targetAccount.enabled);
        }

        /// <summary>
        /// 检查登录界面是否存在用户名输入框
        /// </summary>
        /// <returns>如果存在用户名输入框则返回 true,否则返回 false</returns>
        bool CheckUsernameInputFieldExists()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            var allInputs = canvas.GetComponentsInChildren<InputField>();
            return allInputs != null && allInputs.Length >= 2;
        }

        /// <summary>
        /// 获取当前输入框中的用户名
        /// </summary>
        /// <returns>当前输入框中的用户名,如果未找到输入框或输入框为空则返回空字符串</returns>
        string GetCurrentInputUsername()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return "";
            InputField usernameInput = null;
            InputField input;
            foreach (Transform child in canvas.transform)
            {
                if (child.name == "InputField" || child.name == "InputField (Legacy)")
                {
                    //从GameObject中获取InputField组件,如果存在则认为找到了用户名输入框,并获取其中的文本作为当前输入框的用户名
                    input = child.GetComponent<InputField>();
                    if (input != null)
                    {
                        usernameInput = input;
                        break;
                    }
                }
            }
            // 如果没有通过命名找到输入框,则尝试获取Canvas下的所有InputField组件,并使用第一个作为用户名输入框
            if (usernameInput == null)
            {
                usernameInput = canvas.GetComponentInChildren<InputField>();
            }
            // 如果找到了用户名输入框,则返回其中的文本作为当前输入框的用户名,否则返回空字符串
            if (usernameInput != null)
            {
                return usernameInput.text;
            }
            return "";
        }

        /// <summary>
        /// 根据当前输入框的用户名,找到下一组可用的账号信息
        /// </summary>
        /// <param name="currentUsername">当前输入框的用户名</param>
        /// <returns>下一组可用的账号信息,如果没有找到则返回 null</returns>
        AccountInfo FindNextAccount(string currentUsername)
        {
            if (accounts.Count == 0) return null;
            int currentIndex = -1;
            int nextIndex;
            AccountInfo account; // 临时的账号信息变量,用于在循环中存储当前检查的账号信息,以便后续判断是否启用并返回最终的目标账号信息
            if (!string.IsNullOrEmpty(currentUsername))
            {
                for (int i = 0; i < accounts.Count; i++)
                {
                    // 遍历账号列表,找到当前输入框用户名在账号列表中的索引位置,如果找到了则记录索引位置并跳出循环
                    if (accounts[i].username == currentUsername)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
            // 从当前索引位置开始,循环查找下一组可用的账号信息
            for (int i = 1; i < accounts.Count; i++)
            {
                nextIndex = (currentIndex + i) % accounts.Count;
                account = accounts[nextIndex];
                if (account.enabled)
                {
                    return account;
                }
            }
            return null;
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
                            Debug.Log($"读取账号: {username}, 开关: true");
                        }
                        else if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || username.StartsWith("//") ||(enabledStr != "true" && enabledStr != "false"))
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

        /// <summary>
        /// 执行自动登录操作:填充用户名和密码输入框,勾选记住密码,并点击登录按钮
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="doAutoLogin">是否执行自动登录</param>
        void PerformLogin(string username, string password, bool doAutoLogin)
        {
            if (!doAutoLogin) { return; }
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("未找到Canvas对象!");
                return;
            }

            Debug.Log("找到Canvas对象");
            InputField usernameInput = null;
            InputField passwordInput = null;
            Toggle rememberToggle = null;
            Text btnText;
            string buttonLabel;

            var allInputs = canvas.GetComponentsInChildren<InputField>();
            if (allInputs != null && allInputs.Length >= 2)
            {
                usernameInput = allInputs[0];
                passwordInput = allInputs[1];
            }
            if (usernameInput != null)
            {
                usernameInput.text = username;
                Debug.Log($"用户已填入: {username}");
            }
            else
            {
                Debug.LogWarning("未找到用户名输入框!");
            }
            if (passwordInput != null)
            {
                passwordInput.text = password;
                Debug.Log($"密码已填入: {password}");
            }
            else
            {
                Debug.LogWarning("未找到密码输入框!");
            }

            var rememberToggles = canvas.GetComponentsInChildren<Toggle>();
            if (rememberToggles != null && rememberToggles.Length > 0)
            {
                // 打印找到的Toggle组件数量和名称,以便确认哪个是记住密码的Toggle
                Debug.Log($"找到 {rememberToggles.Length} 个Toggle组件,使用第一个");
                rememberToggle = rememberToggles[0];
            }
            if (rememberToggle != null)
            {
                if (!rememberToggle.isOn)
                {
                    rememberToggle.isOn = true;
                    Debug.Log("已勾选记住密码!");
                }
            }
            var allButtons = canvas.GetComponentsInChildren<Button>();
            Debug.Log($"找到 {allButtons.Length} 个按钮");
            foreach (Button button in allButtons)
            {
                //打印每个按钮的名称和文本组件内容,以便确认哪个是登录按钮
                btnText = button.GetComponentInChildren<Text>();
                buttonLabel = btnText != null ? btnText.text : "无文本组件";
                Debug.Log($"按钮名称: {button.name}, 标签: {buttonLabel}");
            }
            if (allButtons != null && allButtons.Length > 0)
            {
                Debug.LogWarning($"使用第一个Button: {allButtons[0].name}"); _monoBehaviour.StartCoroutine(ClickButtonAfterDelay(allButtons[0], autoLoginDelay));
            }
            else
            {
                Debug.LogError("未找到任何Button组件!");
            }
        }

        /// <summary>
        /// 在指定延迟后自动点击登录按钮,并在登录完成后销毁MonoGo实例
        /// </summary>
        /// <param name="button">要点击的按钮</param>
        /// <param name="delay">延迟时间（秒）</param>
        /// <returns></returns>
        System.Collections.IEnumerator ClickButtonAfterDelay(Button button, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log("自动点击登录按钮!");
            button.onClick.Invoke();

            Debug.Log("登录完成,销毁Mono实例及其游戏对象");
            UnityEngine.Object.Destroy(_monoBehaviour.gameObject);
        }
    }
}
