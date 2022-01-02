using System.Net;
using System.Text.RegularExpressions;

namespace BowieD.UnturnedRUS;

public static class Steam
{
	public static string GetUrlTitle(string url)
	{
		using WebClient webClient = new WebClient();
		return Regex.Match(webClient.DownloadString(url), "\\<title\\b[^>]*\\>\\s*(?<Title>[\\s\\S]*?)\\</title\\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
	}

	public static string GetModNameByTitle(string title)
	{
		return title.Substring(title.LastIndexOf(':')).Substring(1);
	}
}
