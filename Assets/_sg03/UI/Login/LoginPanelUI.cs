using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SaiGame.Services;
using SaiGame.UI;

namespace SG03.UI
{
    // Login panel for _sg03 — mirrors SaiGame.UI.LoginPanelUI,
    // binds to SaiAuth, no auth logic inside.
    public class LoginPanelUI : UIPanelBase
    {
        public override string PanelId => "Login";

        [Header("References")]
        [SerializeField] private SaiAuth saiAuth;

        [Header("Settings")]
        [SerializeField] private bool autoLoadCredentials = true;

        private TextField usernameField;
        private TextField passwordField;
        private Button loginButton;

        protected override void LoadComponents()
        {
            base.LoadComponents();

            if (this.saiAuth == null)
                this.saiAuth = this.GetComponentInParent<SaiAuth>();
        }

        // Self-initialize when used standalone (UIDocument on same object, no UIRouter).
        protected override void Start()
        {
            base.Start();

            if (this.Root != null) return; // Already initialized by UIRouter

            UIDocument doc = this.GetComponent<UIDocument>();
            if (doc == null) return;

            this.BindFromRoot(doc.rootVisualElement);

            if (this.autoLoadCredentials)
                this.LoadCredentialsFromAuth();
        }

        protected override void OnBindElements(VisualElement root)
        {
            this.BindFromRoot(root);
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

        protected override void OnShow()
        {
            this.LoadCredentialsFromAuth();
            this.HideFeedback();
        }

        // Called by LoginPanelUIEditor button — safe to call at runtime only.
        public void LoadCredentialsFromAuth()
        {
            if (this.usernameField == null || this.saiAuth == null) return;

            this.usernameField.SetValueWithoutNotify(this.saiAuth.GetUsername());
            this.passwordField.SetValueWithoutNotify(this.saiAuth.GetPassword());
        }

        private void SubscribeToAuthEvents()
        {
            if (this.saiAuth == null) return;
            this.saiAuth.OnLoginSuccess += this.HandleLoginSuccess;
            this.saiAuth.OnLoginFailure += this.HandleLoginFailure;
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (this.saiAuth == null) return;
            this.saiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.saiAuth.OnLoginFailure -= this.HandleLoginFailure;
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
            SceneManager.LoadScene("1-desk-mana");
        }

        private void HandleLoginFailure(string error)
        {
            this.ShowFeedback(error, isError: true);
        }

        protected virtual void OnDestroy()
        {
            this.UnsubscribeFromAuthEvents();

            if (this.loginButton != null)
                this.loginButton.clicked -= this.OnLoginButtonClicked;
        }
    }
}
