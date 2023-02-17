using System.Xml.Schema;
using System.Xml;
using System.Xml.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

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
      double x_min = double.Parse(config.Element("x_min")?.Value!);
      double x_max = double.Parse(config.Element("x_max")?.Value!);
      double y_min = double.Parse(config.Element("y_min")?.Value!);
      double y_max = double.Parse(config.Element("y_max")?.Value!);
      int iteration = int.Parse(config.Element("iteration")?.Value!);
      int threshold = int.Parse(config.Element("threshold")?.Value!);
      string output_path = config.Element("output_path")?.Value!;

      var image = new Image<Rgba32>(width, height);

      // スレッド数
      int threadCount = Environment.ProcessorCount;

      // スレッドごとの処理範囲
      int range = height / threadCount;

      // スレッドごとに処理を開始する
      Thread[] threads = new Thread[threadCount];
      for (int i = 0; i < threadCount; i++)
      {
        int start = i * range;
        int end = (i == threadCount - 1) ? height : start + range;
        threads[i] = new Thread(() =>
        {
          for (int py = start; py < end; py++)
          {
            double y = py / (double)height * (y_max - y_min) + y_min;
            for (int px = 0; px < width; px++)
            {
              double x = px / (double)width * (x_max - x_min) + x_min;
              Complex z = new(x, y);
              Rgba32 color = (new Func<Rgba32>(() =>
              {
                Complex v = new(0, 0);
                for (int n = 0; n < iteration; n++)
                {
                  v = v * v + z;
                  if (v.Magnitude > 2)
                  {
                    var a = (byte)(255 - (double)threshold * n);
                    return new Rgba32(a, a, a, 255);
                  }
                }
                return new Rgba32(0, 0, 0, 255); // 収束しない場合は黒色を返す
              }))();
              image[px, py] = color;
            }
          }
        });
        threads[i].Start();
      }

      // スレッドの終了を待つ
      foreach (Thread thread in threads)
      {
        thread.Join();
      }

      image.Save(output_path);

      return 0;
    } catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
      return 1;
    }
  }
}
