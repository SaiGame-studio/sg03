using System;
using System.Collections;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>
    /// Dedicated cache of the player's currency balances. Its serialized balance list
    /// remains visible while the game is running in the Unity Inspector.
    /// </summary>
    public class CurrencyWallet : MonoBehaviour
    {
        private const int PageSize = 1000;

        [Header("Live Currency Balances")]
        [SerializeField] private CurrencyBalance[] balances = Array.Empty<CurrencyBalance>();
        [SerializeField] private bool isLoaded;
        [SerializeField] private string lastError;

        public event Action OnBalancesUpdated;
        public bool IsLoaded => this.isLoaded;
        public CurrencyBalance[] Balances => this.balances;

        public void Refresh()
        {
            SaiServer server = SaiServer.Instance;
            if (server == null || !server.IsAuthenticated) return;
            this.StartCoroutine(this.LoadCurrenciesCoroutine(server));
        }

        public bool CanAfford(string currencyItemDefinitionId, int amount)
            => !string.IsNullOrWhiteSpace(currencyItemDefinitionId)
               && this.GetBalance(currencyItemDefinitionId) >= amount;

        public int GetBalance(string currencyItemDefinitionId)
        {
            foreach (CurrencyBalance balance in this.balances)
            {
                if (balance.item_definition_id == currencyItemDefinitionId)
                    return balance.quantity;
            }

            return 0;
        }

        public int GetBalanceByItemCode(string itemCode)
        {
            foreach (CurrencyBalance balance in this.balances)
            {
                if (string.Equals(balance.item_code, itemCode, StringComparison.OrdinalIgnoreCase))
                    return balance.quantity;
            }

            return 0;
        }

        public string GetDefinitionIdByItemCode(string itemCode)
        {
            foreach (CurrencyBalance balance in this.balances)
            {
                if (string.Equals(balance.item_code, itemCode, StringComparison.OrdinalIgnoreCase))
                    return balance.item_definition_id;
            }

            return null;
        }

        private IEnumerator LoadCurrenciesCoroutine(SaiServer server)
        {
            this.isLoaded = false;
            this.lastError = string.Empty;
            Dictionary<string, CurrencyBalance> collected = new Dictionary<string, CurrencyBalance>();
            int offset = 0;
            int total = int.MaxValue;

            while (offset < total)
            {
                bool completed = false;
                string endpoint = $"/api/v1/games/{server.GameId}/inventory?limit={PageSize}&offset={offset}&include_metadata=true&category=currency";
                yield return server.GetRequest(
                    endpoint,
                    response =>
                    {
                        string sanitized = InventoryJsonHelper.StringifyObjectFields(response);
                        InventoryResponse page = JsonUtility.FromJson<InventoryResponse>(sanitized);
                        InventoryItemData[] items = page?.items ?? Array.Empty<InventoryItemData>();
                        total = page?.total ?? 0;
                        offset += items.Length;

                        foreach (InventoryItemData item in items)
                        {
                            string definitionId = item?.item_definition_id;
                            if (string.IsNullOrWhiteSpace(definitionId)) continue;

                            if (!collected.TryGetValue(definitionId, out CurrencyBalance balance))
                            {
                                balance = new CurrencyBalance
                                {
                                    item_definition_id = definitionId,
                                    item_code = item.definition?.item_code,
                                    item_name = item.definition?.name
                                };
                            }

                            balance.quantity += item.quantity;
                            collected[definitionId] = balance;
                        }

                        completed = true;
                    },
                    error =>
                    {
                        this.lastError = error;
                        total = 0;
                        completed = true;
                    });

                if (!completed || offset >= total || offset == 0) break;
            }

            this.balances = new List<CurrencyBalance>(collected.Values).ToArray();
            this.isLoaded = string.IsNullOrEmpty(this.lastError);
            this.OnBalancesUpdated?.Invoke();
        }
    }

    [Serializable]
    public struct CurrencyBalance
    {
        public string item_definition_id;
        public string item_code;
        public string item_name;
        public int quantity;
    }
}
