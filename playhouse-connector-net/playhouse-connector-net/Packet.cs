using Google.Protobuf;
using playhouse_connector_net;
using PlayHouseConnector.network;
using PlayHouseConnector.network.buffer;
using System;
using System.Security.Permissions;

namespace PlayHouseConnector
{
    //public interface Payload 
    //{
    //    byte[] GetData();
    //}

    //public class ProtoPayload : Payload
    //{
    //    public ProtoPayload() { }

    //    public byte[] GetData()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class ReplyCallback
    //{
    //    Action<ReplyPacket> _action;
    //    public ReplyCallback(Action<ReplyPacket> action)
    //    {
    //        this._action = action;
    //    }   

    //    public void SetResult(ReplyPacket packet)
    //    {
    //        _action.Invoke(packet);
    //    }
    //}


    public class Packet : IDisposable
    {

        public  String MsgName { get; private set; }
        private PBuffer? _buffer;


        public Packet(String msgName, PBuffer? buffer)
        {
            MsgName = msgName;
            _buffer= buffer;
        }

        public Packet(IMessage message)  
        {
            MsgName = message.Descriptor.Name;
            _buffer = new PBuffer(message.CalculateSize());
            message.WriteTo(_buffer.Data);
            
        }

        public byte[] GetData()
        {
            return _buffer!.Data;
        }

        public void Dispose()
        {
            if(_buffer!= null)
            {
                _buffer.Dispose();
                _buffer = null;
            }
            
        }

        internal PBuffer MoveBuffer()
        {
            var temp = _buffer;
            _buffer = null;
            return temp!;
        }
    }

    public class ReplyPacket : Packet 
    {
        public int ErrorCode = 0;
        public ReplyPacket(int errorCode,String msgName, PBuffer? buffer):base(msgName, buffer)
        {
            this.ErrorCode = errorCode;
        }
    }


}