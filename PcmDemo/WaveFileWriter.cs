using System;
using System.IO;

namespace PcmDemo
{
	public class WaveFileWriter
	{
		private Stream _stream;
		private BinaryWriter _binaryWriter;
		private uint _dataSize = 0;
		private const uint HEADERSIZE = 8;
		private const uint FORMATDATASIZE = 18;
		private const uint WAVESIZE = 4;
		private const short FORMAT = 1;

		public WaveFileWriter(Stream stream,ushort channels,uint samplesPerSecond,uint averageBytesPerSecond,ushort blockAlign,ushort bitsPerSample)
		{
			_stream = stream;
			_binaryWriter = new BinaryWriter(stream,System.Text.Encoding.ASCII);

			WriteRiffHeader();
			WriteFormatHeader(channels,samplesPerSecond,averageBytesPerSecond,blockAlign,bitsPerSample);
		}

		private void WriteRiffHeader()
		{
			_binaryWriter.Seek(0,SeekOrigin.Begin);
			_binaryWriter.Write(0x46464952);//RIFF
			_binaryWriter.Write(WAVESIZE + HEADERSIZE + FORMATDATASIZE + HEADERSIZE + _dataSize);
			_binaryWriter.Write(0x45564157);//WAVE
		}

		private void WriteFormatHeader(ushort channels,uint samplesPerSecond,uint averageBytesPerSecond,ushort blockAlign,ushort bitsPerSample)
		{
			_binaryWriter.Seek(12,SeekOrigin.Begin);
			_binaryWriter.Write(0x20746D66);//fmt 4
			_binaryWriter.Write(FORMATDATASIZE);
			_binaryWriter.Write(FORMAT);
			_binaryWriter.Write(channels);
			_binaryWriter.Write(samplesPerSecond);
			_binaryWriter.Write(averageBytesPerSecond);
			_binaryWriter.Write(blockAlign);
			_binaryWriter.Write(bitsPerSample);
		}

		public void WriteData(byte[] data,int index, int count)
		{
			_dataSize+=(uint)count;

			WriteRiffHeader();	
			WriteDataHeader();

			_binaryWriter.Seek(0,SeekOrigin.End);
			_binaryWriter.Write(data,index,count);
			
		}

		private void WriteDataHeader()
		{
			_binaryWriter.Seek(37,SeekOrigin.Begin);
			_binaryWriter.Write("data");
			_binaryWriter.Write(_dataSize);			
		}

		public void Close()
		{
			_binaryWriter.Close();
			_stream.Close();
		}
	}
}
