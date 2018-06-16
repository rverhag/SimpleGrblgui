using System;
using System.Collections.Generic;
using System.Linq;

namespace Vhr.Types
{
    public sealed class Settings : Dictionary<string, Setting>
    {
        private static readonly Lazy<Settings> settings = new Lazy<Settings>(() => new Settings());
        public static Settings List
        {
            get { return settings.Value; }
        }

        private Settings()
        {
            foreach (var settingcode in Codes.Setting.Codes)
            {
                var setting = new Setting { Name = settingcode.Key, Description = settingcode.Value };
                Add(settingcode.Key, setting);
            }
        }

        public bool Loaded => this.Where(x => x.Value.Content == null).Count() == 0;
    }

    public class Setting
    {
        public string Name { get; set; }
        public string Description { get; set; }

        private object content = null;
        public object Content
        {
            get
            {
                return content == null ? content : ((Type.ToString().ToLower().Contains("boolean")) ? Convert.ToInt32(content) : Convert.ChangeType(content, Type));
            }
            set
            {
                if (!value.Equals(content))
                {
                    if (content != null )
                    {
                        Update(value);
                    }

                    content = value;
                }
            }
        }

        private Type Type
        {
            get
            {
                if (Description.ToLower().Contains("mask") || Description.ToLower().Contains("rpm") || Description.ToLower().Contains("microseconds") || Description.ToLower().Contains("milliseconds"))
                {
                    return typeof(System.Int16);
                }

                if (Description.ToLower().Contains("millimeters") || Description.ToLower().Contains("mm/min") || Description.ToLower().Contains("mm/sec^2"))
                {
                    return typeof(System.Double);
                }

                if (Description.ToLower().Contains("boolean"))
                {
                    return typeof(System.Boolean);
                }

                return typeof(System.String);
            }
        }

        private void Update(object _value)
        {
            string grblcommand = string.Concat(Name, "=", _value);
            Grbl.Interface.SendCommand(grblcommand);
        }
    }
}
