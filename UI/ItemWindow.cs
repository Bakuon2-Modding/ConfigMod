using MBakuon;
using System.Collections.Generic;
using UnityEngine;
// HashSet は System.Collections.Generic に含まれる

namespace BakuonConfigMod
{
    public class ItemWindow : MonoBehaviour
    {
        // 所有アイテムではないため対象外にするID一覧
        private static readonly HashSet<ItemData.enumItemID> ExcludedItems = new HashSet<ItemData.enumItemID>
        {
            ItemData.enumItemID.Item_Gold,            // リングタブで管理
            ItemData.enumItemID.Item_ShishiCrystal,   // 志士結晶
            ItemData.enumItemID.Item_GachaTicket,     // ガチャチケット
            ItemData.enumItemID.Item_EXPTicketS,      // 経験値の札S
            ItemData.enumItemID.Item_EXPTicketM,      // 経験値の札M
            ItemData.enumItemID.Item_EXPTicketL,      // 経験値の札L
            ItemData.enumItemID.Item_OldMushroom,     // 古木の茸
            ItemData.enumItemID.Item_OldMushroomBig,  // 神樹の茸
            ItemData.enumItemID.Item_CanAtkSkillA,    // Aスキル缶
            ItemData.enumItemID.Item_CanAtkSkillB,    // Bスキル缶
            ItemData.enumItemID.Item_CanAtkSkillC,    // Cスキル缶
            ItemData.enumItemID.Item_CanAP,           // AP缶
            ItemData.enumItemID.Item_LimitBreaker_S,  // ランクアップ素材S
            ItemData.enumItemID.Item_LimitBreaker_M,  // ランクアップ素材M
            ItemData.enumItemID.Item_LimitBreaker_L,  // ランクアップ素材L
            ItemData.enumItemID.Item_JAGASAKA,        // じゃが坂
            ItemData.enumItemID.Item_JAGAMIKE,        // ごまミケ
            ItemData.enumItemID.Item_OOKUBO,          // 大久保
            ItemData.enumItemID.Item_RUFRAIN,         // ルーフレイン
            ItemData.enumItemID.Item_SAKAKI_RYOU,     // 榊 涼
        };

        // アイテムごとの入力テキスト (itemID -> 入力文字列)
        private Dictionary<int, string> inputTexts = new Dictionary<int, string>();

        private Vector2 scrollPos = Vector2.zero;
        private string statusMessage = "";
        private float statusMessageTimer = 0f;

        private void Update()
        {
            if (statusMessageTimer > 0f)
            {
                statusMessageTimer -= Time.deltaTime;
                if (statusMessageTimer <= 0f)
                    statusMessage = "";
            }
        }

        public void DrawTabContent(int scrollHeight)
        {
            GUILayout.Space(4);

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null || gm.stockItemDataList == null || gm.stockItemDataList.Count == 0)
            {
                GUILayout.Label("ゲーム未ロード中。フィールドに入ってから使用してください。");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

            foreach (ItemData item in gm.stockItemDataList)
            {
                // 消費アイテム以外はスキップ
                if (item.property.itemType != ItemData.enumItemType.Item)
                    continue;
                // 対象外アイテムはスキップ
                if (ExcludedItems.Contains(item.property.itemID))
                    continue;

                int id = (int)item.property.itemID;
                int current = item.value;

                if (!inputTexts.ContainsKey(id))
                    inputTexts[id] = current.ToString();

                GUILayout.BeginHorizontal();

                // アイテム名
                string displayName = string.IsNullOrEmpty(item.property.itemName)
                    ? "ID:" + id
                    : item.property.itemName;
                GUILayout.Label(displayName, GUILayout.Width(180));

                // 現在の個数
                GUILayout.Label(current.ToString(), GUILayout.Width(45));

                // 直接入力フィールド
                string newText = GUILayout.TextField(inputTexts[id], GUILayout.Width(65));
                if (newText != inputTexts[id])
                    inputTexts[id] = newText;

                // セットボタン
                if (GUILayout.Button("セット", GUILayout.Width(48)))
                    ApplySet(gm, item.property.itemID, inputTexts[id]);

                // -1 / +1 ボタン
                if (GUILayout.Button("▼", GUILayout.Width(26)))
                    ApplyDelta(gm, item.property.itemID, -1);
                if (GUILayout.Button("▲", GUILayout.Width(26)))
                    ApplyDelta(gm, item.property.itemID, 1);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label(statusMessage);
            }
        }

        private void ApplySet(GameManager gm, ItemData.enumItemID itemID, string text)
        {
            int newValue;
            if (!int.TryParse(text, out newValue))
            {
                ShowStatus("数値を入力してください");
                return;
            }
            if (newValue < 0) newValue = 0;

            int delta = newValue - ItemData.GetItemCount(itemID);
            SaveItemDelta(gm, itemID, delta);
            ShowStatus(ItemData.GetItemName(itemID) + " を " + newValue + " に設定しました");
        }

        private void ApplyDelta(GameManager gm, ItemData.enumItemID itemID, int delta)
        {
            int current = ItemData.GetItemCount(itemID);
            int newValue = current + delta;
            if (newValue < 0) newValue = 0;
            delta = newValue - current;
            if (delta == 0) return;

            SaveItemDelta(gm, itemID, delta);

            // 入力欄も同期
            int id = (int)itemID;
            inputTexts[id] = newValue.ToString();
        }

        private void SaveItemDelta(GameManager gm, ItemData.enumItemID itemID, int delta)
        {
            if (delta == 0) return;

            List<string> newList = ItemData.GetChangedStockItemDataStringList(
                ItemData.GetStockItemDataStringList(), itemID, delta);

            var ncmb = SingletonMonoBehaviour<NCMBManager>.Instance;
            if (ncmb != null)
            {
                ncmb.SaveUserData(null, null, null, newList, null, string.Empty, false);
            }
            else
            {
                // フォールバック: メモリ直接更新
                ItemData.RenewStockItemDataFromStringList(newList);
            }

            // 入力欄を更新後の値に同期
            int id = (int)itemID;
            inputTexts[id] = ItemData.GetItemCount(itemID).ToString();

            LogHelper.LogInfo($"[ItemWindow] {itemID} delta={delta} -> new={ItemData.GetItemCount(itemID)}");
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageTimer = 3f;
        }
    }
}
