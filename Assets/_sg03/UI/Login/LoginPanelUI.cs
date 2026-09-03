using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SaiGame.Services;
using SG03.UI.Components;

namespace SG03.UI
{
    public class LoginPanelUI : SaiBehaviour
    {
        [System.Serializable]
        private class GameInfoResponse
        {
            public string name;
            public string game_name;
            public GameInfoData game;
            public GameInfoData data;
        }

        [System.Serializable]
        private class GameInfoData
        {
            public string name;
        }

        public string PanelId => "Login";

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelAsset;

        [Header("References")]
        [SerializeField] private SaiAuth saiAuth;
        [SerializeField] private GoogleBackendLogin googleBackendLogin;
        [SerializeField] private UIDocument uiDocument;

        [Header("Settings")]
        [SerializeField] private string nextScenes = "1-lobby";
        [SerializeField] private bool autoLoadCredentials = true;

        private const string AutoLoginPreferenceKey = "SG03.Login.AutoLogin";
        private const string AutoLoginUsernameKey = "SG03.Login.AutoLogin.Username";
        private const string AutoLoginPasswordKey = "SG03.Login.AutoLogin.Password";

        private TextField usernameField;
        private TextField passwordField;
        private TextField confirmPasswordField;
        private TextField registerEmailField;
        private Button loginButton;
        private Button googleLoginButton;
        private Button authModeButton;
        private Button passwordVisibilityButton;
        private Button confirmPasswordVisibilityButton;
        private Toggle autoLoginToggle;
        private Label feedbackLabel;
        private Label titleLabel;
        private Label gameNameLabel;
        private VisualElement registerFields;
        private VisualElement confirmPasswordContainer;
        private VisualElement googleLoginContainer;
        private VisualElement root;
        private bool authEventsSubscribed;
        private bool isRegisterMode;
        private string pendingAutoLoginUsername;
        private string pendingAutoLoginPassword;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadSaiAuth();
            this.LoadGoogleBackendLogin();
            this.LoadUIDocument();
        }

        private void LoadSaiAuth()
        {
            // SaiServer persists across scene changes. Do not keep the serialized
            // scene reference because it can point to the duplicate SaiServer that
            // Unity destroys when returning to the login scene.
            SaiAuth activeAuth = SaiServer.Instance?.SaiAuth;
            if (activeAuth != null)
            {
                this.saiAuth = activeAuth;
                return;
            }

            if (this.saiAuth != null) return;
            this.saiAuth = FindFirstObjectByType<SaiAuth>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadSaiAuth", this.gameObject);
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        private void LoadGoogleBackendLogin()
        {
            GoogleBackendLogin activeGoogleLogin = SaiServer.Instance?.GetComponent<GoogleBackendLogin>();
            if (activeGoogleLogin != null)
            {
                this.googleBackendLogin = activeGoogleLogin;
                return;
            }

            if (this.googleBackendLogin != null) return;
            this.googleBackendLogin = FindFirstObjectByType<GoogleBackendLogin>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadGoogleBackendLogin", this.gameObject);
        }

        protected override void Start()
        {
            base.Start();
            this.InitializeStandalonePanel();
            this.LoadCredentialsFromAuthIfEnabled();
            this.TryAutoLoginFromSavedCredentials();
            this.LoadGameName();
        }

        private void InitializeStandalonePanel()
        {
            if (this.root != null) return;
            if (this.uiDocument == null) return;
            this.BindPanelRoot(this.uiDocument.rootVisualElement);
        }

        private void LoadCredentialsFromAuthIfEnabled()
        {
            if (!this.autoLoadCredentials) return;
            this.LoadCredentialsFromAuth();
        }

        private void BindPanelRoot(VisualElement panelRoot)
        {
            if (panelRoot == null) return;
            this.root = panelRoot;
            this.feedbackLabel = this.root.Q<Label>("MessageLabel");
            this.BindFromRoot(this.root);
        }

        private void BindFromRoot(VisualElement root)
        {
            this.usernameField = root.Q<TextField>("UsernameField");
            this.passwordField = root.Q<TextField>("PasswordField");
            this.confirmPasswordField = root.Q<TextField>("ConfirmPasswordField");
            this.registerEmailField = root.Q<TextField>("RegisterEmailField");
            this.loginButton = root.Q<Button>("LoginButton");
            this.googleLoginButton = root.Q<Button>("GoogleLoginButton");
            this.authModeButton = root.Q<Button>("AuthModeButton");
            this.passwordVisibilityButton = root.Q<Button>("PasswordVisibilityButton");
            this.confirmPasswordVisibilityButton = root.Q<Button>("ConfirmPasswordVisibilityButton");
            this.autoLoginToggle = root.Q<Toggle>("AutoLoginToggle");
            this.titleLabel = root.Q<Label>("TitleLabel");
            this.gameNameLabel = root.Q<Label>("GameNameLabel");
            this.registerFields = root.Q<VisualElement>("RegisterFields");
            this.confirmPasswordContainer = root.Q<VisualElement>("ConfirmPasswordContainer");
            this.googleLoginContainer = root.Q<VisualElement>("GoogleLoginContainer");

            if (this.loginButton != null)
                this.loginButton.clicked += this.OnLoginButtonClicked;

            if (this.googleLoginButton != null)
                this.googleLoginButton.clicked += this.OnGoogleLoginButtonClicked;

            if (this.authModeButton != null)
                this.authModeButton.clicked += this.OnAuthModeButtonClicked;

            if (this.passwordVisibilityButton != null)
                this.passwordVisibilityButton.clicked += this.TogglePasswordVisibility;

            if (this.confirmPasswordVisibilityButton != null)
                this.confirmPasswordVisibilityButton.clicked += this.ToggleConfirmPasswordVisibility;

            if (this.autoLoginToggle != null)
                this.autoLoginToggle.RegisterValueChangedCallback(this.OnAutoLoginToggleChanged);

            this.RefreshAuthMode();
            this.SubscribeToAuthEvents();
            this.RefreshAutoLoginToggle();
        }

        private void LoadGameName()
        {
            SaiServer server = SaiServer.Instance;
            if (server == null || string.IsNullOrWhiteSpace(server.GameId)) return;

            string endpoint = $"/api/v1/public/games/{server.GameId}/info";
            server.StartCoroutine(server.GetRequest(
                endpoint,
                response =>
                {
                    try
                    {
                        GameInfoResponse gameInfo = JsonUtility.FromJson<GameInfoResponse>(response);
                        string gameName = gameInfo?.game_name
                            ?? gameInfo?.game?.name
                            ?? gameInfo?.data?.name
                            ?? gameInfo?.name;
                        if (this.gameNameLabel != null && !string.IsNullOrWhiteSpace(gameName))
                            this.gameNameLabel.text = gameName;
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogWarning($"Unable to read game info: {exception.Message}", this.gameObject);
                    }
                },
                error => Debug.LogWarning($"Unable to load game info: {error}", this.gameObject)));
        }

        public void LoadCredentialsFromAuth()
        {
            if (this.usernameField == null || this.saiAuth == null) return;

            this.usernameField.SetValueWithoutNotify(this.saiAuth.GetUsername());
            this.passwordField?.SetValueWithoutNotify(string.Empty);
        }

        private void SubscribeToAuthEvents()
        {
            if (this.authEventsSubscribed) return;
            if (this.saiAuth == null) return;
            this.saiAuth.OnLoginSuccess += this.HandleLoginSuccess;
            this.saiAuth.OnLoginFailure += this.HandleLoginFailure;
            this.authEventsSubscribed = true;

            if (this.googleBackendLogin == null) return;
            this.googleBackendLogin.OnLoginSuccess += this.HandleGoogleLoginSuccess;
            this.googleBackendLogin.OnLoginFailure += this.HandleGoogleLoginFailure;
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (!this.authEventsSubscribed) return;
            if (this.saiAuth == null) return;
            this.saiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.saiAuth.OnLoginFailure -= this.HandleLoginFailure;
            this.authEventsSubscribed = false;

            if (this.googleBackendLogin == null) return;
            this.googleBackendLogin.OnLoginSuccess -= this.HandleGoogleLoginSuccess;
            this.googleBackendLogin.OnLoginFailure -= this.HandleGoogleLoginFailure;
        }

        private void OnLoginButtonClicked()
        {
            this.HideFeedback();
            this.loginButton.SetEnabled(false);

            if (this.isRegisterMode)
            {
                this.Register();
                return;
            }

            this.StartPasswordLogin(
                this.usernameField.value,
                this.passwordField.value,
                onSuccess: _ => this.loginButton.SetEnabled(true),
                onError:   _ => this.loginButton.SetEnabled(true));
        }

        private void StartPasswordLogin(string username, string password,
            System.Action<LoginResponse> onSuccess, System.Action<string> onError)
        {
            if (this.saiAuth == null) return;

            this.pendingAutoLoginUsername = username;
            this.pendingAutoLoginPassword = password;
            this.saiAuth.SetAutoLogin(this.IsAutoLoginEnabled());
            this.saiAuth.Login(username, password, onSuccess, onError);
        }

        private void OnGoogleLoginButtonClicked()
        {
            this.HideFeedback();

            if (this.googleBackendLogin == null)
            {
                this.ShowFeedback("Google login is unavailable.", isError: true);
                return;
            }

            if (this.googleBackendLogin.IsLoggingIn)
            {
                this.googleBackendLogin.CancelLogin();
                return;
            }

            this.SetLoginActionsEnabled(false);
            this.googleLoginButton?.SetEnabled(true);
            this.googleLoginButton.text = "Cancel Google Login";
            this.ShowFeedback("Continue sign-in with Google in your browser.", isError: false);
            this.googleBackendLogin.StartLogin();
        }

        private void Register()
        {
            if (this.saiAuth == null)
            {
                this.loginButton.SetEnabled(true);
                this.ShowFeedback("SaiServer not found.", isError: true);
                return;
            }

            if (this.passwordField.value != this.confirmPasswordField?.value)
            {
                this.loginButton.SetEnabled(true);
                this.ShowFeedback("Passwords do not match.", isError: true);
                return;
            }

            string registeredUsername = this.usernameField.value;

            this.saiAuth.Register(
                this.registerEmailField.value,
                registeredUsername,
                this.passwordField.value,
                onSuccess: _ =>
                {
                    this.loginButton.SetEnabled(true);
                    this.isRegisterMode = false;
                    this.usernameField.SetValueWithoutNotify(registeredUsername);
                    this.passwordField.SetValueWithoutNotify(string.Empty);
                    this.confirmPasswordField?.SetValueWithoutNotify(string.Empty);
                    this.registerEmailField?.SetValueWithoutNotify(string.Empty);
                    this.RefreshAuthMode();
                    this.ShowFeedback("Registration successful. Please log in.", isError: false);
                },
                onError: error =>
                {
                    this.loginButton.SetEnabled(true);
                    this.ShowFeedback(error, isError: true);
                });
        }

        private void OnAuthModeButtonClicked()
        {
            this.isRegisterMode = !this.isRegisterMode;
            this.passwordField?.SetValueWithoutNotify(string.Empty);
            this.confirmPasswordField?.SetValueWithoutNotify(string.Empty);

            this.HideFeedback();
            this.RefreshAuthMode();
        }

        private void TogglePasswordVisibility()
        {
            this.TogglePasswordVisibility(this.passwordField, this.passwordVisibilityButton);
        }

        private void ToggleConfirmPasswordVisibility()
        {
            this.TogglePasswordVisibility(this.confirmPasswordField, this.confirmPasswordVisibilityButton);
        }

        private void TogglePasswordVisibility(TextField field, Button button)
        {
            if (field == null || button == null) return;

            field.isPasswordField = !field.isPasswordField;
            bool isPasswordHidden = field.isPasswordField;
            button.tooltip = isPasswordHidden ? "Show password" : "Hide password";
        }

        private void RefreshAuthMode()
        {
            if (this.registerFields != null)
                this.registerFields.style.display = this.isRegisterMode ? DisplayStyle.Flex : DisplayStyle.None;

            if (this.confirmPasswordContainer != null)
                this.confirmPasswordContainer.style.display = this.isRegisterMode ? DisplayStyle.Flex : DisplayStyle.None;

            if (this.autoLoginToggle != null)
                this.autoLoginToggle.style.display = this.isRegisterMode ? DisplayStyle.None : DisplayStyle.Flex;

            if (this.googleLoginContainer != null)
                this.googleLoginContainer.style.display = this.isRegisterMode ? DisplayStyle.None : DisplayStyle.Flex;

            if (this.titleLabel != null)
                this.titleLabel.text = this.isRegisterMode ? "Create Account" : "Login";

            if (this.usernameField != null)
            {
                this.usernameField.label = string.Empty;
                this.usernameField.textEdition.placeholder = this.isRegisterMode
                    ? "Choose a username"
                    : "Enter username or email";
            }

            if (this.loginButton != null)
                this.loginButton.text = this.isRegisterMode ? "Create Account" : "Log In";

            if (this.authModeButton != null)
                this.authModeButton.text = this.isRegisterMode ? "Back to login" : "Create an account";
        }

        private void HandleLoginSuccess(LoginResponse response)
        {
            this.SaveAutoLoginCredentialsIfEnabled();
            this.EnsurePlayerProfile();
            SceneManager.LoadScene(this.nextScenes);
        }

        private void RefreshAutoLoginToggle()
        {
            if (this.autoLoginToggle == null) return;

            bool isAutoLoginEnabled = PlayerPrefs.GetInt(
                AutoLoginPreferenceKey,
                this.saiAuth != null && this.saiAuth.GetAutoLogin() ? 1 : 0) == 1;
            this.autoLoginToggle.SetValueWithoutNotify(isAutoLoginEnabled);
            this.saiAuth?.SetAutoLogin(isAutoLoginEnabled);
        }

        private void OnAutoLoginToggleChanged(ChangeEvent<bool> changeEvent)
        {
            this.saiAuth?.SetAutoLogin(changeEvent.newValue);
            PlayerPrefs.SetInt(AutoLoginPreferenceKey, changeEvent.newValue ? 1 : 0);

            if (!changeEvent.newValue)
            {
                PlayerPrefs.DeleteKey(AutoLoginUsernameKey);
                PlayerPrefs.DeleteKey(AutoLoginPasswordKey);
            }

            PlayerPrefs.Save();
        }

        private void TryAutoLoginFromSavedCredentials()
        {
            if (!this.IsAutoLoginEnabled() || this.isRegisterMode) return;

            string username = PlayerPrefs.GetString(AutoLoginUsernameKey, string.Empty);
            string password = PlayerPrefs.GetString(AutoLoginPasswordKey, string.Empty);
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return;

            this.SetLoginActionsEnabled(false);
            this.ShowFeedback("Signing in...", isError: false);
            this.StartPasswordLogin(
                username,
                password,
                onSuccess: _ => { },
                onError: _ => this.SetLoginActionsEnabled(true));
        }

        private bool IsAutoLoginEnabled()
        {
            return this.autoLoginToggle != null && this.autoLoginToggle.value;
        }

        private void SaveAutoLoginCredentialsIfEnabled()
        {
            if (!this.IsAutoLoginEnabled()) return;
            if (string.IsNullOrWhiteSpace(this.pendingAutoLoginUsername)
                || string.IsNullOrWhiteSpace(this.pendingAutoLoginPassword)) return;

            PlayerPrefs.SetString(AutoLoginUsernameKey, this.pendingAutoLoginUsername);
            PlayerPrefs.SetString(AutoLoginPasswordKey, this.pendingAutoLoginPassword);
            PlayerPrefs.Save();
        }

        private void EnsurePlayerProfile()
        {
            GamerProgress gamerProgress = SaiServer.Instance?.GamerProgress;
            if (gamerProgress == null) return;

            gamerProgress.GetProgress(
                onSuccess: _ => { },
                onError: error =>
                {
                    if (!IsMissingPlayerProfileError(error)) return;

                    gamerProgress.CreateProgress(
                        onSuccess: _ => { },
                        onError: _ => { });
                });
        }

        private static bool IsMissingPlayerProfileError(string error)
        {
            if (string.IsNullOrEmpty(error)) return false;

            string normalizedError = error.ToLowerInvariant();
            bool isProfileError = normalizedError.Contains("profile")
                || normalizedError.Contains("gamer progress");
            bool isNotFound = normalizedError.Contains("response code: 404");

            return isProfileError && isNotFound;
        }

        private void HandleLoginFailure(string error)
        {
            this.pendingAutoLoginUsername = string.Empty;
            this.pendingAutoLoginPassword = string.Empty;
            this.HideFeedback();
            ToastMessage.ShowError(this.FormatLoginError(error), this.loginButton);
        }

        private void HandleGoogleLoginSuccess(LoginResponse response)
        {
            this.SetLoginActionsEnabled(true);
            this.HandleLoginSuccess(response);
        }

        private void HandleGoogleLoginFailure(string error)
        {
            this.SetLoginActionsEnabled(true);
            string message = error == "cancelled"
                ? "Google sign-in cancelled. You can try again."
                : "Unable to sign in with Google. Please try again.";
            this.ShowFeedback(message, isError: error != "cancelled");
        }

        private void SetLoginActionsEnabled(bool isEnabled)
        {
            this.loginButton?.SetEnabled(isEnabled);
            this.googleLoginButton?.SetEnabled(isEnabled);
            this.authModeButton?.SetEnabled(isEnabled);

            if (isEnabled && this.googleLoginButton != null)
                this.googleLoginButton.text = "Sign in with Google";
        }

        private string FormatLoginError(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return "Unable to log in. Please try again.";

            string normalizedError = error.ToLowerInvariant();
            if (normalizedError.Contains("invalid credential")
                || normalizedError.Contains("unauthorized")
                || normalizedError.Contains("401"))
                return "Invalid username or password.";

            return "Unable to log in. Please try again.";
        }

        private void ShowFeedback(string message, bool isError)
        {
            if (this.feedbackLabel == null) return;

            this.feedbackLabel.text = message;
            this.feedbackLabel.RemoveFromClassList("feedback--error");
            this.feedbackLabel.RemoveFromClassList("feedback--success");
            this.feedbackLabel.AddToClassList(isError ? "feedback--error" : "feedback--success");
        }

        private void HideFeedback()
        {
            if (this.feedbackLabel == null) return;

            this.feedbackLabel.text = string.Empty;
            this.feedbackLabel.RemoveFromClassList("feedback--error");
            this.feedbackLabel.RemoveFromClassList("feedback--success");
        }

        protected virtual void OnDestroy()
        {
            this.UnsubscribeFromAuthEvents();
            this.UnregisterLoginButtonClicked();
        }

        private void UnregisterLoginButtonClicked()
        {
            if (this.loginButton != null)
                this.loginButton.clicked -= this.OnLoginButtonClicked;

            if (this.googleLoginButton != null)
                this.googleLoginButton.clicked -= this.OnGoogleLoginButtonClicked;

            if (this.authModeButton != null)
                this.authModeButton.clicked -= this.OnAuthModeButtonClicked;

            if (this.passwordVisibilityButton != null)
                this.passwordVisibilityButton.clicked -= this.TogglePasswordVisibility;

            if (this.confirmPasswordVisibilityButton != null)
                this.confirmPasswordVisibilityButton.clicked -= this.ToggleConfirmPasswordVisibility;

            if (this.autoLoginToggle != null)
                this.autoLoginToggle.UnregisterValueChangedCallback(this.OnAutoLoginToggleChanged);
        }
    }
}
