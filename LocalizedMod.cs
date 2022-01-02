using System.Xml.Serialization;

namespace BowieD.UnturnedRUS;

public class LocalizedMod
{
	[XmlAttribute]
	public string id;

	[XmlAttribute]
	public string name;
}
