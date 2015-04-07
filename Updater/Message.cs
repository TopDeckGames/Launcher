using System;

namespace Updater
{
    [Serializable]
    public class Message
    {
        public enum MessageType { GetManifest = 1, GetFile = 2, Manifest = 3, File = 4, Error = 5, End = 6 };

        public MessageType Type { get; set; }
        public string Data { get; set; }
        public byte[] BinaryData { get; set; }
    }
}