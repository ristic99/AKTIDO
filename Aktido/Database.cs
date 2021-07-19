using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

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
            string query = "INSERT INTO `nekretnine` (`id`, `naslov`, `cijena`, `osobine`, `korisnik`, `timestamp`, `kanton`, `grad`, `kratki_opis`, `detaljni_opis`, `latitude`, `longitude`, `zavrsen`, `vrsta_prodaje`, `podkategorija`) VALUES (@id, @naslov, @cijena, @osobine, @korisnik, @timestamp, @kanton, @grad, @kratki_opis, @detaljni_opis, @latitude, @longitude, @zavrsen, @vrsta_prodaje, @podkategorija)";
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.CommandText = query;
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
                        command.Connection = connection;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { }
        }

        public static string DBLoadNekretnina(int id)
        {
            Nekretnina nekretnina = new Nekretnina();
            string query = "SELECT * FROM nekretnine WHERE id = @id";
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        connection.Open();
                        MySqlDataReader reader = command.ExecuteReader();
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
                            nekretnina.artikal.latitude = reader.GetDouble("latitude");
                            nekretnina.artikal.longitude = reader.GetDouble("longitude");
                            nekretnina.artikal.zavrsen = reader.GetBoolean("zavrsen");
                            nekretnina.artikal.vrsta_prodaje = reader.GetInt16("vrsta_prodaje");
                        }

                        string output = JsonConvert.SerializeObject(nekretnina);
                        return output;
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static async Task<HashSet<int>> DBLoadCache()
        {
            HashSet<int> cache = new HashSet<int>();
            string query = "SELECT id FROM nekretnine";
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        var reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                            cache.Add(reader.GetInt32(0));
                        return cache;
                    }
                }

            }
            catch (Exception)
            {
                return cache;
            }
        }

        public static async Task<List<Nekretnina>> DBSearchNekretnina(String s_query, int _from, int _to)
        {
            List<Nekretnina> nekretnine = new List<Nekretnina>();
            string query = "SELECT `naslov`,`cijena`,`podkategorija`,`korisnik`,`id`,`timestamp` FROM `nekretnine` WHERE id > 0" + s_query + " ORDER BY `id` ASC LIMIT " + _from + "," + _to;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        var reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            Nekretnina nekretnina = new Nekretnina();
                            nekretnina.artikal = new Artikal();
                            nekretnina.artikal.naslov = reader.GetString(0);
                            nekretnina.artikal.cijena = AktidoCore.CijenaToString(reader.GetInt32(1));
                            nekretnina.artikal.podkategorija = reader.GetInt32(2).ToString();
                            nekretnina.artikal.ime_korisnika = reader.GetString(3);
                            nekretnina.artikal.id = reader.GetInt32(4);
                            nekretnina.artikal.timestamp = reader.GetInt32(5);
                            nekretnine.Add(nekretnina);
                        }
                        return nekretnine;
                    }
                }

            }
            catch (Exception)
            {
                return nekretnine;
            }
        }

        public static int DBSearchCount(String s_query)
        {
            string query = "SELECT COUNT(*) FROM `nekretnine` WHERE id > 1" + s_query;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        connection.Open();
                        var result = Convert.ToInt32(command.ExecuteScalar());
                        return result;
                    }
                }

            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static async Task<List<Kanton>> DBLoadKantoni()
        {
            List<Kanton> kantoni = new List<Kanton>();
            string query = "SELECT `kanton`,`lokacije` FROM `kantoni`";
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        var reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            Kanton kanton = new Kanton();
                            kanton.kanton = reader.GetString(0);
                            kanton.lokacije = AktidoMySqlDataReader.GetLokacije(reader.GetString(1));
                            kantoni.Add(kanton);
                        }
                        return kantoni;
                    }
                }

            }
            catch (Exception)
            {
                return kantoni;
            }
        }

        public static async Task<bool> DBIsOnline()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(database.ToString()))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

    }

}