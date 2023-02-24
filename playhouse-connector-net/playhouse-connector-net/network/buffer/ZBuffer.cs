//using Serilog;
//using System;
//using System.Buffers;
//using System.Buffers.Binary;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace PlayHouseConnector.network.buffer
//{
//    public sealed class ZBuffer
//    {
//        private List<ZSegment> _segments = new List<ZSegment>();

//        //private int _length = 0;

//        public int Position { get; private set; } = 0;
//        public int Length { get; private set; } = 0;


//        private void CalcLength()
//        {
//            for (int i = 0; i < _segments.Count; i++)
//            {
//                Length += _segments[i].GetReadableSize();
//            }
//        }

        
  
//        public bool Read(int bufferOffset,byte[] buffer, int offset, int count)
//        {

//            int totalLength = offset+ Length;
//            int bufferIndex = offset;

//            var getSize = _segments[0].GetReadableSize();

//            count += bufferOffset;

//            try
//            {
//                while (count > 0)
//                {
//                    if (_segments[0].isReadable())
//                    {
//                        if (bufferOffset > 0)
//                        {
//                            _segments[0].ReadByte();
//                            bufferOffset--;
//                        }
//                        else
//                        {
//                            buffer[bufferIndex++] = _segments[0].ReadByte();
//                        }

//                        --count;
//                    }
//                    else
//                    {
//                        if (_segments.Count == 0)
//                        {
//                            break;
//                        }
//                        else
//                        {
//                            _segments.RemoveAt(0);
//                        }
//                    }

//                }
//                return true;
//            }
//            finally
//            {
//                CalcLength();
//            }
            
//        }

//        public bool Get(int bufferOffset, byte[] buffer, int offset, int count)
//        {

//            int totalLength = offset + Length;
//            int bufferIndex = offset;

//            var getSize = _segments[0].GetReadableSize();

//            int listIndex = 0;

//            count += bufferOffset;

//            int segementIndex = _segments[listIndex].GetReadIndex();

//            while (count > 0)
//            {
//                if (_segments[listIndex].isGettable(segementIndex))
//                {
//                    if (bufferOffset > 0)
//                    {
//                        _segments[listIndex].GetByte(segementIndex++);
//                        bufferOffset--;
//                    }
//                    else
//                    {
//                        buffer[bufferIndex++] = _segments[listIndex].GetByte(segementIndex++);
//                    }

//                    --count;
//                }
//                else
//                {
//                    if (listIndex >= _segments.Count - 1)
//                    {
//                        break;
//                    }
//                    else
//                    {
//                        segementIndex = 0;
//                        listIndex++;
//                    }
//                }

//            }
//        }


//        internal void Write(byte[] buffer, int offset, int count)
//        {
//            _segments.Add(new ZSegment(new ArraySegment<byte>(buffer,offset, count)));
//        }
//    }
//}
