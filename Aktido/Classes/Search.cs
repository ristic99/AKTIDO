using System.Collections.Generic;
using System.Threading.Tasks;


namespace Aktido.Classes
{
    class Search
    {
        private List<Nekretnina> nekretnine = new List<Nekretnina>();
        private int _from { get; set; }
        private int _offset = Constants.query_limit;

        public async Task<List<Nekretnina>> GetData(string query)
        {

            for (int _results = Database.DBSearchCount(query); _from <= _results; _from += _offset)
            {
                List<Nekretnina> result = await Database.DBSearchNekretnina(query, _from, _offset);
                nekretnine.AddRange(result);            
            }

            return nekretnine;
        }
    }

    public class SearchQuery
    {
        public int cijena_od = 0;
        public int cijena_do = 10000000;
        public int podkategorija { get; set; }
        public int vrsta_prodaje { get; set; }
        public string query { get; set; }
    }
}
