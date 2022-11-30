/*
*	CITATION: https://web.archive.org/web/20071011040128/http://dotnet.org.za/colin/archive/2006/09/22/Using-DirectSound-to-record-PCM-data.aspx 
*/

using System;
using Microsoft.DirectX.DirectSound;
using System.Threading;

namespace PcmDemo
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			CaptureDevicesCollection captureDeviceCollection = new CaptureDevicesCollection();

			//Check that there is a capture device
			if (captureDeviceCollection.Count == 0) 
				throw new Exception("No Capture Devices");
			
			//Using first one
			DeviceInformation deviceInfo = captureDeviceCollection[0];

			using (Capture capture = new Capture(deviceInfo.DriverGuid))
			{
				//Check that we are able to record at 22KHz Stereo 16Bits per sample (fairly common)			
				CaptureCaps caps = capture.Caps;
				if (!caps.Format22KhzStereo16Bit)
					throw new Exception("Unable to record at 22KHz Stereo 16Bits");
				
				short channels = 2; //Stereo
				short bitsPerSample = 16; //16Bit, alternatively use 8Bits
				int samplesPerSecond = 22050; //11KHz use 11025 , 22KHz use 22050, 44KHz use 44100 etc

				//Set up the wave format to be captured
				WaveFormat waveFormat = new WaveFormat();
				waveFormat.Channels =  channels;
				waveFormat.FormatTag = WaveFormatTag.Pcm;
				waveFormat.SamplesPerSecond = samplesPerSecond;
				waveFormat.BitsPerSample = bitsPerSample;
				waveFormat.BlockAlign = (short)(channels * (bitsPerSample / (short)8));
				waveFormat.AverageBytesPerSecond = waveFormat.BlockAlign * samplesPerSecond;
				
				CaptureBufferDescription bufferDescription = new CaptureBufferDescription();			
				bufferDescription.BufferBytes = waveFormat.AverageBytesPerSecond / 5;//approx 200 milliseconds of pcm data
				bufferDescription.Format = waveFormat;

				WavRecorder recorder = new WavRecorder(bufferDescription,capture,"temp.wav");

				ThreadStart recorderStart = new ThreadStart(recorder.Start);
				Thread recorderThread = new Thread(recorderStart);
				recorderThread.Start();
				Thread.Sleep(10000); //Record 30 seconds of sound
				recorder.Stop();
				recorderThread.Join();
			}
		}		
	}
}
