using System.Text;
using Gridsum.NHBaseThrift.Analyzing;
using Gridsum.NHBaseThrift.Attributes;
using Gridsum.NHBaseThrift.Enums;
using Gridsum.NHBaseThrift.Helpers;
using Gridsum.NHBaseThrift.Network;
using Gridsum.NHBaseThrift.Objects;
using Gridsum.NHBaseThrift.Proxies;

namespace Gridsum.NHBaseThrift.TypeProcessors
{
    /// <summary>
    ///     Thrift������ϢΨһ��ʶ���ʹ��������ṩ����صĻ�������
    /// </summary>
    public class MessageIdentityTypeProcessor : ThriftTypeProcessor
    {
        #region Constructor.

        /// <summary>
        ///     String����ThriftЭ���ֶδ��������ṩ����صĻ�������
        /// </summary>
        public MessageIdentityTypeProcessor()
        {
            _supportedType = typeof(MessageIdentity);
            _expectedDataSize = -1;
        }

        #endregion

        #region Overrides of IntellectTypeProcessor

        /// <summary>
        ///     �ӵ������ͻ�����ת��ΪԪ����
        /// </summary>
        /// <param name="proxy">�ڴ�Ƭ�δ�����</param>
        /// <param name="attribute">�ֶ�����</param>
        /// <param name="analyseResult">�������</param>
        /// <param name="target">Ŀ�����ʵ��</param>
        /// <param name="isArrayElement">��ǰд���ֵ�Ƿ�Ϊ����Ԫ�ر�ʾ</param>
        public override void Process(IMemorySegmentProxy proxy, ThriftPropertyAttribute attribute, ToBytesAnalyseResult analyseResult, object target, bool isArrayElement = false, bool isNullable = false)
        {
            MessageIdentity value = analyseResult.GetValue<MessageIdentity>(target);
            byte[] data = Encoding.UTF8.GetBytes(value.Command);
            proxy.WriteInt32(value.Version.ToBigEndian());
            proxy.WriteInt32(((int)value.CommandLength).ToBigEndian());
            proxy.WriteMemory(data, 0, (uint)data.Length);
            proxy.WriteInt32(((int)value.SequenceId).ToBigEndian());
        }

        /// <summary>
        ///     ��Ԫ����ת��Ϊ�������ͻ�����
        /// </summary>
        /// <param name="instance">Ŀ�����</param>
        /// <param name="result">�������</param>
        /// <param name="container">������������</param>
        public override GetObjectResultTypes Process(object instance, GetObjectAnalyseResult result, INetworkDataContainer container)
        {
            MessageIdentity identity;
            if(!container.TryReadMessageIdentity(out identity)) return GetObjectResultTypes.NotEnoughData;
            result.SetValue(instance, identity);
            return GetObjectResultTypes.Succeed;
        }

        #endregion
    }
}