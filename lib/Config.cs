using System;
using System.IO;
using System.Xml;

namespace MyMap
{
    class Config
    {
        public static string Read(string key)
        {
            key = System.Web.HttpUtility.HtmlEncode(key);
            if (!File.Exists(xml_path))
            {
                return "";
            }
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.Load(xml_path);
            XmlNode root = doc.DocumentElement;
            try
            {
                XmlNode nod = root.SelectSingleNode("/config/" + key);
                if (nod != null)
                {
                    return System.Web.HttpUtility.HtmlDecode(nod.InnerText);
                }
                return string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return string.Empty;
            }
        }

        public static void Write(string key, string value)
        {
            key = System.Web.HttpUtility.HtmlEncode(key);
            value = System.Web.HttpUtility.HtmlEncode(value);

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            XmlNode root;
            if (!File.Exists(xml_path))
            {
                doc.CreateXmlDeclaration("1.0", "utf-8", "yes");
                root = doc.CreateElement("config");
                XmlNode node = doc.CreateElement(key);
                node.InnerText = value;
                root.AppendChild(node);
                doc.AppendChild(root);
                doc.Save(xml_path);
                return;
            }

            doc.Load(xml_path);
            root = doc.DocumentElement;
            if (root == null)
            {
                root = doc.CreateElement("config");
                doc.AppendChild(root);
            }
            XmlNode nod;
            try
            {
                nod = root.SelectSingleNode("/config/" + key);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return;
            }

            if (nod == null)
            {
                nod = doc.CreateElement(key);
                nod.InnerText = value;
                root.AppendChild(nod);
                doc.Save(xml_path);
                return;
            }
            if (nod.InnerText != value)
            {
                nod.InnerText = value;
                doc.Save(xml_path);
            }
        }
        private static string xml_path = AppDomain.CurrentDomain.BaseDirectory + "config.xml";
    }
}
