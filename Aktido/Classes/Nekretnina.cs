using System;
using System.Collections.Generic;


namespace Aktido.Classes
{

    public class Putanja
    {
        public string id { get; set; }
        public string naziv { get; set; }
    }

    public class Osobine
    {
        public string naziv { get; set; }
        public string vrijednost { get; set; }
    }

    public class Korisnik
    {
        public int id { get; set; }
        public string ime { get; set; }
    }

    public class Artikal_MIN
    {
        public int id { get; set; }
        public string podkategorija { get; set; }
        public string cijena { get; set; }      
        public string ime_korisnika { get; set; }
        public DateTime objavljeno { get; set; }
        public int timestamp { get; set; }      
        public string url { get; set; }
    }

    public class Artikal : Artikal_MIN
    {
        private string priv_detaljni_opis;
        private string priv_naslov;
        public string kanton { get; set; }
        public string naslov { get { return priv_naslov; } set { this.priv_naslov = value.Trim(); } }
        public List<string> slike { get; set; }      
        public string grad { get; set; }
        public string placanje { get; set; }
        public string kratki_opis { get; set; }
        public string detaljni_opis { get { return priv_detaljni_opis; } set { this.priv_detaljni_opis = AktidoCore.NormalizeString(AktidoCore.StripHTML(value).Replace("\n","")).Trim(); } }
        public List<Putanja> putanja { get; set; }
        public List<Osobine> osobine { get; set; }
        public Korisnik korisnik { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public bool zavrsen { get; set; }
        public Int16 vrsta_prodaje { get; set; }
    }

    public class Nekretnina
    {
        public Artikal artikal { get; set; }
    }


}
