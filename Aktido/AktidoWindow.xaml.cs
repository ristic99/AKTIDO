using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aktido
{
    public partial class AktidoWindow : Window
    {
        public static List<Kanton> kantoni = new List<Kanton>();     
        BrojNeretnina broj_nekretnina = new BrojNeretnina();    
        HashSet<int> cache;
        private int novih_artikala;
        private bool stop;       
        public AktidoWindow()
        {
            InitializeComponent();

            dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("id") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Podkategorija", Binding = new Binding("podkategorija") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Cijena", Binding = new Binding("cijena") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Objavio", Binding = new Binding("ime_korisnika") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Datum Objave", Binding = new Binding("objavljeno") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "URL", Binding = new Binding("url") });
        }

        public void menuClick(object sender, RoutedEventArgs e)
        {
            Options form = new Options();
            form.Show();
        }

        private async void Search(int kategorija)
        {
            for (Artikli artikli = new Artikli(kategorija); artikli.art_id.Count != 0; await artikli.getNext())
            {
                foreach (int id in artikli.art_id)
                {
                    if (stop) return;
                    if (!cache.Contains(id)) {               
                            string myJsonResponse = await AktidoCore.GetAsync("http://api.pik.ba/artikli/" + id);   
                            Nekretnina nekretnina = JsonConvert.DeserializeObject<Nekretnina>(myJsonResponse);
                            if(nekretnina != null)
                                if(nekretnina.artikal != null) { 
                                    nekretnina.artikal.kanton = (kantoni.Where(c => c.lokacije.Contains(nekretnina.artikal.grad)).Select(c => c.kanton)).FirstOrDefault();
                                    Database.DBAddNekretnina(nekretnina);
                                    addNewArticle(nekretnina);
                            }
                            cache.Add(id);
                            novih_artikala++;
                            txt_Novi.Content = "Novih artikala: " + novih_artikala;
                    }
                    progressBar.Value += 1;
                    lbl_Done.Content = "Izvršeno: " + (int)Math.Ceiling(((double)progressBar.Value) / 30) + "/" + (int)Math.Ceiling(((double)progressBar.Maximum) / 30);
                }         
            }
        }

        private void btn_Vise_Click(object sender, RoutedEventArgs e)
        {
            Artikal_MIN selected = (Artikal_MIN)dataGrid.SelectedItem;
            if (selected != null)
            {
                ArtikalWindow form = new ArtikalWindow(selected.id);
                form.Show();
            }
            else AktidoCore.ShowInfo("Niste odabrali artikal u tabeli!");
        }

        private void btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            stop = true;
            btn_Stop.IsEnabled = false;
            btn_Search.IsEnabled = true;
            progressBar.Value = progressBar.Maximum;
        }

        private void btn_CleanDatagrid_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.Items.Count != 0)
            {
                dataGrid.Items.Clear();
                dataGrid.Items.Refresh();
                novih_artikala = 0;
                txt_Novi.Content = "Novih artikala: 0";
            }
            else AktidoCore.ShowInfo("Tabela je već prazna!");
        }

        private void btn_Pretrazi_Click(object sender, RoutedEventArgs e)
        {
            Pretrazi form = new Pretrazi();
            form.Show();
        }

        private void btn_Export_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItems.Count == 0) { AktidoCore.ShowInfo("Niste ništa selektovali u tabeli!"); return; }

            Artikal_MIN selected = (Artikal_MIN)dataGrid.SelectedItem;
            DataTable newTable = AktidoCore.CreateTable(selected);  

            foreach (Artikal_MIN row in dataGrid.SelectedItems)
            {
                DataRow datarow = newTable.NewRow();
                newTable.Rows.Add(row.id,row.podkategorija, row.cijena, row.ime_korisnika,row.objavljeno,row.url);
            }

            StringBuilder data = AktidoCore.ConvertDataTableToCsvFile(newTable);
            AktidoCore.DialogSave(data);
        }
            

        private void addNewArticle(Nekretnina nekretnina)
        {
            dataGrid.Items.Add(new Artikal_MIN
            {
                id = nekretnina.artikal.id,
                podkategorija = nekretnina.artikal.putanja.ElementAt(1).naziv.ToString(),
                cijena = nekretnina.artikal.cijena,
                ime_korisnika = nekretnina.artikal.korisnik.ime,
                objavljeno = AktidoCore.UnixTimestampToDateTime(nekretnina.artikal.timestamp),
                url = nekretnina.artikal.url,
            });
        }

        private async void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            if (!await Database.DBIsOnline()) { AktidoCore.ShowInfo("Provjerite vezu sa bazom podataka!"); return; }
            progressBar.Value = 0;

            stop = false;
            btn_Stop.IsEnabled = true;
            btn_Search.IsEnabled = false;
            
            string selectedItem = comboBox.Text;
            if (selectedItem.Equals("Stanovi")) { Search(Constants.stanovi); progressBar.Maximum = broj_nekretnina.stanovi; }
            else if (selectedItem.Equals("Kuće")) { Search(Constants.kuce); progressBar.Maximum = broj_nekretnina.kuce; }
            else if (selectedItem.Equals("Poslovni prostori")) { Search(Constants.poslovni_prostori); progressBar.Maximum = broj_nekretnina.prostori; }
            else if (selectedItem.Equals("Zemljišta")) { Search(Constants.zemljista); progressBar.Maximum = broj_nekretnina.zemljista; }
            else if (selectedItem.Equals("Garaže")) { Search(Constants.garaze); progressBar.Maximum = broj_nekretnina.garaze; }
            else if (selectedItem.Equals("Vikendice")) { Search(Constants.vikendice); progressBar.Maximum = broj_nekretnina.vikendice; }
            else if (selectedItem.Equals("Sve")) {
                Search(Constants.stanovi);
                Search(Constants.kuce);
                Search(Constants.poslovni_prostori);
                Search(Constants.zemljista);
                Search(Constants.garaze);
                Search(Constants.vikendice);              
                progressBar.Maximum = broj_nekretnina.sve;
            }
        }

        private async void window_ContentRendered(object sender, EventArgs e)
        {
            await BrojNekretnina();
        }    

        private async Task BrojNekretnina()
        {
            if (!await AktidoCore.checkOLXConnection()) { AktidoCore.ShowInfo("Nemam pristup sajtu! Provjerite internet konekciju."); return; }
            if (!await Database.DBIsOnline()) { AktidoCore.ShowInfo("Provjerite vezu sa bazom podataka!"); return; }
            
            kantoni = await Database.DBLoadKantoni();
            cache = await Database.DBLoadCache();
            await broj_nekretnina.getBrojNekretnina();                

            lbl_BrojArtikala.Content = "Ukupno " + broj_nekretnina.sve.ToString() + " oglasa";
            lbl_Stanovi.Content = "Stanovi: " + broj_nekretnina.stanovi;
            lbl_Kuce.Content = "Kuće: " + broj_nekretnina.kuce;
            lbl_Prostori.Content = "Poslovni prostori: " + broj_nekretnina.prostori;
            lbl_Zemljista.Content = "Zemljišta: " + broj_nekretnina.zemljista;
            lbl_Garaze.Content = "Garaže: " + broj_nekretnina.garaze;
            lbl_Vikendice.Content = "Vikendice: " + broj_nekretnina.vikendice;
            btn_Search.IsEnabled = true;
            btn_Pretrazi.IsEnabled = true;
        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Artikal_MIN selected = (Artikal_MIN)dataGrid.SelectedItem;
            if (selected != null)
            {
                ArtikalWindow form = new ArtikalWindow(selected.id);
                form.Show();
            }
        }
    }
}