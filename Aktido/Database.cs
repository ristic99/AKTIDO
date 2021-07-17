using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aktido
{

    public static class AktidoMySqlDataReader
    {
        public static List<Osobine> GetOsobine(string json)
        {
            List<Osobine> osobine;
            osobine = JsonConvert.DeserializeObject<List<Osobine>>(json);
            return osobine;
        }

        public static List<string> GetLokacije(string json)
        {
            List<string> lokacije;
            lokacije = JsonConvert.DeserializeObject<List<string>>(json);
            return lokacije;
        }
    }

    public class DatabaseJson
    {
        public string server_ip = "127.0.0.1";
        public string username = "root";
        public string password = "";
        public string database = "aktido";
    }

    public class Database
    {
        public static DatabaseJson database_json = LoadCred();
        public static MySqlConnectionStringBuilder database = new MySqlConnectionStringBuilder()
        {
            Server = database_json.server_ip,
            UserID = database_json.username,
            Password = database_json.password,
            Database = database_json.database,
            SslMode = MySqlSslMode.None
        };

        private static DatabaseJson LoadCred()
        {
            if (System.IO.File.Exists(AktidoCore.config_path))
            {
                string lines = File.ReadAllText(AktidoCore.config_path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<DatabaseJson>(lines);
            }
            return new DatabaseJson();
        }

        public static void DBAddNekretnina(Nekretnina nekretnina)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(database.ToString());
                MySqlCommand command = new MySqlCommand();
                string SQL = "INSERT INTO `nekretnine` (`id`, `naslov`, `cijena`, `osobine`, `korisnik`, `timestamp`, `kanton`, `grad`, `kratki_opis`, `detaljni_opis`, `latitude`, `longitude`, `zavrsen`, `vrsta_prodaje`, `podkategorija`) VALUES (@id, @naslov, @cijena, @osobine, @korisnik, @timestamp, @kanton, @grad, @kratki_opis, @detaljni_opis, @latitude, @longitude, @zavrsen, @vrsta_prodaje, @podkategorija)";
                command.CommandText = SQL;
                command.Parameters.AddWithValue("@id", nekretnina.artikal.id);
                command.Parameters.AddWithValue("@naslov", nekretnina.artikal.naslov);
                command.Parameters.AddWithValue("@cijena", AktidoCore.CijenaToInt(nekretnina.artikal.cijena));       
                command.Parameters.AddWithValue("@osobine", JsonConvert.SerializeObject(nekretnina.artikal.osobine));
                command.Parameters.AddWithValue("@korisnik", nekretnina.artikal.korisnik.ime);
                command.Parameters.AddWithValue("@timestamp", nekretnina.artikal.timestamp);
                command.Parameters.AddWithValue("@kanton", nekretnina.artikal.kanton);
                command.Parameters.AddWithValue("@grad", nekretnina.artikal.grad);
                command.Parameters.AddWithValue("@kratki_opis", nekretnina.artikal.kratki_opis);
                command.Parameters.AddWithValue("@detaljni_opis", nekretnina.artikal.detaljni_opis);
                command.Parameters.AddWithValue("@latitude", nekretnina.artikal.latitude);
                command.Parameters.AddWithValue("@longitude", nekretnina.artikal.longitude);
                command.Parameters.AddWithValue("@zavrsen", nekretnina.artikal.zavrsen);
                command.Parameters.AddWithValue("@vrsta_prodaje", nekretnina.artikal.vrsta_prodaje);
                command.Parameters.AddWithValue("@podkategorija", nekretnina.artikal.putanja[1].id);
                command.Connection = conn;
                conn.Open();
                command.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex) {  }
        }

        public static string DBLoadNekretnina(int id)
        {
            Nekretnina nekretnina = new Nekretnina();
            string sql = "SELECT * FROM nekretnine WHERE id = @id";
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cn.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();                    
                        while (reader.Read())
                        {
                            nekretnina.artikal = new Artikal();
                            nekretnina.artikal.korisnik = new Korisnik();

                            nekretnina.artikal.id = reader.GetInt32("id");
                            nekretnina.artikal.naslov = reader.GetString("naslov");
                            nekretnina.artikal.cijena = AktidoCore.CijenaToString(reader.GetInt32("cijena"));
                            nekretnina.artikal.osobine = AktidoMySqlDataReader.GetOsobine(reader.GetString("osobine"));
                            nekretnina.artikal.korisnik.ime = reader.GetString("korisnik");
                            nekretnina.artikal.timestamp = reader.GetInt32("timestamp");
                            nekretnina.artikal.grad = reader.GetString("grad");
                            nekretnina.artikal.kratki_opis = reader.GetString("kratki_opis");
                            nekretnina.artikal.detaljni_opis = reader.GetString("detaljni_opis");
                            nekretnina.artikal.latitude =reader.GetDouble("latitude");
                            nekretnina.artikal.longitude = reader.GetDouble("longitude");
                            nekretnina.artikal.zavrsen = reader.GetBoolean("latitude");
                            nekretnina.artikal.vrsta_prodaje = reader.GetInt16("vrsta_prodaje");
                        }
                        cn.Close();

                        string output = JsonConvert.SerializeObject(nekretnina);
                        return output;
                    }
                }
                catch (Exception ex) { return ""; }
            }
        }

        public static async Task<HashSet<int>> DBLoadCache()
        {
            HashSet<int> cache = new HashSet<int>();
            string sql = "SELECT id FROM nekretnine";
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                    {
                        cn.Open();
                        var reader = await cmd.ExecuteReaderAsync();
                        while (reader.Read())
                            cache.Add(reader.GetInt32(0));       
                        cn.Close();
                        return cache;
                    }
                }
                catch (Exception ex) { return cache; }
            }
        }

        public static async Task<List<Nekretnina>> DBSearchNekretnina(String query, int _from, int _to) // OK!
        {
            List<Nekretnina> nekretnine = new List<Nekretnina>();
            string sql = "SELECT `naslov`,`cijena`,`podkategorija`,`korisnik`,`id`,`timestamp` FROM `nekretnine` WHERE id > 0" + query + " ORDER BY `id` ASC LIMIT " + _from  + "," +_to;
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                    {
                        cn.Open();
                        var reader = await cmd.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            Nekretnina nekretnina = new Nekretnina();
                            nekretnina.artikal = new Artikal();
                            nekretnina.artikal.naslov = reader.GetString(0);
                            nekretnina.artikal.cijena = AktidoCore.CijenaToString(reader.GetInt32(1));
                            nekretnina.artikal.podkategorija = reader.GetString(2);
                            nekretnina.artikal.ime_korisnika = reader.GetString(3);
                            nekretnina.artikal.id = reader.GetInt32(4);
                            nekretnina.artikal.timestamp = reader.GetInt32(5);
                            nekretnine.Add(nekretnina);
                        }
                        cn.Close();
                        return nekretnine;
                    }
                }
                catch (Exception ex) { return nekretnine; }
            }
        }

        public static int DBSearchCount(String query)
        {
            string sql = "SELECT COUNT(*) FROM `nekretnine` WHERE id > 1" + query;
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                    {
                        cn.Open();
                        var result = Convert.ToInt32(cmd.ExecuteScalar());
                        cn.Close();
                        return result;
                    }
                }
                catch (Exception ex) { cn.Close(); return 0; }
            }
        }

        public static async Task<List<Kanton>> DBLoadKantoni()
        {
            List<Kanton> kantoni = new List<Kanton>();
            string sql = "SELECT `kanton`,`lokacije` FROM `kantoni`";
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                    {
                        cn.Open();
                        var reader = await cmd.ExecuteReaderAsync();
                        while (reader.Read()) {
                            Kanton kanton = new Kanton();
                            kanton.kanton = reader.GetString(0);
                            kanton.lokacije = AktidoMySqlDataReader.GetLokacije(reader.GetString(1));
                            kantoni.Add(kanton);
                        }
                        cn.Close();
                        return kantoni;
                    }
                }
                catch (Exception ex) { return kantoni; }
            }
        }

        public static bool DBDoesExists(int id)
        {
            string sql = "SELECT COUNT(*) FROM nekretnine WHERE id = @id";
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cn.Open();
                        var result = Convert.ToInt32(cmd.ExecuteScalar());
                        cn.Close();
                        if (result > 0) return true;     
                        else return false;                       
                    }
                }
                catch (Exception ex) { cn.Close(); return false; }
            }
        }

        public static async Task<bool> DBIsOnline()
        {
            using (MySqlConnection cn = new MySqlConnection(database.ToString()))
            {
                try
                {
                    cn.Open();
                    cn.Close();
                    return true;
                }
                catch (Exception ex) { return false; }
            }
        }

    }

}
