using TikTokLiveSharp.Models.Protobuf;

namespace TikTokLiveSharp.Events.MessageData.Messages
{
    public sealed class Envelope : AMessageData
    {
        public readonly Objects.User User;

        internal Envelope(WebcastEnvelopeMessage msg) 
            : base(msg.Header.RoomId, msg.Header.MessageId, msg.Header.ServerTime)
        {
            User = new Objects.User(0, msg.User.Id, msg.User.Username, null, null, null, null, null, 0, 0, 0, null);
        }
    }
}