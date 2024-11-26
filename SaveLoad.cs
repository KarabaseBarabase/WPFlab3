using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WPFlab3
{
    public class Settings
    {
        public List<NodeDTO> Nodes {  get; set; }
        public bool IsOriented {  get; set; }
        public Settings() { }
        public Settings(List<NodeDTO> nodes, bool isOreinted) { Nodes = nodes; IsOriented = isOreinted; }
    }
    [Serializable]
    public class SettingsManager
    {
        //private string SettingsFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\userSettings.xml";

        public void SaveSettings(Settings graph, string SettingsFilePath)
        {
            //var settings = new JsonSerializerSettings
            //{
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            //};
            string json = JsonConvert.SerializeObject(graph, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }
        public Settings LoadSettings(string SettingsFilePath)
        {
            if (!File.Exists(SettingsFilePath))
                throw new FileNotFoundException("Файл не найден", SettingsFilePath);

            string json = File.ReadAllText(SettingsFilePath);
            return JsonConvert.DeserializeObject<Settings>(json);
        }
    }
}
