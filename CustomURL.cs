using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace BowieD.UnturnedRUS;

public class CustomURL
{
	[XmlAttribute]
	public string url;

	[XmlAttribute]
	public string name;

	[XmlIgnore]
	public MenuItem AsMenuItem => new MenuItem
	{
		Header = name,
		Icon = new Image
		{
			Source = new BitmapImage(new Uri("pack://application:,,,/Resources/" + name + ".png"))
		},
		Tag = url
	};
}
