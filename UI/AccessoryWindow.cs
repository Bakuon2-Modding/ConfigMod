using MBakuon;
using System.Collections.Generic;
using UnityEngine;

namespace BakuonConfigMod
{
    public class AccessoryWindow : MonoBehaviour
    {
        // アクセサリごとの入力テキスト (accessoryID -> 入力文字列)
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
            if (gm == null || gm.stockAccessoryDataList == null || gm.stockAccessoryDataList.Count == 0)
            {
                GUILayout.Label("ゲーム未ロード中。フィールドに入ってから使用してください。");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

            foreach (AccessoryData acc in gm.stockAccessoryDataList)
            {
                int id = (int)acc.property.accessoryID;
                int current = acc.stockValue;

                if (!inputTexts.ContainsKey(id))
                    inputTexts[id] = current.ToString();

                GUILayout.BeginHorizontal();

                // アクセサリ名
                string displayName = string.IsNullOrEmpty(acc.property.accessoryName)
                    ? "ID:" + id
                    : acc.property.accessoryName;
                GUILayout.Label(displayName, GUILayout.Width(180));

                // 現在の個数
                GUILayout.Label(current.ToString(), GUILayout.Width(45));

                // 直接入力フィールド
                string newText = GUILayout.TextField(inputTexts[id], GUILayout.Width(65));
                if (newText != inputTexts[id])
                    inputTexts[id] = newText;

                // セットボタン
                if (GUILayout.Button("セット", GUILayout.Width(48)))
                    ApplySet(gm, acc.property.accessoryID, inputTexts[id]);

                // ▼ / ▲ ボタン
                if (GUILayout.Button("▼", GUILayout.Width(26)))
                    ApplyDelta(gm, acc.property.accessoryID, -1);
                if (GUILayout.Button("▲", GUILayout.Width(26)))
                    ApplyDelta(gm, acc.property.accessoryID, 1);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label(statusMessage);
            }
        }

        private void ApplySet(GameManager gm, AccessoryData.enumAccessoryID accID, string text)
        {
            int newValue;
            if (!int.TryParse(text, out newValue))
            {
                ShowStatus("数値を入力してください");
                return;
            }
            if (newValue < 0) newValue = 0;

            int current = GetStockCount(gm, accID);
            int delta = newValue - current;
            SaveAccessoryDelta(gm, accID, delta);
            ShowStatus(AccessoryData.GetAccessoryName(accID) + " を " + newValue + " に設定しました");
        }

        private void ApplyDelta(GameManager gm, AccessoryData.enumAccessoryID accID, int delta)
        {
            int current = GetStockCount(gm, accID);
            int newValue = current + delta;
            if (newValue < 0) newValue = 0;
            delta = newValue - current;
            if (delta == 0) return;

            SaveAccessoryDelta(gm, accID, delta);

            int id = (int)accID;
            inputTexts[id] = newValue.ToString();
        }

        private void SaveAccessoryDelta(GameManager gm, AccessoryData.enumAccessoryID accID, int delta)
        {
            if (delta == 0) return;

            List<string> newList = AccessoryData.GetChangedStockAccessoryDataStringList(
                AccessoryData.GetStockAccessoryDataStringList(), accID, delta);

            var ncmb = SingletonMonoBehaviour<NCMBManager>.Instance;
            if (ncmb != null)
            {
                ncmb.SaveUserData(null, null, null, null, newList, string.Empty, false);
            }
            else
            {
                AccessoryData.RenewStockAccessoryDataFromStringList(newList);
            }

            int id = (int)accID;
            inputTexts[id] = GetStockCount(gm, accID).ToString();

            LogHelper.LogInfo($"[AccessoryWindow] {accID} delta={delta} -> new={GetStockCount(gm, accID)}");
        }

        private static int GetStockCount(GameManager gm, AccessoryData.enumAccessoryID accID)
        {
            int count = 0;
            foreach (AccessoryData acc in gm.stockAccessoryDataList)
            {
                if (acc.property.accessoryID == accID)
                    count += acc.stockValue;
            }
            return count;
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageTimer = 3f;
        }
    }
}
