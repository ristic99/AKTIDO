using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aktido
{
    class Constants
    {
        public const int query_limit = 10;

        public const int stanovi = 23;
        public const int kuce = 24;
        public const int poslovni_prostori = 25;
        public const int vikendice = 26;
        public const int zemljista = 29;
        public const int garaze = 30;

        public const string _stanovi = "Stanovi";
        public const string _kuce = "Kuće";
        public const string _poslovni_prostori = "Poslovni prostori";
        public const string _vikendice = "Vikendice";
        public const string _zemljista = "Zemljišta";
        public const string _garaze = "Garaže";

        public const string _prodaja = "Prodaja";
        public const string _izdavanje = "Izdavanje";
        public const string _potraznja = "Potražnja";

        public static string _Podkategorija(int value)
        {
            switch (value)
            {
                case stanovi: return _stanovi;
                case kuce: return _kuce;
                case poslovni_prostori: return _poslovni_prostori;
                case vikendice: return _vikendice;
                case zemljista: return _zemljista;
                case garaze: return _garaze;
            }
            return "";
        }

        public static int Podkategorija(string value)
        {
            switch (value)
            {
                case _stanovi : return stanovi;
                case _kuce: return kuce;
                case _poslovni_prostori: return poslovni_prostori;
                case _vikendice: return vikendice;
                case _zemljista: return zemljista;
                case _garaze: return garaze;
            }
            return -1;
        }

        public static int Vrsta(string value)
        {
            switch (value)
            {
                case _prodaja: return 0;
                case _izdavanje: return 1;
                case _potraznja: return 2;
            }
            return -1;
        }
    }
}
