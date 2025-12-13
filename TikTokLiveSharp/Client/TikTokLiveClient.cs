using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.Config;
using TikTokLiveSharp.Debugging;
using TikTokLiveSharp.Errors.Messaging;
using TikTokLiveSharp.Events;
using TikTokLiveSharp.Events.Beta;
using TikTokLiveSharp.Events.Enums;
using TikTokLiveSharp.Events.Objects;
using TikTokLiveSharp.Models.Protobuf;

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
            public long Gift;
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
        public struct SocialTypes
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
        public event TikTokEventHandler<AccessControl> OnAccessControl;
        public event TikTokEventHandler<AccessRecall> OnAccessRecall;
        public event TikTokEventHandler<AlertBoxAuditResult> OnAlertBoxAuditResult;
        public event TikTokEventHandler<Barrage> OnBarrage;
        public event TikTokEventHandler<BindingGift> OnBindingGift;
        public event TikTokEventHandler<BoostCardMessage> OnBoostCardMessage;
        public event TikTokEventHandler<BottomMessage> OnBottomMessage;
        /// <summary>
        /// Closed Caption-Message for Video.
        /// </summary>
        public event TikTokEventHandler<Caption> OnCaption;
        /// <summary>
        /// Event fired when a viewer Commented on the stream.
        /// </summary>
        public event TikTokEventHandler<Chat> OnChatMessage;
        /// <summary>
        /// Event fired when the Host changed the stream's Status (e.g. Stream Ended).
        /// </summary>
        public event TikTokEventHandler<ControlMessage> OnControlMessage;
        /// <summary>
        /// Event fired when the Host pauses the stream.
        /// </summary>
        public event TikTokEventHandler<ControlMessage> OnLivePaused;
        /// <summary>
        /// Event fired when the Host resumes the stream.
        /// </summary>
        public event TikTokEventHandler<ControlMessage> OnLiveResumed;
        /// <summary>
        /// Event fired when the Host ends the stream.
        /// </summary>
        public event TikTokEventHandler<ControlMessage> OnLiveEnded;
        /// <summary>
        /// Event fired when a ControlMessage was received that could not be properly interpreted.
        /// <para>
        /// It's up to you how to interpret this message.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<Models.Protobuf.Messages.ControlMessage> OnUnhandledControlMessage;
        public event TikTokEventHandler<EmoteChat> OnEmoteChat;
        public event TikTokEventHandler<Envelope> OnEnvelope;
        public event TikTokEventHandler<GameRankNotify> OnGameRankNotify;
        /// <summary>
        /// Event fired when a viewer sends a Gift to the Host.
        /// <para>
        /// Fires when a Gift-Streak starts. Gift-Events are fired for subsequent messages.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<TikTokGift> OnGift;
        /// <summary>
        /// Event fired when a viewer sends a Gift to the Host.
        /// <para>
        /// Fires for every GiftMessage, including the StreakEnd-Message.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<GiftMessage> OnGiftMessage;
        public event TikTokEventHandler<GiftPrompt> OnGiftPrompt;
        public event TikTokEventHandler<GoalUpdate> OnGoalUpdate;
        public event TikTokEventHandler<ImDelete> OnImDelete;
        public event TikTokEventHandler<InRoomBannerMessage> OnInRoomBannerMessage;
        /// <summary>
        /// Event fired when a user Joins the stream (as a viewer).
        /// </summary>
        public event TikTokEventHandler<Like> OnLike;
        public event TikTokEventHandler<LinkLayerMessage> OnLinkLayerMessage;
        public event TikTokEventHandler<LinkMessage> OnLinkMessage;
        public event TikTokEventHandler<LinkMicFanTicketMethod> OnLinkMicFanTicketMethod;
        public event TikTokEventHandler<LinkMicMethod> OnLinkMicMethod;
        public event TikTokEventHandler<LinkState> OnLinkState;
        /// <summary>
        /// Event fired for the Host's Intro-Message for the stream.
        /// </summary>
        public event TikTokEventHandler<LiveIntro> OnLiveIntro;
        public event TikTokEventHandler<MarqueeAnnouncement> OnMarqueeAnnouncement;
        /// <summary>
        /// Event fired for each MemberMessage-Event.
        /// <para>
        /// Includes Join- & Subscribe-Messages, among others. Known Sub-Types will fire their own Events as well.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<MemberMessage> OnMemberMessage;
        /// <summary>
        /// Event fired when a MemberMessage was received that could not be properly interpreted.
        /// <para>
        /// It's up to you how to interpret this message.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<Models.Protobuf.Messages.MemberMessage> OnUnhandledMemberMessage;
        /// <summary>
        /// Event fired when a user joins the stream as a viewer.
        /// </summary>
        public event TikTokEventHandler<Join> OnJoin;
        /// <summary>
        /// Event fired when a viewer Subscribed to the Host.
        /// </summary>
        public event TikTokEventHandler<Subscribe> OnSubscribe;
        public event TikTokEventHandler<MsgDetect> OnMsgDetect;
        public event TikTokEventHandler<Notice> OnNotice;
        public event TikTokEventHandler<Notify> OnNotify;
        public event TikTokEventHandler<PartnershipDropsUpdate> OnPartnershipDropsUpdate;
        public event TikTokEventHandler<PartnershipGameOffline> OnPartnershipGameOffline;
        public event TikTokEventHandler<PartnershipPunish> OnPartnershipPunish;
        public event TikTokEventHandler<Perception> OnPerception;
        public event TikTokEventHandler<Poll> OnPoll;
        public event TikTokEventHandler<RankText> OnRankText;
        public event TikTokEventHandler<RankUpdate> OnRankUpdate;
        public event TikTokEventHandler<RoomMessage> OnRoomMessage;
        /// <summary>
        /// Event fired when the Room updates its State.
        /// <para>
        /// This event runs intermittently and contains stats such as ViewerCount.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<RoomUpdate> OnRoomUpdate;
        public event TikTokEventHandler<RoomVerify> OnRoomVerify;
        /// <summary>
        /// Event fired for each SocialMessage-Event.
        /// <para>
        /// Includes Follow-, Like-, Share- & Join-Messages, among others. Known Sub-Types will fire their own Events as well.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<SocialMessage> OnSocialMessage;
        /// <summary>
        /// Event fired when a viewer followed the Host.
        /// </summary>
        public event TikTokEventHandler<Follow> OnFollow;
        /// <summary>
        /// Event fired when a viewer shared the stream.
        /// </summary>
        public event TikTokEventHandler<Share> OnShare;
        /// <summary>
        /// Event fired when a SocialMessage was received that could not be properly interpreted.
        /// <para>
        /// It's up to you how to interpret this message.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<Models.Protobuf.Messages.SocialMessage> OnUnhandledSocialMessage;
        public event TikTokEventHandler<Speaker> OnSpeaker;
        public event TikTokEventHandler<SubCapsule> OnSubCapsule;
        public event TikTokEventHandler<SubNotify> OnSubNotify;
        public event TikTokEventHandler<SubPinEvent> OnSubPinEvent;
        public event TikTokEventHandler<SubscriptionNotify> OnSubscriptionNotify;
        public event TikTokEventHandler<Toast> OnToast;
        public event TikTokEventHandler<UnauthorizedMember> OnUnauthorizedMember;
        /// <summary>
        /// Event fired for a ServerMessage that could not be parsed into a known object.
        /// <para>
        /// It's up to you how to interpret this message.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<Models.Protobuf.Messages.Message> OnUnhandled;
        /// <summary>
        /// Event fired for EVERY ServerMessage.
        /// <para>
        /// Use this for full manual handling of all messages.
        /// </para>
        /// </summary>
        public event TikTokEventHandler<Models.Protobuf.Messages.Message> OnMessage;

        // The events below are less well understood.
        // They are missing a lot of variable-names & -Types (especially Enums).
        public event TikTokEventHandler<GiftBroadcast> OnGiftBroadcast;
        public event TikTokEventHandler<HourlyRank> OnHourlyRank;
        public event TikTokEventHandler<LinkMicArmies> OnLinkMicArmies;
        public event TikTokEventHandler<LinkMicBattle> OnLinkMicBattle;
        public event TikTokEventHandler<LinkMicBattlePunishFinish> OnLinkMicBattlePunishFinish;
        public event TikTokEventHandler<LinkmicBattleTask> OnLinkmicBattleTask;
        public event TikTokEventHandler<OecLiveShopping> OnOecLiveShopping;
        public event TikTokEventHandler<QuestionNew> OnQuestionNew;
        /// <summary>
        /// Event fired when the Host pins a message to the stream?
        /// </summary>
        public event TikTokEventHandler<RoomPinMessage> OnRoomPinMessage;
        public event TikTokEventHandler<SystemMessage> OnSystemMessage;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the TikTok Live client.
        /// Used for retrieving a stream of information from live streams.
        /// </summary>
        /// <param name="hostId">Host-Name for Room to Connect to</param>
        /// <param name="roomId">RoomId to connect to. Overrides HostId when connecting & skips scraping webpage</param>
        /// <param name="settings">Settings for Client</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        public TikTokLiveClient(string hostId, string roomId = null, ClientSettings? settings = null, Dictionary<string, object> clientParams = null)
            : base(hostId, roomId, settings, clientParams)
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
            float? timeout = null,
            float? reconnectInterval = null,
            float? pollingInterval = null,
            string roomId = "",
            bool enableCompression = true,
            bool skipRoomInfo = false,
            Dictionary<string, object> clientParams = null,
            bool processInitialData = true,
            bool enableExtendedGiftInfo = true,
            IWebProxy proxyHandler = null,
            string lang = "en-US",
            uint socketBufferSize = 10_000,
            bool logDebug = true,
            LogLevel logLevel = LogLevel.Error | LogLevel.Warning,
            bool printMessageData = false,
            bool checkForUnparsedData = false,
            string customSigningServer = null,
            string signingServerApiKey = null) 
            : base(uniqueID,
                timeout,
                reconnectInterval,
                pollingInterval,
                roomId,
                skipRoomInfo,
                enableCompression,
                clientParams,
                processInitialData,
                enableExtendedGiftInfo,
                proxyHandler,
                lang,
                socketBufferSize,
                logDebug,
                logLevel,
                printMessageData,
                checkForUnparsedData,
                customSigningServer,
                signingServerApiKey)
        { }
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
            string roomId = await base.Connect(onConnectException);
            if (ShouldLog(LogLevel.Information))
                Debug.Log($"Connected to Room {roomId}");
            RunEvent(OnConnected, Connected);
            return roomId;
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
            // Clear all active gifts
            lock (activeGifts)
            {
                foreach (TikTokGift activeGift in activeGifts.Values)
                    activeGift.FinishStreak();
                activeGifts.Clear();
            }
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
        /// <param name="response">Response to Handle</param>
        protected override void HandleMessages(Models.Protobuf.Messages.Response response)
        {
            if (ShouldLog(LogLevel.Information))
                Debug.Log($"Handling {response.MessagesList.Count} Messages in Response");
            foreach (Models.Protobuf.Messages.Message message in response.MessagesList)
                try
                {
                    HandleMessage(message);
                }
                catch (Exception e)
                {
                    HandleMessageException exc = new HandleMessageException($"Error whilst Handling Message. Stopping Client.{Environment.NewLine}Final Message: {Convert.ToBase64String(message.Payload)}", e);
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(exc);
                    CallOnException(exc);
                    // This is an irrecoverable error. Crash this Thread
                    throw exc;
                }
        }

        /// <summary>
        /// Handles Message received from TikTokServer
        /// </summary>
        /// <param name="message">Message to Handle</param>
        private void HandleMessage(Models.Protobuf.Messages.Message message)
        {
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log($"Handling Message: {message.Method}");
            if (settings.PrintMessageData)
                Debug.Log($"Handling Message:{Environment.NewLine}{Convert.ToBase64String(message.Payload)}");

            string method = message.Method;
            if (method.StartsWith("Webcast"))
                method = method.Substring(7);

            // Run OnMessage
            RunEvent(OnMessage, message);

            using (MemoryStream stream = new MemoryStream(message.Payload))
                switch (method)
                {
                    #region Known
                    case nameof(Models.Protobuf.Messages.AccessControlMessage):
                        Models.Protobuf.Messages.AccessControlMessage accessControlMessage = Deserialize<Models.Protobuf.Messages.AccessControlMessage>(stream);
                        if (accessControlMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(accessControlMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling AccessControlMessage!");
                        RunEvent(OnAccessControl, new AccessControl(accessControlMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.AccessRecallMessage):
                        Models.Protobuf.Messages.AccessRecallMessage accessRecallMessage = Deserialize<Models.Protobuf.Messages.AccessRecallMessage>(stream);
                        if (accessRecallMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(accessRecallMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling AccessRecallMessage!");
                        RunEvent(OnAccessRecall, new AccessRecall(accessRecallMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.AlertBoxAuditResultMessage):
                        Models.Protobuf.Messages.AlertBoxAuditResultMessage alertBoxAuditResultMessage = Deserialize<Models.Protobuf.Messages.AlertBoxAuditResultMessage>(stream);
                        if (alertBoxAuditResultMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(alertBoxAuditResultMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling AlertBoxAuditResultMessage!");
                        RunEvent(OnAlertBoxAuditResult, new AlertBoxAuditResult(alertBoxAuditResultMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.BarrageMessage):
                        Models.Protobuf.Messages.BarrageMessage barrageMessage = Deserialize<Models.Protobuf.Messages.BarrageMessage>(stream);
                        if (barrageMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(barrageMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling BarrageMessage!");
                        RunEvent(OnBarrage, new Barrage(barrageMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.BindingGiftMessage):
                        Models.Protobuf.Messages.BindingGiftMessage bindingGiftMessage = Deserialize<Models.Protobuf.Messages.BindingGiftMessage>(stream);
                        if (bindingGiftMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(bindingGiftMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling BindingGiftMessage!");
                        RunEvent(OnBindingGift, new BindingGift(bindingGiftMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.BoostCardMessage):
                        Models.Protobuf.Messages.BoostCardMessage boostCardMessage = Deserialize<Models.Protobuf.Messages.BoostCardMessage>(stream);
                        if (boostCardMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(boostCardMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling BoostCardMessage!");
                        RunEvent(OnBoostCardMessage, new BoostCardMessage(boostCardMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.BottomMessage):
                        Models.Protobuf.Messages.BottomMessage bottomMessage = Deserialize<Models.Protobuf.Messages.BottomMessage>(stream);
                        if (bottomMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(bottomMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling BottomMessage!");
                        RunEvent(OnBottomMessage, new BottomMessage(bottomMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.CaptionMessage):
                        Models.Protobuf.Messages.CaptionMessage captionMessage = Deserialize<Models.Protobuf.Messages.CaptionMessage>(stream);
                        if (captionMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(captionMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling CaptionMessage!");
                        RunEvent(OnCaption, new Caption(captionMessage));
                        break;
                    case nameof(Models.Protobuf.Messages.ChatMessage):
                        Models.Protobuf.Messages.ChatMessage chatMessage = Deserialize<Models.Protobuf.Messages.ChatMessage>(stream);
                        if (chatMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(chatMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling ChatMessage!");
                        RunEvent(OnChatMessage, new Chat(chatMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.ControlMessage):
                        Models.Protobuf.Messages.ControlMessage controlMessage = Deserialize<Models.Protobuf.Messages.ControlMessage>(stream);
                        if (controlMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(controlMessage, message.Payload);
                        HandleControlMessage(controlMessage, message.Payload);
                        return;
                    case nameof(Models.Protobuf.Messages.EmoteChatMessage):
                        Models.Protobuf.Messages.EmoteChatMessage emoteChatMessage = Deserialize<Models.Protobuf.Messages.EmoteChatMessage>(stream);
                        if (emoteChatMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(emoteChatMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling EmoteChatMessage!");
                        RunEvent(OnEmoteChat, new EmoteChat(emoteChatMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.EnvelopeMessage):
                        Models.Protobuf.Messages.EnvelopeMessage envelopeMessage = Deserialize<Models.Protobuf.Messages.EnvelopeMessage>(stream);
                        if (envelopeMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(envelopeMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling EnvelopeMessage!");
                        RunEvent(OnEnvelope, new Envelope(envelopeMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.GameRankNotifyMessage):
                        Models.Protobuf.Messages.GameRankNotifyMessage gameRankNotifyMessage = Deserialize<Models.Protobuf.Messages.GameRankNotifyMessage>(stream);
                        if (gameRankNotifyMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(gameRankNotifyMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling GameRankNotifyMessage!");
                        RunEvent(OnGameRankNotify, new GameRankNotify(gameRankNotifyMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.GiftMessage):
                        Models.Protobuf.Messages.GiftMessage giftMessage = Deserialize<Models.Protobuf.Messages.GiftMessage>(stream);
                        if (giftMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(giftMessage, message.Payload);
                        HandleGiftMessage(giftMessage);
                        return;
                    case nameof(Models.Protobuf.Messages.GiftPromptMessage):
                        Models.Protobuf.Messages.GiftPromptMessage giftPromptMessage = Deserialize<Models.Protobuf.Messages.GiftPromptMessage>(stream);
                        if (giftPromptMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(giftPromptMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling GiftPromptMessage!");
                        RunEvent(OnGiftPrompt, new GiftPrompt(giftPromptMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.GoalUpdateMessage):
                        Models.Protobuf.Messages.GoalUpdateMessage goalUpdateMessage = Deserialize<Models.Protobuf.Messages.GoalUpdateMessage>(stream);
                        if (goalUpdateMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(goalUpdateMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling GoalUpdateMessage!");
                        RunEvent(OnGoalUpdate, new GoalUpdate(goalUpdateMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.ImDeleteMessage):
                        Models.Protobuf.Messages.ImDeleteMessage imDeleteMessage = Deserialize<Models.Protobuf.Messages.ImDeleteMessage>(stream);
                        if (imDeleteMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(imDeleteMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling ImDeleteMessage!");
                        RunEvent(OnImDelete, new ImDelete(imDeleteMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.InRoomBannerMessage):
                        Models.Protobuf.Messages.InRoomBannerMessage inRoomBannerMessage = Deserialize<Models.Protobuf.Messages.InRoomBannerMessage>(stream);
                        if (inRoomBannerMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(inRoomBannerMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling InRoomBannerMessage!");
                        RunEvent(OnInRoomBannerMessage, new InRoomBannerMessage(inRoomBannerMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.LikeMessage):
                        Models.Protobuf.Messages.LikeMessage likeMessage = Deserialize<Models.Protobuf.Messages.LikeMessage>(stream);
                        if (likeMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(likeMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LikeMessage!");
                        RunEvent(OnLike, new Like(likeMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.LinkLayerMessage):
                        Models.Protobuf.Messages.LinkLayerMessage linkLayerMessage = Deserialize<Models.Protobuf.Messages.LinkLayerMessage>(stream);
                        if (linkLayerMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkLayerMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkLayerMessage!");
                        RunEvent(OnLinkLayerMessage, new LinkLayerMessage(linkLayerMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.LinkMessage):
                        Models.Protobuf.Messages.LinkMessage linkMessage = Deserialize<Models.Protobuf.Messages.LinkMessage>(stream);
                        if (linkMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkMessage!");
                        RunEvent(OnLinkMessage, new LinkMessage(linkMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.LinkMicFanTicketMethod):
                        Models.Protobuf.Messages.LinkMicFanTicketMethod fanTicketMethod = Deserialize<Models.Protobuf.Messages.LinkMicFanTicketMethod>(stream);
                        if (fanTicketMethod == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(fanTicketMethod, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkMicFanTicketMethod!");
                        RunEvent(OnLinkMicFanTicketMethod, new LinkMicFanTicketMethod(fanTicketMethod));
                        return;
                    case nameof(Models.Protobuf.Messages.LinkMicMethod):
                        Models.Protobuf.Messages.LinkMicMethod linkMicMethod = Deserialize<Models.Protobuf.Messages.LinkMicMethod>(stream);
                        if (linkMicMethod == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkMicMethod, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkMicMethod");
                        RunEvent(OnLinkMicMethod, new LinkMicMethod(linkMicMethod));
                        return;
                    case nameof(Models.Protobuf.Messages.LinkStateMessage):
                        Models.Protobuf.Messages.LinkStateMessage linkStateMessage = Deserialize<Models.Protobuf.Messages.LinkStateMessage>(stream);
                        if (linkStateMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkStateMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkStateMessage");
                        RunEvent(OnLinkState, new LinkState(linkStateMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.LiveIntroMessage):
                        Models.Protobuf.Messages.LiveIntroMessage introMessage = Deserialize<Models.Protobuf.Messages.LiveIntroMessage>(stream);
                        if (introMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(introMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LiveIntroMessage!");
                        RunEvent(OnLiveIntro, new LiveIntro(introMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.MarqueeAnnouncementMessage):
                        Models.Protobuf.Messages.MarqueeAnnouncementMessage marqueeAnnouncementMessage = Deserialize<Models.Protobuf.Messages.MarqueeAnnouncementMessage>(stream);
                        if (marqueeAnnouncementMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(marqueeAnnouncementMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling MarqueeAnnouncementMessage!");
                        RunEvent(OnMarqueeAnnouncement, new MarqueeAnnouncement(marqueeAnnouncementMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.MemberMessage):
                        Models.Protobuf.Messages.MemberMessage memberMessage = Deserialize<Models.Protobuf.Messages.MemberMessage>(stream);
                        if (memberMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(memberMessage, message.Payload);
                        HandleMemberMessage(memberMessage, message.Payload);
                        return;
                    case nameof(Models.Protobuf.Messages.MsgDetectMessage):
                        Models.Protobuf.Messages.MsgDetectMessage msgDetectMessage = Deserialize<Models.Protobuf.Messages.MsgDetectMessage>(stream);
                        if (msgDetectMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(msgDetectMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling MsgDetectMessage!");
                        RunEvent(OnMsgDetect, new MsgDetect(msgDetectMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.NoticeMessage):
                        Models.Protobuf.Messages.NoticeMessage noticeMessage = Deserialize<Models.Protobuf.Messages.NoticeMessage>(stream);
                        if (noticeMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(noticeMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling NoticeMessage!");
                        RunEvent(OnNotice, new Notice(noticeMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.NotifyMessage):
                        Models.Protobuf.Messages.NotifyMessage notifyMessage = Deserialize<Models.Protobuf.Messages.NotifyMessage>(stream);
                        if (notifyMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(notifyMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling NotifyMessage!");
                        RunEvent(OnNotify, new Notify(notifyMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.PartnershipDropsUpdateMessage):
                        Models.Protobuf.Messages.PartnershipDropsUpdateMessage partnershipDropsUpdateMessage = Deserialize<Models.Protobuf.Messages.PartnershipDropsUpdateMessage>(stream);
                        if (partnershipDropsUpdateMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(partnershipDropsUpdateMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling PartnershipDropsUpdateMessage!");
                        RunEvent(OnPartnershipDropsUpdate, new PartnershipDropsUpdate(partnershipDropsUpdateMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.PartnershipGameOfflineMessage):
                        Models.Protobuf.Messages.PartnershipGameOfflineMessage partnershipGameOfflineMessage = Deserialize<Models.Protobuf.Messages.PartnershipGameOfflineMessage>(stream);
                        if (partnershipGameOfflineMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(partnershipGameOfflineMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling PartnershipGameOfflineMessage!");
                        RunEvent(OnPartnershipGameOffline, new PartnershipGameOffline(partnershipGameOfflineMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.PartnershipPunishMessage):
                        Models.Protobuf.Messages.PartnershipPunishMessage partnershipPunishMessage = Deserialize<Models.Protobuf.Messages.PartnershipPunishMessage>(stream);
                        if (partnershipPunishMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(partnershipPunishMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling PartnershipPunishMessage!");
                        RunEvent(OnPartnershipPunish, new PartnershipPunish(partnershipPunishMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.PerceptionMessage):
                        Models.Protobuf.Messages.PerceptionMessage perceptionMessage = Deserialize<Models.Protobuf.Messages.PerceptionMessage>(stream);
                        if (perceptionMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(perceptionMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling PerceptionMessage!");
                        RunEvent(OnPerception, new Perception(perceptionMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.PollMessage):
                        Models.Protobuf.Messages.PollMessage pollMessage = Deserialize<Models.Protobuf.Messages.PollMessage>(stream);
                        if (pollMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(pollMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling PollMessage!");
                        RunEvent(OnPoll, new Poll(pollMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.RankTextMessage):
                        Models.Protobuf.Messages.RankTextMessage rankTextMessage = Deserialize<Models.Protobuf.Messages.RankTextMessage>(stream);
                        if (rankTextMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(rankTextMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling RankTextMessage!");
                        RunEvent(OnRankText, new RankText(rankTextMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.RankUpdateMessage):
                        Models.Protobuf.Messages.RankUpdateMessage rankUpdateMessage = Deserialize<Models.Protobuf.Messages.RankUpdateMessage>(stream);
                        if (rankUpdateMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(rankUpdateMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling RankUpdateMessage!");
                        RunEvent(OnRankUpdate, new RankUpdate(rankUpdateMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.RoomMessage):
                        Models.Protobuf.Messages.RoomMessage roomMessage = Deserialize<Models.Protobuf.Messages.RoomMessage>(stream);
                        if (roomMessage == null) 
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(roomMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling RoomMessage!");
                        RunEvent(OnRoomMessage, new RoomMessage(roomMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.RoomUserSeqMessage):
                        Models.Protobuf.Messages.RoomUserSeqMessage roomUserSeqMessage = Deserialize<Models.Protobuf.Messages.RoomUserSeqMessage>(stream);
                        if (roomUserSeqMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(roomUserSeqMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling RoomUpdateMessage");
                        ViewerCount = roomUserSeqMessage.Total;
                        RunEvent(OnRoomUpdate, new RoomUpdate(roomUserSeqMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.RoomVerifyMessage):
                        Models.Protobuf.Messages.RoomVerifyMessage roomVerifyMessage = Deserialize<Models.Protobuf.Messages.RoomVerifyMessage>(stream);
                        if (roomVerifyMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(roomVerifyMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling RoomVerifyMessage");
                        RunEvent(OnRoomVerify, new RoomVerify(roomVerifyMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.SocialMessage):
                        Models.Protobuf.Messages.SocialMessage socialMessage = Deserialize<Models.Protobuf.Messages.SocialMessage>(stream);
                        if (socialMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(socialMessage, message.Payload);
                        HandleSocialMessage(socialMessage, message.Payload);
                        return;
                    case nameof(Models.Protobuf.Messages.SpeakerMessage):
                        Models.Protobuf.Messages.SpeakerMessage speakerMessage = Deserialize<Models.Protobuf.Messages.SpeakerMessage>(stream);
                        if (speakerMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(speakerMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling SpeakerMessage");
                        RunEvent(OnSpeaker, new Speaker(speakerMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.SubCapsuleMessage):
                        Models.Protobuf.Messages.SubCapsuleMessage subCapsuleMessage = Deserialize<Models.Protobuf.Messages.SubCapsuleMessage>(stream);
                        if (subCapsuleMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(subCapsuleMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling SubCapsuleMessage");
                        RunEvent(OnSubCapsule, new SubCapsule(subCapsuleMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.SubNotifyMessage):
                        Models.Protobuf.Messages.SubNotifyMessage subNotifyMessage = Deserialize<Models.Protobuf.Messages.SubNotifyMessage>(stream);
                        if (subNotifyMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(subNotifyMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling SubNotifyMessage");
                        RunEvent(OnSubNotify, new SubNotify(subNotifyMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.SubPinEventMessage):
                        Models.Protobuf.Messages.SubPinEventMessage subPinEventMessage = Deserialize<Models.Protobuf.Messages.SubPinEventMessage>(stream);
                        if (subPinEventMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(subPinEventMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling SubPinEventMessage");
                        RunEvent(OnSubPinEvent, new SubPinEvent(subPinEventMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.SubscriptionNotifyMessage):
                        Models.Protobuf.Messages.SubscriptionNotifyMessage subscriptionNotifyMessage = Deserialize<Models.Protobuf.Messages.SubscriptionNotifyMessage>(stream);
                        if (subscriptionNotifyMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(subscriptionNotifyMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling SubscriptionNotifyMessage");
                        RunEvent(OnSubscriptionNotify, new SubscriptionNotify(subscriptionNotifyMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.ToastMessage):
                        Models.Protobuf.Messages.ToastMessage toastMessage = Deserialize<Models.Protobuf.Messages.ToastMessage>(stream);
                        if (toastMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(toastMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling ToastMessage");
                        RunEvent(OnToast, new Toast(toastMessage));
                        return;
                    case nameof(Models.Protobuf.Messages.UnauthorizedMemberMessage):
                        Models.Protobuf.Messages.UnauthorizedMemberMessage unauthorizedMemberMessage = Deserialize<Models.Protobuf.Messages.UnauthorizedMemberMessage>(stream);
                        if (unauthorizedMemberMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(unauthorizedMemberMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling UnauthorizedMemberMessage");
                        RunEvent(OnUnauthorizedMember, new UnauthorizedMember(unauthorizedMemberMessage));
                        return;
                    #endregion

                    #region Unknown
                    case nameof(Models.Protobuf.UnknownObjects.GiftBroadcastMessage):
                        Models.Protobuf.UnknownObjects.GiftBroadcastMessage giftBroadcastMessage = Deserialize<Models.Protobuf.UnknownObjects.GiftBroadcastMessage>(stream);
                        if (giftBroadcastMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(giftBroadcastMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling GiftBroadcastMessage!");
                        RunEvent(OnGiftBroadcast, new GiftBroadcast(giftBroadcastMessage));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.HourlyRankMessage):
                        Models.Protobuf.UnknownObjects.HourlyRankMessage hourlyRankMessage = Deserialize<Models.Protobuf.UnknownObjects.HourlyRankMessage>(stream);
                        if (hourlyRankMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(hourlyRankMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling HourlyRankMessage!");
                        RunEvent(OnHourlyRank, new HourlyRank(hourlyRankMessage));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.LinkMicArmies):
                        Models.Protobuf.UnknownObjects.LinkMicArmies linkMicArmies = Deserialize<Models.Protobuf.UnknownObjects.LinkMicArmies>(stream);
                        if (linkMicArmies == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkMicArmies, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkMicArmies!");
                        RunEvent(OnLinkMicArmies, new LinkMicArmies(linkMicArmies));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.LinkMicBattle):
                        Models.Protobuf.UnknownObjects.LinkMicBattle linkMicBattle = Deserialize<Models.Protobuf.UnknownObjects.LinkMicBattle>(stream);
                        if (linkMicBattle == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkMicBattle, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkMicBattle!");
                        RunEvent(OnLinkMicBattle, new LinkMicBattle(linkMicBattle));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.LinkMicBattlePunishFinish):
                        Models.Protobuf.UnknownObjects.LinkMicBattlePunishFinish linkMicBattlePunishFinish = Deserialize<Models.Protobuf.UnknownObjects.LinkMicBattlePunishFinish>(stream);
                        if (linkMicBattlePunishFinish == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkMicBattlePunishFinish, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkMicBattlePunishFinish!");
                        RunEvent(OnLinkMicBattlePunishFinish, new LinkMicBattlePunishFinish(linkMicBattlePunishFinish));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.LinkmicBattleTaskMessage):
                        Models.Protobuf.UnknownObjects.LinkmicBattleTaskMessage linkMicBattleTaskMessage = Deserialize<Models.Protobuf.UnknownObjects.LinkmicBattleTaskMessage>(stream);
                        if (linkMicBattleTaskMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(linkMicBattleTaskMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling LinkmicBattleTaskMessage!");
                        RunEvent(OnLinkmicBattleTask, new LinkmicBattleTask(linkMicBattleTaskMessage));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.OecLiveShoppingMessage):
                        Models.Protobuf.UnknownObjects.OecLiveShoppingMessage oecLiveShoppingMessage = Deserialize<Models.Protobuf.UnknownObjects.OecLiveShoppingMessage>(stream);
                        if (oecLiveShoppingMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(oecLiveShoppingMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling OecLiveShoppingMessage!");
                        RunEvent(OnOecLiveShopping, new OecLiveShopping(oecLiveShoppingMessage));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.QuestionNewMessage):
                        Models.Protobuf.UnknownObjects.QuestionNewMessage questionNewMessage = Deserialize<Models.Protobuf.UnknownObjects.QuestionNewMessage>(stream);
                        if (questionNewMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(questionNewMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling QuestionNewMessage!");
                        RunEvent(OnQuestionNew, new QuestionNew(questionNewMessage));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.RoomPinMessage):
                        Models.Protobuf.UnknownObjects.RoomPinMessage roomPinMessage = Deserialize<Models.Protobuf.UnknownObjects.RoomPinMessage>(stream);
                        if (roomPinMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(roomPinMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling RoomPinMessage!");
                        RunEvent(OnRoomPinMessage, new RoomPinMessage(roomPinMessage));
                        break;
                    case nameof(Models.Protobuf.UnknownObjects.SystemMessage):
                        Models.Protobuf.UnknownObjects.SystemMessage systemMessage = Deserialize<Models.Protobuf.UnknownObjects.SystemMessage>(stream);
                        if (systemMessage == null)
                            return;
                        if (settings.CheckForUnparsedData)
                            CheckForUnparsedData(systemMessage, message.Payload);
                        if (ShouldLog(LogLevel.MessageHandling))
                            Debug.Log("Handling SystemMessage!");
                        RunEvent(OnSystemMessage, new SystemMessage(systemMessage));
                        break;
                    #endregion
                    default:
                        if (ShouldLog(LogLevel.Warning))
                        {
                            Debug.LogWarning($"Unknown MessageType: {message.Method}");
                            Debug.LogWarning(Convert.ToBase64String(message.Payload));
                        }
                        // Run Unhandled
                        RunEvent(OnUnhandled, message);
                        return;
                }
        }

        /// <summary>
        /// Handles a ControlMessage
        /// </summary>
        /// <param name="messageEvent">ControlMessage to Handle</param>
        private void HandleControlMessage(Models.Protobuf.Messages.ControlMessage messageEvent, byte[] message)
        {
            if (ShouldLog(LogLevel.MessageHandling))
                Debug.Log("Handling ControlMessage!");
            ControlMessage msg = new ControlMessage(messageEvent);
            RunEvent(OnControlMessage, msg);
            switch (msg.Action)
            {
                case Events.Enums.ControlAction.Stream_Paused:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Stream Paused!");
                    RunEvent(OnLivePaused, msg);
                    return;
                case Events.Enums.ControlAction.Stream_Unpaused:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Stream Unpaused!");
                    RunEvent(OnLiveResumed, msg);
                    return;
                case Events.Enums.ControlAction.Stream_Ended:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Handling Stream Ended!");
                    RunEvent(OnLiveEnded, msg);
                    return;
                default:
                case Events.Enums.ControlAction.Unknown:
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log($"Handling Unhandled ControlMessage!{Environment.NewLine}Action: [{messageEvent.Action}]{Environment.NewLine}[{Convert.ToBase64String(message)}]");
                    RunEvent(OnUnhandledControlMessage, messageEvent);
                    return;
            }
        }

        /// <summary>
        /// Handles a GiftMessage
        /// </summary>
        /// <param name="message">GiftMessage to Handle</param>
        private void HandleGiftMessage(Models.Protobuf.Messages.GiftMessage message)
        {
            GiftId giftId = new GiftId
            {
                Gift = message.GiftId,
                UserName = message.User.DisplayId
            };
            TikTokGift gift = null;
            lock (activeGifts)
            {
                activeGifts.TryGetValue(giftId, out gift);
            }
            if (gift != null)
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Updating Gift[{giftId.Gift}]Amount[{message.ComboCount}]");
                gift.UpdateGiftAmount(message.ComboCount);
            }
            else
            {
                TikTokGift newGift = new TikTokGift(message);
                if (newGift.Gift.IsStreakable)
                {
                    lock (activeGifts)
                        activeGifts.Add(giftId, newGift);
                }
                else
                {
                    newGift.FinishStreak();
                }
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"New Gift[{giftId.Gift}]Amount[{message.ComboCount}]");
                RunEvent(OnGift, newGift);
            }
            if (message.Gift.IsStreakable && message.RepeatEnd == 1)
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"GiftStreak Ended: [{giftId.Gift}] Amount[{message.ComboCount}]");
                lock (activeGifts)
                {
                    activeGifts[giftId].FinishStreak();
                    activeGifts.Remove(giftId);
                }
            }
            if (ShouldLog(LogLevel.MessageHandling))
                Debug.Log("Handling GiftMessage");
            RunEvent(OnGiftMessage, new GiftMessage(message));
        }

        /// <summary>
        /// Handles a WebcastSocialMessage
        /// </summary>
        /// <param name="messageEvent">Message to Handle</param>
        private void HandleSocialMessage(Models.Protobuf.Messages.SocialMessage messageEvent, byte[] message)
        {
            // TODO: Check Action on this specific Share-Message (Multi-Share)
            Match match = Regex.Match(messageEvent.Header.DisplayText.Key, "pm_mt_guidance_viewer_([0-9]+)_share");
            if (match.Success)
            {
                if (int.TryParse(match.Groups[0].Value, out int result))
                {
                    if (ShouldLog(LogLevel.MessageHandling))
                        Debug.Log("Handling Share");
                    RunEvent(OnShare, new Share(messageEvent, result));
                }
                if (ShouldLog(LogLevel.MessageHandling))
                    Debug.Log("Handling SocialMessage");
                RunEvent(OnSocialMessage, new SocialMessage(messageEvent));
                return;
            }
            switch (messageEvent.Action)
            {
                case (long)SocialMessageAction.Follow:
                    if (ShouldLog(LogLevel.MessageHandling))
                        Debug.Log("Handling Follow");
                    RunEvent(OnFollow, new Follow(messageEvent));
                    break;
                case (long)SocialMessageAction.Share:
                    if (ShouldLog(LogLevel.MessageHandling))
                        Debug.Log("Handling Share");
                    RunEvent(OnShare, new Share(messageEvent, 1));
                    break;
                // TODO: Find Action-Values for these Types
            //    case SocialTypes.LikeType:
            //        if (ShouldLog(LogLevel.MessageHandling))
            //            Debug.Log("Handling Like");
            //        Debug.LogError("SocialLike: " + Convert.ToBase64String(message));
            //        RunEvent(OnLike, new Like(messageEvent));
            //        break;
            //    case SocialTypes.JoinType:
            //        if (ShouldLog(LogLevel.MessageHandling))
            //            Debug.Log("Handling Join");
            //        Debug.LogError("SocialJoin: " + Convert.ToBase64String(message));
            //        //       RunEvent(OnJoin, new Join(messageEvent));
            //        break;            
                default:
                case (long)SocialMessageAction.Unknown:
                    // Fallback: Try finding Action through DisplayText
                    switch (messageEvent.Header.DisplayText.Key)
                    {
                        case SocialTypes.FollowType:
                            if (ShouldLog(LogLevel.MessageHandling))
                                Debug.Log("Handling Follow");
                            RunEvent(OnFollow, new Follow(messageEvent));
                            break;
                        case SocialTypes.ShareType:
                            if (ShouldLog(LogLevel.MessageHandling))
                                Debug.Log("Handling Share");
                            RunEvent(OnShare, new Share(messageEvent, 1));
                            break;
                        case SocialTypes.LikeType:
                            if (ShouldLog(LogLevel.MessageHandling))
                                    Debug.Log("Handling Like");
                            RunEvent(OnLike, new Like(messageEvent));
                            break;
                        case SocialTypes.JoinType:
                            if (ShouldLog(LogLevel.MessageHandling))
                                Debug.Log("Handling Join");
                            RunEvent(OnJoin, new Join(messageEvent));
                            break;
                        default:
                            if (ShouldLog(LogLevel.Warning))
                                Debug.LogWarning($"Handling UnhandledSocialMessage!{Environment.NewLine}[{Convert.ToBase64String(message)}]");
                            // Run Unhandled
                            RunEvent(OnUnhandledSocialMessage, messageEvent);
                            break;
                    }
                    break;
            }
            if (ShouldLog(LogLevel.MessageHandling))
                Debug.Log("Handling SocialMessage");
            RunEvent(OnSocialMessage, new SocialMessage(messageEvent));
        }
        
        /// <summary>
        /// Handles a WebcastMemberMessage
        /// </summary>
        /// <param name="msg">Message to Handle</param>
        /// <param name="message">Payload for Message. Printed if ActionType is Unknown</param>
        private void HandleMemberMessage(Models.Protobuf.Messages.MemberMessage msg, byte[] message)
        {
            switch (msg.Action)
            {
                case (long)MemberMessageAction.Joined:
                    if (ShouldLog(LogLevel.MessageHandling))
                        Debug.Log("Handling Join");
                    RunEvent(OnJoin, new Join(msg));
                    break;
                case (long)MemberMessageAction.Subscribed:
                    if (ShouldLog(LogLevel.MessageHandling))
                        Debug.Log("Handling Subscribe");
                    Debug.LogError("MemberSubscribe: " + Convert.ToBase64String(message));
                    RunEvent(OnSubscribe, new Subscribe(msg));
                    break;
//                case 26: // As of yet undetermined
//                case 27: // ?? (Done by host? (User null, User2 == host))
//                case 50: // Share?
                default:
                case (long)MemberMessageAction.Unknown:
                    if (ShouldLog(LogLevel.Warning))
                        Debug.LogWarning($"Handling UnhandledMemberMessage!{Environment.NewLine}[{Convert.ToBase64String(message)}]");
                    RunEvent(OnUnhandledMemberMessage, msg);
                    break;
            }
            if (ShouldLog(LogLevel.MessageHandling))
                Debug.Log("Handling MemberMessage");
            RunEvent(OnMemberMessage, new MemberMessage(msg));
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
                HandleMessageException exc = new HandleMessageException($"Error Deserializing Message. Base64 for Message: [{Convert.ToBase64String(byteData)}]", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                CallOnException(exc); // Inform user of Error
                // This is a recoverable error. Client remains connected. Do not Throw
                return default; // Return null to quietly quit out of this message
            }
            catch (Exception ex)
            {
                byte[] byteData = stream.ToArray();
                HandleMessageException exc = new HandleMessageException($"Error Deserializing Message. Base64 for Message: [{Convert.ToBase64String(byteData)}]", ex);
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
        /// <param name="fullPayload">Full payload for (outer) message</param>
        private void CheckForUnparsedData(AProtoBase msg, byte[] fullPayload)
        {
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log($"Checking {msg.GetType().Name} for Unparsed Data");
            if (msg._extension != null && msg._extension.GetLength() > 0)
            {
                FieldInfo f = msg._extension.GetType().GetField("buffer", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null)
                {
                    byte[] foundMsg = (byte[])f.GetValue(msg._extension);
                    Debug.LogWarning($"Found unparsed Data in Message {msg.GetType()}");
                    Debug.Log($"Unparsed Data: {Environment.NewLine}{Convert.ToBase64String(foundMsg)}");
                    Debug.Log($"Full Payload for Message: {Environment.NewLine}{Convert.ToBase64String(fullPayload)}");
                }
            }
            // Check Child-Objects
            RecursiveCheckForUnparsedData(msg, fullPayload);
        }

        /// <summary>
        /// Checks Properties for Unparsed Data
        /// </summary>
        /// <param name="msg">Message to check Properties on</param>
        /// <param name="fullPayload">Full payload for (outer) message</param>
        private void RecursiveCheckForUnparsedData(AProtoBase msg, byte[] fullPayload)
        {
            PropertyInfo[] properties = msg.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                object obj = properties[i].GetValue(msg);
                if (obj is AProtoBase baseObj)
                    CheckForUnparsedData(baseObj, fullPayload);
            }
        }
        #endregion
        #endregion
        #endregion
    }
}