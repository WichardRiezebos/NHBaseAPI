using System;
using System.Text;
using Gridsum.NHBaseThrift.Exceptions;
using Gridsum.NHBaseThrift.Helpers;
using Gridsum.NHBaseThrift.Objects;
using KJFramework.Net.Channels.Events;
using KJFramework.Net.Channels.Objects;

namespace Gridsum.NHBaseThrift.Network
{
    /// <summary>
    ///    ��·��������
    /// </summary>
    internal sealed class NetworkDataContainer : INetworkDataContainer
    {
        #region Members

        private int _curUsedCount;
        private int _lastReCalculatedUsingCount;
        private ThriftSegmentNode _tail;
        private ThriftSegmentNode _head;
        private ThriftSegmentNode _curUsedHead;
        private ThriftSegmentNode _curGiveBackUsedHead;

        #endregion

        #region Methods.

        /// <summary>
        ///    ׷��һ�θոս��յ�������������ݵ���������
        /// </summary>
        /// <param name="args">���յ�����������</param>
        public void AppendNetworkData(SegmentReceiveEventArgs args)
        {
            if (_head == null) _head = _tail = _curUsedHead = _curGiveBackUsedHead = new ThriftSegmentNode(args);
            else
            {
                ThriftSegmentNode node = new ThriftSegmentNode(args);
                _tail.Next = node;
                node.Previous = _tail;
                _tail = (ThriftSegmentNode) _tail.Next;
            }
        }

        /// <summary>
        ///    ���Լ�鵱ǰʣ��Ŀ������ݳ����Ƿ�����ָ�����ݳ��ȵ�����
        /// </summary>
        /// <param name="length">��Ҫ�������ݳ���</param>
        /// <returns>�������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        public bool CheckEnoughSize(int length)
        {
            NetworkDataCheckResult result;
            return InnerCheckEnoughSize(length, out result);
        }

        /// <summary>
        ///    ���Լ�鵱ǰʣ��Ŀ������ݳ����Ƿ�����ָ�����ݳ��ȵ�����
        /// </summary>
        /// <param name="length">��Ҫ�������ݳ���</param>
        /// <param name="result">�������ݼ����</param>
        /// <returns>�������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        private bool InnerCheckEnoughSize(int length, out NetworkDataCheckResult result)
        {
            result.SegmentCount = 0;
            if (_curUsedHead == null) return false;
            int avaSize = _curUsedHead.RemainingSize - _curUsedCount;
            if (avaSize >= length)
            {
                result.SegmentCount = 1;
                return true;
            }
            result.SegmentCount++;
            SegmentNode usedNode = _curUsedHead;
            SegmentNode nextNode = usedNode.Next;
            do
            {
                if (nextNode == null) return false;
                avaSize += nextNode.RemainingSize;
                result.SegmentCount++;
                if (avaSize >= length) return true;
            } while ((nextNode = nextNode.Next) != null);
            return false;
        }

        /// <summary>
        ///    �����ڲ����ݵ���ʼ����ƫ��
        /// </summary>
        public void ResetOffset()
        {
            _curUsedHead = _curGiveBackUsedHead = _head;
            _curUsedCount = _lastReCalculatedUsingCount;
        }

        /// <summary>
        ///    ���Զ�ȡһ���ֽڵ�����
        /// </summary>
        /// <param name="data">�������True, ������ֶ�Я���˶�ȡ�ɹ�������</param>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        /// <exception cref="IncorrectCalculationException">�ڲ�����Ӧ����ֹҵ�����</exception>
        public bool TryReadByte(out byte data)
        {
            data = 0xFF;
            NetworkDataCheckResult chkResult;
            if (!InnerCheckEnoughSize(1, out chkResult)) return false;
            if (chkResult.SegmentCount != 1) throw new IncorrectCalculationException("#Incorrectly calculated internal network data offset.");
            SegmentNode node = _curUsedHead;
            data = node.Args.GetStub().Segment.Segment.Array[node.Args.GetStub().Segment.UsedOffset + _curUsedCount++];
            ReCalculateCurrentUsedSegment();
            return true;
        }

        /// <summary>
        ///    ���Զ�ȡһ��Int16���͵�����
        /// </summary>
        /// <param name="data">�������True, ������ֶ�Я���˶�ȡ�ɹ�������</param>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        public unsafe bool TryReadInt16(out short data)
        {
            const int expectedDataLength = 2;
            data = 0xFF;
            NetworkDataCheckResult chkResult;
            if (!InnerCheckEnoughSize(expectedDataLength, out chkResult)) return false;
            if (chkResult.SegmentCount == 1)
            {
                SegmentNode node = _curUsedHead;
                fixed (byte* pData = node.Args.GetStub().Segment.Segment.Array)
                {
                    data = *(short*)(pData + node.Args.GetStub().Segment.UsedOffset + _curUsedCount);
                    _curUsedCount += expectedDataLength;
                    ReCalculateCurrentUsedSegment();
                    return true;
                }
            }
            /* Specially optimized for value type.
             * What we expected is DO NOT to generates a new byte[] for undertaking those of 2 bytes data.
             *                                   
             *        remaining only 1 byte data   next available data start here
             *                                          ��   ��
             *      ********************z   z********************
             *                 Segment(1)                          Segment(2) 
             */
            byte* tmpData = stackalloc byte[expectedDataLength];
            FillCrossSegmentData(tmpData, expectedDataLength);
            data = *(short*)tmpData;
            return true;
        }

        /// <summary>
        ///    ���Զ�ȡһ��Int32���͵�����
        /// </summary>
        /// <param name="data">�������True, ������ֶ�Я���˶�ȡ�ɹ�������</param>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        public unsafe bool TryReadInt32(out int data)
        {
            const int expectedDataLength = 4;
            data = 0xFF;
            NetworkDataCheckResult chkResult;
            if (!InnerCheckEnoughSize(expectedDataLength, out chkResult)) return false;
            if (chkResult.SegmentCount == 1)
            {
                SegmentNode node = _curUsedHead;
                fixed (byte* pData = node.Args.GetStub().Segment.Segment.Array)
                {
                    data = *(int*)(pData + node.Args.GetStub().Segment.UsedOffset + _curUsedCount);
                    _curUsedCount += expectedDataLength;
                    ReCalculateCurrentUsedSegment();
                    return true;
                }
            }
            /* Specially optimized for value type.
             * What we expected is DO NOT to generates a new byte[] for undertaking those of 2 bytes data.
             *                                   
             *        remaining only 1 byte data   next available data start here
             *                                          ��   ��
             *      ********************z   zzz******************
             *                 Segment(1)                          Segment(2) 
             */
            byte* tmpData = stackalloc byte[expectedDataLength];
            FillCrossSegmentData(tmpData, expectedDataLength);
            data = *(int*)tmpData;
            return true;
        }

        /// <summary>
        ///    ���Զ�ȡһ��Int64���͵�����
        /// </summary>
        /// <param name="data">�������True, ������ֶ�Я���˶�ȡ�ɹ�������</param>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        public unsafe bool TryReadInt64(out long data)
        {
            const int expectedDataLength = 8;
            data = 0xFF;
            NetworkDataCheckResult chkResult;
            if (!InnerCheckEnoughSize(expectedDataLength, out chkResult)) return false;
            if (chkResult.SegmentCount == 1)
            {
                SegmentNode node = _curUsedHead;
                fixed (byte* pData = node.Args.GetStub().Segment.Segment.Array)
                {
                    data = *(long*)(pData + node.Args.GetStub().Segment.UsedOffset + _curUsedCount);
                    _curUsedCount += expectedDataLength;
                    ReCalculateCurrentUsedSegment();
                    return true;
                }
            }
            /* Specially optimized for value type.
             * What we expected is DO NOT to generates a new byte[] for undertaking those of 2 bytes data.
             *                                   
             *        remaining only 1 byte data   next available data start here
             *                                          ��   ��
             *      ********************z   zzzzzzz**************
             *                 Segment(1)                          Segment(2) 
             */
            byte* tmpData = stackalloc byte[expectedDataLength];
            FillCrossSegmentData(tmpData, expectedDataLength);
            data = *(long*)tmpData;
            return true;
        }

        /// <summary>
        ///    ���Զ�ȡһ���ֽ��������͵�����
        /// </summary>
        /// <param name="length">��Ҫ��ȡ�����ݳ���</param>
        /// <param name="data">�������True, ������ֶ�Я���˶�ȡ�ɹ�������</param>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        public bool TryReadBinaryData(int length, out byte[] data)
        {
            int expectedDataLength = length;
            data = null;
            NetworkDataCheckResult chkResult;
            if (!InnerCheckEnoughSize(expectedDataLength, out chkResult)) return false;
            if (chkResult.SegmentCount == 1)
            {
                SegmentNode node = _curUsedHead;
                data = new byte[length];
                Buffer.BlockCopy(node.Args.GetStub().Segment.Segment.Array, (node.Args.GetStub().Segment.UsedOffset + _curUsedCount), data, 0, length);
                _curUsedCount += expectedDataLength;
                ReCalculateCurrentUsedSegment();
                return true;
            }
            /* Specially optimized for value type.
             * What we expected is DO NOT to generates a new byte[] for undertaking those of 2 bytes data.
             *                                   
             *        remaining only 1 byte data   next available data start here
             *                                          ��   ��
             *      ********************z   zzzzzzzzzz***********
             *                 Segment(1)                          Segment(2) 
             */
            int offset = 0;
            int remainingDataLength = expectedDataLength;
            data = new byte[length];
            for (int i = 0; i < chkResult.SegmentCount; i++)
            {
                int segmentRemainingSize = _curUsedHead.Args.BytesTransferred - _curUsedCount;
                int remainingSize = (segmentRemainingSize > remainingDataLength ? remainingDataLength : segmentRemainingSize);
                Buffer.BlockCopy(_curUsedHead.Args.GetStub().Segment.Segment.Array, (_curUsedHead.Args.GetStub().Segment.UsedOffset + _curUsedCount), data, offset, remainingSize);
                _curUsedCount += remainingSize;
                offset += remainingSize;
                remainingDataLength -= remainingSize;
                ReCalculateCurrentUsedSegment();
            }
            return true;
        }

        /// <summary>
        ///    ���Զ�ȡһ���ֽ��������͵�����
        /// </summary>
        /// <param name="encoding">�����ַ�����ʹ�õı��뼯</param>
        /// <param name="length">��Ҫ��ȡ�����ݳ���</param>
        /// <param name="data">�������True, ������ֶ�Я���˶�ȡ�ɹ�������</param>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        /// <exception cref="ArgumentNullException">��������Ϊ��</exception>
        /// <exception cref="IncorrectCalculationException">�ڲ�����Ӧ����ֹҵ�����</exception>
        public bool TryReadString(Encoding encoding, int length, out string data)
        {
            if(encoding == null) throw new ArgumentNullException("encoding");
            int expectedDataLength = length;
            data = null;
            NetworkDataCheckResult chkResult;
            if (!InnerCheckEnoughSize(expectedDataLength, out chkResult)) return false;
            if (chkResult.SegmentCount == 1)
            {
                SegmentNode node = _curUsedHead;
                data = encoding.GetString(node.Args.GetStub().Segment.Segment.Array, (node.Args.GetStub().Segment.UsedOffset + _curUsedCount), expectedDataLength);
                _curUsedCount += expectedDataLength;
                ReCalculateCurrentUsedSegment();
                return true;
            }
            byte[] rawData;
            if (!TryReadBinaryData(length, out rawData)) throw new IncorrectCalculationException("#Incorrectly calculated internal network data offset.");
            data = encoding.GetString(rawData, 0, rawData.Length);
            return true;
        }

        /// <summary>
        ///    ���Զ�ȡһ��������Thrift��Ϣͷ���͵�����
        /// </summary>
        /// <returns>����һ��ֵ����ֵ��ʾ�˵�ǰ�Ƿ��ȡ�ɹ����������False, ��֤���ڲ���û���㹻�Ŀ��������Թ���ȡ</returns>
        public bool TryReadMessageIdentity(out MessageIdentity identity)
        {
            identity = new MessageIdentity();
            int version, commandLength, seqId;
            string command;
            if (!TryReadInt32(out version)) return false;
            if (!TryReadInt32(out commandLength)) return false;
            if (!TryReadString(Encoding.UTF8, (commandLength = commandLength.ToLittleEndian()), out command)) return false;
            if (!TryReadInt32(out seqId)) return false;
            identity.Version = version.ToLittleEndian();
            identity.CommandLength = (uint) commandLength;
            identity.Command = command;
            identity.SequenceId = (uint) seqId.ToLittleEndian();
            return true;
        }

        /// <summary>
        ///    ����ڲ��������ݵ�ƫ��
        /// </summary>
        public void UpdateOffset()
        {
            if (_curUsedHead != null) ReCalculateCurrentUsedSegment();
            if (_curUsedCount != 0) _lastReCalculatedUsingCount = _curUsedCount;
            ThriftSegmentNode node = (ThriftSegmentNode)(_curUsedHead == null ? _curGiveBackUsedHead : _curUsedHead.Previous);
            while (node != null)
            {
                node.Args.Complete();
                node = (ThriftSegmentNode) node.Previous;
            }
            _curGiveBackUsedHead = _curUsedHead;
            _head = _curUsedHead;
            if (_head != null) _head.Previous = null;
        }


        private unsafe void FillCrossSegmentData(byte* pData, int length)
        {
            for (int i = 0; i < length; i++)
            {
                *(pData + i) = _curUsedHead.Args.GetStub().Segment.Segment.Array[_curUsedHead.Args.GetStub().Segment.UsedOffset + _curUsedCount++];
                ReCalculateCurrentUsedSegment();
            }
        }

        private void ReCalculateCurrentUsedSegment()
        {
            if (_curUsedHead.Args.BytesTransferred - _curUsedCount <= 0)
            {
                if (_curUsedHead.Next != null) _curGiveBackUsedHead = (ThriftSegmentNode) _curGiveBackUsedHead.Next;
                _curUsedHead = (ThriftSegmentNode) _curUsedHead.Next;
                _curUsedCount = 0;
                _lastReCalculatedUsingCount = 0;
            }
        }

        /// <summary>
        ///     DUMP����ǰNetworkContainer�ڲ�����������������
        /// </summary>
        /// <returns>���ذ���������</returns>
        public string Dump()
        {
            if (_head == null) return string.Empty;
            if (!Global.AllowedPrintDumpInfo) return string.Empty;
            ThriftSegmentNode node = _head;
            StringBuilder s = new StringBuilder();
            string spc = " ";
            string nxtSpace = "  ";
            while (node != null)
            {
                byte[] data = new byte[node.RemainingSize];
                Buffer.BlockCopy(node.Args.GetStub().Segment.Segment.Array, node.Args.GetStub().Segment.UsedOffset, data,
                    0, data.Length);
                byte[] array = data;
                s.AppendLine(string.Format("{0}Data: ", spc)).Append(spc).AppendLine("{");
                int round = array.Length/8 + (array.Length%8 > 0 ? 1 : 0);
                int currentOffset, remainningLen;
                for (int j = 0; j < round; j++)
                {
                    currentOffset = j*8;
                    remainningLen = ((array.Length - currentOffset) >= 8 ? 8 : (array.Length - currentOffset));
                    StringBuilder rawByteBuilder = new StringBuilder();
                    rawByteBuilder.Append(nxtSpace);
                    for (int k = 0; k < remainningLen; k++)
                    {
                        rawByteBuilder.AppendFormat("0x{0}", array[currentOffset + k].ToString("X2"));
                        if (k != remainningLen - 1) rawByteBuilder.Append(", ");
                    }
                    rawByteBuilder.Append(new string(' ',
                        (remainningLen == 8 ? 5 : (8 - remainningLen)*4 + (((8 - remainningLen) - 1)*2) + 7)));
                    for (int k = 0; k < remainningLen; k++)
                    {
                        if ((char) array[currentOffset + k] > 126 || (char) array[currentOffset + k] < 32)
                            rawByteBuilder.Append('.');
                        else rawByteBuilder.Append((char) array[currentOffset + k]);
                    }
                    s.AppendLine(string.Format("{0}{1}", nxtSpace, rawByteBuilder));
                }
                node = (ThriftSegmentNode) node.Next;
            }
            s.Append(spc).AppendLine("}");
            return s.ToString();
        }

        /// <summary>
        /// ִ�����ͷŻ����÷��й���Դ��ص�Ӧ�ó����������
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_head == null) return;
            ThriftSegmentNode node = _head;
            while (node != null)
            {
                node.Args.Complete();
                node = (ThriftSegmentNode)node.Next;
            }
            _head = null;
            _tail = null;
            _curUsedHead = null;
            _curGiveBackUsedHead = null;
        }

        #endregion
    }
}