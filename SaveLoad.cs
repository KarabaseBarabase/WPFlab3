using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WPFlab3
{
    [Serializable]
    public class SettingsManager
    {
        //private string SettingsFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\userSettings.xml";

        public void SaveSettings(List<NodeDTO> graph, string SettingsFilePath)
        {
            //var settings = new JsonSerializerSettings
            //{
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            //};
            string json = JsonConvert.SerializeObject(graph, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }
        public List<NodeDTO> LoadSettings(string SettingsFilePath)
        {
            if (!File.Exists(SettingsFilePath))
                throw new FileNotFoundException("Файл не найден", SettingsFilePath);

            string json = File.ReadAllText(SettingsFilePath);
            return JsonConvert.DeserializeObject<List<NodeDTO>>(json);
        }
    }
}
