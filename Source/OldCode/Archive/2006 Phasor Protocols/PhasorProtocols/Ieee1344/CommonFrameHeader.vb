'*******************************************************************************************************
'  CommonFrameHeader.vb - IEEE 1344 Common frame header functions
'  Copyright � 2005 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2005
'  Primary Developer: J. Ritchie Carroll, Operations Data Architecture [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2827
'       Email: jrcarrol@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  11/12/2004 - J. Ritchie Carroll
'       Initial version of source generated
'
'*******************************************************************************************************

Imports System.Runtime.Serialization
Imports System.IO
Imports TVA.DateTime
Imports PhasorProtocols.Common
Imports PhasorProtocols.Ieee1344.Common
Imports TVA.IO.Compression.Common

Namespace Ieee1344

    ' This class generates and parses a frame header specfic to C37.118
    <CLSCompliant(False), Serializable()> _
    Public NotInheritable Class CommonFrameHeader

#Region " Internal Common Frame Header Instance Class "

        ' This class is used to temporarily hold parsed frame header
        Friend Class CommonFrameHeaderInstance

            Implements ICommonFrameHeader

            Private m_idCode As UInt64
            Private m_sampleCount As Int16
            Private m_ticks As Long
            Private m_frameQueue As MemoryStream
            Private m_statusFlags As Int16
            Private m_attributes As Dictionary(Of String, String)
            Private m_tag As Object

            Public Sub New()

            End Sub

            Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)

                Throw New NotImplementedException()

            End Sub

            Public ReadOnly Property This() As IChannel Implements IChannel.This
                Get
                    Return Me
                End Get
            End Property

            Private ReadOnly Property IFrameThis() As Measurements.IFrame Implements Measurements.IFrame.This
                Get
                    Return Me
                End Get
            End Property

            Public ReadOnly Property DerivedType() As System.Type Implements IChannel.DerivedType
                Get
                    Return Me.GetType()
                End Get
            End Property

            Public Property IDCode() As UInt64 Implements ICommonFrameHeader.IDCode
                Get
                    Return m_idCode
                End Get
                Set(ByVal value As UInt64)
                    m_idCode = value
                End Set
            End Property

            Private Property IChannelFrameIDCode() As UInt16 Implements IChannelFrame.IDCode
                Get
                    If m_idCode > UInt16.MaxValue Then
                        Return UInt16.MaxValue
                    Else
                        Return Convert.ToUInt16(m_idCode)
                    End If
                End Get
                Set(ByVal value As UInt16)
                    m_idCode = value
                End Set
            End Property

            Public Sub AppendFrameImage(ByVal binaryImage As Byte(), ByVal offset As Integer, ByVal length As Integer)

                ' Validate CRC of frame image being appended
                If Not ChecksumIsValid(binaryImage, offset, length) Then
                    m_frameQueue = Nothing
                    Throw New InvalidOperationException("Invalid binary image detected - check sum of individual IEEE 1344 interleaved configuration or header frame did not match")
                End If

                ' Create new frame queue to hold combined binary image, if it doesn't already exist
                If m_frameQueue Is Nothing Then
                    m_frameQueue = New MemoryStream

                    ' Include initial header in new stream...
                    m_frameQueue.Write(binaryImage, offset, CommonFrameHeader.BinaryLength)
                End If

                ' Skip past header
                offset += CommonFrameHeader.BinaryLength

                ' Include frame image
                m_frameQueue.Write(binaryImage, offset, length - CommonFrameHeader.BinaryLength)

            End Sub

            Private Function ChecksumIsValid(ByVal buffer As Byte(), ByVal startIndex As Int32, ByVal length As Int32) As Boolean

                Dim sumLength As Int16 = length - 2
                Return EndianOrder.BigEndian.ToUInt16(buffer, startIndex + sumLength) = CRC16(UInt16.MaxValue, buffer, startIndex, sumLength)

            End Function

            Public Property IsFirstFrame() As Boolean
                Get
                    Return CommonFrameHeader.IsFirstFrame(Me)
                End Get
                Set(ByVal value As Boolean)
                    CommonFrameHeader.IsFirstFrame(Me) = value
                End Set
            End Property

            Public Property IsLastFrame() As Boolean
                Get
                    Return CommonFrameHeader.IsLastFrame(Me)
                End Get
                Set(ByVal value As Boolean)
                    CommonFrameHeader.IsLastFrame(Me) = value
                End Set
            End Property

            Public Property FrameCount() As Int16
                Get
                    Return CommonFrameHeader.FrameCount(Me)
                End Get
                Set(ByVal value As Int16)
                    CommonFrameHeader.FrameCount(Me) = value
                End Set
            End Property

            Public ReadOnly Property FrameType() As FrameType Implements ICommonFrameHeader.FrameType
                Get
                    Return CommonFrameHeader.FrameType(Me)
                End Get
            End Property

            Public ReadOnly Property FundamentalFrameType() As FundamentalFrameType Implements IChannelFrame.FrameType, ICommonFrameHeader.FundamentalFrameType
                Get
                    ' Translate IEEE 1344 specific frame type to fundamental frame type
                    Select Case FrameType
                        Case Ieee1344.FrameType.DataFrame
                            Return FundamentalFrameType.DataFrame
                        Case Ieee1344.FrameType.ConfigurationFrame
                            Return FundamentalFrameType.ConfigurationFrame
                        Case Ieee1344.FrameType.HeaderFrame
                            Return FundamentalFrameType.HeaderFrame
                        Case Else
                            Return FundamentalFrameType.Undetermined
                    End Select
                End Get
            End Property

            Public ReadOnly Property FrameLength() As Int16 Implements ICommonFrameHeader.FrameLength
                Get
                    If m_frameQueue IsNot Nothing Then
                        ' If we are cumulating frames, we use this total length instead of length parsed from individual frame
                        Return BinaryLength
                    Else
                        Return CommonFrameHeader.FrameLength(Me)
                    End If
                End Get
            End Property

            Public ReadOnly Property DataLength() As Int16 Implements ICommonFrameHeader.DataLength
                Get
                    Return CommonFrameHeader.DataLength(Me)
                End Get
            End Property

            Public Property InternalSampleCount() As Int16 Implements ICommonFrameHeader.InternalSampleCount
                Get
                    Return m_sampleCount
                End Get
                Set(ByVal value As Int16)
                    m_sampleCount = value
                End Set
            End Property

            Public Property InternalStatusFlags() As Int16 Implements ICommonFrameHeader.InternalStatusFlags
                Get
                    Return m_statusFlags
                End Get
                Set(ByVal value As Int16)
                    m_statusFlags = value
                End Set
            End Property

            Public Property Ticks() As Long Implements IChannelFrame.Ticks
                Get
                    Return m_ticks
                End Get
                Set(ByVal value As Long)
                    m_ticks = value
                End Set
            End Property

            Public Property StartSortTime() As Long Implements TVA.Measurements.IFrame.StartSortTime
                Get
                    Return 0
                End Get
                Set(ByVal value As Long)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property LastSortTime() As Long Implements TVA.Measurements.IFrame.LastSortTime
                Get
                    Return 0
                End Get
                Set(ByVal value As Long)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property LastSortedMeasurement() As TVA.Measurements.IMeasurement Implements TVA.Measurements.IFrame.LastSortedMeasurement
                Get
                    Return Nothing
                End Get
                Set(ByVal value As TVA.Measurements.IMeasurement)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public ReadOnly Property TimeTag() As NtpTimeTag Implements ICommonFrameHeader.TimeTag
                Get
                    Return CommonFrameHeader.TimeTag(Me)
                End Get
            End Property

            Private ReadOnly Property IChannelFrameTimeTag() As UnixTimeTag Implements IChannelFrame.TimeTag
                Get
                    Return New UnixTimeTag(Timestamp)
                End Get
            End Property

            Public ReadOnly Property BinaryImage() As Byte() Implements IBinaryDataProvider.BinaryImage
                Get
                    If m_frameQueue Is Nothing Then
                        Return Nothing
                    Else
                        Return m_frameQueue.ToArray()
                    End If
                End Get
            End Property

            Private ReadOnly Property IBinaryDataProviderBinaryLength() As Integer Implements IBinaryDataProvider.BinaryLength
                Get
                    Return BinaryLength
                End Get
            End Property

            Public ReadOnly Property BinaryLength() As UInt16 Implements IChannel.BinaryLength
                Get
                    If m_frameQueue Is Nothing Then
                        Return 0
                    Else
                        Return m_frameQueue.Length
                    End If
                End Get
            End Property

            Public Sub ParseBinaryImage(ByVal state As IChannelParsingState, ByVal binaryImage() As Byte, ByVal startIndex As Integer) Implements IChannel.ParseBinaryImage

                Throw New NotImplementedException()

            End Sub

            Public ReadOnly Property Cells() As Object Implements IChannelFrame.Cells
                Get
                    Return Nothing
                End Get
            End Property

            Public Property Published() As Boolean Implements IChannelFrame.Published
                Get
                    Return False
                End Get
                Set(ByVal value As Boolean)
                    Throw New NotImplementedException()
                End Set
            End Property

            ' This frame is not complete - it only represents the parsed common "header" for frames
            Public ReadOnly Property IsPartial() As Boolean Implements IChannelFrame.IsPartial
                Get
                    Return True
                End Get
            End Property

            Public ReadOnly Property Timestamp() As Date Implements IChannelFrame.Timestamp
                Get
                    Return New Date(m_ticks)
                End Get
            End Property

            Public Overloads Function Equals(ByVal other As TVA.Measurements.IFrame) As Boolean Implements System.IEquatable(Of Measurements.IFrame).Equals

                Return (CompareTo(other) = 0)

            End Function

            Public Function CompareTo(ByVal other As TVA.Measurements.IFrame) As Integer Implements System.IComparable(Of Measurements.IFrame).CompareTo

                Return m_ticks.CompareTo(other.Ticks)

            End Function

            Public Function CompareTo(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo

                Dim other As Measurements.IFrame = TryCast(obj, Measurements.IFrame)
                If other IsNot Nothing Then Return CompareTo(other)
                Throw New ArgumentException("Frame can only be compared with other IFrames...")

            End Function

            Private Function IFrameClone() As TVA.Measurements.IFrame Implements TVA.Measurements.IFrame.Clone

                Return Me

            End Function

            Private ReadOnly Property IFrameMeasurements() As IDictionary(Of Measurements.MeasurementKey, Measurements.IMeasurement) Implements Measurements.IFrame.Measurements
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Private Property IFramePublishedMeasurements() As Integer Implements Measurements.IFrame.PublishedMeasurements
                Get
                    Return 0
                End Get
                Set(ByVal value As Integer)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData

                Throw New NotImplementedException()

            End Sub

            Public ReadOnly Property Attributes() As Dictionary(Of String, String) Implements IChannel.Attributes
                Get
                    ' Create a new attributes dictionary or clear the contents of any existing one
                    If m_attributes Is Nothing Then
                        m_attributes = New Dictionary(Of String, String)
                    Else
                        m_attributes.Clear()
                    End If

                    m_attributes.Add("Derived Type", DerivedType.Name)
                    m_attributes.Add("Binary Length", BinaryLength)
                    m_attributes.Add("Total Cells", "0")
                    m_attributes.Add("Fundamental Frame Type", FundamentalFrameType & ": " & [Enum].GetName(GetType(FundamentalFrameType), FundamentalFrameType))
                    m_attributes.Add("ID Code", IDCode)
                    m_attributes.Add("Is Partial Frame", IsPartial)
                    m_attributes.Add("Published", Published)
                    m_attributes.Add("Ticks", Ticks)
                    m_attributes.Add("Timestamp", Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    m_attributes.Add("Frame Type", FrameType & ": " & [Enum].GetName(GetType(FrameType), FrameType))
                    m_attributes.Add("Frame Length", FrameLength)
                    m_attributes.Add("64-Bit ID Code", IDCode)
                    m_attributes.Add("Sample Count", InternalSampleCount)
                    m_attributes.Add("Status Flags", InternalStatusFlags)
                    m_attributes.Add("Frame Count", FrameCount)
                    m_attributes.Add("Is First Frame", IsFirstFrame)
                    m_attributes.Add("Is Last Frame", IsLastFrame)

                    Return m_attributes
                End Get
            End Property

            Public Property Tag() As Object Implements IChannel.Tag
                Get
                    Return m_tag
                End Get
                Set(ByVal value As Object)
                    m_tag = value
                End Set
            End Property

        End Class

#End Region

        Public Const BinaryLength As UInt16 = 6

        Private Sub New()

            ' This class contains only global functions and is not meant to be instantiated

        End Sub

        Public Shared Function ParseBinaryImage(ByVal configurationFrame As ConfigurationFrame, ByVal binaryImage As Byte(), ByVal startIndex As Int32) As ICommonFrameHeader

            With New CommonFrameHeaderInstance
                Dim secondOfCentury As UInt32

                secondOfCentury = EndianOrder.BigEndian.ToUInt32(binaryImage, startIndex)
                .InternalSampleCount = EndianOrder.BigEndian.ToInt16(binaryImage, startIndex + 4)

                ' We go ahead and pre-grab cell's status flags so we can determine framelength - we
                ' leave startindex at 6 so that cell will be able to parse flags as needed - note
                ' this increases needed common frame header size by 2 (i.e., BinaryLength + 2) 
                .InternalStatusFlags = EndianOrder.BigEndian.ToInt16(binaryImage, startIndex + 6)

                If .FrameType = Ieee1344.FrameType.DataFrame AndAlso configurationFrame IsNot Nothing Then
                    ' Data frames have subsecond time information
                    .Ticks = (New NtpTimeTag(secondOfCentury + SampleCount(.This) / System.Math.Floor(MaximumSampleCount / configurationFrame.Period) / configurationFrame.FrameRate)).ToDateTime.Ticks
                Else
                    ' For other frames, the best timestamp you can get is down to the whole second
                    .Ticks = (New NtpTimeTag(secondOfCentury)).ToDateTime.Ticks
                End If

                Return .This
            End With

        End Function

        Public Shared Function BinaryImage(ByVal frameHeader As ICommonFrameHeader) As Byte()

            Dim buffer As Byte() = CreateArray(Of Byte)(BinaryLength)

            ' Make sure frame length gets included in status flags for generated binary image
            FrameLength(frameHeader) = frameHeader.BinaryLength

            EndianOrder.BigEndian.CopyBytes(Convert.ToUInt32(frameHeader.TimeTag.Value), buffer, 0)
            EndianOrder.BigEndian.CopyBytes(frameHeader.InternalSampleCount, buffer, 4)

            Return buffer

        End Function

        Public Shared Sub Clone(ByVal sourceFrameHeader As ICommonFrameHeader, ByVal destinationFrameHeader As ICommonFrameHeader)

            destinationFrameHeader.Ticks = sourceFrameHeader.Ticks
            destinationFrameHeader.InternalSampleCount = sourceFrameHeader.InternalSampleCount

        End Sub

        Public Shared ReadOnly Property TimeTag(ByVal frameHeader As ICommonFrameHeader) As NtpTimeTag
            Get
                Return New NtpTimeTag(New Date(frameHeader.Ticks))
            End Get
        End Property

        Public Shared Property FrameType(ByVal frameHeader As ICommonFrameHeader) As FrameType
            Get
                Return frameHeader.InternalSampleCount And FrameTypeMask
            End Get
            Friend Set(ByVal value As FrameType)
                frameHeader.InternalSampleCount = (frameHeader.InternalSampleCount And Not FrameTypeMask) Or value
            End Set
        End Property

        Public Shared Property SampleCount(ByVal frameHeader As ICommonFrameHeader) As Int16
            Get
                Return frameHeader.InternalSampleCount And Not FrameTypeMask
            End Get
            Set(ByVal value As Int16)
                If value > MaximumSampleCount Then
                    Throw New OverflowException("Sample count value cannot exceed " & MaximumSampleCount)
                Else
                    frameHeader.InternalSampleCount = (frameHeader.InternalSampleCount And FrameTypeMask) Or value
                End If
            End Set
        End Property

        Public Shared Property IsFirstFrame(ByVal frameHeader As ICommonFrameHeader) As Boolean
            Get
                Return (frameHeader.InternalSampleCount And Bit12) = 0
            End Get
            Set(ByVal value As Boolean)
                If value Then
                    frameHeader.InternalSampleCount = frameHeader.InternalSampleCount And Not Bit12
                Else
                    frameHeader.InternalSampleCount = frameHeader.InternalSampleCount Or Bit12
                End If
            End Set
        End Property

        Public Shared Property IsLastFrame(ByVal frameHeader As ICommonFrameHeader) As Boolean
            Get
                Return (frameHeader.InternalSampleCount And Bit11) = 0
            End Get
            Set(ByVal value As Boolean)
                If value Then
                    frameHeader.InternalSampleCount = frameHeader.InternalSampleCount And Not Bit11
                Else
                    frameHeader.InternalSampleCount = frameHeader.InternalSampleCount Or Bit11
                End If
            End Set
        End Property

        Public Shared Property FrameCount(ByVal frameHeader As ICommonFrameHeader) As Int16
            Get
                Return frameHeader.InternalSampleCount And FrameCountMask
            End Get
            Set(ByVal value As Int16)
                If value > MaximumFrameCount Then
                    Throw New OverflowException("Frame count value cannot exceed " & MaximumFrameCount)
                Else
                    frameHeader.InternalSampleCount = (frameHeader.InternalSampleCount And Not FrameCountMask) Or value
                End If
            End Set
        End Property

        Public Shared Property FrameLength(ByVal frameHeader As ICommonFrameHeader) As Int16
            Get
                Return frameHeader.InternalStatusFlags And FrameLengthMask
            End Get
            Set(ByVal value As Int16)
                If value > MaximumFrameLength Then
                    Throw New OverflowException("Frame length value cannot exceed " & MaximumFrameLength)
                Else
                    frameHeader.InternalStatusFlags = (frameHeader.InternalStatusFlags And Not FrameLengthMask) Or value
                End If
            End Set
        End Property

        Public Shared Property DataLength(ByVal frameHeader As ICommonFrameHeader) As Int16
            Get
                ' Data length will be frame length minus common header length minus crc16
                Return FrameLength(frameHeader) - BinaryLength - 2
            End Get
            Set(ByVal value As Int16)
                If value > MaximumDataLength Then
                    Throw New OverflowException("Data length value cannot exceed " & MaximumDataLength)
                Else
                    FrameLength(frameHeader) = value + BinaryLength + 2
                End If
            End Set
        End Property

    End Class

End Namespace