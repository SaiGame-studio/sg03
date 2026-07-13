using System.Collections;
using UnityEngine;

namespace SG03
{
    public partial class ClientActions
    {
        private Coroutine ExecuteCardTakeDamage(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            
            string targetId = null;
            int damage = 0;
            int totalDamage = 0;

            foreach (string p in parameters)
            {
                string[] kv = p.Split('=');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLower();
                    string value = kv[1].Trim();
                    if (key == "target") targetId = value;
                    else if (key == "damage") int.TryParse(value, out damage);
                    else if (key == "total_damage") int.TryParse(value, out totalDamage);
                }
            }

            if (string.IsNullOrEmpty(targetId)) return null;
            
            if (this.logActions) Debug.Log($"[CardTakeDamage] target={targetId}, damage={damage}, total_damage={totalDamage}");
            
            Card3DCtrl card = this.cardSpawning?.FindCardById(targetId);
            if (card != null)
            {
                card.Damaged();
                return this.StartCoroutine(this.WaitForCard(card));
            }

            return null;
        }

        private Coroutine ExecuteCardGuarded(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            
            string targetId = null;
            foreach (string p in parameters)
            {
                string[] kv = p.Split('=');
                if (kv.Length == 2 && kv[0].Trim().ToLower() == "target") targetId = kv[1].Trim();
            }
            if (string.IsNullOrEmpty(targetId) && !parameters[0].Contains("="))
            {
                targetId = parameters[0].Trim();
            }

            if (string.IsNullOrEmpty(targetId)) return null;
            
            Card3DCtrl card = this.cardSpawning?.FindCardById(targetId);
            if (card != null)
            {
                card.RunUp();
                return this.StartCoroutine(this.WaitForCard(card));
            }

            return null;
        }

        private Coroutine ExecuteAlphaAttack(string[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            string attackerId = parameters[0].Trim();
            string defenderId = parameters[1].Trim();
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(defenderId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            Card3DCtrl defender = this.cardSpawning?.FindCardById(defenderId);
            if (attacker == null || defender == null) return null;
            
            return this.StartCoroutine(this.AlphaAttackRoutine(attacker, defender));
        }

        private IEnumerator AlphaAttackRoutine(Card3DCtrl attacker, Card3DCtrl defender)
        {
            if (attacker.IsCharacter()) attacker.AttackLunge(defender.transform.position);
            else attacker.AbilityActive();

            yield return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Coroutine ExecuteAlphaAttackOmegaHp(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string attackerId = null;
            foreach (string p in parameters)
            {
                string[] kv = p.Split('=');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLower();
                    string value = kv[1].Trim();
                    if (key == "attacker_card_id" || key == "attacker") attackerId = value;
                }
            }
            if (string.IsNullOrEmpty(attackerId) && !parameters[0].Contains("="))
            {
                attackerId = parameters[0].Trim();
            }

            if (string.IsNullOrEmpty(attackerId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            if (attacker == null || this.deskPosition == null) return null;
            
            return this.StartCoroutine(this.AlphaAttackOmegaHpRoutine(attacker));
        }

        private IEnumerator AlphaAttackOmegaHpRoutine(Card3DCtrl attacker)
        {
            if (attacker.IsCharacter())
            {
                attacker.AttackLunge(this.deskPosition.OmegaTheSource.position);
                yield return new WaitForSeconds(0.15f);
                this.cardSpawning?.ShakeOmegaSourceCards();
            }
            else
            {
                attacker.AbilityActive();
                yield return new WaitForSeconds(0.15f);
                this.cardSpawning?.ShakeOmegaSourceCards();
            }

            yield return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Coroutine ExecuteOmegaAttackAlphaHp(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string attackerId = null;
            foreach (string p in parameters)
            {
                string[] kv = p.Split('=');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLower();
                    string value = kv[1].Trim();
                    if (key == "attacker_card_id" || key == "attacker") attackerId = value;
                }
            }
            if (string.IsNullOrEmpty(attackerId) && !parameters[0].Contains("="))
            {
                attackerId = parameters[0].Trim();
            }

            if (string.IsNullOrEmpty(attackerId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            if (attacker == null || this.deskPosition == null) return null;
            
            return this.StartCoroutine(this.OmegaAttackAlphaHpRoutine(attacker));
        }

        private IEnumerator OmegaAttackAlphaHpRoutine(Card3DCtrl attacker)
        {
            if (attacker.IsCharacter())
            {
                attacker.AttackBackstepLunge(this.deskPosition.AlphaTheSource.position);
                yield return new WaitForSeconds(0.27f);
                this.cardSpawning?.ShakeAlphaSourceCards();
            }
            else
            {
                attacker.AbilityActive();
                yield return new WaitForSeconds(0.15f);
                this.cardSpawning?.ShakeAlphaSourceCards();
            }

            yield return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Coroutine ExecuteOmegaAttack(string[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            string attackerId = parameters[0].Trim();
            string defenderId = parameters[1].Trim();
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(defenderId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            Card3DCtrl defender = this.cardSpawning?.FindCardById(defenderId);
            if (attacker == null || defender == null) return null;

            return this.StartCoroutine(this.OmegaAttackRoutine(attacker, defender));
        }

        private IEnumerator OmegaAttackRoutine(Card3DCtrl attacker, Card3DCtrl defender)
        {
            if (attacker.IsCharacter()) attacker.AttackBackstepLunge(defender.transform.position);
            else attacker.AbilityActive();

            yield return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Coroutine ExecuteCardAbility(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            
            string sourceId = null;
            string abilityName = null;
            string targetId = null;
            string selectedId = null;

            foreach (string p in parameters)
            {
                string[] kv = p.Split('=');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLower();
                    string value = kv[1].Trim();
                    if (key == "source") sourceId = value;
                    else if (key == "ability") abilityName = value;
                    else if (key == "target") targetId = value;
                    else if (key == "selected") selectedId = value;
                }
            }

            // Fallback for old format just in case
            if (string.IsNullOrEmpty(sourceId) && parameters.Length > 0 && !parameters[0].Contains("="))
            {
                sourceId = parameters[0].Trim();
            }

            if (string.IsNullOrEmpty(sourceId)) return null;
            return this.StartCoroutine(this.CardAbilityRoutine(sourceId, targetId, selectedId));
        }

        private IEnumerator CardAbilityRoutine(string sourceId, string targetId, string selectedId)
        {
            Card3DCtrl sourceCard = !string.IsNullOrEmpty(sourceId) ? this.cardSpawning?.FindCardById(sourceId) : null;
            Card3DCtrl targetCard = !string.IsNullOrEmpty(targetId) ? this.cardSpawning?.FindCardById(targetId) : null;
            Card3DCtrl selectedCard = !string.IsNullOrEmpty(selectedId) ? this.cardSpawning?.FindCardById(selectedId) : null;

            if (sourceCard != null) sourceCard.RunUp();
            if (selectedCard != null && targetCard != null) selectedCard.AttackLunge(targetCard.transform.position);

            if (sourceCard != null) yield return this.StartCoroutine(this.WaitForCard(sourceCard));
            if (selectedCard != null) yield return this.StartCoroutine(this.WaitForCard(selectedCard));
        }

        private Coroutine ExecuteOmegaPlaningCharacterAttack(string[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            string attackerId = parameters[0].Trim();
            string defenderId = parameters[1].Trim();
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(defenderId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            Card3DCtrl defender = this.cardSpawning?.FindCardById(defenderId);
            if (attacker == null || defender == null) return null;
            return this.StartCoroutine(this.OmegaPlaningCharacterAttackRoutine(attacker, defender));
        }

        private IEnumerator OmegaPlaningCharacterAttackRoutine(Card3DCtrl attacker, Card3DCtrl defender)
        {
            yield return this.StartCoroutine(this.WaitForCard(attacker));
            // Planning attack: card advances next to the defender on the side it came from
            // (target.x - 1 when attacking from the left, target.x + 1 when from the right).
            // No return — waits for alpha's decision in the next server response.
            Vector3 destination = this.BuildPlanningAttackDestination(attacker.transform.position, defender.transform.position);
            attacker.PlanningLungeTo(destination);
            yield return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Vector3 BuildPlanningAttackDestination(Vector3 attackerPosition, Vector3 defenderPosition)
        {
            float offsetX = attackerPosition.x < defenderPosition.x ? -1f : 1f;
            float offsetZ = attackerPosition.z < defenderPosition.z ? -9f : 9f;
            return new Vector3(defenderPosition.x + offsetX, defenderPosition.y + 0.2f, defenderPosition.z + offsetZ);
        }
    }
}
