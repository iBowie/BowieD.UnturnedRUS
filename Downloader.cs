using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BowieD.UnturnedRUS;

public static class Downloader
{
	public static void DownloadFile(string url, string fileName)
	{
		using WebClient webClient = new WebClient();
		webClient.DownloadFile(url, fileName);
	}

	public static string DownloadString(string url)
	{
		using WebClient webClient = new WebClient();
		webClient.Encoding = Encoding.UTF8;
		return webClient.DownloadString(url);
	}

	public static T DownloadAndDeserialize<T>(string url) where T : class
	{
		using WebClient webClient = new WebClient();
		webClient.Encoding = Encoding.UTF8;
		using TextReader input = new StringReader(webClient.DownloadString(url));
		XmlReader xmlReader = XmlReader.Create(input);
		return new XmlSerializer(typeof(T)).Deserialize(xmlReader) as T;
	}
}
