using System.Xml.Schema;
using System.Xml;
using System.Xml.Linq;

internal static class Program
{
  internal static int Main()
  {
    try
    {
      string xml_path = "./config.xml";
      string xsd_path = "./config.xsd";

      if (File.Exists(xml_path) == false)
      {
        Console.WriteLine("Could not find XML configuration file.");
        return 1;
      }
      if (File.Exists(xsd_path) == false)
      {
        Console.WriteLine("Could not find XSD configuration file.");
        return 1;
      }

      string xml_content = File.ReadAllText(xml_path);
      string xsd_content = File.ReadAllText(xsd_path);

      bool validation_check = true;

      //XMLスキーマオブジェクトの生成
      XmlSchema schema = new();
      using (StringReader stringReader = new(xsd_content))
      {
        schema = XmlSchema.Read(stringReader, null)!;
      }
      // スキーマの追加
      XmlSchemaSet schemaSet = new();
      schemaSet.Add(schema);

      // XML文書の検証を有効化
      XmlReaderSettings settings = new()
      {
        ValidationType = ValidationType.Schema,
        Schemas = schemaSet
      };
      settings.ValidationEventHandler += (object? sender, ValidationEventArgs e) => {
        if (e.Severity == XmlSeverityType.Warning)
        {
          Console.WriteLine($"Validation Warning ({e.Message})");
        }
        if (e.Severity == XmlSeverityType.Error)
        {
          Console.WriteLine($"Validation Error ({e.Message})");
          validation_check = false;
        }
      };

      // XMLデータの読み込み
      using (StringReader stringReader = new(xml_content))
      using (XmlReader xmlReader = XmlReader.Create(stringReader, settings))
      {
        while (xmlReader.Read()) { }
      }

      if (validation_check == false)
      {
        Console.WriteLine("Validation failed...");
        return 1;
      }

      // 設定ファイルからデータを取得
      XDocument xml_document = XDocument.Parse(xml_content);
      XElement config = xml_document.Element("config")!;
      int width = int.Parse(config.Element("width")?.Value!);
      int height = int.Parse(config.Element("height")?.Value!);
      double x_min = int.Parse(config.Element("x_min")?.Value!);
      double x_max = int.Parse(config.Element("x_max")?.Value!);
      double y_min = int.Parse(config.Element("y_min")?.Value!);
      double y_max = int.Parse(config.Element("y_max")?.Value!);
      int iteration = int.Parse(config.Element("iteration")?.Value!);
      int threshold = int.Parse(config.Element("threshold")?.Value!);
      string output_path = config.Element("output_path")?.Value!;

      return 0;
    } catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
      return 1;
    }
  }

}
