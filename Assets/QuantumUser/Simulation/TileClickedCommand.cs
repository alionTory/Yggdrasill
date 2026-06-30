namespace Quantum
{
    using Photon.Deterministic;

    public class TileClickedCommand : DeterministicCommand
    {
        public FPVector2 worldPosition;
        public override void Serialize(BitStream stream) 
        {
            stream.Serialize(ref worldPosition);
        }
    }
}
