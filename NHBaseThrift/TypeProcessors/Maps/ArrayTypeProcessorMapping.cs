using System;
using System.Collections.Generic;
using Gridsum.NHBaseThrift.Attributes;
using Gridsum.NHBaseThrift.Enums;
using KJFramework.Tracing;

namespace Gridsum.NHBaseThrift.TypeProcessors.Maps
{
    /// <summary>
    ///     �������ʹ�����ӳ����ṩ����صĻ���������
    /// </summary>
    public sealed class ArrayTypeProcessorMapping
    {
        #region Constructor.

        /// <summary>
        ///     ThriftЭ�����ʹ�����ӳ����ṩ����صĻ���������
        /// </summary>
        private ArrayTypeProcessorMapping()
        {
            Initialize();
        }

        #endregion

        #region Members.

        private readonly Dictionary<Type, IThriftTypeProcessor> _processor = new Dictionary<Type, IThriftTypeProcessor>();
        public static readonly ArrayTypeProcessorMapping Instance = new ArrayTypeProcessorMapping();
        public readonly static ThriftPropertyAttribute DefaultAttribute = new ThriftPropertyAttribute(0, PropertyTypes.Struct, false);
        private static readonly ITracing _tracing = TracingManager.GetTracing(typeof(ArrayTypeProcessorMapping));

        #endregion

        #region Methods.

        /// <summary>
        ///     ��ʼ������ϵͳ�ڲ��ṩ��ThriftЭ�����ʹ�����
        /// </summary>
        private void Initialize()
        {
            Regist(new StringArrayThriftTypeProcessor());
            Regist(new ByteArrayThriftTypeProcessor());
        }

        /// <summary>
        ///     ע��һ��ThriftЭ�����ʹ�����
        ///     <para>* ��������͵Ĵ������Ѿ����ڣ�������滻������</para>
        /// </summary>
        /// <param name="processor">ThriftЭ�����ʹ�����</param>
        public void Regist(IThriftTypeProcessor processor)
        {
            if (processor == null) return;
            try
            {
                if (_processor.ContainsKey(processor.SupportedType))
                {
                    _processor[processor.SupportedType] = processor;
                    return;
                }
                _processor.Add(processor.SupportedType, processor);
            }
            catch (Exception ex) { _tracing.Error(ex, null); }
        }

        /// <summary>
        ///     ע��һ������ָ��֧�����͵��������ʹ�����
        /// </summary>
        /// <param name="supportedType">֧�ֵĴ�������</param>
        public void UnRegist(Type supportedType)
        {
            if (supportedType == null) return;
            try {  _processor.Remove(supportedType); }
            catch (Exception ex) { _tracing.Error(ex, null); }
        }

        /// <summary>
        ///     ��ȡһ������ָ��֧�����͵�ThriftЭ�����ʹ�����
        /// </summary>
        /// <param name="supportedType">֧�ֵĴ�������</param>
        /// <returns>�����������ʹ�����</returns>
        public IThriftTypeProcessor GetProcessor(Type supportedType)
        {
            if (supportedType == null) return null;
            try
            {
                IThriftTypeProcessor result;
                return _processor.TryGetValue(supportedType, out result) ? result : null;
            }
            catch (Exception ex)
            {
                _tracing.Error(ex, null); 
                return null;
            }
        }

        #endregion
    }
}