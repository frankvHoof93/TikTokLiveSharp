using System.Collections.Generic;
using System.Linq;
using TikTokLiveSharp.Events.Data;
using TikTokLiveSharp.Events.Objects;

namespace TikTokLiveSharp.Events
{
    /// <summary>
    /// ChatMessage sent by Viewer (or Host)
    /// </summary>
    public sealed class Chat : AMessageData
    {
        /// <summary>
        /// User that sent the Message
        /// </summary>
        public readonly User Sender;
        /// <summary>
        /// Message that was Sent
        /// </summary>
        public readonly string Message;

        public readonly bool VisibleToSender;

        public readonly Picture BackgroundImage;

        public readonly string FullScreenTextColor;

        public readonly Picture BackgroundImageV2;

        public readonly PublicAreaCommon PublicAreaCommon;

        /// <summary>
        /// Image for Gift in Message (if applicable)
        /// </summary>
        public readonly Picture GiftImage;

        public readonly int InputType;
        /// <summary>
        /// Users mentioned in ChatMessage
        /// </summary>
        public readonly IReadOnlyList<User> MentionedUsers;
        /// <summary>
        /// Emotes in Message (in order)
        /// </summary>
        public readonly IReadOnlyList<Models.Protobuf.Messages.ChatMessage.EmoteWithIndex> Emotes;
        /// <summary>
        /// Language for Sender
        /// </summary>
        public readonly string ContentLanguage;

        public readonly MsgFilter MsgFilter;
        public readonly int QuickChatScene;
        public readonly int CommunityFlaggedStatus;
        public readonly UserIdentity UserIdentity;
        public readonly IReadOnlyList<CommentQualityScore> CommentQualityScores;

        internal Chat(Models.Protobuf.Messages.ChatMessage msg)
            : base(msg?.Header)
        {
            Sender = msg?.User;
            Message = msg?.Content ?? string.Empty;
            VisibleToSender = msg?.VisibleToSender ?? false;
            BackgroundImage = msg?.BackgroundImage;
            FullScreenTextColor = msg?.FullScreenTextColor ?? string.Empty;
            BackgroundImageV2 = msg?.BackgroundImageV2;
            PublicAreaCommon = msg?.PublicAreaCommon;
            GiftImage = msg?.GiftImage;
            InputType = msg?.InputType ?? -1;
            MentionedUsers = msg?.MentionedUsers?.Count > 0 ? msg.MentionedUsers.Select(u => (User)u).ToList().AsReadOnly() : new List<User>(0).AsReadOnly();
            Emotes = msg?.EmotesList?.Count > 0 ? msg.EmotesList.OrderBy(e => e.Index).ToList().AsReadOnly() : new List<Models.Protobuf.Messages.ChatMessage.EmoteWithIndex>(0).AsReadOnly();
            ContentLanguage = msg?.ContentLanguage ?? string.Empty;
            MsgFilter = msg?.MsgFilter;
            QuickChatScene = msg?.QuickChatScene ?? -1;
            CommunityFlaggedStatus = msg?.CommunityFlaggedStatus ?? -1;
            UserIdentity = msg?.UserIdentity;
            CommentQualityScores = msg?.CommentQualityScores?.Count > 0 ? msg.CommentQualityScores.Select(s => (CommentQualityScore)s).ToList().AsReadOnly() : new List<CommentQualityScore>(0).AsReadOnly();
        }
    }
}