using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CrewChiefV3
{
    abstract class GameDataReader
    {
        protected String filenameToDump;
        
        public Boolean dumpToFile = false;        

        protected abstract Boolean InitialiseInternal();

        public abstract Object ReadGameData();
                
        public abstract void Dispose();

        public abstract void DumpRawGameData();

        public abstract Object ReadGameDataFromFile(String filename);

        public Boolean Initialise()
        {
            Boolean initialised = InitialiseInternal();
            if (initialised && dumpToFile)
            {
                Console.WriteLine("initialising");
                filenameToDump = "c:/temp/game_data_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xml";
            }
            return initialised;
        }

        protected void SerializeObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to write raw game data: " + ex.Message);
            }
        }

        protected T DeSerializeObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(T); }

            T objectOut = default(T);

            try
            {
                string attributeXml = string.Empty;

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);
                string xmlString = xmlDocument.OuterXml;

                using (StringReader read = new StringReader(xmlString))
                {
                    Type outType = typeof(T);

                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        objectOut = (T)serializer.Deserialize(reader);
                        reader.Close();
                    }

                    read.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to read raw game data: " + ex.Message);
            }

            return objectOut;
        }
        
    }
}
