using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    /// <summary>Drives the world-space ATK value displayed at the head of a card.</summary>
    [AddComponentMenu("SG03/BattleState/World Space ATK UI")]
    public sealed class WorldSpaceAtkCtrl : PoolObj
    {
        [Header("Required runtime references")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Display")]
        [SerializeField] private bool faceMainCamera = true;
        [Tooltip("Distance beyond the card's top edge where the ATK UI is displayed.")]
        [SerializeField, Min(0f)] private float aboveCardYOffset = 0.5f;
        [Tooltip("Global Z-axis offset relative to the card's top edge.")]
        [SerializeField] private float cardZOffset = -1.5f;

        private Transform parent;
        private Label attackLabel;
        private Vector3 baseWorldRotation;
        private bool hasBaseWorldRotation;
        private Coroutine deferredUiRefreshRoutine;
        private int attack;

        private void LateUpdate()
        {
            this.UpdateWorldSpacePresentation();
        }

        private void OnEnable()
        {
            this.HandleEnabled();
        }

        private void OnDisable()
        {
            this.HandleDisabled();
        }

        protected override void Start()
        {
            this.InitializeAtkUi();
        }

        private void InitializeAtkUi()
        {
            this.RefreshUi();
        }

        private void HandleEnabled()
        {
            this.RefreshUi();
            this.RefreshUiWhenDocumentIsReady();
        }

        private void HandleDisabled()
        {
            this.deferredUiRefreshRoutine = null;
        }

        private void RefreshUiWhenDocumentIsReady()
        {
            if (this.deferredUiRefreshRoutine != null) return;
            this.deferredUiRefreshRoutine = this.StartCoroutine(this.RefreshUiWhenDocumentIsReadyRoutine());
        }

        private IEnumerator RefreshUiWhenDocumentIsReadyRoutine()
        {
            yield return null;
            this.deferredUiRefreshRoutine = null;
            if (!this.isActiveAndEnabled) yield break;
            this.RefreshUi();
        }

        public override string GetName()
        {
            return nameof(WorldSpaceAtkCtrl);
        }

        /// <summary>Sets the world-space position before this UI is assigned a card.</summary>
        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }

        /// <summary>Assigns the card this UI follows without inheriting its transform.</summary>
        public void SetParent(Transform newParent)
        {
            if (newParent == null) return;

            this.parent = newParent;
            this.transform.SetParent(null, true);
            this.UpdateWorldPositionFromParent();
            this.baseWorldRotation = this.transform.eulerAngles;
            this.hasBaseWorldRotation = true;
        }

        /// <summary>Sets the ATK value currently shown by this UI.</summary>
        public void SetAttack(int value)
        {
            this.attack = Mathf.Max(0, value);
            this.RefreshUi();
        }

        private void UpdateWorldSpacePresentation()
        {
            this.UpdateWorldPositionFromParent();
            this.FaceMainCamera();
        }

        private void UpdateWorldPositionFromParent()
        {
            if (this.parent == null) return;

            Vector3 offset = new Vector3(0f, this.aboveCardYOffset, this.cardZOffset);

            Card3D card = this.parent.GetComponent<Card3D>();
            if (card != null && card.TryGetTopEdgeWorldPosition(out Vector3 topEdge))
            {
                this.transform.position = topEdge + offset;
                return;
            }

            this.transform.position = this.parent.position + offset;
        }

        private void FaceMainCamera()
        {
            if (!this.faceMainCamera || Camera.main == null) return;

            Vector3 directionFromCamera = this.transform.position - Camera.main.transform.position;
            if (directionFromCamera.sqrMagnitude < Mathf.Epsilon) return;

            float horizontalDistance = new Vector2(directionFromCamera.x, directionFromCamera.z).magnitude;
            Vector3 worldRotation = this.hasBaseWorldRotation
                ? this.baseWorldRotation
                : this.transform.eulerAngles;
            worldRotation.x = -Mathf.Atan2(directionFromCamera.y, horizontalDistance) * Mathf.Rad2Deg;
            this.transform.rotation = Quaternion.Euler(worldRotation);
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadUiDocument();
            this.BindUi();
        }

        private void LoadUiDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
        }

        private void BindUi()
        {
            if (this.uiDocument == null)
            {
                Debug.LogWarning($"{this.name}: UIDocument is missing.", this.gameObject);
                return;
            }

            VisualElement root = this.uiDocument.rootVisualElement;
            if (root == null) return;

            this.attackLabel = root.Q<Label>("AttackLabel");
            if (this.attackLabel == null)
                Debug.LogWarning($"{this.name}: UI element 'AttackLabel' is missing.", this.gameObject);
        }

        public void RefreshUi()
        {
            this.BindUi();
            if (this.attackLabel != null) this.attackLabel.text = this.attack.ToString();
        }

        // This UI is returned explicitly through ObjectPool, so it needs no Despawn component.
        protected override void LoadDespawn()
        {
        }
    }
}
