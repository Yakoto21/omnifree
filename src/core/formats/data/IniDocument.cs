using System;
using System.Collections.Generic;

namespace Lodino.Core.Formats.Data
{
    public class IniDocument
    {
        public const string GlobalSectionName = "";

        public Dictionary<string, Dictionary<string, string>> Sections { get; private set; }

        public IniDocument()
        {
            Sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> GetSection(string sectionName, bool createIfNotExists = true)
        {
            string name = sectionName ?? GlobalSectionName;
            Dictionary<string, string> section;
            if (!Sections.TryGetValue(name, out section))
            {
                if (createIfNotExists)
                {
                    section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Sections[name] = section;
                }
            }
            return section;
        }

        public void Set(string sectionName, string key, string value)
        {
            var sec = GetSection(sectionName, true);
            sec[key ?? string.Empty] = value ?? string.Empty;
        }

        public string Get(string sectionName, string key, string defaultValue = "")
        {
            var sec = GetSection(sectionName, false);
            if (sec != null)
            {
                string val;
                if (sec.TryGetValue(key ?? string.Empty, out val))
                {
                    return val;
                }
            }
            return defaultValue;
        }
    }
}
