using System.Text;
using Gridsum.NHBaseThrift.Analyzing;
using Gridsum.NHBaseThrift.Attributes;
using Gridsum.NHBaseThrift.Enums;
using Gridsum.NHBaseThrift.Helpers;
using Gridsum.NHBaseThrift.Network;
using Gridsum.NHBaseThrift.Proxies;

namespace Gridsum.NHBaseThrift.TypeProcessors
{
    /// <summary>
    ///     String����ThriftЭ���ֶδ��������ṩ����صĻ�������
    /// </summary>
    public class StringThriftTypeProcessor : ThriftTypeProcessor
    {
        #region Constructor.

        /// <summary>
        ///     String����ThriftЭ���ֶδ��������ṩ����صĻ�������
        /// </summary>
        public StringThriftTypeProcessor()
        {
            _supportedType = typeof(string);
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
            string value = analyseResult.GetValue<string>(target);
            proxy.WriteSByte((sbyte)attribute.PropertyType);
            proxy.WriteInt16(((short)attribute.Id).ToBigEndian());
            byte[] data = Encoding.UTF8.GetBytes(value);
            proxy.WriteInt32(data.Length.ToBigEndian());
            proxy.WriteMemory(data, 0, (uint) data.Length);
        }

        /// <summary>
        ///     ��Ԫ����ת��Ϊ�������ͻ�����
        /// </summary>
        /// <param name="instance">Ŀ�����</param>
        /// <param name="result">�������</param>
        /// <param name="container">������������</param>
        public override GetObjectResultTypes Process(object instance, GetObjectAnalyseResult result, INetworkDataContainer container)
        {
            int value;
            if(!container.TryReadInt32(out value)) return GetObjectResultTypes.NotEnoughData;
            value = value.ToLittleEndian();
            string content;
            if (!container.TryReadString(Encoding.UTF8, value, out content)) return GetObjectResultTypes.NotEnoughData;
            result.SetValue(instance, content);
            return GetObjectResultTypes.Succeed;
        }

        #endregion
    }
}