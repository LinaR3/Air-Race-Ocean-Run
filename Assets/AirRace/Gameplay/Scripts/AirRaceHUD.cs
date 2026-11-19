using UnityEngine;
using UnityEngine.UI;

namespace AirRace.Gameplay
{
    /// <summary>High-contrast, screen-space race information for large public displays.</summary>
    public sealed class AirRaceHUD : MonoBehaviour
    {
        private static readonly Color Navy = new Color(0.015f, 0.055f, 0.16f, 0.9f);
        private static readonly Color Blue = new Color(0.03f, 0.22f, 0.62f, 0.96f);
        private static readonly Color Yellow = new Color(1f, 0.78f, 0.05f, 1f);
        private static readonly Color White = new Color(1f, 0.98f, 0.9f, 1f);

        private AirRaceRunManager run;
        private Text timeValue;
        private Text checkpointValue;
        private Text scoreValue;
        private Text energyValue;
        private Text boostValue;
        private Text impactValue;
        private Text resultTitle;
        private Text resultDetails;
        private Image energyFill;
        private GameObject raceLayer;
        private GameObject resultLayer;

        public void Initialize(AirRaceRunManager manager)
        {
            run = manager;
            Build();
        }

        private void Update()
        {
            if (run == null || raceLayer == null || resultLayer == null) return;
            bool racing = run.State == RaceState.Racing;
            raceLayer.SetActive(racing);
            resultLayer.SetActive(!racing);

            if (racing)
            {
                timeValue.text = Mathf.CeilToInt(run.TimeRemaining).ToString("00");
                checkpointValue.text = run.NextCheckpoint < run.CheckpointCount
                    ? $"{run.NextCheckpoint + 1:00}  /  {run.CheckpointCount:00}"
                    : "META";
                scoreValue.text = run.Score.ToString("00000");
                energyValue.text = $"ENERGÍA  {Mathf.CeilToInt(run.Energy)}%";
                energyFill.fillAmount = Mathf.Clamp01(run.Energy / 100f);
                boostValue.text = run.IsBoosting
                    ? $"IMPULSO  {run.BoostTimeRemaining:0.0}s  ·  COMBO x{run.Combo}"
                    : run.BoostCharges > 0
                        ? $"LATAS  x{run.BoostCharges}  ·  ESPACIO: IMPULSO"
                        : "SIN CARGA  ·  BUSCA UNA LATA";
                energyFill.color = Color.Lerp(new Color(0.94f, 0.12f, 0.08f), Yellow, Mathf.Clamp01(run.Energy / 100f));
                impactValue.gameObject.SetActive(run.ImpactFeedbackRemaining > 0f);
            }
            else
            {
                resultTitle.text = run.State == RaceState.Won ? "¡VUELTA COMPLETADA!" : "VUELO TERMINADO";
                resultDetails.text = run.State == RaceState.Won
                    ? $"PUNTUACIÓN  {run.Score:00000}\nTIEMPO  {Mathf.CeilToInt(run.TimeRemaining):00} s\n\nPULSA R PARA REINTENTAR"
                    : $"{run.EndReason}\nPUNTUACIÓN  {run.Score:00000}\n\nPULSA R PARA REINTENTAR";
            }
        }

        private void Build()
        {
            var canvasObject = new GameObject("AirRace_HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            raceLayer = MakeRect("Race_HUD", canvasObject.transform).gameObject;
            Panel("Time_Panel", raceLayer.transform, new Vector2(0f, 1f), new Vector2(32f, -30f), new Vector2(260f, 148f));
            Label("TIME_LABEL", raceLayer.transform, "TIEMPO", 25, Yellow, new Vector2(0f, 1f), new Vector2(56f, -45f), new Vector2(180f, 36f), TextAnchor.MiddleLeft);
            timeValue = Label("TIME_VALUE", raceLayer.transform, "60", 58, White, new Vector2(0f, 1f), new Vector2(52f, -73f), new Vector2(190f, 80f), TextAnchor.MiddleLeft);

            Panel("Checkpoint_Panel", raceLayer.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(430f, 138f));
            Label("CHECKPOINT_LABEL", raceLayer.transform, "SIGUIENTE CHECKPOINT", 24, Yellow, new Vector2(0.5f, 1f), new Vector2(0f, -43f), new Vector2(390f, 38f), TextAnchor.MiddleCenter);
            checkpointValue = Label("CHECKPOINT_VALUE", raceLayer.transform, "01  /  06", 52, White, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(390f, 70f), TextAnchor.MiddleCenter);

            Panel("Score_Panel", raceLayer.transform, new Vector2(1f, 1f), new Vector2(-32f, -30f), new Vector2(270f, 124f));
            Label("SCORE_LABEL", raceLayer.transform, "PUNTOS", 24, Yellow, new Vector2(1f, 1f), new Vector2(-62f, -45f), new Vector2(210f, 34f), TextAnchor.MiddleRight);
            scoreValue = Label("SCORE_VALUE", raceLayer.transform, "00000", 48, White, new Vector2(1f, 1f), new Vector2(-56f, -76f), new Vector2(220f, 64f), TextAnchor.MiddleRight);

            Panel("Energy_Panel", raceLayer.transform, new Vector2(0f, 0f), new Vector2(32f, 30f), new Vector2(590f, 148f));
            energyValue = Label("ENERGY_VALUE", raceLayer.transform, "ENERGÍA  72%", 28, White, new Vector2(0f, 0f), new Vector2(58f, 91f), new Vector2(470f, 38f), TextAnchor.MiddleLeft);
            Image barBack = Box("ENERGY_BAR_BACK", raceLayer.transform, new Color(0.01f, 0.025f, 0.08f, 1f), new Vector2(0f, 0f), new Vector2(58f, 60f), new Vector2(474f, 22f));
            energyFill = Box("ENERGY_BAR_FILL", raceLayer.transform, Yellow, new Vector2(0f, 0f), new Vector2(61f, 63f), new Vector2(468f, 16f));
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            energyFill.fillAmount = 0.72f;

            Panel("Boost_Panel", raceLayer.transform, new Vector2(1f, 0f), new Vector2(-32f, 30f), new Vector2(580f, 100f));
            boostValue = Label("BOOST_VALUE", raceLayer.transform, "SIN CARGA  ·  BUSCA UNA LATA", 27, White, new Vector2(1f, 0f), new Vector2(-54f, 61f), new Vector2(520f, 54f), TextAnchor.MiddleRight);
            impactValue = Label("IMPACT_FEEDBACK", raceLayer.transform, "¡REBOTE!", 42, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, -108f), new Vector2(500f, 70f), TextAnchor.MiddleCenter);
            impactValue.gameObject.SetActive(false);
            Label("CONTROL_HINT", raceLayer.transform, "W / S  ALTURA     ·     A / D  GIRO     ·     C  CÁMARA", 22, White, new Vector2(1f, 1f), new Vector2(-34f, -168f), new Vector2(710f, 40f), TextAnchor.MiddleRight);

            resultLayer = MakeRect("Race_Result_Overlay", canvasObject.transform).gameObject;
            Image shade = resultLayer.AddComponent<Image>();
            shade.color = new Color(0.005f, 0.025f, 0.09f, 0.78f);
            resultTitle = Label("RESULT_TITLE", resultLayer.transform, "VUELO TERMINADO", 58, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1200f, 100f), TextAnchor.MiddleCenter);
            resultDetails = Label("RESULT_DETAILS", resultLayer.transform, "PUNTUACIÓN  00000", 34, White, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(1200f, 210f), TextAnchor.MiddleCenter);
            resultLayer.SetActive(false);
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = Box(name, parent, Navy, anchor, position, size);
            image.raycastTarget = false;
            float accentX = anchor.x == 0f ? 10f : anchor.x == 1f ? -size.x + 10f : -size.x * 0.5f + 10f;
            float accentY = anchor.y == 0f ? 10f : -10f;
            Box(name + "_Accent", parent, Blue, anchor, position + new Vector2(accentX, accentY), new Vector2(8f, size.y - 20f));
        }

        private static Image Box(string name, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(string name, Transform parent, string value, int size, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
