using System;
using System.IO;
using Google.Protobuf;

namespace PlayHouseConnector
{
    public interface IPayload : IDisposable
    {
        ReadOnlyMemory<byte> Data { get; }
        ReadOnlySpan<byte> DataSpan => Data.Span;
    }

    public class ProtoPayload : IPayload
    {
        private readonly IMessage _proto;

        public ProtoPayload(IMessage proto)
        {
            _proto = proto;
        }

        public void Dispose()
        {
        }

        public ReadOnlyMemory<byte> Data => _proto.ToByteArray();
    }

    public class EmptyPayload : IPayload
    {
        public void Dispose()
        {
        }

        public ReadOnlyMemory<byte> Data => new();

        public void Output(Stream outputStream)
        {
        }
    }
}