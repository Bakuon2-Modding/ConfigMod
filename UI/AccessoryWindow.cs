using MBakuon;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BakuonConfigMod
{
    public class AccessoryWindow : MonoBehaviour
    {
        // OfflineSaveDataManager.SaveAllData() をリフレクションでキャッシュ
        // NCMBManager.SaveUserData 経由だとデリゲートコールバックが誤発火してルーム再入室するため
        private static MethodInfo _saveAllDataMethod;

        private static void SaveAllData()
        {
            if (_saveAllDataMethod == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BakuonOfflinePatch")
                    {
                        _saveAllDataMethod = asm.GetType("BakuonOfflinePatch.OfflineSaveDataManager")
                            ?.GetMethod("SaveAllData", BindingFlags.Public | BindingFlags.Static);
                        break;
                    }
                }
            }
            _saveAllDataMethod?.Invoke(null, null);
        }

        private Vector2 scrollPos = Vector2.zero;

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
                GUILayout.BeginHorizontal();

                // アクセサリ名
                string displayName = string.IsNullOrEmpty(acc.property.accessoryName)
                    ? "ID:" + (int)acc.property.accessoryID
                    : acc.property.accessoryName;
                GUILayout.Label(displayName, GUILayout.Width(200));

                // 現在の個数
                GUILayout.Label(acc.stockValue.ToString(), GUILayout.Width(40));

                // ▼ / ▲ ボタン（押すと即反映）
                if (GUILayout.Button("▼", GUILayout.Width(26)))
                    ApplyDelta(gm, acc.property.accessoryID, -1);
                if (GUILayout.Button("▲", GUILayout.Width(26)))
                    ApplyDelta(gm, acc.property.accessoryID, 1);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(4);
            if (GUILayout.Button("セーブ", GUILayout.Width(70)))
            {
                SaveAllData();
                LogHelper.LogInfo("[AccessoryWindow] 保存完了");
            }
        }

        private void ApplyDelta(GameManager gm, AccessoryData.enumAccessoryID accID, int delta)
        {
            int current = GetStockCount(gm, accID);
            int newValue = current + delta;
            if (newValue < 0) newValue = 0;
            delta = newValue - current;
            if (delta == 0) return;

            List<string> newList = AccessoryData.GetChangedStockAccessoryDataStringList(
                AccessoryData.GetStockAccessoryDataStringList(), accID, delta);

            // メモリのみ更新。ディスク書き込みは保存ボタンで行う
            AccessoryData.RenewStockAccessoryDataFromStringList(newList);

            LogHelper.LogInfo($"[AccessoryWindow] {accID} -> {newValue}");
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
    }
}
