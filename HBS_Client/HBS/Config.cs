namespace HBS
{

    namespace HBS
    {
        class Config
        {
            /* Behavior */
            // Min/Max delay in seconds between the client calls in active state
            public static int MinActiveDelay = 2;
            public static int MaxActiveDelay = 5;
            // Min/Max delay between the client calls in inactive state
            public static int MinInactiveDelay = 30;
            public static int MaxInactiveDelay = 42;
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
