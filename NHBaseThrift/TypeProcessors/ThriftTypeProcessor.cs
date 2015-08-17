using System;
using NHBaseThrift.Analyzing;
using NHBaseThrift.Attributes;
using NHBaseThrift.Enums;
using NHBaseThrift.Network;
using NHBaseThrift.Proxies;

namespace NHBaseThrift.TypeProcessors
{
    /// <summary>
    ///     ThriftЭ�����ʹ����������࣬�ṩ����صĻ���������
    /// </summary>
    public abstract class ThriftTypeProcessor : IThriftTypeProcessor
    {
        #region Constructor.

        /// <summary>
        ///     ThriftЭ�����ʹ����������࣬�ṩ����صĻ���������
        /// </summary>
        protected ThriftTypeProcessor()
        {
            //support this act by default.
            _supportUnmanagement = true;
        }

        #endregion

        #region Members.

        protected Type _supportedType;
        protected int _expectedDataSize;
        protected bool _supportUnmanagement;

        /// <summary>
        ///     ��ȡһ��ֵ����ֵ��ʾ�˵�ǰ�������Ƿ�֧���Է��йܵķ�ʽ����ִ��
        /// </summary>
        public bool SupportUnmanagement
        {
            get { return _supportUnmanagement; }
        }

        /// <summary>
        ///     ��ȡ֧�ֵ�����
        /// </summary>
        public Type SupportedType
        {
            get { return _supportedType; }
        }

        /// <summary>
        ///    ��ȡһ��ֵ����ֵ��ʾ�˵���ԭʼbyte���������Ϊ������ʱ�������������С�������ݳ���
        ///    <para>* ����Ƕ�̬�������� -1����</para>
        /// </summary>
        public int ExpectedDataSize
        {
            get { return _expectedDataSize; }
        }

        #endregion

        #region Methods.

        /// <summary>
        ///     �ӵ������ͻ�����ת��ΪԪ����
        /// </summary>
        /// <param name="proxy">�ڴ�Ƭ�δ�����</param>
        /// <param name="attribute">�ֶ�����</param>
        /// <param name="analyseResult">�������</param>
        /// <param name="target">Ŀ�����ʵ��</param>
        /// <param name="isArrayElement">��ǰд���ֵ�Ƿ�Ϊ����Ԫ�ر�ʾ</param>
        /// <param name="isNullable">�Ƿ�Ϊ�ɿ��ֶα�ʾ</param>
        public abstract void Process(IMemorySegmentProxy proxy, ThriftPropertyAttribute attribute, ToBytesAnalyseResult analyseResult, object target, bool isArrayElement = false, bool isNullable = false);
        /// <summary>
        ///     ��Ԫ����ת��Ϊ�������ͻ�����
        /// </summary>
        /// <param name="instance">Ŀ�����</param>
        /// <param name="result">�������</param>
        /// <param name="container">������������</param>
        public abstract GetObjectResultTypes Process(object instance, GetObjectAnalyseResult result, INetworkDataContainer container);
        /// <summary>
        ///    ���Լ��һ�µ�ǰ����Ҫ���������ݿ��ó����Ƿ���������͵Ľ�������
        ///    <para>* �˷���ֻ�е�ExpectedDataSize = -1ʱ�Żᱻ����</para>
        /// </summary>
        /// <param name="container">������������</param>
        /// <returns></returns>
        public virtual bool HasEnoughData(INetworkDataContainer container)
        {
            return true;
        }

        #endregion
    }
}