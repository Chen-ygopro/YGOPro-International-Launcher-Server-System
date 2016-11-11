using System;
using System.IO;

namespace System.Xml
{
	public class ConfigManager
	{
		public static string XmlFile = System.Windows.Forms.Application.ExecutablePath + ".config";
		
		#region Read the contents
		/// <summary>
		/// Reads a string value
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string readString(string key,string def="")
		{
			string val = GetAppConfig(key);
            if (string.IsNullOrEmpty(val))
            {
                return def;
            }
            return val;
		}
		/// <summary>
		/// Int value read
		/// </summary>
		/// <param name="key"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static int readInteger(string key, int def)
		{
			int i;
			if (int.TryParse(readString(key), out i))
				return i;
			return def;
		}
		/// <summary>
		/// Float value read
		/// </summary>
		/// <param name="key"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static float readFloat(string key, float def)
		{
			float i;
			if (float.TryParse(readString(key), out i))
				return i;
			return def;
		}
		/// <summary>
		/// Reads an array of int
		/// </summary>
		/// <param name="key"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static int[] readIntegers(string key, int length)
		{
			string temp = readString(key);
			string[] ws = string.IsNullOrEmpty(temp) ? null : temp.Split(',');
			length = Math.Max(ws.Length,length);
			int[] ints = new int[Math.Max(ws.Length,length)];
			if (ws != null && ws.Length > 0 && ws.Length <= length)
			{
				for (int i = 0; i < ws.Length; i++)
				{
					int.TryParse(ws[i], out ints[i]);
				}
			}
			return ints;
		}
		/// <summary>
		/// Reading Boolean
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static bool readBoolean(string key,bool def = false)
		{
            string val = readString(key).ToLower();
            if (string.IsNullOrEmpty(key)) {
                return def;
            }
            if (val == "true")
				return true;
			else
				return false;
		}
		#endregion
		#region XMLAction config
		/// <summary>
		/// Save value
		/// </summary>
		/// <param name="appKey"></param>
		/// <param name="appValue"></param>
		public static void Save(string appKey, string appValue)
		{
			string file = XmlFile;
			if(!File.Exists(file)){
				return;
			}
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(file);
			XmlNode xNode = xDoc.SelectSingleNode("//appSettings");

			XmlElement xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + appKey + "']");
			if (xElem != null) //There are，Update
				xElem.SetAttribute("value", appValue);
			else//Does not exist，Insert
			{
				XmlElement xNewElem = xDoc.CreateElement("add");
				xNewElem.SetAttribute("key", appKey);
				xNewElem.SetAttribute("value", appValue);
				xNode.AppendChild(xNewElem);
			}
			xDoc.Save(file);
		}
		/// <summary>
		/// Get the value
		/// </summary>
		/// <param name="appKey"></param>
		/// <returns></returns>
		public static string GetAppConfig(string appKey)
		{
			string file = XmlFile;
			if(!File.Exists(file)){
				return string.Empty;
			}
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(file);

			XmlNode xNode = xDoc.SelectSingleNode("//appSettings");

			XmlElement xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + appKey + "']");

			if (xElem != null)
			{
				return xElem.Attributes["value"].Value;
			}
			return string.Empty;
		}
		#endregion
	}
}
