using MBakuon;
using UnityEngine;

namespace BakuonConfigMod
{
    public class RingWindow : MonoBehaviour
    {
        private string inputText = "";
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

        public void DrawTabContent()
        {
            GUILayout.Space(4);

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null)
            {
                GUILayout.Label("ゲーム未ロード中。フィールドに入ってから使用してください。");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("現在のリング:", GUILayout.Width(120));
            GUILayout.Label(gm.myCoin.ToString("N0") + " リング", GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("値を入力:", GUILayout.Width(80));
            inputText = GUILayout.TextField(inputText, GUILayout.Width(130));
            if (GUILayout.Button("セット", GUILayout.Width(55)))
                ApplySet(gm);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUILayout.Label("増減:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1,000",   GUILayout.Width(70))) ApplyDelta(gm, 1000);
            if (GUILayout.Button("+10,000",  GUILayout.Width(75))) ApplyDelta(gm, 10000);
            if (GUILayout.Button("+100,000", GUILayout.Width(80))) ApplyDelta(gm, 100000);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1,000",   GUILayout.Width(70))) ApplyDelta(gm, -1000);
            if (GUILayout.Button("-10,000",  GUILayout.Width(75))) ApplyDelta(gm, -10000);
            if (GUILayout.Button("-100,000", GUILayout.Width(80))) ApplyDelta(gm, -100000);
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label(statusMessage);
            }
        }

        private void ApplySet(GameManager gm)
        {
            int newValue;
            if (!int.TryParse(inputText, out newValue))
            {
                ShowStatus("数値を入力してください");
                return;
            }
            if (newValue < 0) newValue = 0;
            SetCoin(gm, newValue);
        }

        private void ApplyDelta(GameManager gm, int delta)
        {
            int newValue = gm.myCoin + delta;
            if (newValue < 0) newValue = 0;
            SetCoin(gm, newValue);
        }

        private void SetCoin(GameManager gm, int value)
        {
            var ncmb = SingletonMonoBehaviour<NCMBManager>.Instance;
            if (ncmb != null)
            {
                // OfflinePatch の NCMBManager_SaveUserData_Patch が intercept して
                // SetMyCoin + OfflineSaveDataManager.SaveAllData を処理する
                ncmb.SaveUserData(value, null, null, null, null, string.Empty, false);
            }
            else
            {
                // NCMBManager が未ロードの場合はメモリのみ更新
                gm.SetMyCoin(value);
            }

            inputText = value.ToString();
            ShowStatus("リングを " + value.ToString("N0") + " に設定しました");
            LogHelper.LogInfo($"[RingWindow] SetCoin: {value}");
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusMessageTimer = 3f;
        }
    }
}
