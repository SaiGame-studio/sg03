using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SaiGame.Services;

namespace SG03.UI
{
    public class LoginPanelUI : SaiBehaviour
    {
        public string PanelId => "Login";

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelAsset;

        [Header("References")]
        [SerializeField] private SaiAuth saiAuth;
        [SerializeField] private UIDocument uiDocument;

        [Header("Settings")]
        [SerializeField] private string nextScenes = "1-lobby";
        [SerializeField] private bool autoLoadCredentials = true;

        private TextField usernameField;
        private TextField passwordField;
        private Button loginButton;
        private Label feedbackLabel;
        private VisualElement root;
        private bool authEventsSubscribed;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadSaiAuth();
            this.LoadUIDocument();
        }

        private void LoadSaiAuth()
        {
            if (this.saiAuth != null) return;
            this.saiAuth = this.GetComponentInParent<SaiAuth>();
            Debug.LogWarning(this.transform.name + ": LoadSaiAuth", this.gameObject);
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        protected override void Start()
        {
            base.Start();
            this.InitializeStandalonePanel();
            this.LoadCredentialsFromAuthIfEnabled();
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
            this.loginButton   = root.Q<Button>("LoginButton");

            if (this.loginButton != null)
                this.loginButton.clicked += this.OnLoginButtonClicked;

            this.SubscribeToAuthEvents();
        }

        public void LoadCredentialsFromAuth()
        {
            if (this.usernameField == null || this.saiAuth == null) return;

            this.usernameField.SetValueWithoutNotify(this.saiAuth.GetUsername());
            this.passwordField.SetValueWithoutNotify(this.saiAuth.GetPassword());
        }

        private void SubscribeToAuthEvents()
        {
            if (this.authEventsSubscribed) return;
            if (this.saiAuth == null) return;
            this.saiAuth.OnLoginSuccess += this.HandleLoginSuccess;
            this.saiAuth.OnLoginFailure += this.HandleLoginFailure;
            this.authEventsSubscribed = true;
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (!this.authEventsSubscribed) return;
            if (this.saiAuth == null) return;
            this.saiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.saiAuth.OnLoginFailure -= this.HandleLoginFailure;
            this.authEventsSubscribed = false;
        }

        private void OnLoginButtonClicked()
        {
            this.HideFeedback();
            this.loginButton.SetEnabled(false);

            this.saiAuth?.Login(
                this.usernameField.value,
                this.passwordField.value,
                onSuccess: _ => this.loginButton.SetEnabled(true),
                onError:   _ => this.loginButton.SetEnabled(true));
        }

        private void HandleLoginSuccess(LoginResponse response)
        {
            SceneManager.LoadScene(this.nextScenes);
        }

        private void HandleLoginFailure(string error)
        {
            this.ShowFeedback(error, isError: true);
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
        }
    }
}
