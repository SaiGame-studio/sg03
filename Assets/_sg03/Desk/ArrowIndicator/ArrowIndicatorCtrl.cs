using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Renders a targeting arrow from a source position to a destination position.
    /// Requires two child GameObjects named "LineBody" and "LineHead",
    /// each carrying a <see cref="LineRenderer"/> component.
    /// Use <see cref="Show"/> to start drawing, <see cref="UpdateTarget"/> to
    /// track the cursor or a hovered card, and <see cref="Hide"/> to dismiss.
    /// </summary>
    [AddComponentMenu("SG03/Battle/Arrow Indicator Ctrl")]
    public class ArrowIndicatorCtrl : SaiBehaviour
    {
        [Header("Line Renderers")]
        [SerializeField] private LineRenderer lineBody;
        [SerializeField] private LineRenderer lineHead;

        [Header("Arrow Head Settings")]
        [SerializeField] private float headLength = 2f;
        [SerializeField] private float headAngle  = 40f;

        [Header("Material")]
        [SerializeField] private Material arrowMaterial;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadArrowMaterial();
            this.LoadLineBody();
            this.LoadLineHead();
        }

        protected virtual void LoadArrowMaterial()
        {
            if (this.arrowMaterial != null) return;
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("ArrowIndicatorMat t:Material");
            if (guids.Length == 0) return;
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            this.arrowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (this.arrowMaterial == null) return;
            Debug.LogWarning(this.transform.name + ": LoadArrowMaterial", this.gameObject);
#endif
        }

        protected override void ResetValue()
        {
            base.ResetValue();
            this.CreateLineBody();
            this.CreateLineHead();
        }

        protected virtual void LoadLineBody()
        {
            if (this.lineBody != null) return;
            Transform child = this.transform.Find("LineBody");
            if (child == null) return;
            this.lineBody = child.GetComponent<LineRenderer>();
            this.ApplyMaterial(this.lineBody);
            Debug.LogWarning(this.transform.name + ": LoadLineBody", this.gameObject);
        }

        protected virtual void LoadLineHead()
        {
            if (this.lineHead != null) return;
            Transform child = this.transform.Find("LineHead");
            if (child == null) return;
            this.lineHead = child.GetComponent<LineRenderer>();
            this.ApplyMaterial(this.lineHead);
            Debug.LogWarning(this.transform.name + ": LoadLineHead", this.gameObject);
        }

        private void CreateLineBody()
        {
            if (this.lineBody != null) return;
            GameObject child = this.GetOrCreateChildObject("LineBody");
            this.lineBody = this.GetOrAddLineRenderer(child);
            this.ApplyMaterial(this.lineBody);
        }

        private void CreateLineHead()
        {
            if (this.lineHead != null) return;
            GameObject child = this.GetOrCreateChildObject("LineHead");
            this.lineHead = this.GetOrAddLineRenderer(child);
            this.ApplyMaterial(this.lineHead);
        }

        private GameObject GetOrCreateChildObject(string childName)
        {
            Transform existing = this.transform.Find(childName);
            if (existing != null) return existing.gameObject;
            GameObject go = new GameObject(childName);
            go.transform.SetParent(this.transform, false);
            return go;
        }

        private LineRenderer GetOrAddLineRenderer(GameObject go)
        {
            LineRenderer lr = go.GetComponent<LineRenderer>();
            if (lr != null) return lr;
            return go.AddComponent<LineRenderer>();
        }

        private void ApplyMaterial(LineRenderer lr)
        {
            if (lr == null) return;
            this.ClearPositions(lr);
            if (this.arrowMaterial != null)
            {
                lr.material = this.arrowMaterial;
                return;
            }
            Shader shader = Shader.Find("SG03/ArrowIndicator");
            if (shader == null) return;
            lr.material = new Material(shader);
        }

        private void ClearPositions(LineRenderer lr)
        {
            if (lr == null) return;
            lr.positionCount = 0;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Activates the arrow and draws it from <paramref name="from"/> to <paramref name="to"/>.
        /// Call once when the player begins selecting a target.
        /// </summary>
        public void Show(Vector3 from, Vector3 to)
        {
            this.gameObject.SetActive(true);
            this.Redraw(from, to);
        }

        /// <summary>
        /// Redraws the arrow to a new destination while keeping the source fixed.
        /// Call every frame while the player is hovering over targets.
        /// </summary>
        public void UpdateTarget(Vector3 from, Vector3 to)
        {
            this.Redraw(from, to);
        }

        /// <summary>Deactivates the arrow.</summary>
        public void Hide()
        {
            this.ClearPositions(this.lineBody);
            this.ClearPositions(this.lineHead);
            this.gameObject.SetActive(false);
        }

        // ─── Drawing ──────────────────────────────────────────────────────────────

        private void Redraw(Vector3 from, Vector3 to)
        {
            this.DrawBody(from, to);
            this.DrawHead(from, to);
        }

        private void DrawBody(Vector3 from, Vector3 to)
        {
            if (this.lineBody == null) return;
            this.lineBody.positionCount = 2;
            this.lineBody.SetPosition(0, from);
            this.lineBody.SetPosition(1, to);
        }

        private void DrawHead(Vector3 from, Vector3 to)
        {
            if (this.lineHead == null) return;
            Vector3 dir       = this.ComputeDirection(from, to);
            Vector3 leftWing  = this.ComputeWing(to, dir,  1f);
            Vector3 rightWing = this.ComputeWing(to, dir, -1f);
            this.lineHead.positionCount = 3;
            this.lineHead.SetPosition(0, leftWing);
            this.lineHead.SetPosition(1, to);
            this.lineHead.SetPosition(2, rightWing);
        }

        // ─── Math helpers ─────────────────────────────────────────────────────────

        private Vector3 ComputeDirection(Vector3 from, Vector3 to)
        {
            Vector3 raw = to - from;
            if (raw.sqrMagnitude < 0.0001f) return Vector3.forward;
            return raw.normalized;
        }

        private Vector3 ComputeWing(Vector3 tip, Vector3 dir, float side)
        {
            Vector3 perp      = this.ComputePerpendicular(dir);
            float   halfSpan  = this.headLength * Mathf.Tan(this.headAngle * Mathf.Deg2Rad);
            Vector3 basePoint = tip - dir * this.headLength;
            return basePoint + perp * (halfSpan * side);
        }

        private Vector3 ComputePerpendicular(Vector3 dir)
        {
            Vector3 reference = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.right;
            return Vector3.Cross(dir, reference).normalized;
        }
    }
}
