
using System;

namespace System.IO
{
	/// <summary>
	/// Description of PacketWriter.
	/// </summary>
	public class PacketWriter : BinaryWriter
	{
		protected MemoryStream m_stream;
		protected int m_PacketByteLength = 4;
		/// <summary>
		/// Contains the packet length
		/// </summary>
		public byte[] Content{
			get{
				return GetContent();
			}
		}
		public int PacketByteLength{
			get{return m_PacketByteLength;}
		}
		
		public PacketWriter(int packetByteLength):base(new MemoryStream())
		{
			m_PacketByteLength = (packetByteLength == 2 )?2:4;
			m_stream = (MemoryStream)OutStream;
		}
		
		public void SetPosition(int pos){
			Seek(pos, SeekOrigin.Begin);
		}
		/// <summary>
		/// Add packet length
		/// </summary>
		private byte[] GetContent(){
			byte[] content = null;
			byte[] raw = m_stream.ToArray();
			using(MemoryStream stream = new MemoryStream(raw.Length + m_PacketByteLength)){
				using(BinaryWriter writer = new BinaryWriter(stream)){
					writer.Write((ushort)raw.Length);
					writer.Write(raw);
				}
				content = stream.ToArray();
			}
			return content;
		}
	}
}
