namespace ONI_MP.Networking
{
    public class BandwidthStats
    {
        public int PacketsSent { get; private set; }
        public int PacketsReceived { get; private set; }
        public int SentPerSecond { get; private set; }
        public int ReceivedPerSecond { get; private set; }
        public long BytesSent { get; private set; }
        public long BytesReceived { get; private set; }
        public long BytesSentSec { get; private set; }
        public long BytesReceivedSec { get; private set; }

        private int _sentThisSecond = 0;
        private int _receivedThisSecond = 0;
        private float _timeAccumulator = 0f;

        public void IncrementSentPackets(int bytes = 0)
        {
            PacketsSent++;
            _sentThisSecond++;
            AddBytesSent(bytes);
        }

        public void IncrementReceivedPackets(int bytes = 0)
        {
            PacketsReceived++;
            _receivedThisSecond++;
            AddBytesReceived(bytes);
        }

        public void ResetPacketCounters()
        {
            PacketsSent = 0;
            PacketsReceived = 0;
        }

        public void UpdatePacketRates(float deltaTime)
        {
            _timeAccumulator += deltaTime;

            if (_timeAccumulator >= 1f)
            {
                SentPerSecond = _sentThisSecond;
                ReceivedPerSecond = _receivedThisSecond;
                _sentThisSecond = 0;
                _receivedThisSecond = 0;
                _timeAccumulator = 0f;
                BytesSentSec = 0;
                BytesReceivedSec = 0;
            }
        }

        public void AddBytesSent(int byteCount)
        {
            BytesSent += byteCount;
            BytesSentSec += byteCount;
        }

        public void AddBytesReceived(int byteCount)
        {
            BytesReceived += byteCount;
            BytesReceivedSec += byteCount;
        }
    }
}
