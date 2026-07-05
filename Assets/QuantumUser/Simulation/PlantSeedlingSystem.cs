namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class PlantSeedlingSystem : SystemMainThread
    {
        /// <summary>
        /// 현재 프레임의 모든 플레이어에 대해 <see cref="SpawnSeedlingCommand"/>가 존재하는지 확인하고, 존재하는 모든 <see cref="SpawnSeedlingCommand"/>에 대해 <see cref="SpawnSeedlingCommand.Execute"/>를 실행.
        /// </summary>
        public override void Update(Frame frame)
        {
            for(int playerIndex = 0; playerIndex<frame.MaxPlayerCount; playerIndex++)
            {
                var tileClickedCommand = frame.GetPlayerCommand<SpawnSeedlingCommand>(playerIndex);
                if (tileClickedCommand != null)
                {
                    tileClickedCommand.Execute(frame);
                }
            }
        }
    }
}