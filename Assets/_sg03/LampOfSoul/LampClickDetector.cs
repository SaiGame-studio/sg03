using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    [AddComponentMenu("SG03/LampOfSoul/Lamp Click Detector")]
    public class LampClickDetector : SaiBehaviour
    {
        // ─── Linked Components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private BattleStateCtrl battleStateCtrl;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private BattleScripts battleScripts;
        [SerializeField] private LampOfSoulCtrl lampOfSoulCtrl;

        [Header("Hover Outline")]
        [SerializeField] private MeshRenderer cylinderRenderer;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private bool isHovered = false;

        private Material[] originalMaterials;
        private Material[] hoveredMaterials;
        private bool isOutlineShowing = false;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleStateCtrl();
            this.LoadMainCamera();
            this.LoadBattleScripts();
            this.LoadCylinderRenderer();
            this.LoadLampOfSoulCtrl();
#if UNITY_EDITOR
            this.LoadOutlineMaterial();
#endif
        }

        protected virtual void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = Object.FindFirstObjectByType<BattleStateCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        protected virtual void LoadMainCamera()
        {
            if (this.mainCamera != null) return;
            this.mainCamera = Camera.main;
            Debug.LogWarning(this.transform.name + ": LoadMainCamera", this.gameObject);
        }

        protected virtual void LoadBattleScripts()
        {
            if (this.battleScripts != null) return;
            this.battleScripts = Object.FindFirstObjectByType<BattleScripts>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleScripts", this.gameObject);
        }

        protected virtual void LoadLampOfSoulCtrl()
        {
            if (this.lampOfSoulCtrl != null) return;
            this.lampOfSoulCtrl = Object.FindFirstObjectByType<LampOfSoulCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadLampOfSoulCtrl", this.gameObject);
        }

        protected virtual void LoadCylinderRenderer()
        {
            if (this.cylinderRenderer != null) return;

            Transform cylinderTransform = this.transform.Find("Cylinder");
            if (cylinderTransform != null)
            {
                this.cylinderRenderer = cylinderTransform.GetComponent<MeshRenderer>();
            }

            if (this.cylinderRenderer == null)
            {
                this.cylinderRenderer = this.GetComponentInChildren<MeshRenderer>(true);
            }

            if (this.cylinderRenderer != null)
            {
                Debug.LogWarning(this.transform.name + ": LoadCylinderRenderer found " + this.cylinderRenderer.name, this.gameObject);
            }
        }

#if UNITY_EDITOR
        protected virtual void LoadOutlineMaterial()
        {
            if (this.outlineMaterial != null) return;
            string[] guids = UnityEditor.AssetDatabase.FindAssets("CylinderURPOutline t:Material");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                this.outlineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                Debug.LogWarning(this.transform.name + ": LoadOutlineMaterial assigned from path " + path, this.gameObject);
            }
        }
#endif

        protected override void Start()
        {
            base.Start();
            this.InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            if (this.cylinderRenderer == null) return;

            Material[] currentShared = this.cylinderRenderer.sharedMaterials;
            if (currentShared == null || currentShared.Length == 0) return;

            // Cache original materials (slot 0 only)
            this.originalMaterials = new Material[] { currentShared[0] };

            // Check if outline material is already in slot 1
            if (currentShared.Length > 1)
            {
                this.hoveredMaterials = currentShared;
            }
            else if (this.outlineMaterial != null)
            {
                // Otherwise, append our outline material to slot 1
                this.hoveredMaterials = new Material[] { currentShared[0], this.outlineMaterial };
            }
            else
            {
                this.hoveredMaterials = this.originalMaterials;
            }

            // Start in the non-hovered state
            this.cylinderRenderer.sharedMaterials = this.originalMaterials;
        }

        protected override void ResetValue()
        {
            base.ResetValue();
            this.battleStateCtrl = null;
            this.mainCamera = null;
            this.battleScripts = null;
            this.cylinderRenderer = null;
            this.outlineMaterial = null;
            this.lampOfSoulCtrl = null;

            // Force re-load components with cleared fields to automate configuration
            this.LoadComponents();
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnDisable()
        {
            this.SetHover(false);
        }

        private void Update()
        {
            this.DetectHover();
            this.DetectClick();
        }

        // ─── Detection ───────────────────────────────────────────────────────────

        private void DetectClick()
        {
            if (!this.IsMouseButtonPressed()) return;
            if (!this.IsLampHit()) return;
            this.OnLampClicked();
        }

        private bool IsMouseButtonPressed()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private bool IsLampHit()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = this.mainCamera.ScreenPointToRay(mousePos);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == this.gameObject) return true;
            }
            return false;
        }

        // ─── Private helpers ─────────────────────────────────────────────────────

        private void OnLampClicked()
        {
            this.LogLampClicked();
            this.DispatchByNextMove();
        }

        private void DispatchByNextMove()
        {
            if (this.battleStateCtrl?.BattleState == null) return;
            NextMoveType nextMove = this.battleStateCtrl.BattleState.NextMove;
            if (nextMove == NextMoveType.card_deploy) this.HandleCardDeploy();
            if (nextMove == NextMoveType.alpha_turn)  this.HandleAlphaTurnEnd();
            if (nextMove == NextMoveType.omega_turn)  this.HandleAlphaDefendingEnd();
        }

        private void HandleAlphaTurnEnd()
        {
            if (this.battleScripts == null) return;
            this.battleScripts.RunAlphaTurnEnd(this.OnAlphaTurnEndSuccess, this.OnAlphaTurnEndError);
        }

        private void HandleAlphaDefendingEnd()
        {
            if (this.battleScripts == null) return;
            this.battleScripts.RunAlphaDefendingEnd(this.OnAlphaDefendingEndSuccess, this.OnAlphaDefendingEndError);
        }

        private void HandleCardDeploy()
        {
            if (this.battleScripts == null) return;
            this.battleScripts.RunCardDeploy(this.OnCardDeploySuccess, this.OnCardDeployError);
        }

        private void OnAlphaTurnEndSuccess(string response)
        {
            Debug.Log("<color=#FFAA33><b>[LampClickDetector] Alpha turn end success</b></color> " + response);
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void OnAlphaTurnEndError(string error)
        {
            Debug.LogError("[LampClickDetector] Alpha turn end error: " + error);
        }

        private void OnAlphaDefendingEndSuccess(string response)
        {
            Debug.Log("<color=#88CCFF><b>[LampClickDetector] Alpha defending end success</b></color> " + response);
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void OnAlphaDefendingEndError(string error)
        {
            Debug.LogError("[LampClickDetector] Alpha defending end error: " + error);
        }

        private void OnCardDeploySuccess(string response)
        {
            Debug.Log("<color=#FF88FF><b>[LampClickDetector] Card deploy success</b></color> " + response);
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void OnCardDeployError(string error)
        {
            Debug.LogError("[LampClickDetector] Card deploy error: " + error);
        }

        private void LogLampClicked()
        {
            int turn   = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.Turn   : 0;
            int action = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.Action : 0;
            string nextMove = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.NextMove.ToString() : "";
            Debug.Log($"<color=#FFD700><b>[LampClickDetector] Lamp clicked — Turn={turn}, Action={action}, nextMove={nextMove}</b></color>");
        }

        // ─── Hover detection ──────────────────────────────────────────────────────

        private void DetectHover()
        {
            if (Mouse.current == null) return;
            bool isHit = this.IsLampHit();
            if (isHit != this.isHovered)
            {
                this.SetHover(isHit);
            }
            else if (this.isHovered)
            {
                this.UpdateOutlineState();
            }
        }

        private void SetHover(bool hover)
        {
            this.isHovered = hover;
            this.UpdateOutlineState();
        }

        private void UpdateOutlineState()
        {
            if (this.cylinderRenderer == null) return;
            if (this.originalMaterials == null || this.hoveredMaterials == null) return;

            bool shouldShow = this.isHovered && this.CanShowOutline();
            if (shouldShow != this.isOutlineShowing)
            {
                this.isOutlineShowing = shouldShow;
                this.cylinderRenderer.sharedMaterials = shouldShow ? this.hoveredMaterials : this.originalMaterials;
            }
        }

        private bool CanShowOutline()
        {
            // Do not show outline if client actions are resuming/fast-forwarding
            if (this.battleStateCtrl != null && this.battleStateCtrl.ClientActions != null)
            {
                if (this.battleStateCtrl.ClientActions.IsResuming) return false;
            }

            // Only allow outline if the lamp is at Alpha's position and finished moving
            if (this.lampOfSoulCtrl != null)
            {
                return this.lampOfSoulCtrl.IsAtAlpha && !this.lampOfSoulCtrl.IsAnimating;
            }

            return false;
        }
    }
}
