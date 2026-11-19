using System;
using UnityEngine;
using UnityEngine.UI;

namespace AirRace.Gameplay
{
    /// <summary>Large-display flight HUD: navigation, flight data and power state.</summary>
    public sealed class AirRaceHUD : MonoBehaviour
    {
        private static readonly Color Navy = new Color(0.015f, 0.055f, 0.16f, 0.66f);
        private static readonly Color Blue = new Color(0.04f, 0.27f, 0.58f, 0.96f);
        private static readonly Color Yellow = new Color(1f, 0.78f, 0.05f, 1f);
        private static readonly Color White = new Color(0.96f, 1f, 0.98f, 1f);
        private static readonly Color Mint = new Color(0.35f, 1f, 0.78f, 1f);

        private AirRaceRunManager run;
        private AircraftFlightController aircraft;
        private Text timeValue;
        private Text speedValue;
        private Text altitudeValue;
        private Text gateValue;
        private Text scoreValue;
        private Text energyValue;
        private Text boostValue;
        private Text impactValue;
        private Text resultTitle;
        private Text resultDetails;
        private Image energyFill;
        private GameObject raceLayer;
        private GameObject resultLayer;
        private AirRaceRadarDisplay radar;
        private AirRaceCheckpoint[] checkpoints = Array.Empty<AirRaceCheckpoint>();

        public void Initialize(AirRaceRunManager manager)
        {
            run = manager;
            aircraft = FindFirstObjectByType<AircraftFlightController>();
            checkpoints = FindObjectsByType<AirRaceCheckpoint>(FindObjectsSortMode.None);
            Array.Sort(checkpoints, (a, b) => a.Index.CompareTo(b.Index));
            Build();
        }

        private void Update()
        {
            if (run == null || raceLayer == null || resultLayer == null) return;
            bool racing = run.State == RaceState.Racing;
            raceLayer.SetActive(racing);
            resultLayer.SetActive(!racing);

            if (!racing)
            {
                resultTitle.text = run.State == RaceState.Won ? "¡VUELTA COMPLETADA!" : "VUELO TERMINADO";
                resultDetails.text = run.State == RaceState.Won
                    ? $"PUNTUACIÓN  {run.Score:00000}\nTIEMPO  {Mathf.CeilToInt(run.TimeRemaining):00} s\n\nPULSA R PARA REINTENTAR"
                    : $"{run.EndReason}\nPUNTUACIÓN  {run.Score:00000}\n\nPULSA R PARA REINTENTAR";
                return;
            }

            timeValue.text = Mathf.CeilToInt(run.TimeRemaining).ToString("00");
            scoreValue.text = run.Score.ToString("00000");
            energyValue.text = $"ENERGÍA DE VUELO  {Mathf.CeilToInt(run.Energy)}%";
            energyFill.fillAmount = Mathf.Clamp01(run.Energy / 100f);
            energyFill.color = Color.Lerp(new Color(0.94f, 0.12f, 0.08f), Yellow, Mathf.Clamp01(run.Energy / 100f));
            boostValue.text = run.IsBoosting
                ? $"IMPULSO  {run.BoostTimeRemaining:0.0}s  ·  COMBO x{run.Combo}"
                : run.BoostCharges > 0 ? $"LATAS  x{run.BoostCharges}  ·  ESPACIO: IMPULSO" : "SIN CARGA  ·  BUSCA UNA LATA";
            impactValue.gameObject.SetActive(run.ImpactFeedbackRemaining > 0f);

            if (aircraft != null)
            {
                speedValue.text = $"{Mathf.RoundToInt(aircraft.AirSpeed * 3.6f):000}  <size=20>KM/H</size>";
                altitudeValue.text = $"{Mathf.RoundToInt(aircraft.Altitude):000}  <size=20>M</size>";
                Vector3[] gatePositions = new Vector3[checkpoints.Length];
                for (int i = 0; i < checkpoints.Length; i++) gatePositions[i] = checkpoints[i].transform.position;
                AirRaceCanPickup[] pickupObjects = FindObjectsByType<AirRaceCanPickup>(FindObjectsSortMode.None);
                Vector3[] canPositions = new Vector3[pickupObjects.Length];
                for (int i = 0; i < pickupObjects.Length; i++) canPositions[i] = pickupObjects[i].transform.position;
                radar.SetRoute(aircraft.transform.position, aircraft.Heading, gatePositions, run.NextCheckpoint, canPositions);

                if (run.NextCheckpoint < checkpoints.Length)
                {
                    float distance = Vector3.Distance(aircraft.transform.position, checkpoints[run.NextCheckpoint].transform.position);
                    gateValue.text = $"PUERTA  {run.NextCheckpoint + 1:00}   ·   {Mathf.RoundToInt(distance):000} M";
                }
                else gateValue.text = "CRUZA LA META";
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

            Panel("Flight_Data_Panel", raceLayer.transform, new Vector2(0f, 1f), new Vector2(30f, -28f), new Vector2(350f, 142f));
            Label("SPEED_LABEL", raceLayer.transform, "VELOCIDAD", 20, Mint, new Vector2(0f, 1f), new Vector2(56f, -43f), new Vector2(145f, 30f), TextAnchor.MiddleLeft);
            speedValue = Label("SPEED_VALUE", raceLayer.transform, "000 KM/H", 38, White, new Vector2(0f, 1f), new Vector2(54f, -81f), new Vector2(230f, 50f), TextAnchor.MiddleLeft);
            Label("ALTITUDE_LABEL", raceLayer.transform, "ALTITUD", 20, Mint, new Vector2(0f, 1f), new Vector2(215f, -43f), new Vector2(110f, 30f), TextAnchor.MiddleLeft);
            altitudeValue = Label("ALTITUDE_VALUE", raceLayer.transform, "000 M", 34, White, new Vector2(0f, 1f), new Vector2(214f, -82f), new Vector2(120f, 48f), TextAnchor.MiddleLeft);

            Panel("Time_Panel", raceLayer.transform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(230f, 106f));
            Label("TIME_LABEL", raceLayer.transform, "TIEMPO", 19, Yellow, new Vector2(0.5f, 1f), new Vector2(0f, -31f), new Vector2(190f, 28f), TextAnchor.MiddleCenter);
            timeValue = Label("TIME_VALUE", raceLayer.transform, "60", 48, White, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(190f, 56f), TextAnchor.MiddleCenter);

            Panel("Score_Panel", raceLayer.transform, new Vector2(1f, 1f), new Vector2(-30f, -28f), new Vector2(270f, 112f));
            Label("SCORE_LABEL", raceLayer.transform, "PUNTOS", 20, Yellow, new Vector2(1f, 1f), new Vector2(-52f, -32f), new Vector2(200f, 28f), TextAnchor.MiddleRight);
            scoreValue = Label("SCORE_VALUE", raceLayer.transform, "00000", 43, White, new Vector2(1f, 1f), new Vector2(-50f, -69f), new Vector2(200f, 52f), TextAnchor.MiddleRight);

            // The route replaces the old floating checkpoint counter with actual spatial guidance.
            Panel("Radar_Panel", raceLayer.transform, new Vector2(0f, 0f), new Vector2(28f, 26f), new Vector2(316f, 338f));
            Label("RADAR_LABEL", raceLayer.transform, "RADAR DE RUTA", 21, Mint, new Vector2(0f, 0f), new Vector2(54f, 316f), new Vector2(250f, 32f), TextAnchor.MiddleLeft);
            var radarObject = new GameObject("ROUTE_RADAR", typeof(RectTransform), typeof(AirRaceRadarDisplay));
            radarObject.transform.SetParent(raceLayer.transform, false);
            RectTransform radarRect = radarObject.GetComponent<RectTransform>();
            radarRect.anchorMin = radarRect.anchorMax = Vector2.zero;
            radarRect.pivot = Vector2.zero;
            radarRect.anchoredPosition = new Vector2(48f, 82f);
            radarRect.sizeDelta = new Vector2(270f, 220f);
            radar = radarObject.GetComponent<AirRaceRadarDisplay>();
            gateValue = Label("ACTIVE_GATE", raceLayer.transform, "PUERTA  01   ·   000 M", 20, White, new Vector2(0f, 0f), new Vector2(52f, 45f), new Vector2(270f, 30f), TextAnchor.MiddleLeft);

            // Small flight-path reference at screen center, leaving the view unobstructed.
            Label("FLIGHT_RETICLE", raceLayer.transform, "＋", 34, Mint, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f), TextAnchor.MiddleCenter);
            impactValue = Label("IMPACT_FEEDBACK", raceLayer.transform, "¡REBOTE!", 40, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(480f, 64f), TextAnchor.MiddleCenter);
            impactValue.gameObject.SetActive(false);

            Panel("Energy_Panel", raceLayer.transform, new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(510f, 158f));
            energyValue = Label("ENERGY_VALUE", raceLayer.transform, "ENERGÍA DE VUELO  72%", 25, White, new Vector2(1f, 0f), new Vector2(-56f, 112f), new Vector2(430f, 36f), TextAnchor.MiddleRight);
            Box("ENERGY_BAR_BACK", raceLayer.transform, new Color(0.01f, 0.025f, 0.08f, 1f), new Vector2(1f, 0f), new Vector2(-56f, 78f), new Vector2(430f, 20f));
            energyFill = Box("ENERGY_BAR_FILL", raceLayer.transform, Yellow, new Vector2(1f, 0f), new Vector2(-56f, 80f), new Vector2(426f, 14f));
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            energyFill.fillAmount = 0.72f;
            boostValue = Label("BOOST_VALUE", raceLayer.transform, "SIN CARGA  ·  BUSCA UNA LATA", 22, Yellow, new Vector2(1f, 0f), new Vector2(-56f, 38f), new Vector2(430f, 32f), TextAnchor.MiddleRight);

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
            float accentX = anchor.x == 0f ? 8f : -size.x + 8f;
            float accentY = anchor.y == 0f ? 8f : -8f;
            Box(name + "_Accent", parent, Blue, anchor, position + new Vector2(accentX, accentY), new Vector2(6f, size.y - 16f));
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
