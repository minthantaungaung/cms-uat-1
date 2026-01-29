using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public interface ITemplateLoader
    {
        public IDictionary<string, NotificationMessage> GetNotiMsgListJson();
        public IDictionary<string, LocalizationMessage> GetLocalizationJson();
    }
    public class TemplateLoader : ITemplateLoader
    {
        private readonly IDictionary<string, NotificationMessage> NotificationCollection;
        private readonly IDictionary<string, LocalizationMessage> LocaleCollection;

        public TemplateLoader()
        {
            try
            {
                string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "notificationmessages.json");
                var jsonString = System.IO.File.ReadAllText(filePath);
                NotificationCollection = JsonConvert.DeserializeObject<IDictionary<string, NotificationMessage>>(jsonString);                
            }
            catch { }

            try {
                string localeFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "localizationmessage.json");
                var localeJsonString = System.IO.File.ReadAllText(localeFilePath);
                LocaleCollection = JsonConvert.DeserializeObject<IDictionary<string, LocalizationMessage>>(localeJsonString);
            } catch { }
        }

        public IDictionary<string, NotificationMessage> GetNotiMsgListJson()
        {
            return NotificationCollection;
        }

        IDictionary<string, LocalizationMessage> ITemplateLoader.GetLocalizationJson()
        {
            return LocaleCollection;
        }
    }
}
