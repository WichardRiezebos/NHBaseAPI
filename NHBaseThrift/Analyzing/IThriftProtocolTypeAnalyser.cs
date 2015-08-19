namespace Gridsum.NHBaseThrift.Analyzing
{
    /// <summary>
    ///     ThriftЭ�����ͷ�����Ԫ�ӿڣ��ṩ����صĻ���������
    /// </summary>
    internal interface IThriftProtocolTypeAnalyser<out T, in K>
    {
        #region Methods.

        /// <summary>
        ///     ����һ�������е�����Thrift��Ա����
        /// </summary>
        /// <param name="type">Ҫ����������</param>
        /// <returns>���ط����Ľ��</returns>
        T Analyse(K type);
        /// <summary>
        ///     ��յ�ǰ���еķ������
        /// </summary>
        void Clear();

        #endregion
    }
}