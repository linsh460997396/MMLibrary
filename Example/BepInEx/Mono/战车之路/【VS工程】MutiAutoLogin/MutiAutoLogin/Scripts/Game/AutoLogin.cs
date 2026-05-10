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
        public bool useDefaultCredentialsWhenNoFile = false;
        public string defaultUsername = "";
        public string defaultPassword = "";
        public float autoLoginDelay = 1f;

        [Header("=== 外部账号文件设置 ===")]
        public string accountFilePath = "BepInEx/plugins/MutiAutoLogin/accounts.txt";

        private MonoBehaviour _monoBehaviour;
        private List<AccountInfo> accounts = new List<AccountInfo>();

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

        public AutoLogin() { }

        public AutoLogin(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        public void SetMonoBehaviour(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        public void Execute()
        {
            if (_monoBehaviour != null)
            {
                _monoBehaviour.StartCoroutine(ExecuteCoroutine());
            }
            else
            {
                Debug.LogError("MonoBehaviour 未设置，无法启动协程");
            }
        }

        System.Collections.IEnumerator ExecuteCoroutine()
        {
            if (!enableAutoLogin)
            {
                Debug.Log("自动登录功能已禁用");
                yield break;
            }

            Debug.Log("开始执行自动登录流程...");

            if (!LoadAccountsFromFile())
            {
                if (useDefaultCredentialsWhenNoFile)
                {
                    Debug.Log("未找到外部账号文件，使用默认账号");
                    PerformLogin(defaultUsername, defaultPassword, true);
                }
                else
                {
                    Debug.Log("未找到外部账号文件，且未启用默认凭据填充，不执行任何操作");
                }
                yield break;
            }

            float waitTime = 0f;
            float maxWaitTime = 10f;
            while (!CheckUsernameInputFieldExists() && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
            }

            if (waitTime >= maxWaitTime)
            {
                Debug.LogWarning($"等待用户名输入框超时({maxWaitTime}s)，跳过自动登录");
                yield break;
            }

            Debug.Log($"用户名输入框已就绪，等待时间: {waitTime}s");

            string currentInputUsername = GetCurrentInputUsername();
            AccountInfo targetAccount = FindNextAccount(currentInputUsername);

            if (targetAccount == null)
            {
                Debug.Log("没有找到可用的下一组账号");
                yield break;
            }

            Debug.Log(
                $"当前输入框用户: {(string.IsNullOrEmpty(currentInputUsername) ? "空" : currentInputUsername)}"
            );
            Debug.Log($"准备登录账号: {targetAccount.username}");

            PerformLogin(targetAccount.username, targetAccount.password, targetAccount.enabled);
        }

        bool CheckUsernameInputFieldExists()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
                return false;

            var allInputs = canvas.GetComponentsInChildren<InputField>();
            return allInputs != null && allInputs.Length >= 2;
        }

        string GetCurrentInputUsername()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
                return "";

            InputField usernameInput = null;
            InputField input;

            foreach (Transform child in canvas.transform)
            {
                if (child.name == "InputField" || child.name == "InputField (Legacy)")
                {
                    input = child.GetComponent<InputField>();
                    if (input != null)
                    {
                        usernameInput = input;
                        break;
                    }
                }
            }

            if (usernameInput == null)
            {
                usernameInput = canvas.GetComponentInChildren<InputField>();
            }

            if (usernameInput != null)
            {
                return usernameInput.text;
            }

            return "";
        }

        AccountInfo FindNextAccount(string currentUsername)
        {
            if (accounts.Count == 0)
                return null;

            int currentIndex = -1;
            int nextIndex;
            AccountInfo account;

            if (!string.IsNullOrEmpty(currentUsername))
            {
                for (int i = 0; i < accounts.Count; i++)
                {
                    if (accounts[i].username == currentUsername)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

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
                string username;
                string password;
                string enabledStr;

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

                        if (
                            string.IsNullOrWhiteSpace(username)
                            || string.IsNullOrWhiteSpace(password)
                        )
                        {
                            Debug.LogWarning($"账号配置格式错误，用户名或密码为空: {line}");
                            continue;
                        }

                        if (enabledStr != "true" && enabledStr != "false")
                        {
                            Debug.LogWarning($"账号配置格式错误，启用状态必须为 true 或 false: {line}");
                            continue;
                        }

                        if (enabledStr == "true")
                        {
                            accounts.Add(new AccountInfo(username, password, true));
                            Debug.Log($"读取账号: {username}, 开关: true");
                        }
                        else
                        {
                            Debug.LogWarning($"账号 {username} 已禁用，跳过");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"账号配置格式错误，需要至少3个部分(username-password-enabled): {line}");
                    }
                }

                if (accounts.Count == 0)
                {
                    Debug.LogWarning("账号文件为空");
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

        void PerformLogin(string username, string password, bool doAutoLogin)
        {
            if (!doAutoLogin)
            {
                Debug.Log("自动登录已禁用");
                return;
            }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("未找到Canvas对象!");
                return;
            }
            Debug.Log("找到Canvas对象");

            InputField usernameInput = null;
            InputField passwordInput = null;
            Button loginButton = null;
            Toggle rememberToggle = null;
            InputField input;
            Toggle toggle;
            Button btn;
            Text btnText;
            string buttonLabel;

            foreach (Transform child in canvas.transform)
            {
                if (child.name == "InputField" || child.name == "InputField (Legacy)")
                {
                    input = child.GetComponent<InputField>();
                    if (input != null && usernameInput == null)
                    {
                        usernameInput = input;
                    }
                }
                else if (child.name == "InputField (1)" || child.name == "InputField (Legacy) (1)")
                {
                    input = child.GetComponent<InputField>();
                    if (input != null)
                    {
                        passwordInput = input;
                    }
                }
                else if (child.name.StartsWith("Toggle"))
                {
                    toggle = child.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        rememberToggle = toggle;
                    }
                }
                else if (child.name.StartsWith("Button"))
                {
                    btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        btnText = child.GetComponentInChildren<Text>();
                        buttonLabel = btnText != null ? btnText.text : "";

                        if (
                            buttonLabel.Contains("登录")
                            || buttonLabel.Contains("登陆")
                            || buttonLabel.Contains("Login")
                        )
                        {
                            Debug.Log($"找到登录按钮: {child.name} - {buttonLabel}");
                            loginButton = btn;
                        }
                    }
                }
            }

            if (usernameInput == null)
            {
                var allInputs = canvas.GetComponentsInChildren<InputField>();
                if (allInputs != null && allInputs.Length >= 2)
                {
                    usernameInput = allInputs[0];
                    passwordInput = allInputs[1];
                }
            }

            if (usernameInput != null)
            {
                usernameInput.text = username;
                Debug.Log($"用户名已填入: {username}");
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

            if (rememberToggle != null)
            {
                if (!rememberToggle.isOn)
                {
                    rememberToggle.isOn = true;
                    Debug.Log("已勾选记住密码");
                }
            }

            if (loginButton != null)
            {
                Debug.Log($"找到登录按钮，将在 {autoLoginDelay} 秒后点击");
                _monoBehaviour.StartCoroutine(ClickButtonAfterDelay(loginButton, autoLoginDelay));
            }
            else
            {
                var allButtons = canvas.GetComponentsInChildren<Button>();
                if (allButtons != null && allButtons.Length > 0)
                {
                    Debug.LogWarning($"使用第一个Button: {allButtons[0].name}");
                    _monoBehaviour.StartCoroutine(
                        ClickButtonAfterDelay(allButtons[0], autoLoginDelay)
                    );
                }
                else
                {
                    Debug.LogError("未找到任何Button组件!");
                }
            }
        }

        System.Collections.IEnumerator ClickButtonAfterDelay(Button button, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log("自动点击登录按钮!");
            button.onClick.Invoke();

            Debug.Log("登录完成，销毁MonoGo实例");
            UnityEngine.Object.Destroy(_monoBehaviour.gameObject);
        }
    }
}
