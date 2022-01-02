using System.Xml.Serialization;

namespace BowieD.UnturnedRUS;

public class Donation
{
	[XmlAttribute]
	public string displayName { get; set; }

	[XmlAttribute]
	public float donated { get; set; }

	public override string ToString()
	{
		return displayName + " - " + donated + " р.";
	}
}
