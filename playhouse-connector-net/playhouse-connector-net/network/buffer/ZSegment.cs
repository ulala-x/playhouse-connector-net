//using System;
//using System.Buffers.Binary;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;

//namespace PlayHouseConnector.network.buffer
//{
//    public class ZSegment 
//    {
//        public const int SEGMENT_SIZE = 1024 * 4;

//        public int GetReadableSize()
//        {
//            return _writePos - _readPos;
//        }


//        internal readonly ArraySegment<byte> Data;
//        private int _writePos = 0;
//        private int _readPos = 0;

//        public ZSegment(ArraySegment<byte> segement)
//        {

//            Data = segement;
//        }

        

//        static ZSegment Create(){

//            return ZSegmentPool.Instance.Rent();
//        }

//        public void Return()
//        {
//            ZSegmentPool.Instance.Return(this);
            
//        }

//        public Span<byte> ReadData(int size)
//        {

//            if(GetReadableSize() < size)
//            {
//                throw new IndexOutOfRangeException();
//            }

//            var span =  new Span<byte>(Data.Array , _readPos, size);
//            _readPos += size;
//            return span;
//        }

//        public void AddData(byte[] data,int offset,int count)
//        {
//            if(SEGMENT_SIZE - _writePos < count)
//            {
//                throw new IndexOutOfRangeException();
//            }

//            //DynamicBuffer.BlockCopy(data, offset, Data.Array!, Data.Offset + this._writePos, count);
//            this._writePos += count;
//        }

//        public void Clear()
//        {
//            _readPos = 0;
//            _writePos = 0;
//        }

//        //internal uint GetUnsignedByte(int offset)
//        //{
//        //    return Data.Array![Data.Offset+ _readPos + offset];
//        //}

//        //internal uint GetUnsignedShort(int offset)
//        //{
//        //    return (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(Data.AsSpan(position + offset, 2)));
//        //    //if (BitConverter.IsLittleEndian)
//        //    //{
//        //    //    return BinaryPrimitives.ReadUInt16LittleEndian(Data.AsSpan(offset,2));
//        //    //}
//        //    //else
//        //    //{
//        //    //    return BinaryPrimitives.ReadUInt16BigEndian(Data.AsSpan(offset,2));
//        //    //}
//        //}

//        internal bool isReadable()
//        {
//            return _readPos  >  _writePos;
//        }

//        internal byte ReadByte()
//        {
//            return Data.Array[_readPos++];
//        }

//        internal int GetReadIndex()
//        {
//            return _readPos;
//        }

//        internal bool isGettable(int segementIndex)
//        {
//            if(segementIndex >=_writePos || segementIndex < 0)
//            {
//                return false;
//            }
//            return true;
//        }

//        internal byte GetByte(int index)
//        {
//            if(index >= _writePos)
//            {
//                throw new IndexOutOfRangeException();
//            }
//            return Data.Array[index];
                
//        }
//    }
//}
