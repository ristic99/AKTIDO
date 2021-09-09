using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Aktido.Classes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Aktido.Views
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

        public void MenuClick(object sender, RoutedEventArgs e)
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
                            if(nekretnina?.artikal != null) { 
                                nekretnina.artikal.kanton = (AktidoCore.kantoni.Where(c => c.lokacije.Contains(nekretnina.artikal.grad)).Select(c => c.kanton)).FirstOrDefault();
                                Database.DBAddNekretnina(nekretnina);
                                AddNewArticle(nekretnina);
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
                newTable.Rows.Add(row.id,row.podkategorija, row.cijena, row.ime_korisnika,row.objavljeno,row.url);

            StringBuilder data = AktidoCore.ConvertDataTableToCsvFile(newTable);
            AktidoCore.DialogSave(data);
        }
            

        private void AddNewArticle(Nekretnina nekretnina)
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

            Search(AktidoCore.GetEstateId(comboBox.Text));
            progressBar.Maximum = AktidoCore.GetEstateCount(comboBox.Text);
        }

        private async void window_ContentRendered(object sender, EventArgs e)
        {
            await Start();
        }    

        private async Task Start()
        {
            if (!await AktidoCore.CheckOlxConnection()) { AktidoCore.ShowInfo("Nemam pristup sajtu! Provjerite internet konekciju."); return; }
            if (!await Database.DBIsOnline()) { AktidoCore.ShowInfo("Provjerite vezu sa bazom podataka!"); return; }

            await AktidoCore.BuildCache();
            await AktidoCore.LoadCantons();      
            await AktidoCore.GetNumberOfEstates();

            lbl_BrojArtikala.Content = "Ukupno " + AktidoCore.GetEstateCount("Sve") + " oglasa";
            lbl_Stanovi.Content = "Stanovi: " + AktidoCore.GetEstateCount("Stanovi");
            lbl_Kuce.Content = "Kuće: " + AktidoCore.GetEstateCount("Kuće");
            lbl_Prostori.Content = "Poslovni prostori: " + AktidoCore.GetEstateCount("Poslovni prostori");
            lbl_Zemljista.Content = "Zemljišta: " + AktidoCore.GetEstateCount("Zemljišta");
            lbl_Garaze.Content = "Garaže: " + AktidoCore.GetEstateCount("Garaže");
            lbl_Vikendice.Content = "Vikendice: " + AktidoCore.GetEstateCount("Vikendice");
            btn_Search.IsEnabled = true;
            btn_Pretrazi.IsEnabled = true;
        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Artikal_MIN selected = (Artikal_MIN)dataGrid.SelectedItem;
            if (selected == null) return;
            new ArtikalWindow(selected.id).Show();
        }
    }
}