using NUnit.Framework;
using Photon.Deterministic;
using Quantum;

namespace Yggdrasill.Tests.EditMode
{
    /// <summary>
    /// SpawnSeedlingCommand 에 대한 단위 테스트.
    ///
    /// 검증 기준은 SpawnSeedlingCommand.cs 의 주석(명세)이다.
    /// - CreateFromView: "새 SpawnSeedlingCommand 인스턴스 생성",
    ///   "x -> WorldPosition 에 반영", "y -> WorldPosition 에 반영".
    /// - Serialize: WorldPosition(_worldPosition) 를 직렬화한다.
    ///
    /// Frame 이 필요한 Execute 의 계약(월드 좌표에 묘목 생성)은
    /// 실제 시뮬레이션이 있어야 검증 가능하므로 PlayMode 통합 테스트
    /// (PlantSeedlingSystemTests)에서 검증한다.
    /// </summary>
    public class SpawnSeedlingCommandTests
    {
        // FromRoundedFloat_UNSAFE 는 float 를 FP(1/65536 정밀도)로 반올림하므로
        // 부동소수점 왕복 오차를 흡수할 여유 있는 허용 오차를 둔다.
        private const float Tolerance = 1e-3f;

        /// <summary>
        /// 호출마다 새 인스턴스를 리턴해야 한다.
        /// </summary>
        [Test]
        public void CreateFromView_ReturnsDistinctInstancesPerCall()
        {
            var first = SpawnSeedlingCommand.CreateFromView(1f, 1f);
            var second = SpawnSeedlingCommand.CreateFromView(1f, 1f);

            Assert.AreNotSame(first, second);
        }

        /// <summary>
        /// CreateFromView의 인수로 주어진 좌표가 WorldPosition에 반영되어야 한다.
        /// </summary>
        [TestCase(0f, 0f)]
        [TestCase(12.75f, -8.5f)]
        [TestCase(-100.125f, 100.125f)]
        public void CreateFromView_MapsBothArguments(float x, float y)
        {
            var command = SpawnSeedlingCommand.CreateFromView(x, y);

            Assert.AreEqual(x, command.WorldPosition.X.AsFloat, Tolerance, "WorldPosition.X");
            Assert.AreEqual(y, command.WorldPosition.Y.AsFloat, Tolerance, "WorldPosition.Y");
        }

        /// <summary>
        /// 직렬화 후 역질렬화하면 상태가 보존되어야 한다.
        /// </summary>
        [Test]
        public void Serialize_RoundTrip_PreservesWorldPosition()
        {
            var original = SpawnSeedlingCommand.CreateFromView(7.5f, -13.25f);

            var writeStream = new BitStream(new byte[64]);
            writeStream.Writing = true;
            original.Serialize(writeStream);

            // 쓰여진 바이트로 새 읽기 스트림을 만들어 처음(위치 0)부터 역직렬화한다.
            var readStream = new BitStream(writeStream.ToArray());
            readStream.Reading = true;
            var restored = new SpawnSeedlingCommand();
            restored.Serialize(readStream);

            Assert.AreEqual(original.WorldPosition, restored.WorldPosition);
            Assert.AreEqual(original.WorldPosition.X.RawValue, restored.WorldPosition.X.RawValue, "X.RawValue");
            Assert.AreEqual(original.WorldPosition.Y.RawValue, restored.WorldPosition.Y.RawValue, "Y.RawValue");
        }
    }
}
