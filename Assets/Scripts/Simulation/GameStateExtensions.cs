using System;

namespace Quantum
{
    public static class GameStateExtensions
    {
        /// <summary>
        /// 현재 게임 상태에 따라 다음 상태를 구한다.
        /// </summary>
        public static GameState Next(this GameState gameState, int currentPlayerCount, int minimumPlayerCount)
            => gameState switch
            {
                GameState.Pending => minimumPlayerCount <= currentPlayerCount ? GameState.Running : GameState.Pending,
                GameState.Running => GameState.Running,
                _ => throw new ArgumentOutOfRangeException(nameof(gameState), gameState, null)
            };

    }
}