using System;
using System.IO;

namespace Armat.Localization;

public struct TextRecord
{
	[System.Xml.Serialization.XmlAttribute]
	public String Key;
	[System.Xml.Serialization.XmlAttribute]
	public String Value;
};

[System.Xml.Serialization.XmlRoot(ElementName = LocalizationDocument.RootXmlElementName)]
public class LocalizationDocument
{
	public const String RootXmlElementName = "LocalizableStringDictionary";

	[System.Xml.Serialization.XmlElement(ElementName = "String")]
	public TextRecord[]? Records;

	public static LocalizationDocument? Load(Stream stream)
	{
		System.Xml.Serialization.XmlSerializer serializer = new(typeof(LocalizationDocument));

		// prohibit DTD processing - translation files never need it, and it protects
		// against XML entity-expansion attacks when loading untrusted files
		System.Xml.XmlReaderSettings readerSettings = new()
		{
			DtdProcessing = System.Xml.DtdProcessing.Prohibit
		};
		using System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stream, readerSettings);

		// de-serialize from stream
		return serializer.Deserialize(xmlReader) as LocalizationDocument;
	}
	public void Save(Stream stream)
	{
		System.Xml.Serialization.XmlSerializer serializer = new(typeof(LocalizationDocument));

		// create the XML writer object
		var xmlWriterSetting = new System.Xml.XmlWriterSettings()
		{
			Indent = true,
			IndentChars = "    ",
			OmitXmlDeclaration = true
		};
		using System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(stream, xmlWriterSetting);

		// serialize into the stream
		serializer.Serialize(xmlWriter, this);
	}
}
