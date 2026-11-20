using UnityEngine;

namespace AirRace.Gameplay
{
    public enum RaceState { Ready, Countdown, Racing, Won, Failed }

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
        [SerializeField] private float startCountdownSeconds = 3f;

        public RaceState State { get; private set; } = RaceState.Ready;
        public int NextCheckpoint { get; private set; }
        public int Score { get; private set; }
        public int BoostCharges { get; private set; }
        public float Energy { get; private set; } = 72f;
        public float TimeRemaining { get; private set; }
        public float CountdownRemaining { get; private set; }
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
            gameObject.AddComponent<AirRaceHUD>().Initialize(this);
        }

        private void Update()
        {
            if (State == RaceState.Countdown)
            {
                CountdownRemaining = Mathf.Max(0f, CountdownRemaining - Time.deltaTime);
                if (CountdownRemaining <= 0f) State = RaceState.Racing;
                return;
            }

            if (State != RaceState.Racing) return;

            ImpactFeedbackRemaining = Mathf.Max(0f, ImpactFeedbackRemaining - Time.deltaTime);
            comboWindowRemaining = Mathf.Max(0f, comboWindowRemaining - Time.deltaTime);

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (IsBoosting) BoostTimeRemaining = Mathf.Max(0f, BoostTimeRemaining - Time.deltaTime);
            else Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);

            if (Energy <= 0f) Fail("La energía de vuelo se agotó");
            else if (TimeRemaining <= 0f) Fail("Se agotó el tiempo");
        }

        public bool StartRace()
        {
            if (State != RaceState.Ready) return false;
            CountdownRemaining = Mathf.Max(0.1f, startCountdownSeconds);
            State = RaceState.Countdown;
            return true;
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
