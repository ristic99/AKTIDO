using System;

namespace Aktido.Classes { 

    class Constants
    {
        public const int query_limit = 10;

        public static string config_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Aktido\AktidoConfig";
    }
}
