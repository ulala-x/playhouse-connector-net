//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PlayHouseConnector.network.buffer
//{
//    public  sealed  class ZSegmentPool
//    {
//        private const int CheckSizeMB = 1024*1024*100;
//        private readonly int segmentSize = ZSegment.SEGMENT_SIZE;

//        private readonly List<byte[]> _chunks = new List<Byte[]>(capacity:10);
//        private readonly ConcurrentQueue<ArraySegment<byte>> _segments = new ConcurrentQueue<ArraySegment<byte>>();

//        private static readonly Lazy<ZSegmentPool> _instance = new Lazy<ZSegmentPool>(() => new ZSegmentPool());
//        public static ZSegmentPool Instance { get { return _instance.Value; } }

//        ZSegmentPool() {
//            byte[] buffer = new byte[CheckSizeMB];

//            for(int i = 0; i < buffer.Length; i +=segmentSize )            
//            {
//                _segments.Enqueue(new ArraySegment<byte>(buffer, i, segmentSize));

//            }
//            _chunks.Add(buffer);
//        }

//        public ZSegment Rent()
//        {
//            ArraySegment<byte> segement;
//            if(_segments.TryDequeue(out segement))
//            {
//                return new ZSegment(segement);
//            }
//            throw new Exception("Memory is insufficient");
//        }

//        public void Return(ZSegment segement)
//        {
//            segement.Clear();
//            _segments.Enqueue(segement.Data);
//        }
//    }
//}
