using System.Collections;
using System.IO;
using MBakuon;
using UnityEngine;

namespace BakuonConfigMod
{
    // BakuonConfigMod が管理するゲームアクション (メニュー開閉など) を毎フレーム処理するコンポーネント。
    // DontDestroyOnLoad な GameObject にアタッチされる。
    public class GameActions : MonoBehaviour
    {
        public static bool IsUIVisible { get; private set; } = true;

        private void Update()
        {
            ConfigModInput.UpdateState();

            // リバインド中は誤作動を防ぐためスキップ
            if (ConfigModInput.IsRebinding) return;

            HandleScreenshot();
            HandleUIToggle();
            HandleMenuToggle();
        }

        // ─── スクリーンショット ───────────────────────────────────────────

        private void HandleScreenshot()
        {
            if (!ConfigModInput.GetDown("Screenshot")) return;
            StartCoroutine(CaptureScreenshot());
        }

        private static IEnumerator CaptureScreenshot()
        {
            // フレームの描画が完全に終わってからキャプチャする
            yield return new WaitForEndOfFrame();

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));
            Directory.CreateDirectory(dir);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename  = "screenshot_" + timestamp + ".png";
            string fullPath  = Path.Combine(dir, filename);

            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.Destroy(tex);

            LogHelper.LogInfo("Screenshot saved: " + fullPath);

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm != null)
                gm.ShowSystemMessage(filename + " を保存しました");
        }

        // ─── UI 表示/非表示 ──────────────────────────────────────────────

        private void HandleUIToggle()
        {
            if (!ConfigModInput.GetDown("UIToggle")) return;
            ToggleUIVisible();
        }

        public static void ToggleUIVisible()
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return;
            if (gm.isForcusdInputField) return;
            if (!SingletonMonoBehaviour<MenuScreenManager>.Instance) return;

            IsUIVisible = !IsUIVisible;
            gm.SwitchInterfaceVisible(IsUIVisible);
        }

        public static void SetUIVisible(bool visible)
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return;
            if (!SingletonMonoBehaviour<MenuScreenManager>.Instance) return;

            IsUIVisible = visible;
            gm.SwitchInterfaceVisible(visible);
        }

        // ─── メニュー開閉 ────────────────────────────────────────────────

        private static void HandleMenuToggle()
        {
            if (!ConfigModInput.GetDown("MenuToggle")) return;

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm != null && gm.isForcusdInputField) return;

            // ダンジョン中は SuteageIsekiController の専用メニューを使う
            var suteage = Object.FindObjectOfType<SuteageIsekiController>();
            if (suteage != null)
            {
                suteage.PressedMenuOpenHandle();
                return;
            }

            var menu = SingletonMonoBehaviour<MenuScreenManager>.Instance;
            if (menu == null) return;
            menu.PressedMenuButton();
        }
    }
}
