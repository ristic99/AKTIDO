using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aktido
{
    class Search
    {
        public int broj_rezultata { get; set; }
        public int _from { get; set; }

        private int _offset = Constants.query_limit;

        public Search(string query)
        {
            broj_rezultata = Database.DBSearchCount(query);
        }

        public void Correction()
        {
            _from += _offset;
        }

        public async Task<List<Nekretnina>> getResults(string query)
        {
            return await Database.DBSearchNekretnina(query, _from, _offset);
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
