﻿/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Rtp
{
    /// <summary>
    /// A collection of RtpPackets
    /// </summary>
    public class RtpFrame : Media.Common.BaseDisposable, IEnumerable<RtpPacket>// IDictionary, IList, etc? IClonable
    {
        //Todo, should be Lifetime Disposable        (Where Lifetime is given by expected duration + connection time by default or 1 Minute)
        //This also will appear in derived types for no reason.
        /// <summary>
        /// Assembles a single packet by skipping any ContributingSourceListOctets and optionally Extensions and a certain profile header. 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="useExtensions"></param>
        /// <param name="profileHeaderSize"></param>
        /// <returns></returns>
        public static Common.MemorySegment AssemblePacket(RtpPacket packet, bool useExtensions = false, int profileHeaderSize = 0)
        {
            //Set to profileHeaderSize
            int localSize = profileHeaderSize;
            if (packet.Extension)
            {
                //Use the extension
                using (RtpExtension extension = packet.GetExtension())
                {
                    //If present and complete
                    if (extension != null && extension.IsComplete)
                    {
                        //If the data should be included then include it
                        if (useExtensions)
                        {
                            localSize += packet.ContributingSourceListOctets + RtpExtension.MinimumSize;
                                    packet.Payload.Offset + localSize,
                                    (packet.Payload.Count - localSize) - packet.PaddingOctets);
                        }
                        localSize += extension.Size;
                    }
                }
            }
            localSize += packet.ContributingSourceListOctets;
                packet.Payload.Offset + localSize,
                (packet.Payload.Count - localSize) - packet.PaddingOctets);
        }
        //readonly ValueType
        /// <summary>
        /// The DateTime in which the instance was created.
        /// </summary>
        public readonly DateTime Created;
        /// The maximum amount of packets which can be contained in a frame
        /// </summary>
        internal protected int MaxPackets = 1024;
        /// Indicates if Add operations will ensure that all packets added have the same <see cref="RtpPacket.PayloadType"/>
        /// </summary>
        internal protected bool AllowsMultiplePayloadTypes { get; set; }
        ///// Indicates if duplicate packets will be allowed to be stored.
        ///// </summary>
        //public bool AllowDuplicatePackets { get; internal protected set; }
        /// Updating the SequenceNumber of a contained packet can still cause unintended results.
        /// </summary>
        internal readonly protected List<RtpPacket> Packets;
        //Also needs a Dictionary to be able to maintain state of remove operations...
        //Could itself be a Dictionary to ensure that a packet is not already present but if packets are added out of order then the buffer would need to be created again...
        /// After a single RtpPacket is <see cref="Depacketize">depacketized</see> it will be placed into this list with the appropriate index.
        /// </summary>
        public readonly SortedList<int, Common.MemorySegment> Depacketized;
        //The problem with that is that children classes may add more than one depacketization per packet, (that data may or may not be from the payload of the packet)
        //To remove all of them there would have to be a way to store packets and their data to all segments.
        // e.g. ConcurrentThesaurus<RtpPacket, Common.MemorySegment> , but it would be impossible to preserve the order of packets in the dictionary without using SortedDictionary and custom comparer for every derived type
        
        //A structure with the Packet and all corresponding Segments could work, this makes removing easy but it required the DecodingOrder to be seperately stored for each packet.
        //This is because there may be multiple access units within a single packet which have multiple decoding order vectors.
            //Au 1, 3, 5
        //Rtp 1
            //Au 0, 2, 4
        internal class PaD : Common.BaseDisposable
        {
            #region Fields
            {
                if (Common.IDisposedExtensions.IsNullOrDisposed(pad)) throw new InvalidOperationException("pad is NullOrDisposed");
            }
                : base(packet.ShouldDispose)
            {
                if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) throw new InvalidOperationException("packet is NullOrDisposed");
            }
            protected override void Dispose(bool disposing)
            {
                if (IsDisposed) return;
                {
                    for (int i = 0, e = Parts.Count; i < e; ++i)
                    {
                        using (Common.MemorySegment ms = Parts[0])
                        {
                            Parts.RemoveAt(0);
                        }
                    }
                }
            }
        }
        //1) A PaD is created with the packet.
        //2) The Parts member is created empty.
        //3) The DecodingOrder number is increased or set from the packet sequenceNumber
        //4) The packet data is depacketized to the Parts member.
        //5) If more data remains in the packet go to Step 1
        // Repeat as necessary for the data in the packet.
        
        //Each packet would be allowed to have many buffers and they could be removed according to the packet which owns them (or the MemorySegment even better in some cases)
        //Could also maybe just use sequenceNumber since it wouldn't change the semantic and would be lighter on memory.
        /// The amount of packets contained which had the Marker bit set.
        /// </summary>
        internal int m_MarkerCount = 0;
        /// Timestamp, SynchronizationSourceIdentifier of all contained packets.
        /// </summary>
        internal int m_Timestamp = -1, m_Ssrc = -1;
        /// The PayloadType of all contained packets, if the value has not been determined than it's default value is -1.
        /// </summary>
        internal int m_PayloadType = -1;
        //Marker index, Offset into Packets
        //0, 1
        //1, 3
        //2, 7
        //3, 9
        //Dictionary<int, int> MarkerPackets
        //public int MarkerCount => MarkerPackets.Count
        /// The Lowest and Highest SequenceNumber in the contained RtpPackets or -1 if no RtpPackets are contained
        /// </summary>
        internal int m_LowestSequenceNumber = -1, m_HighestSequenceNumber = -1;
        /// Useful for depacketization.. might not need a field as buffer can be created on demand from SegmentStream, just need to determine if every call to Buffer should maintain position or not.
        /// </summary>
        internal protected Common.SegmentStream m_Buffer;
        /// Gets the expected PayloadType of all contained packets or -1 if has not <see cref="SpecifiedPayloadType"/>
        /// </summary>
        public int PayloadType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_PayloadType; }
            #region Unused set
            //internal protected set
            //{
            //    //When the value is less than 0 this means clear...
            //    if (value < 0)
            //    {
            //        m_PayloadType = -1;
                    
            //        Clear();
            //    }
            //    foreach (RtpPacket packet in Packets) packet.PayloadType = value; //packet.Header.First16Bits.Last8Bits = (byte)value;
            //    m_PayloadType = (byte)(HasMarker ? value | RFC3550.CommonHeaderBits.RtpMarkerMask : value);
            //}
            #endregion
        }
        /// Indicates the amount of packets stored that have the Marker bit set.
        /// </summary>
        public int MarkerCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MarkerCount; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            protected set { m_MarkerCount = value; }
        }
        public Common.SegmentStream Buffer
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return HasBuffer ? m_Buffer : m_Buffer = new Common.SegmentStream(Depacketized.Values); }
        }
        /// Gets or sets the SynchronizationSourceIdentifier of All Packets Contained or -1 if not assigned.
        /// </summary>
        public int SynchronizationSourceIdentifier
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Ssrc; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected set
            {
                m_Ssrc = value;
            }
        }
        /// Gets or sets the Timestamp of All Packets Contained or -1 if unassigned.
        /// </summary>
        public int Timestamp
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Timestamp; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected set { m_Timestamp = value; }
        }
        /// Gets the packet at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal protected RtpPacket this[int index]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Packets[index]; }
            #region Unused set
            /*private*/
            //set { Packets[index] = value; }
            #endregion
        }
        /// Gets a value indicating if the <see cref="PayloadType"/> was specified.
        /// </summary>
        public bool SpecifiedPayloadType
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_PayloadType >= 0; }
        }
        /// Indicates if there are any packets have been <see cref="Depacketize">depacketized</see>
        /// </summary>
        public bool HasDepacketized
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Depacketized.Count > 0; }
        }
        /// Indicates if the <see cref="Buffer"/> is not null and CanRead.
        /// </summary>
        public bool HasBuffer
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == (m_Buffer == null) && m_Buffer.CanRead; }
        }
        /// Indicates if all contained RtpPacket instances have a Transferred Value otherwise false.
        /// </summary>
        public bool Transferred
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return IsEmpty ? false : Packets.All(p => p.Transferred.HasValue); }
        }
        /// <summary>
        /// Indicates if the RtpFrame <see cref="Disposed"/> is False AND <see cref="IsMissingPackets"/> is False AND <see cref="HasMarker"/> is True.
        /// </summary>
        public virtual bool IsComplete
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return false == IsDisposed && false == IsMissingPackets && HasMarker; }
        }
        //Would be given any arbitrary RtpFrame and would implement that logic there.
        /// If at least 2 packets are contained, Indicates if all contained packets are sequential up the Highest Sequence Number contained in the RtpFrame.
        /// False if less than 2 packets are contained or there is a not a gap in the contained packets sequence numbers.
        /// </summary>
        /// <remarks>This function does not check <see cref="HasMarker"/></remarks>
        public bool IsMissingPackets
        {
            get
            {
                int count = Count;
                {
                    //No packets
                    case 0: return true;
                    //Single packet only
                    case 1: return false;
                    //Skip the range check for 2 packets
                    case 2: return ((short)(m_LowestSequenceNumber - m_HighestSequenceNumber) != -1); //(should be same as 1 + (short)((m_LowestSequenceNumber - m_HighestSequenceNumber)) == 0 but saves an additional addition)
                    //2 or more packets, cache the m_LowestSequenceNumber and check all packets to be sequential starting at offset 1
                    default: RtpPacket p; for (int nextSeq = m_LowestSequenceNumber == ushort.MaxValue ? ushort.MinValue : m_LowestSequenceNumber + 1, i = 1; i < count; ++i)
                        {
                            //Scope the packet
                            p = Packets[i];
                            if (p.SequenceNumber != nextSeq) return true;
                            //if ((short)(p.SequenceNumber - nextSeq) != 0) return true;
                            nextSeq = nextSeq == ushort.MaxValue ? ushort.MinValue : nextSeq + 1; //++nextSeq;
                        }
                        return false;
                }
            }
        }
        /// <summary>
        /// Indicates if a contained packet has the marker bit set. (Usually the last packet in a frame)
        /// </summary>
        public bool HasMarker
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MarkerCount > 0; }
        }
        /// The amount of Packets in the RtpFrame
        /// </summary>
        public int Count
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Packets.Count; }
        }
        /// Indicates if there are packets in the RtpFrame
        /// </summary>
        public bool IsEmpty
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Packets.Count == 0; }
        }
        
        /// <summary>
        /// Gets the 16 bit unsigned value which is associated with the highest sequence number contained or -1 if no RtpPackets are contained.
        /// Usually the packet at the highest offset
        /// </summary>
        public int HighestSequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_HighestSequenceNumber; }
        }
        /// Gets the 16 bit unsigned value which is associated with the lowest sequence number contained or -1 if no RtpPackets are contained.
        /// Usually the packet at the lowest offset
        /// </summary>
        public int LowestSequenceNumber
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_LowestSequenceNumber; }
        }
        /// Creates an instance which has no packets and an undetermined <see cref="PayloadType"/>
        /// </summary>
        /// <param name="shouldDispose">Indicates if the instance will <see cref="Clear"/> when <see cref="Dispose"/> is called.</param>
        public RtpFrame(bool shouldDispose)
            : base(shouldDispose)
        {
            //Indicate when this instance was created
            Created = DateTime.UtcNow;
            Packets = new List<RtpPacket>();
            Depacketized = new SortedList<int, Common.MemorySegment>();
        }
        /// Creates an instance which has no packets and an undetermined <see cref="PayloadType"/> and will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        public RtpFrame() : this(true) { }
        /// Creates an instance which has no packets and and the given <see cref="PayloadType"/> and will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        /// <param name="payloadType"></param>
        public RtpFrame(int payloadType) : this()
        {
            //Should be bound from 0 - 127 inclusive...
            if (payloadType > byte.MaxValue) throw Common.Binary.CreateOverflowException("payloadType", payloadType, byte.MinValue.ToString(), byte.MaxValue.ToString());
            m_PayloadType = (byte)payloadType;
        }
        /// Creates an instance which has no packets and and the given <see cref="PayloadType"/>, <see cref="Timestamp"/> and <see cref="SynchronizationSourceIdentifier"/> and will dispose when <see cref="Dispose"/> is called.
        /// </summary>
        /// <param name="payloadType"></param>
        /// <param name="timeStamp"></param>
        /// <param name="ssrc"></param>
        public RtpFrame(int payloadType, int timeStamp, int ssrc) 
            :this(payloadType)
        {
            //Assign the Synconrization Source Identifier
            m_Ssrc = ssrc;
            m_Timestamp = timeStamp;    
        }
        //The semantics of each instance cannot easily be traced without knowing what to expect from each delegation
        //This requires the use of interfaces to properly 'do'.
        /// Creates an instance and if the packet is not null assigns properties from the given packet and optionally adds the packet to the list of stored packets.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="addPacket"></param>
        /// <param name="shouldDispose"></param>
        public RtpFrame(RtpPacket packet, bool addPacket = true, bool shouldDispose = true)
            :this(shouldDispose)
        {
            if(Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;
            
            m_Timestamp = packet.Timestamp;
        }
        /// Clone and existing RtpFrame
        /// </summary>
        /// <param name="f">The frame to clonse</param>
        /// <param name="referencePackets">Indicate if contained packets should be referenced</param>
        public RtpFrame(RtpFrame f, bool referencePackets = false, bool referenceBuffer = false, bool shouldDispose = true)
            : base(shouldDispose) //If shouldDispose is true when referencePackets is true then Dispose will clear both lists.
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(f)) return;
            
            m_Timestamp = f.m_Timestamp;
            if (referencePackets) Packets = f.Packets; //Assign the list from the packets in te frame (changes to list reflected in both instances)
            else Packets = new List<RtpPacket>(f); //Create the list from the packets in the frame (changes to list not reflected in both instances)
            //If you reference the packets you also reference the buffer...
            if (referenceBuffer)
            {
                //Assign it
                m_Buffer = f.m_Buffer;
                Depacketized = f.Depacketized;
            }
            else
            {
                //Create the list
                Depacketized = new SortedList<int, Common.MemorySegment>();
                
                //Can't create a new one because of the implications
                m_Buffer = f.m_Buffer; 
            }           
            //ShouldDispose = f.ShouldDispose;
        }
        ///// Destructor.
        ///// </summary>
        //~RtpFrame() { Dispose(); } 
        /// <summary>
        /// Gets an enumerator of All Contained Packets at the time of the call
        /// </summary>
        /// <returns>The enumerator of the contained packets</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public IEnumerator<RtpPacket> GetEnumerator() { return Packets.GetEnumerator(); }
        /// If HasDepacketized is true then returns all data already depacketized otherwise all packets are iterated and depacketized and memory is reclaimed afterwards.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Common.MemorySegment> GetDepacketizion(bool inplace = true)
        {
            //If there is already a Depacketizion use the memory in place.
            if (HasDepacketized)
            {
                foreach (Common.MemorySegment ms in Depacketized.Values)
                {
                    yield return ms;
                }
            }
            foreach (RtpPacket packet in Packets)
            {
                Depacketize(packet);
                if (HasDepacketized)
                {
                    //Yeild what was depacketized for this packet.
                    foreach (Common.MemorySegment ms in Depacketized.Values)
                    {
                        yield return ms;
                    }
                    FreeDepacketizedMemory(inplace);
                }
            }
        }
        /// Adds a RtpPacket to the RtpFrame. The first packet added sets the SynchronizationSourceIdentifier and Timestamp if not already set.
        /// </summary>
        /// <param name="packet">The RtpPacket to Add</param>
        /// <param name="allowPacketsAfterMarker">Indicates if the packet shouldbe allowed even if the packet's sequence number is greater than or equal to <see cref="HighestSequenceNumber"/> and <see cref="IsComplete"/> is true.</param>
        public void Add(RtpPacket packet, bool allowPacketsAfterMarker = true, bool allowDuplicates = false)
        {
            //If the packet is disposed of this frame is then do not add.
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet) || IsDisposed) return;
            if (count == 0)
            {
                if (m_Ssrc == -1) m_Ssrc = ssrc;
                else if (ssrc != m_Ssrc) throw new ArgumentException("packet.SynchronizationSourceIdentifier must match frame SynchronizationSourceIdentifier", "packet");
                else if (ts != m_Timestamp) throw new ArgumentException("packet.Timestamp must match frame Timestamp", "packet");
                else if (AllowsMultiplePayloadTypes == false && pt != PayloadType) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");
                if (packet.Marker)
                {
                    m_MarkerCount = 1;
                }
                
                //Should mark as dirty if not dispose.
                //DisposeBuffer();
            }
            //Check payload type if indicated
            if (AllowsMultiplePayloadTypes == false && pt != PayloadType) throw new ArgumentException("packet.PayloadType must match frame PayloadType", "packet");
            if (false == allowDuplicates && (m_LowestSequenceNumber == seq || m_HighestSequenceNumber == seq)) throw new InvalidOperationException("Cannot have duplicate packets in the same frame.");
            bool packetMarker = packet.Marker;
            if (HasMarker)
            {
                //Check if the packet is allowed
                if (false == allowPacketsAfterMarker) throw new InvalidOperationException("Cannot add packets after the marker packet.");
            }
            int insert = 0, tempSeq = 0;
            while (insert < count && (short)(seq - (tempSeq = Packets[insert].SequenceNumber)) >= 0)
            {
                //move the index
                ++insert;
            }
            if (false == allowDuplicates && tempSeq == seq) throw new InvalidOperationException("Cannot have duplicate packets in the same frame.");
            if (insert == 0)
            {
                Packets.Insert(0, packet);
            }
            else if (insert >= count) //Handle add
            {
                Packets.Add(packet);
            }
            else Packets.Insert(insert, packet); //Insert
            if (packetMarker) ++m_MarkerCount;
        }
        /// Calls <see cref="Add"/> and indicates if the operations was a success
        /// </summary>
        public bool TryAdd(RtpPacket packet, bool allowPacketsAfterMarker = true, bool allowDuplicates = false)
        {
            if (IsDisposed) return false;
            catch { return false; }            
        }
        /// <summary>
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="packet">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        //public bool Contains(RtpPacket packet) { return Packets.Contains(packet); }
        /// Indicates if the RtpFrame contains a RtpPacket
        /// </summary>
        /// <param name="sequenceNumber">The RtpPacket to check</param>
        /// <returns>True if the packet is contained, otherwise false.</returns>
        public bool Contains(int sequenceNumber) { return IndexOf(ref sequenceNumber) >= 0; }
        internal bool Contains(ref int sequenceNumber) { return IndexOf(ref sequenceNumber) >= 0; }
        internal protected int IndexOf(int sequenceNumber) { return IndexOf(ref sequenceNumber); }
        /// Indicates if the RtpFrame contains a RtpPacket based on the given sequence number.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number to check</param>
        /// <returns>The index of the packet is contained, otherwise -1.</returns>
        internal protected int IndexOf(ref int sequenceNumber)
        {
            int count = Count;
            {
                case 0: return -1;
                case 1:
                    {
                        return m_HighestSequenceNumber == sequenceNumber ? 0 : -1;
                    }
                case 2:
                    {
                        if (m_LowestSequenceNumber == sequenceNumber) return 0;
                    }
                //Only optimal if sequenceNumber is @ 1 otherwise default cases may be faster, this saves 2 additional range changes
                case 3:
                    {
                        if (Packets[1].SequenceNumber == sequenceNumber) return 1;
                    }
                    //Still really only just saves 2 checks, still not optimals
                //case 4:
                //    {
                //        if (Packets[2].SequenceNumber == sequenceNumber) return 2;
                //    }
                //case 5:
                //    {
                //        if (Packets[3].SequenceNumber == sequenceNumber) return 3;
                //    }
                default:
                    {
                        //Fast path when no roll over occur, e.g. m_Packets[0].SequenceNumber > m_Packets.Last().SequenceNumber
                        //if (m_HighestSequenceNumber > m_LowestSequenceNumber && (sequenceNumber <= m_HighestSequenceNumber && sequenceNumber >= m_LowestSequenceNumber)) return true;
                        if (sequenceNumber == m_LowestSequenceNumber) return 0;
                        if (sequenceNumber == m_HighestSequenceNumber) return count - 1;
                        //Loop from 1 to count - 1 since they were checked above
                        for (int i = Common.Binary.Max(1, sequenceNumber - m_LowestSequenceNumber), e = count - 1; i < e; ++i)
                        {
                            //Obtain packet at i
                            p = Packets[i];
                            if (p.SequenceNumber == sequenceNumber) return i; // i
                            p = Packets[--e];
                            if (p.SequenceNumber == sequenceNumber) return e; // e
                        }
                    }
            }
        }
        //bool Remove(int seq, out RtpPacket packet, out int index)
        /// Removes a RtpPacket from the RtpFrame by the given Sequence Number.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the RtpPacket to remove</param>
        /// <returns>A RtpPacket with the sequence number if removed, otherwise null</returns>
        public RtpPacket Remove(int sequenceNumber) { return Remove(ref sequenceNumber); }
        
        [CLSCompliant(false)]
        public RtpPacket Remove(ref int sequenceNumber) //ref
        {
            int count = Count;
            if (i < 0) return null;
            RtpPacket p = Packets[i];
            Packets.RemoveAt(i);
            switch (--count)
            {
                case 0: 
                    m_LowestSequenceNumber = m_HighestSequenceNumber = -1; 
                    
                    //m_PayloadType &= Common.Binary.SevenBitMaxValue;
                    //Only 1 packet remains, saves a count - 1 and an array access.
                case 1:
                    {
                        //m_LowestSequenceNumber = m_HighestSequenceNumber = Packets[0].SequenceNumber; 
                        
                        //If this was at 0 then remap 0 to 1
                        if (sequenceNumber == m_LowestSequenceNumber) m_LowestSequenceNumber = m_HighestSequenceNumber;
                        else m_HighestSequenceNumber = m_LowestSequenceNumber;
                        goto CheckMarker;
                //only 2 packets is really default also but this saves a count - 1 instruction
                    //It also saves one access to the array when possible.
                case 2:
                    {
                        switch (i)
                        {
                            case 0://(sequenceNumber == m_LowestSequenceNumber)
                                {
                                    m_LowestSequenceNumber = Packets[0].SequenceNumber;
                                    break;
                                }
                            case 1: break; //Index 1 when there was 3 packets cannot effect the lowest or highest but may have a marker if multiple marker packets are stored.
                            case 2://(sequenceNumber == m_HighestSequenceNumber)
                                {
                                    m_HighestSequenceNumber = Packets[1].SequenceNumber;
                                    break;
                                }
                        }
                    }
                default:
                    {
                        //Skip the access of the array for all cases but when the sequence was == to the m_LowestSequenceNumber (i == 0)
                        if(i == 0) //(sequenceNumber == m_LowestSequenceNumber)
                        {
                            m_LowestSequenceNumber = Packets[0].SequenceNumber; //First
                        }
                        else if (sequenceNumber == m_HighestSequenceNumber)// (i >= count)
                        {
                            m_HighestSequenceNumber = Packets[count - 1].SequenceNumber; //Last
                        }
                    }
            }
            //Check for marker when i >= count and unset marker bit if present. (Todo, if AllowMultipleMarkers this needs to be counted)
            if (m_MarkerCount > 0 && p.Marker) --m_MarkerCount;
            //Remove any memory assoicated with the packet by getting the key of the packet.
            //Force if the packet should be disposed... (the packet is not really being disposed just yet..)
            FreeDepacketizedMemory((short)sequenceNumber, p.ShouldDispose); //(sequenceNumber);
            
            return p;            //Notes, i contains the offset where p was stored.
        }
        /// Empties the RtpFrame by clearing the underlying List of contained RtpPackets
        /// </summary>
        internal protected void RemoveAllPackets() //bool disposeBuffer
        {
            //Packets.Clear();
            //Depacketized.Clear();
        }
        /// Disposes all contained packets.
        /// Disposes the buffer
        /// Clears the contained packets.
        /// </summary>
        public void Clear()
        {
            //////Multiple threads adding packets would not effect count but removing definitely would...
            ////Packets.Clear();
            for (int e = Packets.Count; --e >= 0; --e)
            {
                //Flag / Mark removing
                //using (RtpPacket p = Packets[e])
                //{
                //    //Must either choose to persist or free at this point.
                //    //if (p.ShouldDispose)
                //    FreeDepacketizedMemory(GetPacketKey(p.SequenceNumber), p.ShouldDispose);
                //}
                //Packets.RemoveAt(e);

                Remove(ref m_LowestSequenceNumber);
            }
            //////DisposeAllPackets();
            //////DisposeBuffer();
            //////FreeDepacketizedMemory();
            //////RemoveAllPackets(); 
        }
        /// <summary>
        /// Disposes all contained packets. 
        /// </summary>
        internal protected void DisposeAllPackets()
        {
            //System.Linq.ParallelEnumerable.ForAll(Packets.AsParallel(), (t) => t.Dispose());
            foreach (RtpPacket p in Packets) p.Dispose();
        }
        //This also has no place in the API unless forcefully made up, e.g. ProcessPacket could be Assemble
        //Assemble a packet means to take a rtp packet and get the data which is needed for the decoder
        //sometimes the extensions are needed, most of the time there is only the need to skip the csrc list if present
        /// Calls <see cref="RtpFrame.AssemblePacket"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="useExtensions"></param>
        /// <param name="profileHeaderSize"></param>
        /// <returns></returns>
        public virtual Common.MemorySegment Assemble(RtpPacket packet, bool useExtensions = false, int profileHeaderSize = 0)
        {
            return RtpFrame.AssemblePacket(packet, useExtensions, profileHeaderSize);
        }
        /// Assembles the RtpFrame into a IEnumerable by use of concatenation, the ExtensionBytes and Payload of all contained RtpPackets into a single sequence (excluding the RtpHeader)
        /// <see cref="RtpFrame.AssemblePacket"/>
        /// </summary>
        /// <returns>The byte array containing the assembled frame</returns>
        public IEnumerable<byte> Assemble(bool useExtensions = false, int profileHeaderSize = 0)
        {
            //The result
            IEnumerable<byte> sequence = Common.MemorySegment.Empty;
                                                            //Use the static functionality by default RtpFrame.AssemblePacket(packet, useExtensions, profileHeaderSize)
            foreach (RtpPacket packet in Packets) sequence = sequence.Concat(Assemble(packet, useExtensions, profileHeaderSize));
                 
            return sequence;
        }
        /// Depacketizes all contained packets ignoring <see cref="IsComplete"/>.
        /// </summary>
        public void Depacketize() { Depacketize(true); }
        /// Depacketizes all contained packets if possible.
        /// </summary>
        /// <param name="allowIncomplete">Determines if <see cref="IsComplete"/> must be true</param>
        public virtual void Depacketize(bool allowIncomplete)
        {
            //May allow incomplete packets.
            if (false == allowIncomplete && false == IsComplete) return;
            //{
            //    RtpPacket p = Packets[i];
            //}
            foreach (RtpPacket packet in Packets) Depacketize(packet);
        }
        
        //Virtual so dervied types can call their Depacketize method with any options they may require
        /// Depacketizes a single packet
        /// </summary>
        /// <param name="packet"></param>
        public virtual void Depacketize(RtpPacket packet)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(packet)) return;
        }
        /// Takes all depacketized segments and writes them to the buffer.
        /// Disposes any existing buffer. Creates a new buffer.
        /// </summary>
        internal protected void PrepareBuffer() //bool, persist, action pre pre write, post write
        {
            //Ensure there is something to write to the buffer
            if (false == HasDepacketized) return;
            DisposeBuffer();
            m_Buffer = new Common.SegmentStream(Depacketized.Values);
            //foreach (KeyValuePair<int, Common.MemorySegment> pair in Depacketized)
            //{
            //    //Get the segment
            //    Common.MemorySegment value = pair.Value;
            //    if (Common.IDisposedExtensions.IsNullOrDisposed(value) || value.Count == 0) continue;
            //    m_Buffer.Write(value.Array, value.Offset, value.Count);
            //}
            //m_Buffer.Seek(0, System.IO.SeekOrigin.Begin);
        }
        /// If <see cref="HasDepacketized"/>, Copies the memory already depacketized to an array at the given offset.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns>The amount of bytes copied</returns>
        public int CopyTo(byte[] buffer, int offset)
        {
            int total = 0;
            foreach (KeyValuePair<int, Common.MemorySegment> pair in Depacketized)
            {
                //Get the segment
                Common.MemorySegment value = pair.Value;
                if (Common.IDisposedExtensions.IsNullOrDisposed(value) || value.Count == 0) continue;
                System.Array.Copy(value.Array, value.Offset, buffer, offset, value.Count);
                total += value.Count;
                offset += value.Count;
            }
        }
        /// virtual so it's easy to keep the same API, not really needed though since Dispose is also overridable.
        /// </summary>
        internal virtual protected void DisposeBuffer()
        {
            if (m_Buffer != null)
            {
                m_Buffer.Dispose();
            }
        }
        internal protected void FreeDepacketizedMemory(bool force = false)
        {
            //iterate each key in Depacketized
            foreach (KeyValuePair<int, Common.MemorySegment> pair in Depacketized)
            {
                //Set ShouldDispose = true and call Dispose.
                if (force || pair.Value.ShouldDispose)
                    Common.BaseDisposable.SetShouldDispose(pair.Value, true, false);
            }
            Depacketized.Clear();
        }
        //const uint OrderMask = 0xffff0000;
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //internal protected int GetOrderNumber(ref int key) { return (int)((short)key & OrderMask); }
        //[CLSCompliant(false)]
        //internal protected /*virtual*/ int GetPacketKey(ref int key)
        //{
        //    unchecked { return (short)key; }
        //}
        //internal protected /*virtual*/ int GetPacketKey(int key)
        //{
        //    //Todo, could allow unsafe calls here to improve performance Int32ToInt16Bits
        //    return GetPacketKey(ref key);
        //}
        /// Removes memory refereces related to the given key.
        /// By default if the memory was persisted it is left in the list.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="force"></param>
        internal protected void FreeDepacketizedMemory(int key, bool force = false)
        {
            //Needs a method to virtually determine the key of the packet.
            if (Depacketized.ContainsKey(key))
            {
                //Obtain the segment
                Common.MemorySegment segment = Depacketized[key];
                if (force || segment.ShouldDispose)
                {
                    //Dispose the memory
                    segment.Dispose();
                    Depacketized.Remove(key);
                }                
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        {
            if (IsDisposed) return;
            {
                //Remove packets and any memory
                Clear();
                DisposeBuffer();
                FreeDepacketizedMemory(true);
            }
        }
        //Otherwise there is no standard code for obtaining a Derived type besides using IsSubClassOf.
        //Then would still be unable to tell what profile it supports if using Dynamic..

    }

    //#region Todo IRtpFrame

    ////Where this is probably going
    ////Would be something like the facade pattern.
    //public interface IRtpFrame : Common.IDisposed
    //{
    //    /// <summary>
    //    /// Gets theSynchronizationSourceIdentifier of all contained packets or -1 if unassigned.
    //    /// </summary>
    //    int SynchronizationSourceIdentifier { get; }

    //    /// <summary>
    //    /// Indicates if at least one packet contained has the Marker bit set.
    //    /// </summary>
    //    bool HasMarker { get; }

    //    /// <summary>
    //    /// The amount of contained packets which have the Marker bit set.
    //    /// </summary>
    //    int MarkerCount { get; }

    //    /// <summary>
    //    /// Gets the PayloadType of all contained packets or -1 if unassigned.
    //    /// </summary>
    //    int PayloadType { get; }

    //    /// <summary>
    //    /// Gets the Timestamp of all contained packets or -1 if unassigned.
    //    /// </summary>
    //    int Timestamp { get; }

    //    /// <summary>
    //    /// Indicates the number of contained packets.
    //    /// </summary>
    //    int PacketCount { get; }

    //    /// <summary>
    //    /// Attempts to add the packet to the frame
    //    /// </summary>
    //    /// <param name="packet">The packet to add</param>
    //    /// <returns>True if the packet was added, otherwise false.</returns>
    //    bool TryAdd(RtpPacket packet);

    //    /// <summary>
    //    /// Adds the given packet to the frame.
    //    /// </summary>
    //    /// <param name="packet">The packet to add.</param>
    //    void Add(RtpPacket packet);

    //    /// <summary>
    //    /// Attempts to remove the packet with the given sequence number and returns the packet if contained.
    //    /// </summary>
    //    /// <param name="sequenceNumber">The sequence number of the packet to remove.</param>
    //    /// <param name="packet">The packet which has been removed from the frame.</param>
    //    /// <returns>True if the packet was contained, otherwise false.</returns>
    //    bool TryRemove(int sequenceNumber, out RtpPacket packet);

    //    /// <summary>
    //    /// Removes the packet with the given sequence number
    //    /// </summary>
    //    /// <param name="sequenceNumber">The sequence number</param>
    //    /// <returns>The packet removed or null of nothing was removed.</returns>
    //    RtpPacket Remove(int sequenceNumber);
        
    //    //void Remove(RtpPacket packet); //Disposes packet using(Remove(packet.SequenceNumber)) ;

    //    /// <summary>
    //    /// Removes all contained packets.
    //    /// </summary>
    //    void ClearPackets();

    //    /// <summary>
    //    /// Clears all packets, resets PayloadType, Timestamp and ssrc.
    //    /// </summary>
    //    //void Reset();

    //    /// <summary>
    //    /// Indicates if <see cref="Depacketize"/> was called.
    //    /// </summary>
    //    bool HasDepacketized { get; }

    //    /// <summary>
    //    /// Indicates how many depacketized segments are contained.
    //    /// </summary>
    //    int SegmentCount { get; }

    //    /// <summary>
    //    /// Indicates how many bytes of memory are used by depacketized segments.
    //    /// </summary>
    //    int DepacketizedSize { get; }

    //    /// <summary>
    //    /// Free memory used by depacketized segments.
    //    /// </summary>
    //    void ClearDepacketization();

    //    //GetDepacketizedSegments

    //    //void Depacketize

    //    //Packetize

    //    //Repacketize

    //    //Extension or static?
    //    //CreateMemoryStream() //from GetDepacketizedSegments

    //Should also implement or have a getter for the RtpProfile to which it corresponds...

    //}

    //#endregion

    #region To be implemented via RtpFrame

    //Doesn't need to inerhit but can although it looks weird.
    //Could just compose the frame in a member and add all the stuff related to packetization here.
    //RFC3550 could define a Framing (DePacketization) (Packetizer) which used the assemble methodology.
    //Would also be aware of various SDP parameters and PayloadType implementation..
    //public class Framing //Depacketization //Packetization //Packetizer //: RtpFrame
    //{
    //    #region Depacketization

    //    //Could be out or Ref on  Depacketize.
    //    internal protected RtpFrame Frame;

    //    //public virtual bool HasVariableSizeHeader { get; protected set; }

    //    //public virtual int HeaderSize {get; protected set; 

    //    //public virtual bool UsesExtensions { get; protected set; }

    //    public virtual int GetPacketKey(RtpPacket packet) { return packet.Timestamp - packet.SequenceNumber; }

    //    //public Func<RtpPacket, int> KeyGenerator;

    //    //delegate int KeyGenerator(RtpPacket packet);

    //    //public Func<bool> HasMarkerLogic;

    //    //delegate bool HasMarkerDelegate();

    //    //Could be out or ref on Depacketize
    //    //SortedMemory(should be keyed by sequence number by default but would require a Ushort comparer)
    //    internal protected SortedList<int, Common.MemorySegment> Segments; //Or Packetized // Packets

    //    //not needed if not stored.
    //    /// <summary>
    //    /// Indicates if there are any segments allocated
    //    /// </summary>
    //    public bool HasSegments { get { return Segments.Count > 0; } }

    //    /// <summary>
    //    /// Creates a MemoryStream from the <see cref="Segments"/> of data depacketized.
    //    /// </summary>
    //    /// <returns></returns>
    //    public System.IO.MemoryStream PrepareBuffer()
    //    {
    //        System.IO.MemoryStream result = new System.IO.MemoryStream(Segments.Values.Sum(d => d.Count));

    //        foreach (var pair in Segments)
    //        {
    //            Common.MemorySegment value = pair.Value;

    //            result.Write(value.Array, value.Offset, value.Count);
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// Depacketize's the frame with the default options.
    //    /// </summary>
    //    public virtual void Depacketize() { Depacketize(true); } //RtpFrame frame (what), SortedList<int, Common.MemorySegment> Segments (where)

    //    /// <summary>
    //    /// Depacketizes the payload segment of Frame
    //    /// </summary>
    //    public void Depacketize(bool allowIncomplete) //RtpFrame Frame (what), SortedList<int, Common.MemorySegment> Segments (where)
    //    {
    //        //Ensure the Frame is not null
    //        if (Common.IDisposedExtensions.IsNullOrDisposed(Frame)) return;

    //        //Check IsComplete if required
    //        if (false == allowIncomplete && false == Frame.IsComplete) return;

    //        //Iterate packet's in Frame
    //        foreach (RtpPacket packet in Frame)
    //        {
    //            //Depacketize the packet
    //            Depacketize(packet);
    //        }
    //    }

    //    /// <summary>
    //    /// Calls <see cref="Depacketize"/> in parallel
    //    /// </summary>
    //    /// <param name="allowIncomplete"></param>
    //    public void ParallelDepacketize(bool allowIncomplete) //RtpFrame frame (what), SortedList<int, Common.MemorySegment> Segments (where)
    //    {
    //        //Ensure the Frame is not null
    //        if (Common.IDisposedExtensions.IsNullOrDisposed(Frame)) return;

    //        //Check IsComplete if required
    //        if (false == allowIncomplete && false == Frame.IsComplete) return;

    //        //In parallel Depacketize each packet.
    //        ParallelEnumerable.ForAll(Frame.AsParallel(), Depacketize);
    //    }

    //    /// <summary>
    //    /// Depacketize a single packet
    //    /// </summary>
    //    /// <param name="packet"></param>
    //    public virtual void Depacketize(RtpPacket packet) //SortedList<int, Common.MemorySegment> Segments (where)
    //    {
    //        //Calulcate the key
    //        int key = GetPacketKey(packet);

    //        //Save for previously Depacketized packets
    //        if (Segments.ContainsKey(key)) return;

    //        //Add the data in the PayloadDataSegment.
    //        Segments.Add(key, packet.PayloadDataSegment);
    //    }

    //    #endregion

    //    #region Packetization

    //    /// <summary>
    //    /// Packetizes the sourceData to a RtpFrame.
    //    /// </summary>
    //    public virtual RtpFrame Packetize(byte[] sourceData, int offset, int count, int bytesPerPayload, int sequenceNumber, int timeStamp, int ssrc, int payloadType, bool setMarker = true)
    //    {
    //        RtpFrame result = new RtpFrame();

    //        bool marker = false;

    //        while (count > 0)
    //        {
    //            //Subtract for consumed bytes and compare to bytesPerPayload
    //            if ((count -= bytesPerPayload) <= bytesPerPayload)
    //            {
    //                bytesPerPayload = count;

    //                marker = setMarker;
    //            }

    //            //Move the offset
    //            offset += bytesPerPayload;

    //            //Add the packet created from the sourceData at the offset, increase the sequence number.
    //            result.Add(new RtpPacket(new RtpHeader(2, false, false, marker, payloadType, 0, ssrc, sequenceNumber++, timeStamp), 
    //                new Common.MemorySegment(sourceData, offset, bytesPerPayload)));
    //        }

    //        return result;
    //    }

    //    #endregion

    //    #region Repacketization

    //    //Should return frame, this would imply that both frames would exist for ashort period of time.
    //    //Otherwise have an inplace option.
    //    /// <summary>
    //    /// Repacketizes the payload segment of Frame according to the given options.
    //    /// </summary>
    //    /// <param name="bytesPerPayload"></param>
    //    public virtual void Repacketize(RtpFrame frame, int bytesPerPayload) // bool inPlace
    //    {
    //        RtpPacket current = null;

    //        foreach (RtpPacket packet in frame)
    //        {
    //            if (packet.Length > bytesPerPayload)
    //            {
    //                //split
    //            }
    //            else
    //            {
    //                //join

    //                if (current == null) current = packet;
    //                else
    //                {

    //                    if (current.Length + packet.Length > bytesPerPayload)
    //                    {
    //                        //split
    //                    }
    //                    else
    //                    {
    //                        //join
    //                    }

    //                }
    //            }
    //        }

    //        //hard to modify frame in place...

    //        //Better to take a new frame and populate and swap and replace.

    //        int currentSize = 0;

    //        for (int i = 0, e = frame.Count; i < e; ++i)
    //        {
    //            current = frame[i];

    //            int currentLength = current.Length;

    //            if (currentSize + currentLength > bytesPerPayload)
    //            {
    //                //Split

    //                //Add a packet with bytesPerPayload from current 

    //                //Add a packet with currentLength - bytesPerPayload

    //                //Increas index 
    //                ++i;
    //            }
    //            else
    //            {
    //                //Remove current (reset for index again)
    //                Frame.Packets.RemoveAt(i--);

    //                //Join
    //                currentSize += current.Length;

    //                //Make a new packet and combine.

    //                continue;
    //            }
    //        }


    //        return;
    //    }

    //    #endregion

    //    //Dispose();
    //}

    #endregion

    #region Other concepts thought up but not used.

    //Since it does not inherit the frame this does not work very well.
    //Could have MultiMarkerFrame be dervived but Add is not virtual / overloadable.
    //public class MultiPayloadRtpFrame
    //{
    //    internal readonly protected Dictionary<int, RtpFrame> Frames = new Dictionary<int, RtpFrame>();

    //    public bool TryRemove(int payloadType, out RtpFrame frame) { return Common.Extensions.Generic.Dictionary.DictionaryExtensions.TryRemove(Frames, ref payloadType, out frame); }

    //    public bool ContainsPayloadType(int payloadType) { return Frames.ContainsKey(payloadType); }

    //    public bool TryGetFrame(int payloadType, out RtpFrame result) { return Frames.TryGetValue(payloadType, out result); }
    //}

    //public class DynamicRtpFrame : RtpFrame
    //{
    //    //void GetDepacketizer(int payloadType);
    //    //readonly Action<RtpPacket> Depacketize;
    //}

    //public class RtpFrameExtensions
    //{
    //    //public static RtpFrame CreateTypedFrame => Depacketize
    //}

    #endregion

    //A construct for sending a RtpFrame from an existing byte[] only using a single new RtpHeader and a MemorySegment for the Packetized data would be very efficient for sending.
    //foreach RtpPacket packet in Packetize(byte[] data, RtpHeader header = null, int bytesPerPacket) while(offset < data.Length) RtpPacket packet = Packetize(data, offset, bytesPerPacket) => Send(packet); offset += bytesPerpacket;

    //Packetize would need IEnumerable<MemorySegment> overload

    //A construct for Depacketizing a RtpFrame such as Depacketized might be better seperate for purposes when you only will decode a single portion of data at a time, the data would not have to be stored for each packet
    //foreach MemorySegment ms in frame.Depacketized => foreach RtpPacket packet in frame yield return Depacketize(packet)

    //Depacketized would need IEnumerable<MemorySegment> overload or it could even be on the frame if frame was IEnumerable<MemorySegment>
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the RtpFrame class is correct
    /// </summary>
    internal class RtpFrameUnitTests
    {
        public void TestAddingRandomPackets()
        {
            //Create a frame with an unspecified payload
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame())
            {
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 2
                });
                //if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 4
                });
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 3
                });
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 5
                });
                if (frame.IsMissingPackets) throw new Exception("Frame is qmissing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 1
                });
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 10,
                    Marker = true
                });
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 7
                });
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 9
                });
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 8
                });
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");
                if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = 6
                });
                if (frame.Count != 10) throw new Exception("Frame must have 10 packets");
                if (false == frame.HasMarker) throw new Exception("Frame must have marker");
                if (frame.IsMissingPackets) throw new Exception("Frame should not be missing packets");
                //Console.WriteLine(string.Join(",", frame.Select(p => p.SequenceNumber).ToArray()));
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue
                });
                if (false == frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                });                
                if (false == frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MinValue
                });
                if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                for (int nextSeq = 65534, i = 0; i < 13; ++i)
                {
                    if (frame.Packets[i].SequenceNumber != nextSeq) throw new Exception("Invalid order");
                }
            }
        }
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                   {
                       SequenceNumber = ushort.MinValue,
                       Marker = true
                   });

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue
                });
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                });
                try
                {
                    frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                    {
                        SequenceNumber = 1
                    }, false); //allowPacketsAfterMarker false
                }
                catch(InvalidOperationException)
                {
                    //Expected because allowPacketsAfterMarker was false.
                    {
                        SequenceNumber = 1
                    });
                    {
                        SequenceNumber = 2,
                        Marker = true
                    });
                    {
                        if (removed.SequenceNumber != 2) throw new Exception("Should have SequenceNumber 2");
                    }
                    {
                        if (removed.SequenceNumber != 1) throw new Exception("Should have SequenceNumber 1");
                    }
                }
                using (Media.Rtp.RtpPacket packet = frame.Remove(1))
                {
                    if (packet != null) throw new Exception("Packet is not null");
                }
                using (Media.Rtp.RtpPacket packet = frame.Remove(ushort.MaxValue))
                {
                    if(packet == null || packet.SequenceNumber != ushort.MaxValue) throw new Exception("Packet is null");
                }
                {
                    if (packet == null || packet.SequenceNumber != ushort.MinValue) throw new Exception("Packet is null");
                }
                {
                    if (packet == null || packet.SequenceNumber != ushort.MaxValue - 1) throw new Exception("Packet is null");
                }

                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MinValue
                });
                {
                    SequenceNumber = 1
                });
                using(frame.Remove(1)) ;
            }
        }
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                frame.Add(new Media.Rtp.RtpPacket(2, false, false, Media.Common.MemorySegment.EmptyBytes)
                {
                    SequenceNumber = ushort.MaxValue - 1
                });
                {
                    SequenceNumber = ushort.MaxValue
                });
                {
                    SequenceNumber = ushort.MinValue,
                    Marker = true
                });
            }
        }
        {
            //Create a frame
            using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
            {
                //Add packets to the frame
                for (int i = 0; i < 15; ++i)
                {
                    {
                        SequenceNumber = i,
                        Marker = i == 14
                    });
                }
                using (frame.Remove(14))
                {
                    //Frame doesn't have a marker packet anymore
                    if (frame.IsComplete) throw new Exception("Frame is complete");
                    if (frame.HasMarker) throw new Exception("Frame has marker");
                    if (frame.IsMissingPackets) throw new Exception("Frame is missing packets");
                }

                using (frame.Remove(1))
                {
                    if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                }
            }
        }
        {
            //Create a frame
            unchecked
            {
                using (Media.Rtp.RtpFrame frame = new Media.Rtp.RtpFrame(0))
                {
                    //Add 15 packets to the frame
                    for (ushort i = ushort.MaxValue - 5; i != 10; ++i)
                    {
                        {
                            SequenceNumber = i,
                            Marker = i == 9
                        });
                    }
                    using (frame.Remove(0))
                    {
                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                    }
                    using (frame.Remove(2))
                    {
                        if (false == frame.IsMissingPackets) throw new Exception("Frame is not missing packets");
                    }
                    using (frame.Remove(9))
                    {
                        if (frame.IsComplete) throw new Exception("Frame is complete");
                    }
                }
            }
        }
        {
            //Create a frame
            unchecked
            {
                int expected = 0;
                {
                    byte payloadValue = byte.MinValue;
                    for (ushort i = ushort.MaxValue - 5; i != 10; ++i)
                    {
                        //1 byte in the payload for easy testing.
                        frame.Add(new Media.Rtp.RtpPacket(2, false, false, new byte[] { payloadValue++ })
                        {
                            SequenceNumber = i,
                            Marker = i == 9
                        });
                    }
                    int frameCount = frame.Count;
                    frame.Depacketize();
                    if (frame.HasDepacketized)
                    {
                        System.IO.Stream buffer = frame.Buffer;
                        if (buffer.Length != frameCount) throw new Exception("More data in buffer than expected");
                        while (buffer.Position < frameCount)
                        {
                            //If the byte is out of order then throw an exception
                            if (buffer.ReadByte() != expected++) throw new Exception("Data at wrong position");
                        }
                    }
                    else throw new Exception("HasDepacketized");
                    if (frame.Depacketized.Count != frameCount) throw new Exception("More data in Depacketized than expected");
                    frame.Depacketize();
                    if (frame.Depacketized.Count != frameCount) throw new Exception("More data in Depacketized than expected");
                    if (frame.HasDepacketized)
                    {
                        System.IO.Stream buffer = frame.Buffer;
                        if (buffer.Length != frameCount) throw new Exception("More data in buffer than expected");
                        if (buffer.Position != frameCount) throw new Exception("Position changed in buffer");
                    }
                    else throw new Exception("HasDepacketized");
                    {
                        int index;
                        while (reorder.Count < frameCount)
                        {
                            //Calulate a random index between 0 and the frame.Count - 1
                            index = Utility.Random.Next(0, frame.Count - 1);
                            reorder.Add(frame[index]);
                            //Warining, (this will not set m_LowestSequenceNumber or m_HighestSeqenceNumber in frame) we are working with the List directly!
                            //This will also leave Depacketized with 16 entries which are still alive and valid because they are in the reordered packet.
                            //Publicly this field is not accessible, it's restricted to the internal use of the library and protected use for the dervied classed.
                            frame.Packets.RemoveAt(index);
                            reorder.Depacketize();
                            //This required the SegmentList to work the way we need it to.
                            {
                                //Console.WriteLine("Index:" + index + " reorder.Count: " + reorder.Count);
                                int lastByte = -1, currentByte = -1;
                                while (buffer.Position < buffer.Length)
                                {
                                    //Read the byte if not already read
                                    if (currentByte == -1)
                                    {
                                        lastByte = currentByte = buffer.ReadByte();
                                        
                                        continue;
                                    }
                                    currentByte = buffer.ReadByte();
                                    if (currentByte < lastByte) throw new Exception("Data out of order");
                                }
                            }
                        for (ushort i = ushort.MaxValue - 5, j = 0; i != 10; ++i)
                        {
                            if (reorder.IndexOf(i) != j++) throw new Exception("frame order incorrect.");
                        }
                        if (false == frame.IsEmpty) throw new Exception("frame.IsEmpty");
                        if (reorder.Count != frameCount) throw new Exception("reorder.Count");
                        reorder.Depacketize();
                        if (reorder.Depacketized.Count != frameCount) throw new Exception("More data in Depacketized than expected");
                        if (reorder.HasDepacketized)
                        {
                            expected = 0;
                            if (buffer.Length != frameCount) throw new Exception("More data in buffer than expected");
                            while (buffer.Position < frameCount)
                            {
                                //If the byte is out of order then throw an exception
                                if (buffer.ReadByte() != expected++) throw new Exception("Data at wrong position");
                            }
                        }
                        else throw new Exception("HasDepacketized");
                        //Test the SegmentStream (Not really part of this test yet because it's not used.)
                        {
                            expected = 0;
                            while (test.Position < frameCount)
                            {
                                //If the byte is out of order then throw an exception
                                if (test.ReadByte() != expected++) throw new Exception("Data at wrong position");
                            }
                        }
                    }
                }
            }
        }
    }
}