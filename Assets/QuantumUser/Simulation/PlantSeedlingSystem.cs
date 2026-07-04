namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class PlantSeedlingSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
            for(int playerIndex = 0; playerIndex<frame.MaxPlayerCount; playerIndex++)
            {
                var tileClickedCommand = frame.GetPlayerCommand<TileClickedCommand>(playerIndex);
                if (tileClickedCommand != null)
                {
                    var seedlingPrefab =
                        frame.FindAsset<EntityPrototype>("QuantumUser/Resources/Prefabs/SeedlingEntityPrototype");
                    EntityRef seedling = frame.Create(seedlingPrefab);
                    Transform2D* transform = frame.Unsafe.GetPointer<Transform2D>(seedling);
                    transform->Position = tileClickedCommand.worldPosition;
                }
            }
        }
    }
}