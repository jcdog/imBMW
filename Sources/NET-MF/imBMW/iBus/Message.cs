using System;
using System.Text;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class Message
    {
        public static int PacketLengthMin { get { return 5; } }
        public const int PacketLengthMax = 258;

        byte source;
        byte destination;
        byte[] data;
        byte check;

        byte[] packet;
        int packetLength;
        string packetDump;
        string dataDump;
        protected DeviceAddress sourceDevice = DeviceAddress.Unset;
        protected DeviceAddress destinationDevice = DeviceAddress.Unset;
        PerformanceInfo performanceInfo;

        public Message(DeviceAddress source, DeviceAddress destination, params byte[] data)
            : this(source, destination, null, data)
        {
        }

        public Message(DeviceAddress source, DeviceAddress destination, string description, params byte[] data)
        {
            if (source == DeviceAddress.Unset || source == DeviceAddress.Unknown)
            {
                throw new ArgumentException("Wrong source device");
            }
            if (destination == DeviceAddress.Unset || destination == DeviceAddress.Unknown)
            {
                throw new ArgumentException("Wrong destination device");
            }
            init((byte)source, (byte)destination, data, description);
            sourceDevice = source;
            destinationDevice = destination;
        }

        public Message(byte source, byte destination, params byte[] data)
            : this(source, destination, null, data)
        {
        }

        public Message(byte source, byte destination, string description, params byte[] data)
        {
            init(source, destination, data, description);
        }

        void init(byte source, byte destination, byte[] data, string description = null)
        {
            // packet = source + length + destination + data + chksum
            //                            |   ===== length =====    |
            
            byte check = 0x00;
            check ^= source;
            check ^= (byte)(data.Length + 2);
            check ^= destination;
            foreach (byte b in data)
            {
                check ^= b;
            }

            init(source, destination, data, data.Length + 4, check, description);  
        }

        protected void init(byte source, byte destination, byte[] data, int packetLength, byte check, string description = null) 
        {
            if (source.IsInternal() || destination.IsInternal())
            {
                throw new Exception("iBus messages are not for internal devices.");
            }

            this.source = source;
            this.destination = destination;
            Data = data;
            ReceiverDescription = description;
            PacketLength = packetLength;
            CRC = check;
        }

        public static Message TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet, length))
            {
                return null;
            }

            return new Message(packet[0], packet[2], packet.SkipAndTake(3, ParseDataLength(packet)));
        }

        protected delegate int IntFromByteArray(byte[] packet);

        public static bool IsValid(byte[] packet, int length = -1)
        {
            return IsValid(packet, ParsePacketLength, length);
        }

        protected static bool IsValid(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (length < PacketLengthMin)
            {
                return false;
            }

            int packetLength = packetLengthCallback(packet);
            if (length < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            byte check = 0x00;
            for (int i = 0; i < packetLength - 1; i++)
            {
                check ^= packet[i];
            }
            return check == packet[packetLength - 1];
        }

        public static bool CanStartWith(byte[] packet, int length = -1)
        {
            return CanStartWith(packet, ParsePacketLength, length);
        }

        protected static bool CanStartWith(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }

            int packetLength = packetLengthCallback(packet);
            if (packetLength > -1 && packetLength < PacketLengthMin)
            {
                return false;
            }

            if (length < PacketLengthMin)
            {
                return true;
            }

            if (length >= packetLength && !IsValid(packet, packetLengthCallback, length))
            {
                return false;
            }

            return true;
        }

        protected static int ParsePacketLength(byte[] packet)
        {
            if (packet.Length < 2)
            {
                return -1;
            }
            return packet[1] + 2;
        }

        protected static int ParseDataLength(byte[] packet)
        {
            return ParsePacketLength(packet) - 4;
        }

        public byte CRC
        {
            get
            {
                return check;
            }
            private set
            {
                check = value;
            }
        }

        public int PacketLength
        {
            get
            {
                return packetLength;
            }
            private set
            {
                packetLength = value;
            }
        }

        public byte[] Packet
        {
            get
            {
                if (this.packet != null)
                {
                    return this.packet;
                }

                byte[] packet = new byte[PacketLength];
                packet[0] = source;
                packet[1] = (byte)(PacketLength - 2);
                packet[2] = destination;
                data.CopyTo(packet, 3);
                packet[PacketLength - 1] = check;

                this.packet = packet;
                return packet;
            }
        }

        public byte[] Data
        {
            get
            {
                return data;
            }
            private set
            {
                data = value;
            }
        }

        public String PacketDump
        {
            get
            {
                if (packetDump == null)
                {
                    packetDump = Packet.ToHex(' ');
                }
                return packetDump;
            }
        }

        public String DataDump
        {
            get
            {
                if (dataDump == null)
                {
                    dataDump = data.ToHex(' ');
                }
                return dataDump;
            }
        }

        public DeviceAddress SourceDevice {
            get
            {
                if (sourceDevice == DeviceAddress.Unset)
                {
                    try
                    {
                        sourceDevice = (DeviceAddress)source;
                    }
                    catch (InvalidCastException)
                    {
                        sourceDevice = DeviceAddress.Unknown;
                    }
                }
                return sourceDevice;
            }
        }


        public virtual DeviceAddress DestinationDevice
        {
            get
            {
                if (destinationDevice == DeviceAddress.Unset)
                {
                    try
                    {
                        destinationDevice = (DeviceAddress)destination;
                    }
                    catch (InvalidCastException)
                    {
                        destinationDevice = DeviceAddress.Unknown;
                    }
                }
                return destinationDevice;
            }
        }

        public virtual bool Compare(Message message)
        {
            if (message == null)
            {
                return false;
            }
            return SourceDevice == message.SourceDevice
                && DestinationDevice == message.DestinationDevice
                && Data.Compare(message.Data);
        }

        public virtual bool Compare(byte[] packet)
        {
            return Packet.Compare(packet);
        }

        /// <summary>
        /// Description of the message set by a receiver or by MessageRegistry
        /// </summary>
        public string ReceiverDescription { get; set; }

        /// <summary>
        /// Custom delay after sending the message in milliseconds. Zero = default (20ms).
        /// </summary>
        public byte AfterSendDelay { get; set; }

        public PerformanceInfo PerformanceInfo
        {
            get
            {
                if (performanceInfo == null)
                {
                    performanceInfo = new PerformanceInfo();
                }
                return performanceInfo;
            }
        }

        public override string ToString()
        {
            return this.ToPrettyString();
        }
    }

    public class PerformanceInfo 
    {
        /// <summary>
        /// Time when the message was enqueued.
        /// Available only when debugging
        /// </summary>
        public DateTime TimeEnqueued { get; set; }

        /// <summary>
        /// Time when the message was started processing.
        /// Available only when debugging
        /// </summary>
        public DateTime TimeStartedProcessing { get; set; }

        /// <summary>
        /// Time when the message was ended processing.
        /// Available only when debugging
        /// </summary>
        public DateTime TimeEndedProcessing { get; set; }

        public override string ToString()
        {
            // TODO Change to Cpu.SystemClock
            if (TimeStartedProcessing != default(DateTime))
            {
                string s = "";
                if (TimeEndedProcessing != default(DateTime))
                {
                    TimeSpan span = TimeEndedProcessing - TimeStartedProcessing;
                    s = "Processed: " + span.GetTotalSeconds() + "." + span.Milliseconds.ToString().PrependToLength(3, '0'); // TODO use string format
                }
                if (TimeEnqueued != default(DateTime))
                {
                    if (s != "")
                    {
                        s += " + ";
                    }
                    TimeSpan span = TimeStartedProcessing - TimeEnqueued;
                    s += "In queue: " + span.GetTotalSeconds() + "." + span.Milliseconds.ToString().PrependToLength(3, '0'); // TODO use string format
                }
                return s;
            }
            return String.Empty;
        }

    }
}
