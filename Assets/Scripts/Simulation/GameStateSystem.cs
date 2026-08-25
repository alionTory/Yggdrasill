using UnityEngine;

namespace Quantum
{
    public class GameStateSystem:SystemMainThread
    {
        private readonly FrameAdapter _frameAdapter = new();
        public override void OnInit(Frame f)
        {
            _frameAdapter.SetFrame(f);
            Initialize(_frameAdapter);
        }

        public void Initialize(FrameAdapter frame)
        {
            HandleGameStateChange(frame, GameState.Pending);
        }


        public override void Update(Frame f)
        {
            _frameAdapter.SetFrame(f);
            UpdateGameState(_frameAdapter);
        }

        public void UpdateGameState(FrameAdapter frame)
        {
            var nextState = frame.GameState.Next(frame.PlayerCount, frame.MinimumPlayerCount);
            if(nextState != frame.GameState)
                HandleGameStateChange(frame, nextState);
        }

        private void HandleGameStateChange(FrameAdapter frame, GameState newState)
        {
            frame.GameState = newState;
            frame.Events.GameStateChanged(newState);
            if(newState == GameState.Pending)
                frame.SystemDisable<GameLogicSystemGroup>();
            else if(newState==GameState.Running)
                frame.SystemEnable<GameLogicSystemGroup>();
        }
    }
}