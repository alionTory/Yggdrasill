namespace Quantum
{
    using Photon.Deterministic;

    public class SpawnSeedlingCommand : DeterministicCommand
    {
        /// <summary>
        /// 묘목을 생성할 위치의 월드 좌표.
        /// </summary>
        public FPVector2 WorldPosition => _worldPosition;

        private FPVector2 _worldPosition;

        /// <summary>
        /// 새 <see cref="SpawnSeedlingCommand"/> 인스턴스 생성.
        /// </summary>
        /// <param name="x">묘목을 생성할 위치의 월드 좌표 x. WorldPosition.X에 반영됨.</param>
        /// <param name="y">묘목을 생성할 위치의 월드 좌표 y. WorldPosition.Y에 반영됨.</param>
        /// <remarks>이 생성자는 비결정론적이므로, 시뮬레이션 코드에서 실행되어서는 안 됨.</remarks>
        public static SpawnSeedlingCommand CreateFromView(float x, float y)
        {
            return new SpawnSeedlingCommand
            {
                _worldPosition = new FPVector2(FP.FromRoundedFloat_UNSAFE(x), FP.FromRoundedFloat_UNSAFE(y))
            };
        }

        public override void Serialize(BitStream stream)
        {
            stream.Serialize(ref _worldPosition);
        }

        /// <summary>
        /// 월드 좌표 WorldPosition에 묘목을 생성함.
        /// </summary>
        public unsafe void Execute(Frame frame)
        {
            var seedlingPrefab =
                frame.FindAsset<EntityPrototype>("Resources/Prefabs/SeedlingEntityPrototype");
            EntityRef seedling = frame.Create(seedlingPrefab);
            Transform2D* transform = frame.Unsafe.GetPointer<Transform2D>(seedling);
            transform->Position = WorldPosition;
        }
    }
}