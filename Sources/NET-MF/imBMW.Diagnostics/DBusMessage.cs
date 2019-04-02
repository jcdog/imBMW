using System;
using imBMW.Tools;
using System.Text;
using imBMW.iBus;

namespace imBMW.Diagnostics
{
    /// <summary>
    /// BMW DS2 Diagnostic Bus (DBus) message packet
    /// </summary>
    public class DBusMessage : Message
    {
        byte[] packet;
        byte check;
        int packetLength;

        public static new int PacketLengthMin { get { return 4; } }

        string dataString;
        
        public DBusMessage(DeviceAddress device, params byte[] data)
            : this(device, null, data)
        { }

        public DBusMessage(DeviceAddress device, string description, params byte[] data)
            : base (device, DeviceAddress.Diagnostic, description, data)
        {
            // packet = device + length + data + chksum
            //          |     ===== length =====      |

            var packetLength = data.Length + 3;
            byte check = 0x00;
            check ^= (byte)device;
            check ^= (byte)packetLength;
            foreach (byte b in data)
            {
                check ^= b;
            }

            PacketLength = packetLength;
            CRC = check;
        }

        public DeviceAddress Device
        {
            get
            {
                return SourceDevice;
            }
        }

        public override DeviceAddress DestinationDevice
        {
            get
            {
                return DeviceAddress.Diagnostic;
            }
        }

        public string DataString
        {
            get
            {
                if (dataString == null)
                {
                    dataString = Encoding.UTF8.GetString(Data);
                }
                return dataString;
            }
        }

        public new byte CRC
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

        public new int PacketLength
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

        public new byte[] Packet
        {
            get
            {
                if (this.packet != null)
                {
                    return this.packet;
                }

                byte[] packet = new byte[PacketLength];
                packet[0] = (byte)Device;
                packet[1] = (byte)(PacketLength);
                Data.CopyTo(packet, 2);
                packet[PacketLength - 1] = CRC;

                this.packet = packet;
                return packet;
            }
        }

        public bool Compare(DBusMessage message)
        {
            return Device == message.Device && Data.Compare(message.Data);
        }

        public Message ToIBusMessage()
        {
            return new Message(SourceDevice, DestinationDevice, ReceiverDescription, Data);
        }

        public static new Message TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet))
            {
                return null;
            }

            return new DBusMessage((DeviceAddress)packet[0], packet.SkipAndTake(2, ParseDataLength(packet)));
        }
        
        public static new bool IsValid(byte[] packet, int length = -1)
        {
            return IsValid(packet, ParsePacketLength, length);
        }

        protected static new bool IsValid(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (length < PacketLengthMin)
            {
                return false;
            }

            byte packetLength = (byte)ParsePacketLength(packet);
            if (length < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            byte check = 0x00;
            for (byte i = 0; i < packetLength - 1; i++)
            {
                check ^= packet[i];
            }
            return check == packet[packetLength - 1];
        }

        public static new bool CanStartWith(byte[] packet, int length = -1)
        {
            return CanStartWith(packet, ParsePacketLength, length);
        }

        protected static new bool CanStartWith(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }

            var packetLength = packetLengthCallback(packet);
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

        protected static new int ParsePacketLength(byte[] packet)
        {
            if (packet.Length < 2)
            {
                return -1;
            }
            return packet[1];
        }

        protected static new int ParseDataLength(byte[] packet)
        {
            return ParsePacketLength(packet) - 3;
        }
    }
}