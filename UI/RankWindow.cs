using System.Collections.Generic;
using UnityEngine;

namespace BakuonConfigMod
{
    public class RankWindow : MonoBehaviour
    {
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
            if (gm == null)
            {
                GUILayout.Label("ゲーム未ロード中。フィールドに入ってから使用してください。");
                return;
            }

            List<UnitData> units = gm.stockUnitDataList;
            if (units == null || units.Count == 0)
            {
                GUILayout.Label("キャラクターデータがありません。");
                return;
            }

            // ヘッダー
            GUILayout.BeginHorizontal();
            GUILayout.Label("キャラクター", GUILayout.Width(160));
            GUILayout.Label("ランク", GUILayout.Width(150));
            GUILayout.EndHorizontal();

            string[] rankLabels = { "★1", "★2", "★3" };

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

            foreach (UnitData unit in units)
            {
                if (unit == null || unit.stockValue <= 0) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(unit.property.characterName, GUILayout.Width(160));

                int current = unit.rarelity - 1; // Toolbar は 0-based
                int selected = GUILayout.Toolbar(current, rankLabels, GUILayout.Width(150));
                if (selected != current)
                {
                    SetRank(gm, unit, selected + 1);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            // 一括変更ボタン
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("全キャラ一括:", GUILayout.Width(100));
            for (int r = 1; r <= 3; r++)
            {
                if (GUILayout.Button("全員 ★" + r, GUILayout.Width(75)))
                {
                    SetAllRanks(gm, units, r);
                }
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label(statusMessage);
            }
        }

        private void SetRank(GameManager gm, UnitData unit, int rank)
        {
            unit.rarelity = rank;
            Save(gm);
            ShowStatus(unit.property.characterName + " のランクを ★" + rank + " に変更しました");
            LogHelper.LogInfo($"[RankWindow] {unit.property.characterName} rarelity={rank}");
        }

        private void SetAllRanks(GameManager gm, List<UnitData> units, int rank)
        {
            int count = 0;
            foreach (UnitData unit in units)
            {
                if (unit != null && unit.stockValue > 0)
                {
                    unit.rarelity = rank;
                    count++;
                }
            }
            Save(gm);
            ShowStatus("全キャラ(" + count + "体)のランクを ★" + rank + " に変更しました");
            LogHelper.LogInfo($"[RankWindow] SetAllRanks: rank={rank}, count={count}");
        }

        private void Save(GameManager gm)
        {
            var ncmb = SingletonMonoBehaviour<NCMBManager>.Instance;
            if (ncmb != null)
            {
                var unitStrings = UnitData.GetStockUnitDataStringList();
                ncmb.SaveUserData(null, null, unitStrings, null, null, string.Empty, false);
            }
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageTimer = 3f;
        }
    }
}
