using System;
using UnityEngine;

namespace MiceToBeHome
{
    public class GameManager : Singleton<GameManager>
    {
        public GameState State { get; private set; } = GameState.MainMenu;
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        // True only while unpausing (Resume). Restart also transitions out of Paused, so the flow
        // controller must not rely on PreviousState == Paused to tell resume and restart apart.
        public bool Resuming { get; private set; }

        public event Action<GameState> StateChanged;

        // Evento para que otros controladores (GameFlowController) manejen la petición de empezar la persecución
        public event Action BeginChaseRequested;

        public bool IsPlaying => State == GameState.Playing;
        public bool IsEditing => State == GameState.Editing;

        public void NewGame() => SetState(GameState.Editing);

        public void BeginChase()
        {
            if (State == GameState.Editing)
            {
                SetState(GameState.Playing);
            }
        }

        // Llamar desde UI para pedir empezar la persecución pero delegar la decisión/animación a quien escuche BeginChaseRequested.
        public void RequestBeginChase()
        {
            if (State == GameState.Editing)
            {
                BeginChaseRequested?.Invoke();
            }
        }

        public void TogglePause()
        {
            if (State == GameState.Playing || State == GameState.Editing)
            {
                SetState(GameState.Paused);
            }
            else if (State == GameState.Paused)
            {
                Resuming = true;
                SetState(PreviousState);
                Resuming = false;
            }
        }

        public void Win()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Victory);
            }
        }

        public void Lose()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Defeat);
            }
        }

        public void Restart() => SetState(GameState.Editing);

        public void ReturnToMenu() => SetState(GameState.MainMenu);

        public void QuitGame()
        {
            Application.Quit();
        }

        public void SetState(GameState next)
        {
            if (State == next)
            {
                return;
            }

            PreviousState = State;
            State = next;
            Time.timeScale = ResolveTimeScale(next);
            StateChanged?.Invoke(next);
        }

        public void Emit()
        {
            Time.timeScale = ResolveTimeScale(State);
            StateChanged?.Invoke(State);
        }

        private static float ResolveTimeScale(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                case GameState.Victory:
                case GameState.Defeat:
                    return 0f;
                default:
                    return 1f;
            }
        }
    }
}
