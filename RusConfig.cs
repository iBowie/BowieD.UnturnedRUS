using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BowieD.UnturnedRUS;

public class RusConfig
{
	public CustomURL[] urls;

	[XmlArray("mods")]
	[XmlArrayItem("mod")]
	public LocalizedMod[] mods;

	public Donation[] donationList;

	[XmlAttribute]
	public string unturnedVersion { get; set; }

	[XmlAttribute]
	public string rusVersion { get; set; }

	[XmlAttribute]
	public string installerVersion { get; set; }

	[XmlAttribute]
	public string modVersion { get; set; }

	public string modDownloadURL { get; set; }

	public string cityDownloadURL { get; set; }

	public string baseDownloadURL { get; set; }

	public string qiwiNumber { get; set; }

	public string webMoneyNumber { get; set; }

	public string message { get; set; }

	public float minDonation { get; set; }

	[XmlIgnore]
	public List<Donation> Donations => donationList.ToList();
}
