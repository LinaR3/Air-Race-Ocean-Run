using System;
using TMPro;
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
        private static readonly Color LabelColor = new Color(0.78f, 0.89f, 0.96f, 1f);

        private AirRaceRunManager run;
        private AircraftFlightController aircraft;
        private TMP_Text timeValue;
        private TMP_Text speedValue;
        private TMP_Text altitudeValue;
        private TMP_Text gateValue;
        private TMP_Text scoreValue;
        private TMP_Text energyValue;
        private TMP_Text boostValue;
        private TMP_Text impactValue;
        private TMP_Text resultTitle;
        private TMP_Text resultDetails;
        private Image energyFill;
        private GameObject raceLayer;
        private GameObject startLayer;
        private GameObject resultLayer;
        private GameObject pauseLayer;
        private GameObject optionsLayer;
        private GameObject exitLayer;
        private GameObject pauseButton;
        private GameObject startButtons;
        private TMP_Text startTitle;
        private TMP_Text startDetails;
        private TMP_Text countdownValue;
        private TMP_Text[] startControlLines;
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
            bool paused = run.State == RaceState.Paused;
            bool terminal = run.State == RaceState.Won || run.State == RaceState.Failed;
            raceLayer.SetActive(racing);
            if (pauseButton != null) pauseButton.SetActive(racing);
            pauseLayer.SetActive(paused && !optionsLayer.activeSelf && !exitLayer.activeSelf);
            startLayer.SetActive(run.State == RaceState.Ready || run.State == RaceState.Countdown);
            if (startButtons != null) startButtons.SetActive(run.State == RaceState.Ready);
            resultLayer.SetActive(terminal);

            if (run.State == RaceState.Ready)
            {
                startTitle.text = "AIR RACE OCEAN RUN";
                startDetails.text = "OBJETIVO: 6 CHECKPOINTS Y META";
                startDetails.fontSize = 30;
                startDetails.rectTransform.anchoredPosition = new Vector2(0f, 135f);
                foreach (TMP_Text line in startControlLines) line.gameObject.SetActive(true);
                countdownValue.text = "LISTA LA CARRERA";
                countdownValue.fontSize = 34;
                countdownValue.rectTransform.anchoredPosition = new Vector2(0f, -260f);
                return;
            }

            if (run.State == RaceState.Countdown)
            {
                startTitle.text = "PREPÁRATE";
                startDetails.text = "MANTÉN EL RUMBO HACIA LA PRIMERA PUERTA";
                startDetails.fontSize = 26;
                startDetails.rectTransform.anchoredPosition = new Vector2(0f, 28f);
                foreach (TMP_Text line in startControlLines) line.gameObject.SetActive(false);
                countdownValue.text = Mathf.CeilToInt(run.CountdownRemaining).ToString();
                countdownValue.fontSize = 96;
                countdownValue.rectTransform.anchoredPosition = new Vector2(0f, -135f);
                return;
            }

            if (terminal)
            {
                resultTitle.text = run.State == RaceState.Won ? "¡VUELTA COMPLETADA!" : "VUELO TERMINADO";
                resultDetails.text = run.State == RaceState.Won
                    ? $"PUNTUACIÓN  {run.Score:00000}\nTIEMPO  {Mathf.CeilToInt(run.TimeRemaining):00} s"
                    : $"{run.EndReason}\nPUNTUACIÓN  {run.Score:00000}";
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
                    gateValue.text = $"PUERTA {run.NextCheckpoint + 1:00} · {Mathf.RoundToInt(distance):000} M";
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

            EnsureEventSystem();

            raceLayer = MakeRect("Race_HUD", canvasObject.transform).gameObject;
            pauseButton = ActionButton("PAUSE_BUTTON", raceLayer.transform, "PAUSA", new Vector2(1f, 1f), new Vector2(-58f, -115f), new Vector2(104f, 48f), Yellow, () => run.PauseRace()).gameObject;

            // Keep the flight picture open: labels float over the scene with a subtle shadow, not opaque cards.
            Label("SPEED_LABEL", raceLayer.transform, "VELOCIDAD", 15, LabelColor, new Vector2(0f, 1f), new Vector2(38f, -32f), new Vector2(160f, 24f), TextAnchor.MiddleLeft);
            speedValue = Label("SPEED_VALUE", raceLayer.transform, "000 KM/H", 34, White, new Vector2(0f, 1f), new Vector2(38f, -65f), new Vector2(205f, 45f), TextAnchor.MiddleLeft);
            Label("ALTITUDE_LABEL", raceLayer.transform, "ALTITUD", 15, LabelColor, new Vector2(0f, 1f), new Vector2(258f, -32f), new Vector2(140f, 24f), TextAnchor.MiddleLeft);
            altitudeValue = Label("ALTITUDE_VALUE", raceLayer.transform, "000 M", 34, White, new Vector2(0f, 1f), new Vector2(258f, -65f), new Vector2(145f, 45f), TextAnchor.MiddleLeft);

            Label("TIME_LABEL", raceLayer.transform, "TIEMPO", 15, LabelColor, new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(170f, 22f), TextAnchor.MiddleCenter);
            timeValue = Label("TIME_VALUE", raceLayer.transform, "60", 40, White, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(180f, 48f), TextAnchor.MiddleCenter);

            Label("SCORE_LABEL", raceLayer.transform, "PUNTOS", 15, LabelColor, new Vector2(1f, 1f), new Vector2(-38f, -29f), new Vector2(170f, 22f), TextAnchor.MiddleRight);
            scoreValue = Label("SCORE_VALUE", raceLayer.transform, "00000", 36, White, new Vector2(1f, 1f), new Vector2(-38f, -65f), new Vector2(190f, 45f), TextAnchor.MiddleRight);

            // Only the circular route graphic remains; no rectangular radar canvas or backing plate.
            Label("RADAR_LABEL", raceLayer.transform, "RUTA", 14, LabelColor, new Vector2(0f, 0f), new Vector2(42f, 295f), new Vector2(200f, 22f), TextAnchor.MiddleLeft);
            var radarObject = new GameObject("ROUTE_RADAR", typeof(RectTransform), typeof(AirRaceRadarDisplay));
            radarObject.transform.SetParent(raceLayer.transform, false);
            RectTransform radarRect = radarObject.GetComponent<RectTransform>();
            radarRect.anchorMin = radarRect.anchorMax = Vector2.zero;
            radarRect.pivot = Vector2.zero;
            radarRect.anchoredPosition = new Vector2(42f, 66f);
            radarRect.sizeDelta = new Vector2(232f, 232f);
            radar = radarObject.GetComponent<AirRaceRadarDisplay>();
            gateValue = Label("ACTIVE_GATE", raceLayer.transform, "PUERTA 01 · 000 M", 15, White, new Vector2(0f, 0f), new Vector2(42f, 34f), new Vector2(270f, 24f), TextAnchor.MiddleLeft);

            impactValue = Label("IMPACT_FEEDBACK", raceLayer.transform, "¡REBOTE!", 32, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(480f, 52f), TextAnchor.MiddleCenter);
            impactValue.gameObject.SetActive(false);

            energyValue = Label("ENERGY_VALUE", raceLayer.transform, "ENERGÍA 72%", 18, White, new Vector2(1f, 0f), new Vector2(-44f, 92f), new Vector2(330f, 28f), TextAnchor.MiddleRight);
            Box("ENERGY_BAR_BACK", raceLayer.transform, new Color(0.01f, 0.025f, 0.08f, 0.78f), new Vector2(1f, 0f), new Vector2(-44f, 66f), new Vector2(320f, 10f));
            energyFill = Box("ENERGY_BAR_FILL", raceLayer.transform, Yellow, new Vector2(1f, 0f), new Vector2(-44f, 66f), new Vector2(316f, 6f));
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            energyFill.fillAmount = 0.72f;
            boostValue = Label("BOOST_VALUE", raceLayer.transform, "SIN CARGA · BUSCA UNA LATA", 16, Yellow, new Vector2(1f, 0f), new Vector2(-44f, 34f), new Vector2(360f, 26f), TextAnchor.MiddleRight);

            startLayer = MakeRect("Race_Start_Overlay", canvasObject.transform).gameObject;
            Image startShade = startLayer.AddComponent<Image>();
            startShade.color = new Color(0.005f, 0.025f, 0.09f, 0.72f);
            startShade.raycastTarget = false;
            Panel("START_CARD", startLayer.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1420f, 760f));
            startTitle = Label("START_TITLE", startLayer.transform, "AIR RACE OCEAN RUN", 56, Yellow,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(1320f, 86f), TextAnchor.MiddleCenter);
            startDetails = Label("START_OBJECTIVE", startLayer.transform, string.Empty, 30, White,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(1320f, 42f), TextAnchor.MiddleCenter);
            string[] controlLines = {
                "W / S o ↑ / ↓   SUBIR Y BAJAR",
                "A / D o ← / →   GIRAR",
                "SHIFT   FRENAR Y GIRAR MÁS CERRADO",
                "SPACE   IMPULSO Y GIRO DE BARRIL",
                "PILONES RECARGAN ENERGÍA · LATAS DAN IMPULSO"
            };
            float[] controlLineY = { 76f, 30f, -16f, -62f, -108f };
            startControlLines = new TMP_Text[controlLines.Length];
            for (int i = 0; i < controlLines.Length; i++)
                startControlLines[i] = Label("START_CONTROL_" + (i + 1), startLayer.transform, controlLines[i], 26, White,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, controlLineY[i]), new Vector2(1320f, 38f), TextAnchor.MiddleCenter);
            countdownValue = Label("START_PROMPT", startLayer.transform, "", 34, Yellow,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -270f), new Vector2(1300f, 68f), TextAnchor.MiddleCenter);
            startButtons = MakeRect("START_MENU_BUTTONS", startLayer.transform).gameObject;
            ActionButton("START_PLAY", startButtons.transform, "JUGAR", new Vector2(0.5f, 0.5f), new Vector2(0f, -195f), new Vector2(300f, 62f), new Color(0.85f, 0.04f, 0.12f), () => run.StartRace());
            ActionButton("START_OPTIONS", startButtons.transform, "OPCIONES", new Vector2(0.5f, 0.5f), new Vector2(0f, -272f), new Vector2(300f, 56f), Blue, () => ShowOptions(false));
            ActionButton("START_EXIT", startButtons.transform, "SALIR", new Vector2(0.5f, 0.5f), new Vector2(0f, -342f), new Vector2(300f, 52f), Navy, () => ShowExit(false));

            resultLayer = MakeRect("Race_Result_Overlay", canvasObject.transform).gameObject;
            Image shade = resultLayer.AddComponent<Image>();
            shade.color = new Color(0.005f, 0.025f, 0.09f, 0.78f);
            resultTitle = Label("RESULT_TITLE", resultLayer.transform, "VUELO TERMINADO", 58, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1200f, 100f), TextAnchor.MiddleCenter);
            resultDetails = Label("RESULT_DETAILS", resultLayer.transform, "PUNTUACIÓN  00000", 34, White, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(1200f, 210f), TextAnchor.MiddleCenter);
            ActionButton("RESULT_RETRY", resultLayer.transform, "REINTENTAR", new Vector2(0.5f, 0.5f), new Vector2(-165f, -210f), new Vector2(300f, 62f), new Color(0.85f, 0.04f, 0.12f), () => run.Restart());
            ActionButton("RESULT_MENU", resultLayer.transform, "MENÚ", new Vector2(0.5f, 0.5f), new Vector2(165f, -210f), new Vector2(300f, 62f), Blue, () => run.ReturnToMenu());
            resultLayer.SetActive(false);

            pauseLayer = MakeRect("Race_Pause_Overlay", canvasObject.transform).gameObject;
            Image pauseShade = pauseLayer.AddComponent<Image>(); pauseShade.color = new Color(0.005f, 0.025f, 0.09f, 0.58f); pauseShade.raycastTarget = true;
            Label("PAUSE_TITLE", pauseLayer.transform, "PAUSA", 56, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 90f), TextAnchor.MiddleCenter);
            ActionButton("PAUSE_CONTINUE", pauseLayer.transform, "CONTINUAR", new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(340f, 62f), new Color(0.85f, 0.04f, 0.12f), () => run.ResumeRace());
            ActionButton("PAUSE_RETRY", pauseLayer.transform, "REINTENTAR", new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(340f, 58f), Blue, () => run.Restart());
            ActionButton("PAUSE_MENU", pauseLayer.transform, "MENÚ", new Vector2(0.5f, 0.5f), new Vector2(0f, -115f), new Vector2(340f, 54f), Navy, () => run.ReturnToMenu());
            pauseLayer.SetActive(false);

            optionsLayer = MakeRect("Race_Options_Overlay", canvasObject.transform).gameObject;
            Image optionsShade = optionsLayer.AddComponent<Image>(); optionsShade.color = new Color(0.005f, 0.025f, 0.09f, 0.72f); optionsShade.raycastTarget = true;
            Label("OPTIONS_TITLE", optionsLayer.transform, "OPCIONES", 48, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 125f), new Vector2(900f, 76f), TextAnchor.MiddleCenter);
            Label("OPTIONS_CONTROLS", optionsLayer.transform, "WASD / FLECHAS  VOLAR\nSHIFT  FRENAR Y CERRAR EL GIRO\nSPACE  IMPULSO\nESC  PAUSA", 24, White, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(900f, 150f), TextAnchor.MiddleCenter);
            ActionButton("OPTIONS_FULLSCREEN", optionsLayer.transform, "PANTALLA COMPLETA", new Vector2(0.5f, 0.5f), new Vector2(0f, -88f), new Vector2(380f, 58f), Blue, () => Screen.fullScreen = !Screen.fullScreen);
            ActionButton("OPTIONS_BACK", optionsLayer.transform, "VOLVER", new Vector2(0.5f, 0.5f), new Vector2(0f, -162f), new Vector2(300f, 54f), Navy, () => ShowOptions(true));
            optionsLayer.SetActive(false);

            exitLayer = MakeRect("Race_Exit_Overlay", canvasObject.transform).gameObject;
            Image exitShade = exitLayer.AddComponent<Image>(); exitShade.color = new Color(0.005f, 0.025f, 0.09f, 0.72f); exitShade.raycastTarget = true;
            Label("EXIT_NOTICE", exitLayer.transform, "GRACIAS POR JUGAR\nCIERRA ESTA PESTAÑA PARA SALIR", 28, White, new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(900f, 120f), TextAnchor.MiddleCenter);
            ActionButton("EXIT_BACK", exitLayer.transform, "VOLVER", new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(300f, 58f), Blue, () => ShowExit(true));
            exitLayer.SetActive(false);
        }

        private void ShowOptions(bool close)
        {
            optionsLayer.SetActive(!close);
            if (!close) { startLayer.SetActive(false); pauseLayer.SetActive(false); exitLayer.SetActive(false); }
            else if (run.State == RaceState.Ready) startLayer.SetActive(true);
            else pauseLayer.SetActive(run.State == RaceState.Paused);
        }

        private void ShowExit(bool close)
        {
            exitLayer.SetActive(!close);
            if (!close) { startLayer.SetActive(false); }
            else if (run.State == RaceState.Ready) startLayer.SetActive(true);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static Button ActionButton(string name, Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = buttonObject.GetComponent<Image>(); image.color = color; image.raycastTarget = true;
            Button button = buttonObject.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            ColorBlock colors = button.colors; colors.highlightedColor = Color.Lerp(color, Color.white, 0.22f); colors.pressedColor = Color.Lerp(color, Color.black, 0.2f); button.colors = colors;
            Label(name + "_LABEL", buttonObject.transform, caption, 22, White, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(16f, 8f), TextAnchor.MiddleCenter);
            return button;
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

        private static TMP_Text Label(string name, Transform parent, string value, int size, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            TMP_Text text = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset flightFont = Resources.Load<TMP_FontAsset>("Fonts/AUDIOWIDE-REGULAR SDF");
            if (flightFont == null) flightFont = TMP_Settings.defaultFontAsset;
            text.font = flightFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = name.EndsWith("_LABEL", StringComparison.Ordinal) || name == "RADAR_LABEL"
                ? FontStyles.Normal : FontStyles.Bold;
            text.characterSpacing = name.EndsWith("_LABEL", StringComparison.Ordinal) || name == "RADAR_LABEL" ? 2f : 0f;
            text.color = color;
            text.alignment = alignment == TextAnchor.MiddleCenter ? TextAlignmentOptions.Center :
                alignment == TextAnchor.MiddleRight ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.005f, 0.018f, 0.04f, 0.9f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;
            return text;
        }
    }
}
