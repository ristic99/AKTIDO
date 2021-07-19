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
            OptionsWindow form = new OptionsWindow();
            form.Show();
        }

        private async void Search(int kategorija)
        {
            for (Articles artikli = new Articles(kategorija); artikli.art_id.Count != 0; await artikli.getNext())
            {
                foreach (int id in artikli.art_id)
                {
                    if (stop) return;
                    if (!AktidoCore.cache.Contains(id)) {               
                            string myJsonResponse = await AktidoCore.GetAsync("http://api.pik.ba/artikli/" + id);   
                            Nekretnina nekretnina = JsonConvert.DeserializeObject<Nekretnina>(myJsonResponse);
                            if(nekretnina != null)
                                if(nekretnina.artikal != null) { 
                                    nekretnina.artikal.kanton = (AktidoCore.kantoni.Where(c => c.lokacije.Contains(nekretnina.artikal.grad)).Select(c => c.kanton)).FirstOrDefault();
                                    Database.DBAddNekretnina(nekretnina);
                                    addNewArticle(nekretnina);
                                    AktidoCore.cache.Add(id);
                                    novih_artikala++;
                                    txt_Novi.Content = "Novih artikala: " + novih_artikala;                           
                            }
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
            PretraziWindow form = new PretraziWindow();
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
            if(selectedItem.Equals("Sve")){
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == "Stanovi").id);
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == "Kuće").id);
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == "Poslovni prostori").id);
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == "Zemljišta").id);
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == "Garaže").id);
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == "Vikendice").id);
            }
            else 
                Search(AktidoCore.Estate.FirstOrDefault(u => u.name == selectedItem).id);

            progressBar.Maximum = AktidoCore.Estate.FirstOrDefault(u => u.name == selectedItem).count;
        }

        private async void window_ContentRendered(object sender, EventArgs e)
        {
            await Start();
        }    

        private async Task Start()
        {
            if (!await AktidoCore.checkOLXConnection()) { AktidoCore.ShowInfo("Nemam pristup sajtu! Provjerite internet konekciju."); return; }
            if (!await Database.DBIsOnline()) { AktidoCore.ShowInfo("Provjerite vezu sa bazom podataka!"); return; }

            await AktidoCore.BuildCache();
            await AktidoCore.LoadCantons();      
            await AktidoCore.GetNumberOfEstates();

            lbl_BrojArtikala.Content = "Ukupno " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Sve")].count + " oglasa";
            lbl_Stanovi.Content = "Stanovi: " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Stanovi")].count;
            lbl_Kuce.Content = "Kuće: " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Kuće")].count;
            lbl_Prostori.Content = "Poslovni prostori: " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Poslovni prostori")].count;
            lbl_Zemljista.Content = "Zemljišta: " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Zemljišta")].count;
            lbl_Garaze.Content = "Garaže: " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Garaže")].count;
            lbl_Vikendice.Content = "Vikendice: " + AktidoCore.Estate[AktidoCore.Estate.FindIndex(c => c.name == "Vikendice")].count;
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