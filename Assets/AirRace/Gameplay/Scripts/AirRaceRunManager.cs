using UnityEngine;

namespace AirRace.Gameplay
{
    public enum RaceState { Racing, Won, Failed }

    public sealed class AirRaceRunManager : MonoBehaviour
    {
        public static AirRaceRunManager Instance { get; private set; }

        [SerializeField] private int checkpointCount = 6;
        [SerializeField] private float durationSeconds = 60f;
        [SerializeField] private float energyDrainPerSecond = 3.5f;
        [SerializeField] private float checkpointEnergy = 26f;
        [SerializeField] private float canEnergy = 30f;
        [SerializeField] private int maximumBoostCharges = 3;
        [SerializeField] private float comboInputWindow = 0.9f;

        public RaceState State { get; private set; } = RaceState.Racing;
        public int NextCheckpoint { get; private set; }
        public int Score { get; private set; }
        public int BoostCharges { get; private set; }
        public float Energy { get; private set; } = 72f;
        public float TimeRemaining { get; private set; }
        public float BoostTimeRemaining { get; private set; }
        public float ImpactFeedbackRemaining { get; private set; }
        public int Combo { get; private set; }
        public string EndReason { get; private set; }
        public int CheckpointCount => checkpointCount;
        public bool IsBoosting => BoostTimeRemaining > 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            TimeRemaining = durationSeconds;
        }

        private void Update()
        {
            if (State != RaceState.Racing) return;

            ImpactFeedbackRemaining = Mathf.Max(0f, ImpactFeedbackRemaining - Time.deltaTime);
            comboWindowRemaining = Mathf.Max(0f, comboWindowRemaining - Time.deltaTime);

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (IsBoosting) BoostTimeRemaining = Mathf.Max(0f, BoostTimeRemaining - Time.deltaTime);
            else Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);

            if (Energy <= 0f) Fail("La energía de vuelo se agotó");
            else if (TimeRemaining <= 0f) Fail("Se agotó el tiempo");
        }

        public bool PassCheckpoint(int checkpointIndex)
        {
            if (State != RaceState.Racing || checkpointIndex != NextCheckpoint) return false;

            NextCheckpoint++;
            Score += 10;
            Energy = Mathf.Min(100f, Energy + checkpointEnergy);
            return true;
        }

        public void CollectCan()
        {
            if (State != RaceState.Racing) return;
            Score += 25;
            Energy = Mathf.Min(100f, Energy + canEnergy);
            BoostCharges = Mathf.Min(maximumBoostCharges, BoostCharges + 1);
        }

        public bool TryActivateBoost()
        {
            if (State != RaceState.Racing) return false;

            if (IsBoosting)
            {
                Combo = comboWindowRemaining > 0f ? Combo + 1 : 1;
                comboWindowRemaining = comboInputWindow;
                return true;
            }

            if (BoostCharges <= 0) return false;
            BoostCharges--;
            BoostTimeRemaining = 3f;
            Combo = 1;
            comboWindowRemaining = comboInputWindow;
            return true;
        }

        public void ScoreCompletedRoll()
        {
            if (State == RaceState.Racing) Score += 10 * Mathf.Max(1, Combo);
        }

        public void HitPylon()
        {
            if (State != RaceState.Racing) return;
            Energy = Mathf.Max(1f, Energy - 18f);
            ImpactFeedbackRemaining = 1.1f;
        }

        public void TryFinish()
        {
            if (State != RaceState.Racing || NextCheckpoint < checkpointCount) return;
            State = RaceState.Won;
        }

        public void Fail(string reason)
        {
            if (State != RaceState.Racing) return;
            State = RaceState.Failed;
            EndReason = reason;
        }

        private void OnGUI()
        {
            if (State == RaceState.Racing)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(24, 18, 260, 34), $"TIME  {Mathf.CeilToInt(TimeRemaining):00}");
                GUI.Label(new Rect(24, 52, 260, 34), $"GATES  {NextCheckpoint}/{checkpointCount}");
                GUI.Label(new Rect(24, 86, 260, 34), $"SCORE  {Score}");
                GUI.Label(new Rect(24, 120, 260, 34), $"FLIGHT  {Mathf.CeilToInt(Energy)}%   BOOSTS  {BoostCharges}");
                GUI.Label(new Rect(24, 154, 260, 34), IsBoosting ? $"BOOST  {BoostTimeRemaining:0.0}s   COMBO x{Combo}" : "WASD fly · Space boost · C camera");
                GUI.Box(new Rect(24, 188, 220, 14), GUIContent.none);
                GUI.color = new Color(1f, 0.76f, 0.05f);
                GUI.Box(new Rect(27, 191, 214f * Energy / 100f, 8), GUIContent.none);
                GUI.color = Color.white;
                if (ImpactFeedbackRemaining > 0f)
                {
                    GUI.color = new Color(1f, 0.45f, 0.12f);
                    GUI.Label(new Rect(Screen.width * 0.5f - 100f, 42f, 200f, 32f), "IMPACT · REBOUND");
                    GUI.color = Color.white;
                }
            }
            else
            {
                string title = State == RaceState.Won ? "RACE COMPLETE" : "FLIGHT ENDED";
                string reason = State == RaceState.Won ? $"Score: {Score}    Time: {Mathf.CeilToInt(TimeRemaining)}s" : EndReason;
                GUI.Box(new Rect(Screen.width * 0.5f - 180, Screen.height * 0.5f - 58, 360, 116), title + "\n\n" + reason + "\n\nPress R to retry");
            }
        }

        public void Restart()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private float comboWindowRemaining;
    }
}
