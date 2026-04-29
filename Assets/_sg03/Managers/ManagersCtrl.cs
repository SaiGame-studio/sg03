using SaiGame.Services;
using SG03.Quest;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/Managers/Managers Ctrl")]
    public class ManagersCtrl : SaiSingleton<ManagersCtrl>
    {
        [Header("Linked Child Managers")]
        [SerializeField] private ProfileManager profileManager;
        [SerializeField] private CardDataManager cardDataManager;
        [SerializeField] private QuestDailyManager questDailyManager;

        public ProfileManager ProfileManager => this.profileManager;
        public CardDataManager CardDataManager => this.cardDataManager;
        public QuestDailyManager QuestDailyManager => this.questDailyManager;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadProfileManager();
            this.LoadCardDataManager();
            this.LoadQuestDailyManager();
        }

        protected virtual void LoadProfileManager()
        {
            if (this.profileManager != null) return;
            this.profileManager = this.GetComponentInChildren<ProfileManager>(true);
            Debug.LogWarning(this.transform.name + "LoadProfileManager", this.gameObject);
        }

        protected virtual void LoadCardDataManager()
        {
            if (this.cardDataManager != null) return;
            this.cardDataManager = this.GetComponentInChildren<CardDataManager>(true);
            Debug.LogWarning(this.transform.name + "LoadCardDataManager", this.gameObject);
        }

        protected virtual void LoadQuestDailyManager()
        {
            if (this.questDailyManager != null) return;
            this.questDailyManager = this.GetComponentInChildren<QuestDailyManager>(true);
            Debug.LogWarning(this.transform.name + "LoadQuestDailyManager", this.gameObject);
        }
    }
}
