namespace Quantum
{
    public unsafe class FrameAdapter
    {
        private Frame _frame;

        public virtual void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        public virtual GameState GameState
        {
            get => _frame.Global->CurrentGameState;
            set => _frame.Global->CurrentGameState = value;
        }

        public virtual int PlayerCount => _frame.PlayerConnectedCount;

        /*
         * 위그드라실은 시뮬레이션 세션 당 인원 수가 고정되므로, 최대 인원 == 최소 인원 임. <br/>
         * 따라서 MinimumPlayerCount의 값으로 MaxPlayerCount를 주어도 됨.
         */
        public virtual int MinimumPlayerCount => _frame.MaxPlayerCount;

        public Frame.FrameEvents Events => _frame.Events;

        public virtual void SystemEnable<T>() where T : SystemBase
        {
            _frame.SystemEnable<T>();
        }

        public virtual void SystemDisable<T>() where T : SystemBase
        {
            _frame.SystemDisable<T>();
        }
    }
}