using System;
using System.Net;
using System.Threading;
using KJFramework.EventArgs;
using KJFramework.Net.Channels;
using KJFramework.Net.ProtocolStacks;
using KJFramework.Net.Transaction;
using KJFramework.Net.Transaction.Helpers;
using KJFramework.Net.Transaction.Identities;
using KJFramework.Tracing;
using NHBaseThrift.Messages;
using NHBaseThrift.Network.Transactions;

namespace NHBaseThrift.Network.Agents
{
    /// <summary>
    ///     �ͻ������Ӵ��������ṩ����صĻ�������
    /// </summary>
    public class ThriftConnectionAgent : IThriftConnectionAgent
    {
        #region Constructor

        /// <summary>
        ///     �ͻ������Ӵ��������ṩ����صĻ�������
        /// </summary>
        /// <param name="channel">�ͻ�������</param>
        /// <param name="transactionManager">���������</param>
        public ThriftConnectionAgent(IMessageTransportChannel<ThriftMessage> channel, ThriftMessageTransactionManager transactionManager)
        {
            if (channel == null) throw new ArgumentNullException("channel");
            if (!channel.IsConnected) throw new ArgumentException("Cannot wrap this msg channel, because current msg channel has been disconnected!");
            if (transactionManager == null) throw new ArgumentNullException("transactionManager");
            _sequenceId = 0;
            _channel = channel;
            _transactionManager = transactionManager;
            _channel.Disconnected += ChannelDisconnected;
            _channel.ReceivedMessage += ChannelReceivedMessage;
        }

        #endregion

        #region Members

        private int _sequenceId;
        private bool _isInitiativeDisconnect;
        private IMessageTransportChannel<ThriftMessage> _channel;
        protected static readonly ITracing _tracing = TracingManager.GetTracing(typeof (ThriftConnectionAgent));

        #endregion

        #region Events

        //inner msg channel disconect event.
        void ChannelDisconnected(object sender, EventArgs e)
        {
            _channel.Disconnected -= ChannelDisconnected;
            _channel.ReceivedMessage -= ChannelReceivedMessage;
            if (_isInitiativeDisconnect) return;
            DisconnectedHandler(null);
        }

        //new message arrived.
        void ChannelReceivedMessage(object sender, LightSingleArgEventArgs<System.Collections.Generic.List<ThriftMessage>> e)
        {
            IMessageTransportChannel<ThriftMessage> msgChannel = (IMessageTransportChannel<ThriftMessage>)sender;
            foreach (ThriftMessage message in e.Target)
            {
                if (message == null) continue;
                _tracing.Info("L: {0}\r\nR: {1}\r\n{2}", msgChannel.LocalEndPoint, msgChannel.RemoteEndPoint, message.ToString());
                //rebuilds corresponding TransactionIdentity for current RSP message.
                TransactionIdentity identity = IdentityHelper.Create((IPEndPoint) msgChannel.LocalEndPoint, (int) message.Identity.SequenceId, false);
                _transactionManager.Active(identity, message);
            }
        }

        #endregion

        #region Implementation of IConnectionAgent

        private object _tag;
        private readonly ThriftMessageTransactionManager _transactionManager;

        /// <summary>
        ///     ��ȡ�����ø�������
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        ///     ��ȡ���������
        /// </summary>
        public ThriftMessageTransactionManager TransactionManager
        {
            get { return _transactionManager; }
        }

        /// <summary>
        ///     �����ر����Ӵ�����
        ///     * �����رյ���Ϊ����ر��ڲ���ͨ���ŵ�����
        /// </summary>
        public void Close()
        {
            _isInitiativeDisconnect = true;
            if(_channel != null && _channel.IsConnected)
            {
                _channel.Disconnect();
                DisconnectedHandler(null);
            }
        }

        /// <summary>
        ///     ��ȡ�ڲ���ͨ���ŵ�
        ///     * һ�㲻����ʹ�ô˷���ֱ�Ӳ����ڲ���ͨ���ŵ�
        /// </summary>
        /// <returns>����ͨ���ŵ�</returns>
        public IMessageTransportChannel<ThriftMessage> GetChannel()
        {
            return _channel;
        }

        /// <summary>
        ///     ����һ���µ�����
        /// </summary>
        /// <returns>����һ����Կͻ��˵�������</returns>
        public ThriftMessageTransaction CreateTransaction()
        {
            return TransactionManager.Create(Interlocked.Increment(ref _sequenceId), GetChannel());
        }

        /// <summary>
        ///     �Ͽ��¼�
        /// </summary>
        public event EventHandler Disconnected;
        private void DisconnectedHandler(EventArgs e)
        {
            EventHandler handler = Disconnected;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     �µ����ﴴ��������ʱ������¼�
        /// </summary>
        public event EventHandler<LightSingleArgEventArgs<IMessageTransaction<ThriftMessage>>> NewTransaction;
        private void NewTransactionHandler(LightSingleArgEventArgs<IMessageTransaction<ThriftMessage>> e)
        {
            EventHandler<LightSingleArgEventArgs<IMessageTransaction<ThriftMessage>>> handler = NewTransaction;
            if (handler != null) handler(this, e);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     ����һ���µ����Ӵ�����
        /// </summary>
        /// <param name="iep">Զ���ս���ַ</param>
        /// <param name="protocolStack">Э��ջ</param>
        /// <param name="transactionManager">���������</param>
        /// <returns>����޷����ӵ�Զ�̵�ַ���򷵻�null.</returns>
        public static IThriftConnectionAgent Create(IPEndPoint iep, IProtocolStack<ThriftMessage> protocolStack, ThriftMessageTransactionManager transactionManager)
        {
            if (iep == null) throw new ArgumentNullException("iep");
            if (protocolStack == null) throw new ArgumentNullException("protocolStack");
            if (transactionManager == null) throw new ArgumentNullException("transactionManager");
            ITransportChannel transportChannel = new TcpTransportChannel(iep);
            transportChannel.Connect();
            if (!transportChannel.IsConnected) return null;
            IMessageTransportChannel<ThriftMessage> msgChannel = new MessageTransportChannel<ThriftMessage>((IRawTransportChannel)transportChannel, protocolStack, new ThriftProtocolSegmentDataParser((ThriftProtocolStack) protocolStack));
            return new ThriftConnectionAgent(msgChannel, transactionManager);
        }

        #endregion
    }
}