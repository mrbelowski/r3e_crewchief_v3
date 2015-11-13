using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV3.PCars
{
    public class PCarsUDPreader : GameDataReader
    {
        private int sequenceWrapsAt = 63;
        private Boolean strictPacketOrdering = true;
        private Dictionary<int, int> lastSequenceNumberForPacketType = new Dictionary<int, int>
        {
            {0, -1},{1, -1},{2, -1}
        };

        private int telemCount = 0;
        private int strCount = 0;
        private int addStrCount = 0;

        private Boolean newSpotterData = true;
        private Boolean running = false;
        private GCHandle handle;
        private Boolean initialised = false;
        private List<CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper> dataToDump;
        private CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private int udpPort = UserSettings.GetUserSettings().getInt("udp_data_port");

        private pCarsAPIStruct workingGameState = new pCarsAPIStruct();
        private pCarsAPIStruct currentGameState = new pCarsAPIStruct();
        private pCarsAPIStruct previousGameState = new pCarsAPIStruct();

        private const int sParticipantInfoStrings_PacketSize = 1347;
        private const int sParticipantInfoStringsAdditional_PacketSize = 1028;
        private const int sTelemetryData_PacketSize = 1367;

        private byte[] receivedDataBuffer;

        private IPEndPoint broadcastAddress;
        private UdpClient udpClient;

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper>(), filenameToDump);
            }
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null)
            {
                dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper[]>(dataFilesPath + filename);
            }
            if (dataReadFromFile.Length > dataReadFromFileIndex)
            {
                CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                return structWrapperData;
            }
            else
            {
                return null;
            }
        }

        protected override Boolean InitialiseInternal()
        {
            workingGameState.mVersion = 5;
            currentGameState.mVersion = 5;
            previousGameState.mVersion = 5;
            if (dumpToFile)
            {
                dataToDump = new List<CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper>();
            }
            this.broadcastAddress = new IPEndPoint(IPAddress.Any, udpPort);
            this.udpClient = new UdpClient();
            this.udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.udpClient.ExclusiveAddressUse = false; // only if you want to send/receive on same machine.
            this.udpClient.Client.Bind(this.broadcastAddress);
            this.receivedDataBuffer = new byte[this.udpClient.Client.ReceiveBufferSize];
            this.running = true;
            this.udpClient.Client.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, ReceiveCallback, this.udpClient.Client);
            this.initialised = true;
            Console.WriteLine("Listening for UDP data on port " + udpPort);

            return true;
        }
        
        private void ReceiveCallback(IAsyncResult result)
        {
            //Socket was the passed in as the state
            Socket socket = (Socket)result.AsyncState;
            try
            {
                int received = socket.EndReceive(result);
                if (received > 0)
                {
                    // do something with the data
                    lock (this)
                    {
                        // TODO: what's in the header? Is offset 0 correct?
                        readFromOffset(0, this.receivedDataBuffer);
                    }                    
                }                
            }
            catch (Exception e)
            {
                //Handle error
                Console.WriteLine("Error receiving UDP data " + e.StackTrace);
            }
            //Restablish the callback
            if (running)
            {
                socket.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
        }

        private void copyParticipantsArray(pCarsAPIStruct source, pCarsAPIStruct destination)
        {
            if (source.mParticipantData != null)
            {
                destination.mParticipantData = new pCarsAPIParticipantStruct[source.mParticipantData.Count()];
                for (int i = 0; i < source.mParticipantData.Count(); i++)
                {
                    destination.mParticipantData[i] = source.mParticipantData[i];
                }
            }            
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper structWrapper = new CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper();
            structWrapper.ticksWhenRead = DateTime.Now.Ticks;
            lock (this)
            {
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise UDP client");
                    }
                }
                // TODO: figure out the reference / copy semantics of nested structs. Do we actually need to clone this here?
                // does the struct get copied anyway when we pass it around?
                previousGameState = currentGameState;
                copyParticipantsArray(currentGameState, previousGameState);
                currentGameState = workingGameState;
                copyParticipantsArray(workingGameState, currentGameState);
                if (forSpotter)
                {
                    newSpotterData = false;
                }
            }
            structWrapper.data = currentGameState;
            if (!forSpotter && dumpToFile && dataToDump != null && currentGameState.mTrackLocation != null &&
                currentGameState.mTrackLocation.Length > 0)
            {
                dataToDump.Add(structWrapper);
            }
            return structWrapper;                       
        }

        private int readFromOffset(int offset, byte[] rawData)
        {
            // the first 2 bytes are the version - discard it for now
            int frameTypeAndSequence = rawData[offset + 2];
            int frameType = frameTypeAndSequence & 3;
            int sequence = frameTypeAndSequence >> 2;
            int frameLength = 0;
            if (frameType == 0)
            {
                telemCount++;
                frameLength = sTelemetryData_PacketSize;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sTelemetryData telem = (sTelemetryData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sTelemetryData));
                workingGameState = StructHelper.MergeWithExistingState(workingGameState, telem);
                handle.Free();
                newSpotterData = true;                
            }
            else if (frameType == 1)
            {
                strCount++;
                frameLength = sParticipantInfoStrings_PacketSize;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sParticipantInfoStrings strings = (sParticipantInfoStrings)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStrings));
                workingGameState = StructHelper.MergeWithExistingState(workingGameState, strings);
                handle.Free();
            }
            else if (frameType == 2)
            {
                addStrCount++;
                frameLength = sParticipantInfoStringsAdditional_PacketSize;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sParticipantInfoStringsAdditional additional = (sParticipantInfoStringsAdditional)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStringsAdditional));
                workingGameState = StructHelper.MergeWithExistingState(workingGameState, additional);
                handle.Free();
            }
            return frameLength + offset;
        }

        public override void Dispose()
        {
            if (udpClient != null)
            {
                stop();
                udpClient.Close();
            }
        }

        public override bool hasNewSpotterData()
        {
            return newSpotterData;
        }

        public override void stop()
        {
            running = false;
            if (udpClient != null && udpClient.Client != null && udpClient.Client.Connected)
            {
                udpClient.Client.Disconnect(true);
            }
        }

        private Boolean checkSequence(int sequence, int packetNumber)
        {
            if (lastSequenceNumberForPacketType.ContainsKey(sequence))
            {
                int lastSequenceNumber = lastSequenceNumberForPacketType[sequence];
                lastSequenceNumberForPacketType[sequence] = packetNumber;
                if (lastSequenceNumber != -1)
                {
                    if (lastSequenceNumber == sequenceWrapsAt)
                    {
                        if (packetNumber != 0)
                        {
                            Console.WriteLine("Out of order packet - expected sequence number 0, got " + lastSequenceNumber);
                            return false;
                        }
                    }
                    else if (lastSequenceNumber + 1 != packetNumber)
                    {
                        Console.WriteLine("Out of order packet - expected sequence number " + (lastSequenceNumber + 1) + " got " + lastSequenceNumber);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
