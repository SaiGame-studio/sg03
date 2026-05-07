using SaiGame.Services;
using SG03;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    public class BattleStateCtrl : SaiBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleState battleState;
        [SerializeField] private BattleScripts battleScripts;
        [SerializeField] private CardSpawning cardSpawning;
        [SerializeField] private CardSelection cardSelection;
        [SerializeField] private CardHoverDetector cardHoverDetector;
        [SerializeField] private CardHolderHoverDetector cardHolderHoverDetector;
        [SerializeField] private BattleCardDefinitions battleCardDefinitions;

        public BattleState BattleState => this.battleState;
        public BattleScripts BattleScripts => this.battleScripts;
        public CardSpawning CardSpawning => this.cardSpawning;
        public CardSelection CardSelection => this.cardSelection;
        public CardHoverDetector CardHoverDetector => this.cardHoverDetector;
        public CardHolderHoverDetector CardHolderHoverDetector => this.cardHolderHoverDetector;
        public BattleCardDefinitions BattleCardDefinitions => this.battleCardDefinitions;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleState();
            this.LoadBattleScripts();
            this.LoadCardSpawning();
            this.LoadCardSelection();
            this.LoadCardHoverDetector();
            this.LoadCardHolderHoverDetector();
            this.LoadBattleCardDefinitions();
        }

        protected virtual void LoadBattleScripts()
        {
            if (this.battleScripts != null) return;
            this.battleScripts = this.GetComponentInChildren<BattleScripts>(true);
            Debug.LogWarning(this.transform.name + ": LoadBattleScripts", this.gameObject);
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            this.battleState = this.GetComponentInChildren<BattleState>(true);
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        protected virtual void LoadCardSpawning()
        {
            if (this.cardSpawning != null) return;
            this.cardSpawning = this.GetComponentInChildren<CardSpawning>(true);
            Debug.LogWarning(this.transform.name + ": LoadCardSpawning", this.gameObject);
        }

        protected virtual void LoadCardSelection()
        {
            if (this.cardSelection != null) return;
            this.cardSelection = this.GetComponentInChildren<CardSelection>(true);
            Debug.LogWarning(this.transform.name + ": LoadCardSelection", this.gameObject);
        }

        protected virtual void LoadCardHoverDetector()
        {
            if (this.cardHoverDetector != null) return;
            this.cardHoverDetector = this.GetComponentInChildren<CardHoverDetector>(true);
            Debug.LogWarning(this.transform.name + ": LoadCardHoverDetector", this.gameObject);
        }

        protected virtual void LoadCardHolderHoverDetector()
        {
            if (this.cardHolderHoverDetector != null) return;
            this.cardHolderHoverDetector = this.GetComponentInChildren<CardHolderHoverDetector>(true);
            Debug.LogWarning(this.transform.name + ": LoadCardHolderHoverDetector", this.gameObject);
        }

        protected virtual void LoadBattleCardDefinitions()
        {
            if (this.battleCardDefinitions != null) return;
            this.battleCardDefinitions = this.GetComponentInChildren<BattleCardDefinitions>(true);
            Debug.LogWarning(this.transform.name + ": LoadBattleCardDefinitions", this.gameObject);
        }
    }
}
