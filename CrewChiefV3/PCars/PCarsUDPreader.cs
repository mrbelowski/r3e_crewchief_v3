﻿using System;
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
        private GCHandle handle;
        private Boolean initialised = false;
        private List<CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper> dataToDump;
        private CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private int udpPort = 5606; // TODO: get from properties

        private pCarsAPIStruct workingGameState = new pCarsAPIStruct();
        private pCarsAPIStruct currentGameState = new pCarsAPIStruct();
        private pCarsAPIStruct previousGameState = new pCarsAPIStruct();

        private byte[] receivedDataBuffer;

        private IPEndPoint broadcastAddress;
        private UdpClient udpClient;

        private byte[] timsDumpFileData = File.ReadAllBytes("c:/projects/udp.bin");

        Boolean readFromTimsDumpFile = true;

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
            this.udpClient.Client.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, ReceiveCallback, this.udpClient.Client);

            if (readFromTimsDumpFile) 
            {
                ThreadStart readFileWork = readTimsFile;
                Thread readFileThread = new Thread(readFileWork);
                readFileThread.Start();
            }
            initialised = true;

            return true;
        }

        private void readTimsFile()
        {
            int offset = 0;
            while (offset < timsDumpFileData.Count())
            {
                lock (this)
                {
                    offset = readFromOffset(offset, timsDumpFileData);
                    Thread.Sleep(20);
                }
            }
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
                //Restablish the callback
                socket.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch (Exception e)
            {
                //Handle error
            }
        }

        public override Object ReadGameData(Boolean allowRecording)
        {
            CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper structWrapper = new CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper();
            structWrapper.ticksWhenRead = DateTime.Now.Ticks;
            lock (this)
            {
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                previousGameState = currentGameState;
                currentGameState = StructHelper.ClonePCarsAPIStruct(workingGameState);
            }
            structWrapper.data = currentGameState;
            if (allowRecording && dumpToFile && dataToDump != null && currentGameState.mTrackLocation != null &&
                currentGameState.mTrackLocation.Length > 0)
            {
                dataToDump.Add(structWrapper);
            }
            return structWrapper;                       
        }

        private int readFromOffset(int offset, byte[] rawData)
        {
            // the first 2 bytes are the version - discard it for now
            int frameType = rawData[offset + 2];
            int frameLength = 0;
            if (frameType == 0)
            {
                frameLength = 941;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sTelemetryData telem = (sTelemetryData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sTelemetryData));
                workingGameState = StructHelper.MergeWithExistingState(workingGameState, telem);
                handle.Free();
            }
            else if (frameType == 1)
            {
                frameLength = 1347;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sParticipantInfoStrings strings = (sParticipantInfoStrings)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStrings));
                workingGameState = StructHelper.MergeWithExistingState(workingGameState, strings);
                handle.Free();
            }
            else if (frameType == 2)
            {
                frameLength = 1028;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sParticipantInfoStringsAdditional additional = (sParticipantInfoStringsAdditional)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStringsAdditional));
                workingGameState = StructHelper.MergeWithExistingState(workingGameState, additional);
                handle.Free();
            }
            return frameLength + offset;
        }

        public override void Dispose()
        {
            udpClient.Close();
        }
    }
}
