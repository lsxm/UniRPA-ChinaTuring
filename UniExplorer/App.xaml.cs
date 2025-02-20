﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace UniExplorer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static string launchArgsStr;

        public static string LaunchArgsStr
        {
            get
            {
                return launchArgsStr;
            }
            set
            {
                launchArgsStr = value;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                string s = string.Empty;
                for (int i = 0; i < e.Args.Length; i++)
                {
                    s += e.Args[i] + " ";
                }

                LaunchArgsStr = s;
            }
            //LaunchArgsStr = "AAEAAAD/////AQAAAAAAAAAMAgAAAE1QbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LCBWZXJzaW9uPTEuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49bnVsbAwDAAAATldpbmRvd3NCYXNlLCBWZXJzaW9uPTMuMC4wLjAsIEN1bHR1cmU9TmV1dHJhbCwgUHVibGljS2V5VG9rZW49MzFiZjM4NTZhZDM2NGUzNQUBAAAAM1BsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JTdGF0dXNNb2RlbAEAAAAePFNlbGVjdG9ySXRlbXM+a19fQmFja2luZ0ZpZWxkBLQBU3lzdGVtLkNvbGxlY3Rpb25zLk9iamVjdE1vZGVsLk9ic2VydmFibGVDb2xsZWN0aW9uYDFbW1BsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtLCBQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LCBWZXJzaW9uPTEuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49bnVsbF1dAwAAAAIAAAAJBAAAAAUEAAAAtAFTeXN0ZW0uQ29sbGVjdGlvbnMuT2JqZWN0TW9kZWwuT2JzZXJ2YWJsZUNvbGxlY3Rpb25gMVtbUGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW0sIFBsdWdpbnMuU2hhcmVkLkxpYnJhcnksIFZlcnNpb249MS4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsXV0CAAAACF9tb25pdG9yEkNvbGxlY3Rpb25gMStpdGVtcwQDwgFTeXN0ZW0uQ29sbGVjdGlvbnMuT2JqZWN0TW9kZWwuT2JzZXJ2YWJsZUNvbGxlY3Rpb25gMStTaW1wbGVNb25pdG9yW1tQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LkxpYnJhcnlzLlNlbGVjdG9ySXRlbSwgUGx1Z2lucy5TaGFyZWQuTGlicmFyeSwgVmVyc2lvbj0xLjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGxdXQMAAACgAVN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLkxpc3RgMVtbUGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW0sIFBsdWdpbnMuU2hhcmVkLkxpYnJhcnksIFZlcnNpb249MS4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsXV0DAAAACQUAAAAJBgAAAAUFAAAAwgFTeXN0ZW0uQ29sbGVjdGlvbnMuT2JqZWN0TW9kZWwuT2JzZXJ2YWJsZUNvbGxlY3Rpb25gMStTaW1wbGVNb25pdG9yW1tQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LkxpYnJhcnlzLlNlbGVjdG9ySXRlbSwgUGx1Z2lucy5TaGFyZWQuTGlicmFyeSwgVmVyc2lvbj0xLjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGxdXQEAAAAKX2J1c3lDb3VudAAIAwAAAAAAAAAEBgAAAKABU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuTGlzdGAxW1tQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LkxpYnJhcnlzLlNlbGVjdG9ySXRlbSwgUGx1Z2lucy5TaGFyZWQuTGlicmFyeSwgVmVyc2lvbj0xLjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGxdXQMAAAAGX2l0ZW1zBV9zaXplCF92ZXJzaW9uBAAALlBsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtW10CAAAACAgJBwAAAAIAAAACAAAABwcAAAAAAQAAAAQAAAAELFBsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtAgAAAAkIAAAACQkAAAANAgUIAAAALFBsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtBQAAAApfaXNDaGVja2VkDF9pdGVtQ29udGVudCA8SXRlbUNvbnRlbnRGdWxsPmtfX0JhY2tpbmdGaWVsZBs8QXR0cmlidXRlcz5rX19CYWNraW5nRmllbGQLX0lzU2VsZWN0ZWQDAQEEAw5TeXN0ZW0uQm9vbGVhbr0BU3lzdGVtLkNvbGxlY3Rpb25zLk9iamVjdE1vZGVsLk9ic2VydmFibGVDb2xsZWN0aW9uYDFbW1BsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtQXR0cmlidXRlLCBQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LCBWZXJzaW9uPTEuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49bnVsbF1dAwAAAA5TeXN0ZW0uQm9vbGVhbgIAAAAIAQEGCgAAABs8UGFuZSBDbGFzc05hbWU9IiMzMjc2OSIgLz4GCwAAABs8UGFuZSBDbGFzc05hbWU9IiMzMjc2OSIgLz4JDAAAAAgBAAEJAAAACAAAAAgBAQYNAAAATTxXaW5kb3cgTmFtZT0iU0FQIExvZ29uIDc1MCIgQ2xhc3NOYW1lPSIjMzI3NzAiIFByb2Nlc3NOYW1lPSJzYXBsb2dvbi5leGUiIC8+Bg4AAABNPFdpbmRvdyBOYW1lPSJTQVAgTG9nb24gNzUwIiBDbGFzc05hbWU9IiMzMjc3MCIgUHJvY2Vzc05hbWU9InNhcGxvZ29uLmV4ZSIgLz4JDwAAAAgBAAUMAAAAvQFTeXN0ZW0uQ29sbGVjdGlvbnMuT2JqZWN0TW9kZWwuT2JzZXJ2YWJsZUNvbGxlY3Rpb25gMVtbUGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW1BdHRyaWJ1dGUsIFBsdWdpbnMuU2hhcmVkLkxpYnJhcnksIFZlcnNpb249MS4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsXV0CAAAACF9tb25pdG9yEkNvbGxlY3Rpb25gMStpdGVtcwQDywFTeXN0ZW0uQ29sbGVjdGlvbnMuT2JqZWN0TW9kZWwuT2JzZXJ2YWJsZUNvbGxlY3Rpb25gMStTaW1wbGVNb25pdG9yW1tQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LkxpYnJhcnlzLlNlbGVjdG9ySXRlbUF0dHJpYnV0ZSwgUGx1Z2lucy5TaGFyZWQuTGlicmFyeSwgVmVyc2lvbj0xLjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGxdXQMAAACpAVN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLkxpc3RgMVtbUGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW1BdHRyaWJ1dGUsIFBsdWdpbnMuU2hhcmVkLkxpYnJhcnksIFZlcnNpb249MS4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsXV0DAAAACRAAAAAJEQAAAAEPAAAADAAAAAkSAAAACRMAAAAFEAAAAMsBU3lzdGVtLkNvbGxlY3Rpb25zLk9iamVjdE1vZGVsLk9ic2VydmFibGVDb2xsZWN0aW9uYDErU2ltcGxlTW9uaXRvcltbUGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW1BdHRyaWJ1dGUsIFBsdWdpbnMuU2hhcmVkLkxpYnJhcnksIFZlcnNpb249MS4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsXV0BAAAACl9idXN5Q291bnQACAMAAAAAAAAABBEAAACpAVN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljLkxpc3RgMVtbUGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW1BdHRyaWJ1dGUsIFBsdWdpbnMuU2hhcmVkLkxpYnJhcnksIFZlcnNpb249MS4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsXV0DAAAABl9pdGVtcwVfc2l6ZQhfdmVyc2lvbgQAADdQbHVnaW5zLlNoYXJlZC5MaWJyYXJ5LkxpYnJhcnlzLlNlbGVjdG9ySXRlbUF0dHJpYnV0ZVtdAgAAAAgICRQAAAABAAAAAQAAAAESAAAAEAAAAAAAAAABEwAAABEAAAAJFQAAAAMAAAADAAAABxQAAAAAAQAAAAQAAAAENVBsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtQXR0cmlidXRlAgAAAAkWAAAADQMHFQAAAAABAAAABAAAAAQ1UGx1Z2lucy5TaGFyZWQuTGlicmFyeS5MaWJyYXJ5cy5TZWxlY3Rvckl0ZW1BdHRyaWJ1dGUCAAAACRcAAAAJGAAAAAkZAAAACgUWAAAANVBsdWdpbnMuU2hhcmVkLkxpYnJhcnkuTGlicmFyeXMuU2VsZWN0b3JJdGVtQXR0cmlidXRlAwAAAApfaXNDaGVja2VkBV9uYW1lBl92YWx1ZQMBAQ5TeXN0ZW0uQm9vbGVhbgIAAAAIAQEGGgAAAAlDbGFzc05hbWUGGwAAAAYjMzI3NjkBFwAAABYAAAAIAQEGHAAAAAROYW1lBh0AAAANU0FQIExvZ29uIDc1MAEYAAAAFgAAAAgBAQYeAAAACUNsYXNzTmFtZQYfAAAABiMzMjc3MAEZAAAAFgAAAAgBAQYgAAAAC1Byb2Nlc3NOYW1lBiEAAAAMc2FwbG9nb24uZXhlCw==";
            Plugins.Shared.Library.Librarys.LanguageLocalization.ToChinese();
        }
    }
}
