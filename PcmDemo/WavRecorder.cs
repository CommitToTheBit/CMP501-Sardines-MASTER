using System;
using System.Threading;
using Microsoft.DirectX.DirectSound;
using System.IO;

namespace PcmDemo
{
	public class WavRecorder
	{
		private object _lockObject = new object();
		private bool _stopRecording = false;
		private CaptureBufferDescription _bufferDescription;
		private AutoResetEvent _resetEvent;
		private Notify _notify;
		private CaptureBuffer _buffer;
		private int _bufferSize;
		private string _fileName;
		
		public WavRecorder(CaptureBufferDescription bufferDescription, Capture capture, string fileName)
		{
			_fileName = fileName;
			_bufferSize = bufferDescription.BufferBytes;			
			_bufferDescription = bufferDescription;
			_buffer = new CaptureBuffer(bufferDescription,capture);
			
			CreateNotifyPositions();
		}

		private void CreateNotifyPositions()
		{
			_resetEvent = new AutoResetEvent(false);
			_notify = new Notify(_buffer);
			BufferPositionNotify bufferPositionNotify1 = new BufferPositionNotify();
			bufferPositionNotify1.Offset = _bufferSize / 2 - 1;
			bufferPositionNotify1.EventNotifyHandle = _resetEvent.Handle;				
			BufferPositionNotify bufferPositionNotify2 = new BufferPositionNotify();
			bufferPositionNotify2.Offset = _bufferSize - 1;
			bufferPositionNotify2.EventNotifyHandle = _resetEvent.Handle;				

			_notify.SetNotificationPositions(new BufferPositionNotify[]{bufferPositionNotify1,bufferPositionNotify2});
		}

		public void Start()
		{
			int halfBuffer = _bufferSize / 2;

			lock (_lockObject)
			{
				_stopRecording = false;
			}

			FileStream file = null;
			WaveFileWriter fileWriter = null;
			file = new FileStream(_fileName,FileMode.Create,FileAccess.Write,FileShare.Read,1024);
			WaveFormat waveFormat = _bufferDescription.Format;
			fileWriter = new WaveFileWriter(file,(ushort)waveFormat.Channels,(uint)waveFormat.SamplesPerSecond,(uint)waveFormat.AverageBytesPerSecond,(ushort)waveFormat.BlockAlign,(ushort)waveFormat.BitsPerSample);
			
			try
			{
				_buffer.Start(true);			
				
				bool readFirstBufferPart = true;
				int offset = 0;
				
				MemoryStream memStream = new MemoryStream(halfBuffer);

				while (true)
				{
					_resetEvent.WaitOne();
					memStream.Seek(0,SeekOrigin.Begin);
					_buffer.Read(offset,memStream,halfBuffer,LockFlag.None);
					readFirstBufferPart = !readFirstBufferPart;
					offset = readFirstBufferPart ? 0 : halfBuffer;
					
					byte[] dataToWrite = memStream.GetBuffer();
					
					fileWriter.WriteData(dataToWrite,0,halfBuffer);

					lock (_lockObject)
					{
						if (_stopRecording)
						{
							_buffer.Stop();
							break;
						}
					}				
				}
			}
			finally
			{
				if (fileWriter != null)	fileWriter.Close();
				if (file != null) file.Close();
			}
		}

		public void Stop()
		{
			lock (_lockObject)
			{
				_stopRecording = true;
			}
		}
	}
}
