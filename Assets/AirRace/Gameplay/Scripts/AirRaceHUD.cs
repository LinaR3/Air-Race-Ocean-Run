using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AirRace.Gameplay
{
    /// <summary>Readable large-display HUD and minimal in-world-inspired menu system.</summary>
    public sealed class AirRaceHUD : MonoBehaviour
    {
        private static readonly Color Navy = new Color(0.012f, 0.045f, 0.12f, 0.78f);
        private static readonly Color Blue = new Color(0.035f, 0.24f, 0.52f, 0.98f);
        private static readonly Color Red = new Color(0.88f, 0.035f, 0.12f, 1f);
        private static readonly Color Yellow = new Color(1f, 0.76f, 0.04f, 1f);
        private static readonly Color White = new Color(0.98f, 0.99f, 1f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.84f, 0.92f, 1f);

        private AirRaceRunManager run;
        private AircraftFlightController aircraft;
        private AirRaceRadarDisplay radar;
        private AirRaceCheckpoint[] checkpoints = Array.Empty<AirRaceCheckpoint>();
        private TMP_Text timeValue, speedValue, altitudeValue, gateValue, scoreValue, energyValue, boostValue;
        private TMP_Text resultTitle, resultDetails, startPrompt, countdownValue;
        private Image energyFill;
        private GameObject raceLayer, startLayer, resultLayer, pauseLayer, optionsLayer, exitLayer;
        private GameObject pauseButton, startButtons;
        private TMP_Text[] startControlLines;
        private float radarUpdateTimer;
        private bool showingOptions;
        private bool showingExit;

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
            if (run == null || raceLayer == null) return;
            bool racing = run.State == RaceState.Racing;
            bool paused = run.State == RaceState.Paused;
            bool terminal = run.State == RaceState.Won || run.State == RaceState.Failed;
            raceLayer.SetActive(racing);
            pauseButton.SetActive(racing);
            pauseLayer.SetActive(paused && !showingOptions && !showingExit);
            startLayer.SetActive((run.State == RaceState.Ready || run.State == RaceState.Countdown) && !showingOptions && !showingExit);
            optionsLayer.SetActive(showingOptions);
            exitLayer.SetActive(showingExit);
            startButtons.SetActive(run.State == RaceState.Ready);
            resultLayer.SetActive(terminal);

            if (run.State == RaceState.Ready)
            {
                startPrompt.text = "PULSA JUGAR PARA COMENZAR";
                countdownValue.text = "";
                SetControlLines(true);
                return;
            }
            if (run.State == RaceState.Countdown)
            {
                startPrompt.text = "ALÍNEATE CON LA PRIMERA PUERTA";
                countdownValue.text = Mathf.CeilToInt(run.CountdownRemaining).ToString();
                SetControlLines(false);
                return;
            }
            if (terminal)
            {
                resultTitle.text = run.State == RaceState.Won ? "VUELTA COMPLETADA" : "VUELO TERMINADO";
                resultDetails.text = run.State == RaceState.Won
                    ? $"PUNTUACIÓN  {run.Score:00000}     ·     TIEMPO  {Mathf.CeilToInt(run.TimeRemaining):00} s"
                    : $"{run.EndReason}\nPUNTUACIÓN  {run.Score:00000}";
                return;
            }

            timeValue.text = Mathf.CeilToInt(run.TimeRemaining).ToString("00");
            scoreValue.text = run.Score.ToString("00000");
            energyValue.text = $"ENERGÍA  {Mathf.CeilToInt(run.Energy)}%";
            energyFill.fillAmount = Mathf.Clamp01(run.Energy / 100f);
            energyFill.color = Color.Lerp(Red, Yellow, Mathf.Clamp01(run.Energy / 100f));
            boostValue.text = run.IsBoosting
                ? $"IMPULSO ACTIVO  {run.BoostTimeRemaining:0.0}s   ·   COMBO x{run.Combo}"
                : run.BoostCharges > 0
                    ? $"LATA LISTA  x{run.BoostCharges}   ·   ESPACIO PARA IMPULSO"
                    : "SIN LATAS   ·   SIGUE LA RUTA PARA RECARGAR";

            if (aircraft == null) return;
            speedValue.text = Mathf.RoundToInt(aircraft.AirSpeed * 3.6f).ToString("000");
            altitudeValue.text = Mathf.RoundToInt(aircraft.Altitude).ToString("000");
            radarUpdateTimer -= Time.unscaledDeltaTime;
            if (radarUpdateTimer <= 0f)
            {
                radarUpdateTimer = 0.12f;
                Vector3[] gatePositions = new Vector3[checkpoints.Length];
                for (int i = 0; i < checkpoints.Length; i++) gatePositions[i] = checkpoints[i].transform.position;
                AirRaceCanPickup[] pickupObjects = FindObjectsByType<AirRaceCanPickup>(FindObjectsSortMode.None);
                Vector3[] canPositions = new Vector3[pickupObjects.Length];
                for (int i = 0; i < pickupObjects.Length; i++) canPositions[i] = pickupObjects[i].transform.position;
                radar.SetRoute(aircraft.transform.position, aircraft.Heading, gatePositions, run.NextCheckpoint, canPositions);
            }

            if (run.NextCheckpoint < checkpoints.Length)
            {
                float distance = Vector3.Distance(aircraft.transform.position, checkpoints[run.NextCheckpoint].transform.position);
                gateValue.text = $"SIGUIENTE PUERTA {run.NextCheckpoint + 1:00} / {checkpoints.Length:00}     ·     {Mathf.RoundToInt(distance)} m";
            }
            else gateValue.text = "RUTA COMPLETA  ·  CRUZA LA META";
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.45f;
            EnsureEventSystem();

            raceLayer = MakeRect("Race_HUD", canvasObject.transform).gameObject;
            AddTopScrim(raceLayer.transform);
            pauseButton = ActionButton("PAUSE_BUTTON", raceLayer.transform, "PAUSA", new Vector2(1f, 1f), new Vector2(-58f, -46f), new Vector2(100f, 58f), Navy, () => run.PauseRace()).gameObject;

            // Open-center HUD: no opaque dashboard covering the flight view.
            Label("SPEED_LABEL", raceLayer.transform, "VELOCIDAD", 16, Muted, new Vector2(0f, 1f), new Vector2(52f, -34f), new Vector2(175f, 25f), TextAlignmentOptions.Left);
            speedValue = Label("SPEED_VALUE", raceLayer.transform, "000", 50, White, new Vector2(0f, 1f), new Vector2(48f, -82f), new Vector2(170f, 62f), TextAlignmentOptions.Left);
            Label("SPEED_UNIT", raceLayer.transform, "KM/H", 15, Yellow, new Vector2(0f, 1f), new Vector2(174f, -96f), new Vector2(74f, 30f), TextAlignmentOptions.Left);
            Label("ALTITUDE_LABEL", raceLayer.transform, "ALTITUD", 16, Muted, new Vector2(0f, 1f), new Vector2(282f, -34f), new Vector2(150f, 25f), TextAlignmentOptions.Left);
            altitudeValue = Label("ALTITUDE_VALUE", raceLayer.transform, "000", 50, White, new Vector2(0f, 1f), new Vector2(278f, -82f), new Vector2(150f, 62f), TextAlignmentOptions.Left);
            Label("ALTITUDE_UNIT", raceLayer.transform, "M", 15, Yellow, new Vector2(0f, 1f), new Vector2(391f, -96f), new Vector2(32f, 30f), TextAlignmentOptions.Left);

            Label("TIME_LABEL", raceLayer.transform, "TIEMPO", 16, Muted, new Vector2(0.5f, 1f), new Vector2(0f, -29f), new Vector2(160f, 25f), TextAlignmentOptions.Center);
            timeValue = Label("TIME_VALUE", raceLayer.transform, "60", 52, White, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(180f, 64f), TextAlignmentOptions.Center);
            Label("SCORE_LABEL", raceLayer.transform, "PUNTOS", 16, Muted, new Vector2(1f, 1f), new Vector2(-148f, -34f), new Vector2(150f, 25f), TextAlignmentOptions.Right);
            scoreValue = Label("SCORE_VALUE", raceLayer.transform, "00000", 44, White, new Vector2(1f, 1f), new Vector2(-46f, -82f), new Vector2(250f, 58f), TextAlignmentOptions.Right);

            Label("RADAR_LABEL", raceLayer.transform, "RADAR DE RUTA", 15, Muted, new Vector2(0f, 0f), new Vector2(42f, 291f), new Vector2(240f, 24f), TextAlignmentOptions.Left);
            var radarObject = new GameObject("ROUTE_RADAR", typeof(RectTransform), typeof(AirRaceRadarDisplay));
            radarObject.transform.SetParent(raceLayer.transform, false);
            RectTransform radarRect = radarObject.GetComponent<RectTransform>();
            radarRect.anchorMin = radarRect.anchorMax = Vector2.zero;
            radarRect.pivot = Vector2.zero;
            radarRect.anchoredPosition = new Vector2(40f, 65f);
            radarRect.sizeDelta = new Vector2(230f, 230f);
            radar = radarObject.GetComponent<AirRaceRadarDisplay>();
            gateValue = Label("ACTIVE_GATE", raceLayer.transform, "SIGUIENTE PUERTA 01 / 06 · 000 m", 18, White, new Vector2(0f, 0f), new Vector2(42f, 28f), new Vector2(420f, 32f), TextAlignmentOptions.Left);

            energyValue = Label("ENERGY_VALUE", raceLayer.transform, "ENERGÍA 72%", 19, White, new Vector2(1f, 0f), new Vector2(-48f, 105f), new Vector2(345f, 30f), TextAlignmentOptions.Right);
            Box("ENERGY_BAR_BACK", raceLayer.transform, new Color(0.01f, 0.025f, 0.08f, 0.72f), new Vector2(1f, 0f), new Vector2(-48f, 78f), new Vector2(330f, 9f));
            energyFill = Box("ENERGY_BAR_FILL", raceLayer.transform, Yellow, new Vector2(1f, 0f), new Vector2(-48f, 78f), new Vector2(326f, 5f));
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Horizontal;
            energyFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            boostValue = Label("BOOST_VALUE", raceLayer.transform, "SIN LATAS · SIGUE LA RUTA", 16, Yellow, new Vector2(1f, 0f), new Vector2(-48f, 34f), new Vector2(580f, 30f), TextAlignmentOptions.Right);
            Label("FLIGHT_CONTROLS_HINT", raceLayer.transform, "WASD / FLECHAS  VUELO      SHIFT  FRENO      SPACE  IMPULSO      ESC  PAUSA", 14, White, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1000f, 24f), TextAlignmentOptions.Center);

            startLayer = MakeRect("Race_Start_Overlay", canvasObject.transform).gameObject;
            // Menu uses the world scene as its hero artwork instead of a plane illustration or opaque modal.
            AddEdgeScrims(startLayer.transform);
            Label("START_KICKER", startLayer.transform, "OCEAN FLIGHT  /  NIVEL 01", 18, Yellow, new Vector2(0f, 0.5f), new Vector2(88f, 277f), new Vector2(680f, 30f), TextAlignmentOptions.Left);
            Label("START_TITLE", startLayer.transform, "AIR RACE\nOCEAN RUN", 74, White, new Vector2(0f, 0.5f), new Vector2(84f, 112f), new Vector2(740f, 170f), TextAlignmentOptions.Left);
            Label("START_SUBTITLE", startLayer.transform, "VUELO ARCADE SOBRE EL OCÉANO", 20, Muted, new Vector2(0f, 0.5f), new Vector2(90f, 9f), new Vector2(700f, 34f), TextAlignmentOptions.Left);
            Label("START_OBJECTIVE", startLayer.transform, "6 PUERTAS  ·  1 VUELTA  ·  60 SEGUNDOS", 18, White, new Vector2(0f, 0.5f), new Vector2(90f, -43f), new Vector2(700f, 34f), TextAlignmentOptions.Left);
            string[] controls = { "W / S  ·  SUBIR / BAJAR", "A / D  ·  GIRAR     ↑ ↓ ← →  ·  ALTERNATIVA", "SHIFT  ·  FRENAR Y CERRAR EL GIRO", "SPACE  ·  IMPULSO Y GIRO DE BARRIL" };
            float[] lineY = { -105f, -147f, -189f, -231f };
            startControlLines = new TMP_Text[controls.Length];
            for (int i = 0; i < controls.Length; i++)
                startControlLines[i] = Label("START_CONTROL_" + (i + 1), startLayer.transform, controls[i], 16, Muted, new Vector2(0f, 0.5f), new Vector2(90f, lineY[i]), new Vector2(780f, 28f), TextAlignmentOptions.Left);
            startPrompt = Label("START_PROMPT", startLayer.transform, "PULSA JUGAR PARA COMENZAR", 17, Yellow, new Vector2(0f, 0.5f), new Vector2(90f, -292f), new Vector2(700f, 34f), TextAlignmentOptions.Left);
            countdownValue = Label("START_COUNTDOWN", startLayer.transform, "", 96, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(240f, 130f), TextAlignmentOptions.Center);
            startButtons = MakeRect("START_MENU_BUTTONS", startLayer.transform).gameObject;
            ActionButton("START_PLAY", startButtons.transform, "JUGAR  ›", new Vector2(1f, 0.5f), new Vector2(-196f, 65f), new Vector2(300f, 68f), Red, () => run.StartRace());
            ActionButton("START_OPTIONS", startButtons.transform, "OPCIONES", new Vector2(1f, 0.5f), new Vector2(-196f, -20f), new Vector2(300f, 58f), Blue, () => ShowOptions(false));
            ActionButton("START_EXIT", startButtons.transform, "SALIR", new Vector2(1f, 0.5f), new Vector2(-196f, -96f), new Vector2(300f, 54f), Navy, () => ShowExit(false));

            resultLayer = MakeRect("Race_Result_Overlay", canvasObject.transform).gameObject;
            AddCenterScrim(resultLayer.transform);
            resultTitle = Label("RESULT_TITLE", resultLayer.transform, "VUELO TERMINADO", 58, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 98f), new Vector2(1150f, 82f), TextAlignmentOptions.Center);
            resultDetails = Label("RESULT_DETAILS", resultLayer.transform, "PUNTUACIÓN  00000", 26, White, new Vector2(0.5f, 0.5f), new Vector2(0f, 13f), new Vector2(1150f, 94f), TextAlignmentOptions.Center);
            ActionButton("RESULT_RETRY", resultLayer.transform, "REINTENTAR", new Vector2(0.5f, 0.5f), new Vector2(-164f, -91f), new Vector2(300f, 62f), Red, () => run.Restart());
            ActionButton("RESULT_MENU", resultLayer.transform, "MENÚ", new Vector2(0.5f, 0.5f), new Vector2(164f, -91f), new Vector2(300f, 62f), Blue, () => run.ReturnToMenu());
            resultLayer.SetActive(false);

            pauseLayer = MakeRect("Race_Pause_Overlay", canvasObject.transform).gameObject;
            AddCenterScrim(pauseLayer.transform);
            Label("PAUSE_TITLE", pauseLayer.transform, "PAUSA", 56, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 139f), new Vector2(900f, 82f), TextAlignmentOptions.Center);
            ActionButton("PAUSE_CONTINUE", pauseLayer.transform, "CONTINUAR", new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(340f, 62f), Red, () => run.ResumeRace());
            ActionButton("PAUSE_RETRY", pauseLayer.transform, "REINTENTAR", new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(340f, 58f), Blue, () => run.Restart());
            ActionButton("PAUSE_MENU", pauseLayer.transform, "MENÚ", new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(340f, 54f), Navy, () => run.ReturnToMenu());
            pauseLayer.SetActive(false);

            optionsLayer = MakeRect("Race_Options_Overlay", canvasObject.transform).gameObject;
            AddCenterScrim(optionsLayer.transform);
            Label("OPTIONS_TITLE", optionsLayer.transform, "OPCIONES", 48, Yellow, new Vector2(0.5f, 0.5f), new Vector2(0f, 139f), new Vector2(900f, 76f), TextAlignmentOptions.Center);
            Label("OPTIONS_CONTROLS", optionsLayer.transform, "WASD / FLECHAS  ·  VUELO\nSHIFT  ·  FRENAR Y CERRAR EL GIRO\nSPACE  ·  IMPULSO Y GIRO DE BARRIL\nESC  ·  PAUSA", 22, White, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 170f), TextAlignmentOptions.Center);
            ActionButton("OPTIONS_FULLSCREEN", optionsLayer.transform, "PANTALLA COMPLETA", new Vector2(0.5f, 0.5f), new Vector2(0f, -81f), new Vector2(380f, 58f), Blue, () => Screen.fullScreen = !Screen.fullScreen);
            ActionButton("OPTIONS_BACK", optionsLayer.transform, "VOLVER", new Vector2(0.5f, 0.5f), new Vector2(0f, -155f), new Vector2(300f, 54f), Navy, () => ShowOptions(true));
            optionsLayer.SetActive(false);

            exitLayer = MakeRect("Race_Exit_Overlay", canvasObject.transform).gameObject;
            AddCenterScrim(exitLayer.transform);
            Label("EXIT_NOTICE", exitLayer.transform, "GRACIAS POR JUGAR\nCIERRA ESTA PESTAÑA PARA SALIR", 28, White, new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(900f, 120f), TextAlignmentOptions.Center);
            ActionButton("EXIT_BACK", exitLayer.transform, "VOLVER", new Vector2(0.5f, 0.5f), new Vector2(0f, -68f), new Vector2(300f, 58f), Blue, () => ShowExit(true));
            exitLayer.SetActive(false);
        }

        private void SetControlLines(bool visible)
        {
            foreach (TMP_Text line in startControlLines) line.gameObject.SetActive(visible);
        }

        private void ShowOptions(bool close)
        {
            showingOptions = !close;
            if (!close) showingExit = false;
            if (close && run.State == RaceState.Paused) pauseLayer.SetActive(true);
        }

        private void ShowExit(bool close)
        {
            showingExit = !close;
            if (close && run.State == RaceState.Ready) startLayer.SetActive(true);
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
            Label(name + "_LABEL", buttonObject.transform, caption, 20, White, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(16f, 8f), TextAlignmentOptions.Center);
            return button;
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void AddTopScrim(Transform parent)
        {
            Box("HUD_TOP_SCRIM", parent, new Color(0.005f, 0.02f, 0.06f, 0.38f), new Vector2(0.5f, 1f), new Vector2(0f, -53f), new Vector2(2100f, 130f));
        }

        private static void AddEdgeScrims(Transform parent)
        {
            // Stepped low-opacity vignettes keep both text columns readable without a hard modal card.
            Vector2 leftCenter = new Vector2(0f, 0.5f);
            Box("MENU_LEFT_SCRIM_01", parent, new Color(0.006f, 0.03f, 0.08f, 0.52f), leftCenter, Vector2.zero, new Vector2(650f, 1400f));
            Box("MENU_LEFT_SCRIM_02", parent, new Color(0.006f, 0.03f, 0.08f, 0.38f), leftCenter, new Vector2(650f, 0f), new Vector2(130f, 1400f));
            Box("MENU_LEFT_SCRIM_03", parent, new Color(0.006f, 0.03f, 0.08f, 0.24f), leftCenter, new Vector2(780f, 0f), new Vector2(130f, 1400f));
            Box("MENU_LEFT_SCRIM_04", parent, new Color(0.006f, 0.03f, 0.08f, 0.12f), leftCenter, new Vector2(910f, 0f), new Vector2(130f, 1400f));
            Box("MENU_RIGHT_SCRIM", parent, new Color(0.006f, 0.03f, 0.08f, 0.2f), new Vector2(1f, 0.5f), new Vector2(-360f, 0f), new Vector2(620f, 1400f));
        }

        private static void AddCenterScrim(Transform parent)
        {
            Image image = Box("CENTER_SCRIM", parent, new Color(0.005f, 0.025f, 0.07f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 430f));
            image.raycastTarget = true;
        }

        private static Image Box(string name, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }

        private static TMP_Text Label(string name, Transform parent, string value, int size, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            TMP_Text text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value; text.fontSize = size; text.fontStyle = name.EndsWith("_LABEL", StringComparison.Ordinal) ? FontStyles.Normal : FontStyles.Bold;
            text.characterSpacing = name.EndsWith("_LABEL", StringComparison.Ordinal) ? 1.2f : 0f;
            text.color = color; text.alignment = alignment; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>(); shadow.effectColor = new Color(0.005f, 0.018f, 0.04f, 0.95f); shadow.effectDistance = new Vector2(1.6f, -1.6f); shadow.useGraphicAlpha = true;
            return text;
        }
    }
}
