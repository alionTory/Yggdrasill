using UnityEngine;

namespace Quantum
{
    public class GameStateSystem:SystemMainThread, ISignalOnPlayerAdded
    {
        public override void OnInit(Frame f)
        {
            var frameAdapter = new FrameAdapter(f);
            Initialize(frameAdapter);
        }

        public void Initialize(FrameAdapter frame)
        {
            HandleGameStateChange(frame, GameState.Pending);
        }

        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {
            var frameAdapter = new FrameAdapter(f);
            Log.Info($"OnPlayerAdded. playercount: {frameAdapter.PlayerCount},  minimumplayercount: {frameAdapter.MinimumPlayerCount}, next gamestate: {frameAdapter.GameState.Next(frameAdapter.PlayerCount, frameAdapter.MinimumPlayerCount)}");
            UpdateGameState(frameAdapter);
        }

        public override void Update(Frame f)
        {
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