using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core
{
    public static class AppSettingsHelper
    {
        private static IConfiguration _config;

        public static void AppSettingsConfigure(IConfiguration config)
        { 
        _config= config;
        }

        public static string GetSetting(string Key)
        {
            return _config.GetSection(Key).Value;
        }
    }
}
