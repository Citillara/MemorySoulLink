using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MemorySoulLink
{
    internal class XmlTools
    {
        public static T FromXml<T>(Stream data)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
            T s = (T)xsSubmit.Deserialize(data);

            data.Close();
            data.Dispose();
            return s;
        }

        public static void ToXml<T>(object o, Stream destStream)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.NewLineOnAttributes = false;
            using (XmlWriter writer = XmlWriter.Create(destStream, xmlWriterSettings))
            {
                xsSubmit.Serialize(writer, o);
                writer.Flush();
            }
            if (destStream != null)
            {
                destStream.Close();
                destStream.Dispose();
            }

        }

        public static void WriteSchema<T>(string path)
        {

            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                XmlSchemas schemas = new XmlSchemas();
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemas);
                XmlTypeMapping mapping = new XmlReflectionImporter().ImportTypeMapping(typeof(T));
                exporter.ExportTypeMapping(mapping);
                XmlTextWriter xwriter = new XmlTextWriter(file, new UTF8Encoding());
                xwriter.Formatting = Formatting.Indented;


                foreach (XmlSchema schema in schemas)
                {
                    schema.Write(xwriter);
                }

            }
        }
    }
}
