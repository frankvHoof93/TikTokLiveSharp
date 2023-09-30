using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.Proxy;
using TikTokLiveSharp.Debugging;
using TikTokLiveSharp.Errors.Messaging;
using TikTokLiveSharp.Events;
using TikTokLiveSharp.Events.MessageData.Messages;
using TikTokLiveSharp.Models.Protobuf;
using TikTokLiveSharp.Models.Protobuf.Messages;
using TikTokLiveSharp.Models.Protobuf.Messages.Generic;
using static TikTokLiveSharp.Models.Protobuf.Messages.WebcastControlMessage;
using static TikTokLiveSharp.Models.Protobuf.Messages.WebcastMemberMessage;
using BarrageMessage = TikTokLiveSharp.Events.MessageData.Messages.BarrageMessage;
using LinkMicMethod = TikTokLiveSharp.Events.MessageData.Messages.LinkMicMethod;
using RoomMessage = TikTokLiveSharp.Events.MessageData.Messages.RoomMessage;
using TikTokGift = TikTokLiveSharp.Events.MessageData.Objects.TikTokGift;

namespace TikTokLiveSharp.Client
{
    /// <summary>
    /// Client for TikTokLive-Connections
    /// </summary>
    public class TikTokLiveClient : TikTokBaseClient
    {
        #region InnerTypes
        /// <summary>
        /// ID for Active Gifts
        /// </summary>
        private struct GiftId
        {
            /// <summary>
            /// GiftID
            /// </summary>
            public ulong Gift;
            /// <summary>
            /// UserName of Sender
            /// </summary>
            public string UserName;

            public override int GetHashCode()
            {
                return Gift.GetHashCode() + UserName.GetHashCode();
            }
        }
        /// <summary>
        /// Types of SocialMessages
        /// </summary>
        private struct SocialTypes
        {
            /// <summary>
            /// EventType for Like-Message
            /// </summary>
            public const string LikeType = "pm_mt_msg_viewer";
            /// <summary>
            /// EventType for Follow-Message
            /// </summary>
            public const string FollowType = "pm_main_follow_message_viewer_2";
            /// <summary>
            /// EventType for Share-Message
            /// </summary>
            public const string ShareType = "pm_mt_guidance_share";
            /// <summary>
            /// EventType for Join-Message
            /// </summary>
            public const string JoinType = "pm_mt_join_message_other_viewer";
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gifts Currently on a GiftStreak
        /// </summary>
        private readonly Dictionary<GiftId, TikTokGift> activeGifts = new Dictionary<GiftId, TikTokGift>();
        #endregion

        #region Events
        /// <summary>
        /// Event fired when the client connects.
        /// </summary>
        public event TikTokEventHandler<bool> OnConnected;
        /// <summary>
        /// Event fired when the client disconnects.
        /// </summary>
        public event TikTokEventHandler<bool> OnDisconnected;
        public event TikTokEventHandler<RoomMessage> OnRoomIntro;
        public event TikTokEventHandler<RoomMessage> OnRoomMessage;
        public event TikTokEventHandler<RoomMessage> OnSystemMessage;
        /// <summary>
        /// Event fired when comments are received.
        /// </summary>
        public event TikTokEventHandler<Comment> OnComment;
        /// <summary>
        /// Event fired when the view count updates.
        /// </summary>
        public event TikTokEventHandler<RoomViewerData> OnViewerData;
        /// <summary>
        /// Event fired when the live has ended.
        /// </summary>
        public event TikTokEventHandler OnLiveEnded;
        public event TikTokEventHandler OnLivePaused;
        /// <summary>
        /// Event fired when a gift-Message is received.
        /// <para>
        /// Fires for every GiftMessage
        /// </para>
        /// </summary>
        public event TikTokEventHandler<GiftMessage> OnGiftMessage;
        /// <summary>
        /// Event fired when a gift-Message is received.
        /// <para>
        /// Fires when a gift-streak starts. Gift-Events are fired for subsequent messages.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<TikTokGift> OnGift;
        /// <summary>
        /// Event fired when someone likes the stream.
        /// </summary>
        public event TikTokEventHandler<Like> OnLike;
        /// <summary>
        /// Event fired when the stream is shared.
        /// </summary>
        public event TikTokEventHandler<Share> OnShare;
        /// <summary>
        /// Event fired when someone follows.
        /// </summary>
        public event TikTokEventHandler<Follow> OnFollow;
        /// <summary>
        /// Event fired when someone joins the stream.
        /// </summary>
        public event TikTokEventHandler<Join> OnJoin;
        /// <summary>
        /// Event fired when a user subscribes.
        /// </summary>
        public event TikTokEventHandler<Subscribe> OnSubscribe;
        /// <summary>
        /// Event fired when a LinkMicBattle starts
        /// </summary>
        public event TikTokEventHandler<LinkMicBattle> OnLinkMicBattle;
        public event TikTokEventHandler<LinkMicArmies> OnLinkMicArmies;
        /// <summary>
        /// Data for LinkMic-Connection(s)
        /// </summary>
        public event TikTokEventHandler<LinkMicMethod> OnLinkMicMethod;
        public event TikTokEventHandler<LinkMicFanTicket> OnLinkMicFanTicket;
        public event TikTokEventHandler<RankText> OnRankText;
        public event TikTokEventHandler<RankUpdate> OnRankUpdate;
        /// <summary>
        /// Closed Caption-Message for Video
        /// </summary>
        public event TikTokEventHandler<Caption> OnClosedCaption;
        public event TikTokEventHandler<PollMessage> OnPollMessage;
        public event TikTokEventHandler<InRoomBanner> OnInRoomBanner;
        /// <summary>
        /// Event Fired when Host pins a Comment to the Stream
        /// </summary>
        public event TikTokEventHandler<RoomPinMessage> OnRoomPinMessage;
        public event TikTokEventHandler<DetectMessage> OnDetectMessage;
        public event TikTokEventHandler<BarrageMessage> OnBarrageMessage;
        public event TikTokEventHandler<GoalUpdate> OnGoalUpdate;
        public event TikTokEventHandler<UnauthorizedMember> OnUnauthorizedMember;
        public event TikTokEventHandler<LinkMessage> OnLinkMessage;
        public event TikTokEventHandler<LinkLayerMessage> OnLinkLayerMessage;
        public event TikTokEventHandler<GiftBroadcast> OnGiftBroadcast;
        public event TikTokEventHandler<ShopMessage> OnShopMessage;
        public event TikTokEventHandler<IMDelete> OnIMDelete;
        public event TikTokEventHandler<Question> OnQuestion;
        public event TikTokEventHandler<Envelope> OnEnvelope;
        public event TikTokEventHandler<SubNotify> OnSubNotify;
        public event TikTokEventHandler<Emote> OnEmote;
        /// <summary>
        /// Event fired when an unhandled social event is received from the webcast.
        /// It's up to you how you can interpret this message.
        /// </summary>
        public event TikTokEventHandler<WebcastSocialMessage> UnhandledSocialEvent;
        /// <summary>
        /// Event fired when an unhandled member event is received from the webcast.
        /// It's up to you how you can interpret this message.
        /// </summary>
        public event TikTokEventHandler<WebcastMemberMessage> UnhandledMemberEvent;
        /// <summary>
        /// Event fired when an unhandled event is received from the webcast.
        /// It's up to you how you can interpret this message.
        /// </summary>
        public event TikTokEventHandler<Models.Protobuf.Messages.Generic.Message> UnhandledEvent;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the TikTok Live client.
        /// Used for retrieving a stream of information from live streams.
        /// </summary>
        /// <param name="hostId">Host-Name for Room to Connect to</param>
        /// <param name="settings">Settings for Client</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        public TikTokLiveClient(string hostId, ClientSettings? settings = null, Dictionary<string, object> clientParams = null)
            : base(hostId, settings, clientParams)
        { }

        /// <summary>
        /// Creates a new instance of the TikTok Live client.
        /// Used for retrieving a stream of information from live streams.
        /// </summary>
        /// <param name="uniqueID">Host-Name for Room to Connect to</param>
        /// <param name="timeout">Timeout for Connections</param>
        /// <param name="pollingInterval">Polling Interval for WebSocket-Connection</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        /// <param name="processInitialData">Whether to process Data received when Connecting</param>
        /// <param name="enableExtendedGiftInfo">Whether to download List of Gifts on Connect</param>
        /// <param name="proxyHandler">Proxy for Connection</param>
        /// <param name="lang">ISO-Language for Client</param>
        /// <param name="socketBufferSize">BufferSize for WebSocket-Messages</param>
        /// <param name="logDebug">Whether to log messages to the Console</param>
        /// <param name="logLevel">LoggingLevel for debugging</param>
        /// <param name="printMessageData">Whether to print Base64-Data for Messages to Console</param>
        /// <param name="checkForUnparsedData">Whether to check Messages for Unparsed Data</param>
        public TikTokLiveClient(string uniqueID,
            TimeSpan? timeout,
            TimeSpan? pollingInterval,
            Dictionary<string, object> clientParams = null,
            bool processInitialData = true,
            bool enableExtendedGiftInfo = true,
            RotatingProxy proxyHandler = null,
            string lang = "en-US",
            uint socketBufferSize = 10_000,
            bool logDebug = true,
            LogLevel logLevel = LogLevel.Error | LogLevel.Warning,
            bool printMessageData = false,
            bool checkForUnparsedData = false) 
            : base(uniqueID,
                timeout,
                pollingInterval,
                clientParams,
                processInitialData,
                enableExtendedGiftInfo,
                proxyHandler,
                lang,
                socketBufferSize,
                logDebug,
                logLevel,
                printMessageData,
                checkForUnparsedData)
        { }
        /// <summary>
        /// Disconnects Socket when destroying Client
        /// </summary>
        ~TikTokLiveClient()
        {
            socketClient?.Disconnect();
        }
        #endregion

        #region Method
        #region Connection
        /// <summary>
        /// Connects to Room
        /// </summary>
        /// <param name="onConnectException">Callback for possible Exceptions during Connect</param>
        /// <returns>Task to await. Result is RoomID</returns>
        protected override async Task<string> Connect(Action<Exception> onConnectException = null)
        {
            string roomID = await base.Connect(onConnectException);
            if (ShouldLog(LogLevel.Information))
                Debug.Log($"Connected to Room {roomID}");
            RunEvent(OnConnected, Connected);
            return roomID;
        }
        /// <summary>
        /// Closes & Stops Connection
        /// </summary>
        /// <returns>Task to await</returns>
        protected override async Task Disconnect()
        {
            await base.Disconnect();
            if (ShouldLog(LogLevel.Information))
                Debug.Log("Disconnected");
            RunEvent(OnDisconnected, Connected);
        }
        #endregion

        #region HandleMessage
        /// <summary>
        /// Handles Response from TikTokServer
        /// <para>
        /// Handles each Message in Response
        /// </para>
        /// </summary>
        /// <param name="webcastResponse">Response to Handle</param>
        protected override void HandleWebcastMessages(WebcastResponse webcastResponse)
        {
            if (ShouldLog(LogLevel.Information))
                Debug.Log($"Handling {webcastResponse.Messages.Count} Messages in Response");
            foreach (Message message in webcastResponse.Messages)
            {
                try
                {
                    HandleMessage(message);
                }
                catch (Exception e)
                {
                    WebcastMessageException exc = new WebcastMessageException($"Error whilst Handling Message. Stopping Client.{Environment.NewLine}Final Message: {Convert.ToBase64String(message.Binary)}", e);
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(exc);
                    CallOnException(exc);
                    // This is an irrecoverable error. Crash this Thread
                    throw exc;
                }
            }
        }
        /// <summary>
        /// Handles Message received from TikTokServer
        /// </summary>
        /// <param name="message">Message to Handle</param>
        private void HandleMessage(Message message)
        {
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Handling Message: " + message.Type);
            if (settings.PrintMessageData)
            {
                string msg = Convert.ToBase64String(message.Binary);
                Debug.Log($"Handling Message:{Environment.NewLine}{msg}");
            }

            using (MemoryStream stream = new MemoryStream(message.Binary))
                switch (message.Type)
                {
                    #region ConnectionEvents
                    case nameof(WebcastControlMessage):
                        WebcastControlMessage controlMessage = Deserialize<WebcastControlMessage>(stream);
                        if (controlMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(controlMessage);
                            switch (controlMessage.Action)
                            {                                
                                case ControlAction.Stream_Paused:
                                    if (ShouldLog(LogLevel.Verbose))
                                        Debug.Log("Handling Stream Paused!");
                                    RunEvent(OnLivePaused);
                                    return;
                                case ControlAction.Stream_Ended:
                                    if (ShouldLog(LogLevel.Verbose))
                                        Debug.Log("Handling Stream Ended!");
                                    RunEvent(OnLiveEnded);
                                    return;
                                default:
                                case ControlAction.Unknown:
                                    if (ShouldLog(LogLevel.Warning))
                                        Debug.LogWarning($"Handling Unhandled ControlMessage!{Environment.NewLine}[{Convert.ToBase64String(message.Binary)}]");
                                    // Run Unhandled
                                    RunEvent(UnhandledEvent, message);
                                    return;
                            }
                        }
                        return;
                    case nameof(SystemMessage):
                        SystemMessage systemMessage = Deserialize<SystemMessage>(stream);
                        if (systemMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(systemMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling System Message!");
                            RunEvent(OnSystemMessage, new RoomMessage(systemMessage));
                        }
                        return;
                    #endregion

                    #region RoomStatus
                    case nameof(WebcastLiveIntroMessage):
                        WebcastLiveIntroMessage introMessage = Deserialize<WebcastLiveIntroMessage>(stream);
                        if (introMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(introMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling IntroMessage");
                            RunEvent(OnRoomIntro, new RoomMessage(introMessage));
                        }
                        return;
                    case nameof(WebcastRoomUserSeqMessage):
                        WebcastRoomUserSeqMessage userSeqMessage = Deserialize<WebcastRoomUserSeqMessage>(stream);
                        if (userSeqMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(userSeqMessage);
                            ViewerCount = userSeqMessage.ViewerCount;
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling ViewerData");
                            RunEvent(OnViewerData, new RoomViewerData(userSeqMessage));
                        }
                        return;
                    case nameof(Models.Protobuf.Messages.RoomMessage):
                        Models.Protobuf.Messages.RoomMessage roomMessage = Deserialize<Models.Protobuf.Messages.RoomMessage>(stream);
                        if (roomMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(roomMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling RoomMessage");
                            RunEvent(OnRoomMessage, new RoomMessage(roomMessage));
                        }
                        return;
                    case nameof(WebcastRoomMessage):
                        WebcastRoomMessage webcastRoomMessage = Deserialize<WebcastRoomMessage>(stream);
                        if (webcastRoomMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastRoomMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling RoomMessage");
                            RunEvent(OnRoomMessage, new RoomMessage(webcastRoomMessage));
                        }
                        return;
                    case nameof(WebcastCaptionMessage):
                        WebcastCaptionMessage captionMessage = Deserialize<WebcastCaptionMessage>(stream);
                        if (captionMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(captionMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling Closed Captions");
                            RunEvent(OnClosedCaption, new Caption(captionMessage));
                        }
                        return;
                    #endregion

                    #region User-Interaction
                    case nameof(WebcastChatMessage):
                        WebcastChatMessage chatMessage = Deserialize<WebcastChatMessage>(stream);
                        if (chatMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(chatMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling Comment");
                            RunEvent(OnComment, new Comment(chatMessage));
                        }
                        return;
                    case nameof(WebcastLikeMessage):
                        WebcastLikeMessage likeMessage = Deserialize<WebcastLikeMessage>(stream);
                        if (likeMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(likeMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling Like");
                            RunEvent(OnLike, new Like(likeMessage));
                        }
                        return;
                    case nameof(WebcastGiftMessage):
                        WebcastGiftMessage giftMessage = Deserialize<WebcastGiftMessage>(stream);
                        if (giftMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(giftMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log("Handling GiftMessage");
                            HandleGiftMessage(giftMessage);
                        }
                        return;
                    case nameof(WebcastSocialMessage):
                        WebcastSocialMessage socialMessage = Deserialize<WebcastSocialMessage>(stream);
                        if (socialMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(socialMessage);
                            HandleSocialMessage(socialMessage, message.Binary);
                        }
                        return;
                    case nameof(WebcastMemberMessage):
                        WebcastMemberMessage memberMessage = Deserialize<WebcastMemberMessage>(stream);
                        if (memberMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(memberMessage);
                            HandleMemberMessage(memberMessage, message.Binary);
                        }
                        return;
                    #endregion

                    #region Host-Interaction
                    case nameof(WebcastPollMessage):
                        WebcastPollMessage pollMessage = Deserialize<WebcastPollMessage>(stream);
                        if (pollMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(pollMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling PollMessage");
                            RunEvent(OnPollMessage, new PollMessage(pollMessage));
                        }
                        return;
                    case nameof(WebcastRoomPinMessage):
                        WebcastRoomPinMessage pinMessage = Deserialize<WebcastRoomPinMessage>(stream);
                        if (pinMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(pinMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling RoomPinMessage");
                            RunEvent(OnRoomPinMessage, new RoomPinMessage(pinMessage));
                        }
                        return;
                    case nameof(WebcastGoalUpdateMessage):
                        WebcastGoalUpdateMessage goalUpdateMessage = Deserialize<WebcastGoalUpdateMessage>(stream);
                        if (goalUpdateMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(goalUpdateMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling GoalUpdate");
                            RunEvent(OnGoalUpdate, new GoalUpdate(goalUpdateMessage));
                        }
                        return;
                    #endregion

                    #region LinkMic
                    case nameof(WebcastLinkMicBattle):
                        WebcastLinkMicBattle linkMicBattleMessage = Deserialize<WebcastLinkMicBattle>(stream);
                        if (linkMicBattleMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(linkMicBattleMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkMicBattle");
                            RunEvent(OnLinkMicBattle, new LinkMicBattle(linkMicBattleMessage));
                        }
                        return;
                    case nameof(WebcastLinkMicArmies):
                        WebcastLinkMicArmies linkMicArmiesMessage = Deserialize<WebcastLinkMicArmies>(stream);
                        if (linkMicArmiesMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(linkMicArmiesMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkMicArmies");
                            RunEvent(OnLinkMicArmies, new LinkMicArmies(linkMicArmiesMessage));
                        }
                        return;
                    case nameof(Models.Protobuf.Messages.LinkMicMethod):
                        Models.Protobuf.Messages.LinkMicMethod linkMicMethodMessage = Deserialize<Models.Protobuf.Messages.LinkMicMethod>(stream);
                        if (linkMicMethodMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(linkMicMethodMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkMicMethod");
                            RunEvent(OnLinkMicMethod, new LinkMicMethod(linkMicMethodMessage));
                        }
                        return;
                    case nameof(Models.Protobuf.Messages.WebcastLinkMicMethod):
                        Models.Protobuf.Messages.WebcastLinkMicMethod webcastLinkMicMethodMessage = Deserialize<Models.Protobuf.Messages.WebcastLinkMicMethod>(stream);
                        if (webcastLinkMicMethodMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastLinkMicMethodMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkMicMethod");
                            RunEvent(OnLinkMicMethod, new LinkMicMethod(webcastLinkMicMethodMessage));
                        }
                        return;
                    case nameof(WebcastLinkMicFanTicketMethod):
                        WebcastLinkMicFanTicketMethod webcastFanTicketMessage = Deserialize<WebcastLinkMicFanTicketMethod>(stream);
                        if (webcastFanTicketMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastFanTicketMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkMicFanTicket");
                            RunEvent(OnLinkMicFanTicket, new LinkMicFanTicket(webcastFanTicketMessage));
                        }
                        return;
                    #endregion

                    #region Rank
                    case nameof(WebcastRankTextMessage):
                        WebcastRankTextMessage webcastRankTextMessage = Serializer.Deserialize<WebcastRankTextMessage>(stream);
                        if (webcastRankTextMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastRankTextMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling RankText");
                            RunEvent(OnRankText, new RankText(webcastRankTextMessage));
                        }
                        return;
                    case nameof(WebcastRankUpdateMessage):
                        WebcastRankUpdateMessage webcastRankUpdateMessage = Serializer.Deserialize<WebcastRankUpdateMessage>(stream);
                        if (webcastRankUpdateMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastRankUpdateMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling RankUpdate");
                            RunEvent(OnRankUpdate, new RankUpdate(webcastRankUpdateMessage));
                        }
                        return;
                    case nameof(WebcastHourlyRankMessage):
                        WebcastHourlyRankMessage webcastHourlyRankUpdateMessage = Serializer.Deserialize<WebcastHourlyRankMessage>(stream);
                        if (webcastHourlyRankUpdateMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastHourlyRankUpdateMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling RankUpdate");
                            RunEvent(OnRankUpdate, new RankUpdate(webcastHourlyRankUpdateMessage));
                        }
                        return;
                    #endregion

                    #region Others
                    case nameof(WebcastInRoomBannerMessage):
                        WebcastInRoomBannerMessage webcastInRoomBannerMessage = Deserialize<WebcastInRoomBannerMessage>(stream);
                        if (webcastInRoomBannerMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastInRoomBannerMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling InRoomBanner");
                            RunEvent(OnInRoomBanner, new InRoomBanner(webcastInRoomBannerMessage));
                        }
                        return;
                    case nameof(WebcastMsgDetectMessage):
                        WebcastMsgDetectMessage detectMessage = Deserialize<WebcastMsgDetectMessage>(stream);
                        if (detectMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(detectMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling DetectMessage");
                            RunEvent(OnDetectMessage, new DetectMessage(detectMessage));
                        }
                        return;
                    case nameof(WebcastBarrageMessage):
                        WebcastBarrageMessage barrageMessage = Deserialize<WebcastBarrageMessage>(stream);
                        if (barrageMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(barrageMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling BarrageMessage");
                            RunEvent(OnBarrageMessage, new BarrageMessage(barrageMessage));
                        }
                        return;
                    case nameof(WebcastUnauthorizedMemberMessage):
                        WebcastUnauthorizedMemberMessage unauthorizedMemberMessage = Deserialize<WebcastUnauthorizedMemberMessage>(stream);
                        if (unauthorizedMemberMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(unauthorizedMemberMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling UnauthorizedMember");
                            RunEvent(OnUnauthorizedMember, new UnauthorizedMember(unauthorizedMemberMessage));
                        }
                        return;
                    case nameof(WebcastLinkMessage):
                        WebcastLinkMessage linkMessage = Deserialize<WebcastLinkMessage>(stream);
                        if (linkMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(linkMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkMessage");
                            RunEvent(OnLinkMessage, new LinkMessage(linkMessage));
                        }
                        return;
                    case nameof(WebcastLinkLayerMessage):
                        WebcastLinkLayerMessage linkLayerMessage = Deserialize<WebcastLinkLayerMessage>(stream);
                        if (linkLayerMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(linkLayerMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling LinkLayerMessage");
                            RunEvent(OnLinkLayerMessage, new LinkLayerMessage(linkLayerMessage));
                        }
                        return;
                    case nameof(WebcastGiftBroadcastMessage):
                        WebcastGiftBroadcastMessage webcastGiftBroadcast = Deserialize<WebcastGiftBroadcastMessage>(stream);
                        if (webcastGiftBroadcast != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(webcastGiftBroadcast);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling GiftBroadcast");
                            RunEvent(OnGiftBroadcast, new GiftBroadcast(webcastGiftBroadcast));
                        }
                        return;
                    case nameof(WebcastOecLiveShoppingMessage):
                        WebcastOecLiveShoppingMessage oecLiveShoppingMessage = Deserialize<WebcastOecLiveShoppingMessage>(stream);
                        if (oecLiveShoppingMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(oecLiveShoppingMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling ShopMessage");
                            RunEvent(OnShopMessage, new ShopMessage(oecLiveShoppingMessage));
                        }
                        return;
                    case nameof(WebcastImDeleteMessage):
                        WebcastImDeleteMessage imDeleteMessage = Deserialize<WebcastImDeleteMessage>(stream);
                        if (imDeleteMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(imDeleteMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling IMDelete");
                            RunEvent(OnIMDelete, new IMDelete(imDeleteMessage));
                        }
                        return;
                    case nameof(WebcastQuestionNewMessage):
                        WebcastQuestionNewMessage newQuestionMessage = Deserialize<WebcastQuestionNewMessage>(stream);
                        if (newQuestionMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(newQuestionMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling Question");
                            RunEvent(OnQuestion, new Question(newQuestionMessage));
                        }
                        return;
                    case nameof(WebcastEnvelopeMessage):
                        WebcastEnvelopeMessage envelopeMessage = Deserialize<WebcastEnvelopeMessage>(stream);
                        if (envelopeMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(envelopeMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling Envelope");
                            RunEvent(OnEnvelope, new Envelope(envelopeMessage));
                        }
                        return;
                    case nameof(WebcastSubNotifyMessage):
                        WebcastSubNotifyMessage subNotifyMessage = Deserialize<WebcastSubNotifyMessage>(stream);
                        if (subNotifyMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(subNotifyMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling SubNotify");
                            RunEvent(OnSubNotify, new SubNotify(subNotifyMessage));
                        }
                        return;
                    case nameof(WebcastEmoteChatMessage):
                        WebcastEmoteChatMessage emoteMessage = Deserialize<WebcastEmoteChatMessage>(stream);
                        if (emoteMessage != null)
                        {
                            if (settings.CheckForUnparsedData)
                                CheckForUnparsedData(emoteMessage);
                            if (ShouldLog(LogLevel.Verbose))
                                Debug.Log($"Handling Emote");
                            RunEvent(OnEmote, new Emote(emoteMessage));
                        }
                        return;
                    #endregion

                    default:
                        if (ShouldLog(LogLevel.Warning))
                        {
                            Debug.LogWarning($"Unknown MessageType: {message.Type}");
                            Debug.LogWarning(Convert.ToBase64String(message.Binary));
                        }
                        // Run Unhandled
                        RunEvent(UnhandledEvent, message);
                        return;
                }
        }
        /// <summary>
        /// Handles a WebcastGiftMessage
        /// </summary>
        /// <param name="message">GiftMessage to Handle</param>
        private void HandleGiftMessage(WebcastGiftMessage message)
        {
            GiftId giftId = new GiftId
            {
                Gift = message.GiftId,
                UserName = message.Sender.UniqueId
            };
            if (activeGifts.ContainsKey(giftId))
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Updating Gift[{giftId.Gift}]Amount[{message.Amount}]");
                activeGifts[giftId].UpdateGiftAmount(message.Amount);
            }
            else
            {
                TikTokGift newGift = new TikTokGift(message);
                lock (activeGifts)
                    activeGifts.Add(giftId, newGift);
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"New Gift[{giftId.Gift}]Amount[{message.Amount}]");
                RunEvent(OnGift, newGift);
            }
            if (message.RepeatEnd)
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"GiftStreak Ended: [{giftId.Gift}] Amount[{message.Amount}]");
                activeGifts[giftId].FinishStreak();
                activeGifts.Remove(giftId);
            }
            else
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Handling GiftMessage");
                RunEvent(OnGiftMessage, new GiftMessage(message));
            }
        }
        /// <summary>
        /// Handles a WebcastSocialMessage
        /// </summary>
        /// <param name="messageEvent">Message to Handle</param>
        private void HandleSocialMessage(WebcastSocialMessage messageEvent, byte[] message)
        {
            Match match = Regex.Match(messageEvent.Header.SocialData.Type, "pm_mt_guidance_viewer_([0-9]+)_share");
            if (match.Success)
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Handling Share");
                if (int.TryParse(match.Groups[0].Value, out int result))
                    RunEvent(OnShare, new Share(messageEvent, result));
                return;
            }
            switch (messageEvent.Header.SocialData.Type)
            {
                case SocialTypes.LikeType:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Like");
                    RunEvent(OnLike, new Like(messageEvent));
                    return;
                case SocialTypes.FollowType:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Follow");
                    RunEvent(OnFollow, new Follow(messageEvent));
                    return;
                case SocialTypes.ShareType:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Share");
                    RunEvent(OnShare, new Share(messageEvent, 1));
                    return;
                case SocialTypes.JoinType:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Join");
                    RunEvent(OnJoin, new Join(messageEvent));
                    return;
                default:
                    if (ShouldLog(LogLevel.Warning))
                        Debug.LogWarning($"Handling UnhandledSocialEvent!{Environment.NewLine}[{Convert.ToBase64String(message)}]");
                    // Run Unhandled
                    RunEvent(UnhandledSocialEvent, messageEvent);
                    return;
            }
        }
        /// <summary>
        /// Handles a WebcastMemberMessage
        /// </summary>
        /// <param name="msg">Message to Handle</param>
        private void HandleMemberMessage(WebcastMemberMessage msg, byte[] message)
        {
            switch (msg.Action)
            {
                case (int)MemberMessageAction.Joined:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Join");
                    RunEvent(OnJoin, new Join(msg));
                    return;
                case (int)MemberMessageAction.Subscribed:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Subscribe");
                    RunEvent(OnSubscribe, new Subscribe(msg));
                    return;
//                case 26: // As of yet undetermined
//                case 27: // ?? (Done by host? (User null, User2 == host))
//                case 50: // Share?
                default:
                case (int)MemberMessageAction.Unknown:
                    if (ShouldLog(LogLevel.Warning))
                        Debug.LogWarning($"Handling UnhandledMemberMessage!{Environment.NewLine}[{Convert.ToBase64String(message)}]");
                    RunEvent(UnhandledMemberEvent, msg);
                    return;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Invokes Event. Catches Exceptions
        /// </summary>
        /// <param name="evt">Event to Invok</param>
        /// <param name="e">Arguments for Event</param>
        private void RunEvent(EventHandler evt, EventArgs e = null) => RunEvent(() => evt?.Invoke(this, e));
        /// <summary>
        /// Invokes Event. Catches Exceptions
        /// </summary>
        /// <param name="evt">Event to Invok</param>
        /// <param name="e">Arguments for Event</param>
        private void RunEvent(TikTokEventHandler evt, EventArgs e = null) => RunEvent(() => evt?.Invoke(this, e));
        /// <summary>
        /// Invokes Event. Catches Exceptions
        /// </summary>
        /// <typeparam name="TEventArgs">Type of Event-Args for Event</typeparam>
        /// <param name="evt">Event to Invok</param>
        /// <param name="args">Arguments for Event</param>
        private void RunEvent<TEventArgs>(TikTokEventHandler<TEventArgs> evt, TEventArgs args) => RunEvent(() => evt?.Invoke(this, args));
        /// <summary>
        /// Invokes Action. Catches Exceptions
        /// </summary>
        /// <param name="action">Action to Invoke</param>
        private void RunEvent(Action action)
        {
#if UNITY // This Code is strictly for TikTokLive-Unity
            // Run Events on Unity's Main Thread instead of the Socket-Thread. This will solve about 75% of user-errors.
            TikTokLiveUnity.Utils.Dispatcher.RunOnMainThread(() => {
#endif
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    UserCallbackException exc = new UserCallbackException("Error during custom User-Event", e);
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(exc);
                    CallOnException(exc);
                    // This is a recoverable error. Client remains connected. Do not Throw
                }
#if UNITY
            });
#endif
        }
        #endregion

        #region Parsing
        /// <summary>
        /// Deserializes Stream to Message
        /// </summary>
        /// <typeparam name="T">MessageType to parse to</typeparam>
        /// <param name="stream">Stream to Parse</param>
        /// <returns>Message for Stream-Data</returns>
        private T Deserialize<T>(MemoryStream stream) where T : IExtensible
        {
            try
            {
                T msg = Serializer.Deserialize<T>(stream);
                return msg;
            }
            catch (OverflowException e)
            {
                byte[] byteData = stream.ToArray();
                WebcastMessageException exc = new WebcastMessageException($"Error Deserializing Message. Base64 for Message: [{Convert.ToBase64String(byteData)}]", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                CallOnException(exc); // Inform user of Error
                // This is a recoverable error. Client remains connected. Do not Throw
                return default; // Return null to quietly quit out of this message
            }
            catch (Exception ex)
            {
                byte[] byteData = stream.ToArray();
                WebcastMessageException exc = new WebcastMessageException($"Error Deserializing Message. Base64 for Message: [{Convert.ToBase64String(byteData)}]", ex);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                CallOnException(exc); // Inform user of Error
                // This is a recoverable error. Client remains connected. Do not Throw
                return default; // Return null to quietly quit out of this message
            }
        }

        #region Debug
        /// <summary>
        /// Searches Object for Unparsed Data
        /// <para>
        /// Uses Reflection to pull out the unparsed data. Should not be used in production
        /// </para>
        /// </summary>
        /// <param name="msg">Object to Check</param>
        private void CheckForUnparsedData(AProtoBase msg)
        {
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log($"Checking {msg.GetType().Name} for Unparsed Data");
            if (msg._extension != null && msg._extension.GetLength() > 0)
            {
                FieldInfo f = msg._extension.GetType().GetField("buffer", BindingFlags.Instance | BindingFlags.NonPublic);
                byte[] foundMsg = (byte[])f?.GetValue(msg._extension);
                Debug.LogWarning($"Found unparsed Data in Message {msg.GetType()}");
                Debug.Log($"Unparsed Data: {Environment.NewLine}{Convert.ToBase64String(foundMsg)}");
            }
            // Check Child-Objects
            RecursiveCheckForUnparsedData(msg);
        }

        /// <summary>
        /// Checks Properties for Unparsed Data
        /// </summary>
        /// <param name="msg">Message to check Properties on</param>
        private void RecursiveCheckForUnparsedData(AProtoBase msg)
        {
            PropertyInfo[] properties = msg.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                object obj = properties[i].GetValue(msg);
                if (obj != null && typeof(AProtoBase).IsAssignableFrom(obj.GetType()))
                    CheckForUnparsedData((AProtoBase)obj);
            }
        }
        #endregion
        #endregion
        #endregion
    }
}
