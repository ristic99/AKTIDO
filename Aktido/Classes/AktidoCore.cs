using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace Aktido.Classes
{
    public static class StringExtensionMethods
    {
        public static List<string> Between(this string source, string start, string end)
        {
            string pattern = string.Format("{0}({1}){2}", Regex.Escape(start), ".+?", Regex.Escape(end));
            return (from Match m in Regex.Matches(source, pattern) select m.Groups[1].Value).ToList();
        }
    }

    public class Articles
    {
        public List<int> art_id = new List<int>() { 1 };
        private int stranica { get; set; }
        private int kategorija { get; set; }

        public Articles(int arg)
        {
            kategorija = arg;
        }

        public async Task getNext()
        {        
            string response = await AktidoCore.GetAsync("https://www.olx.ba/pretraga?sort_order=desc&kategorija=" + kategorija + "&sort_po=datum&stranica=" + stranica);
            List<string> ids = StringExtensionMethods.Between(response, "id=\"art_", "\"");
            art_id = ids.ConvertAll(int.Parse);
            stranica++;
        }
    }

    class Kind
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    class Estate
    {
        public int id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
    }

    public class BrojRezultata
    {
        public string brojrezultata { get; set; }
    }

    public class Kanton
    {
        public string kanton;
        public List<string> lokacije;
    }

    class AktidoCore
    {
        public static HashSet<int> cache;

        public static List<Kanton> kantoni = new List<Kanton>();

        public static List<Estate> Estate = new List<Estate>()
        {
            new Estate() { id = 23, name = "Stanovi", count = 0 },
            new Estate() { id = 24, name = "Kuće", count = 0 },
            new Estate() { id = 25, name = "Poslovni prostori", count = 0 },
            new Estate() { id = 26, name = "Vikendice", count = 0 },
            new Estate() { id = 29, name = "Zemljišta", count = 0 },
            new Estate() { id = 30, name = "Garaže", count = 0 },
            new Estate() { id = 2, name = "Sve", count = 0 }
        };

        public static List<Kind> Kind = new List<Kind>()
        {
            new Kind() { id = 0, name = "Prodaja" },
            new Kind() { id = 1, name = "Izdavanje" },
            new Kind() { id = 2, name = "Potražnja" },
            new Kind() { id = -1, name = "Sve" }
        };

        public static async Task BuildCache()
        {
            cache = await Database.DBLoadCache();
        }

        public static async Task LoadCantons()
        {
            kantoni = await Database.DBLoadKantoni();
        }

        public static async Task GetNumberOfEstates()
        {
            foreach (Estate estate in AktidoCore.Estate)
            {
                string response = await GetAsync("https://www.olx.ba/pretraga?json=da&kategorija=" + AktidoCore.Estate.FirstOrDefault(u => u.name == estate.name).id + "&vrsta=&od=&do=&kanton=&kvadrata_min=&kvadrata_max=&samobroj=da");
                BrojRezultata deserialized = JsonConvert.DeserializeObject<BrojRezultata>(response);
                estate.count = Int32.Parse(deserialized.brojrezultata.Replace(".", ""));
            }
        }

        public static async Task<string> GetAsync(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
        public static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        public static void ShowInfo(string info)
        {
            MessageBox.Show(info, "AKTIDO Software", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void SaveData(StringBuilder data, string filePath)
        {
            using (StreamWriter objWriter = new StreamWriter(filePath))
            {
                objWriter.WriteLine(data);
            }
        }

        public static string NormalizeString(string input)
        {
            int len = input.Length, index = 0, i = 0;
            var src = input.ToCharArray();
            bool skip = false;
            char ch;
            for (; i < len; i++)
            {
                ch = src[i];
                switch (ch)
                {
                    case '\u0020':
                    case '\u00A0':
                    case '\u1680':
                    case '\u2000':
                    case '\u2001':
                    case '\u2002':
                    case '\u2003':
                    case '\u2004':
                    case '\u2005':
                    case '\u2006':
                    case '\u2007':
                    case '\u2008':
                    case '\u2009':
                    case '\u200A':
                    case '\u202F':
                    case '\u205F':
                    case '\u3000':
                    case '\u2028':
                    case '\u2029':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                        if (skip) continue;
                        src[index++] = ch;
                        skip = true;
                        continue;
                    default:
                        skip = false;
                        src[index++] = ch;
                        continue;
                }
            }

            return new string(src, 0, index);
        }

        public static StringBuilder ConvertDataTableToCsvFile(DataTable dtData)
        {
            StringBuilder data = new StringBuilder();
            for (int column = 0; column < dtData.Columns.Count; column++)
            {
                if (column == dtData.Columns.Count - 1)
                    data.Append(dtData.Columns[column].ColumnName.ToString().Replace(",", ";"));
                else
                    data.Append(dtData.Columns[column].ColumnName.ToString().Replace(",", ";") + ';');
            }

            data.Append(Environment.NewLine);

            for (int row = 0; row < dtData.Rows.Count; row++)
            {
                for (int column = 0; column < dtData.Columns.Count; column++)
                {
                    if (column == dtData.Columns.Count - 1)
                        data.Append(dtData.Rows[row][column].ToString().Replace(",", ";"));
                    else
                        data.Append(dtData.Rows[row][column].ToString().Replace(",", ";") + ';');
                }

                if (row != dtData.Rows.Count - 1)
                    data.Append(Environment.NewLine);
            }
            return data;
        }

        public static void DialogSave(StringBuilder data)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = DateTime.Now.ToString("MM dd yyyy - HH mm");
            dlg.DefaultExt = ".csv";
            dlg.Filter = "Text documents (.csv)|*.csv";

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) return;
            string filename = dlg.FileName;
            AktidoCore.SaveData(data, filename);
            ShowInfo("Exportovano! Ime tabele: " + DateTime.Now.ToString("MM dd yyyy - HH mm") + ".csv");
        }

        public static int CijenaToInt(string cijena)
        {
            Regex regex = new Regex(@"\d+(.)?\d+\s+");
            Match match = regex.Match(cijena);
            return match.Success ? int.Parse(match.Value.Replace(".", "")) : 0;
        }

        public static string CijenaToString(int cijena)
        {
            return cijena != 0 ? cijena.ToString("C0", CultureInfo.CreateSpecificCulture("ba-BA")) : "Po dogovoru";
        }

        public static DataTable CreateTable(object o)
        {
            DataTable dt = new DataTable("OutputData");
            o.GetType().GetProperties().ToList().ForEach(f =>
            {
                try
                {
                    if (f.Name == "timestamp") return;
                    f.GetValue(o, null);
                    dt.Columns.Add(f.Name, f.PropertyType);
                }
                catch { }
            });
            return dt;
        }

        public static async Task<bool> CheckOlxConnection()
        { 
            try
            {
                Ping myPing = new Ping();
                var pingReply = await myPing.SendPingAsync("olx.ba", 3000, new byte[32], new PingOptions(64, true));
                return pingReply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
