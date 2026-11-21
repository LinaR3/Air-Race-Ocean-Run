using UnityEngine.InputSystem;

namespace AirRace.Gameplay
{
    /// <summary>Keyboard snapshot for one frame of aircraft controls.</summary>
    public readonly struct AircraftFlightInput
    {
        public bool IsAvailable { get; }
        public float Pitch { get; }
        public float Turn { get; }
        public bool IsBraking { get; }
        public bool StartPressed { get; }
        public bool PausePressed { get; }
        public bool RestartPressed { get; }
        public bool BoostPressed { get; }

        private AircraftFlightInput(Keyboard keyboard)
        {
            IsAvailable = keyboard != null;
            if (keyboard == null)
            {
                Pitch = 0f;
                Turn = 0f;
                IsBraking = false;
                StartPressed = false;
                PausePressed = false;
                RestartPressed = false;
                BoostPressed = false;
                return;
            }

            Pitch = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            Turn = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                   (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            IsBraking = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            StartPressed = keyboard.enterKey.wasPressedThisFrame;
            PausePressed = keyboard.escapeKey.wasPressedThisFrame;
            RestartPressed = keyboard.rKey.wasPressedThisFrame;
            BoostPressed = keyboard.spaceKey.wasPressedThisFrame;
        }

        public static AircraftFlightInput Read(Keyboard keyboard) => new AircraftFlightInput(keyboard);
    }
}
