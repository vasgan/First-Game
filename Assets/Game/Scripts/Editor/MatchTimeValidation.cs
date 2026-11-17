using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class MatchTimeValidation
    {
        [MenuItem("First Game/Validate Match Time Controls")]
        public static void Validate()
        {
            GameObject gameObject = new("MatchTimeValidation");
            GameController game = gameObject.AddComponent<GameController>();
            try
            {
                Require(game.SimulationSpeed == 1f && !game.IsPaused && Time.timeScale == 1f,
                    "A new match must start at normal speed.");
                game.ToggleSimulationSpeed();
                Require(game.SimulationSpeed == 2f && Time.timeScale == 2f,
                    "Speed control must switch the match to ×2.");
                game.TogglePause();
                Require(game.IsPaused && Time.timeScale == 0f && !game.CanInteract,
                    "Pause must stop scaled game actions.");
                game.ToggleSimulationSpeed();
                Require(game.SimulationSpeed == 1f && Time.timeScale == 0f,
                    "Speed may be selected from the pause menu without resuming.");
                game.TogglePause();
                Require(!game.IsPaused && Time.timeScale == 1f && game.CanInteract,
                    "Resume must restore the selected match speed.");
                Debug.Log("FIRST_GAME_MATCH_TIME_VALIDATION_PASSED");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                Time.timeScale = 1f;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Match time validation failed: " + message);
        }
    }
}
