using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Aktido
{
    public partial class Pretrazi : Window
    {    
        public Pretrazi()
        {
            InitializeComponent();

            dataGrid.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("id") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Podkategorija", Binding = new Binding("podkategorija") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Cijena", Binding = new Binding("cijena") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Objavio", Binding = new Binding("ime_korisnika") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Datum Objave", Binding = new Binding("objavljeno") });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "URL", Binding = new Binding("url") });

            cBoxKanton.Items.Add("Iz svih lokacija");
        }

        private void windowPretrazi_ContentRendered(object sender, EventArgs e)
        {
            foreach (Kanton kanton in AktidoCore.kantoni)
            {
                cBoxKanton.Items.Add(kanton.kanton);
            }
        }

        private void cBoxKanton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selection = (sender as ComboBox).SelectedItem as string;
            if (selection.Equals("")) return;
            if (selection.Equals("Iz svih lokacija")) { cBoxLokacija.Visibility = Visibility.Hidden; return; }

            cBoxLokacija.Items.Clear();
            cBoxLokacija.Visibility = Visibility.Visible;
            List<string> lokacije = AktidoCore.kantoni.Single(s => s.kanton.Equals(selection)).lokacije;
            cBoxLokacija.Items.Add("Mjesto");
            foreach (String lokacija in lokacije)
            {
                cBoxLokacija.Items.Add(lokacija);
            }
            cBoxLokacija.SelectedIndex = 0;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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

        private async void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            if (!await Database.DBIsOnline()) { AktidoCore.ShowInfo("Provjerite vezu sa bazom podataka!"); return; }

            dataGrid.Items.Clear();

            SearchQuery search = new SearchQuery();
            search.podkategorija = AktidoCore.Estate.FirstOrDefault(u => u.name == comboBox_Podkategorija.Text).id;
            search.vrsta_prodaje = AktidoCore.Kind.FirstOrDefault(u => u.name == comboBox_Vrsta.Text).id;

            if (search.podkategorija != 2)
                search.query += " AND podkategorija = " + search.podkategorija;
            if (search.vrsta_prodaje != -1)
                search.query += " AND vrsta_prodaje = " + search.vrsta_prodaje;

            var _od = Int32.TryParse(txtCijenaOd.Text, out search.cijena_od);
            var _do = Int32.TryParse(txtCijenaDo.Text, out search.cijena_do);

            if (_od)
                search.query += " AND cijena >= " + search.cijena_od;
            if (_do)
                search.query += " AND cijena <= " + search.cijena_do;

            if (!cBoxKanton.Text.Equals("Iz svih lokacija")) { 
                search.query += " AND kanton = '" + cBoxKanton.Text + "'";
                if (!cBoxLokacija.Text.Equals("Sve") && !cBoxLokacija.Text.Equals("Mjesto"))
                    search.query += " AND grad = '" + cBoxLokacija.Text + "'";
            }

            await SearchDB(search.query);
        }

        private async Task SearchDB(string query)
        {
            List<Nekretnina> nekretnine = new List<Nekretnina>();

            for (Search results = new Search(query); results._from <= results.broj_rezultata; results.Correction())
            {
                List<Nekretnina> result = await results.getResults(query);
                nekretnine.AddRange(result);
            }

            foreach (Nekretnina nekretnina in nekretnine)
            {
                nekretnina.artikal.url = "https://www.olx.ba/artikal/" + nekretnina.artikal.id;
                nekretnina.artikal.podkategorija = AktidoCore.Estate.FirstOrDefault(c => c.id == Int32.Parse(nekretnina.artikal.podkategorija)).name; //Constants._Podkategorija(Int32.Parse(nekretnina.artikal.podkategorija));
                if (nekretnina.artikal.cijena.Equals("0")) nekretnina.artikal.cijena = "Po dogovoru";
                addNewArticle(nekretnina);
            }

            txt_Rez.Content = "Broj rezultata: " + nekretnine.Count();
        }

        private void addNewArticle(Nekretnina nekretnina)
        {
            dataGrid.Items.Add(new Artikal_MIN
            {
                id = nekretnina.artikal.id,
                podkategorija = nekretnina.artikal.podkategorija,
                cijena = nekretnina.artikal.cijena,
                ime_korisnika = nekretnina.artikal.ime_korisnika,
                objavljeno = AktidoCore.UnixTimestampToDateTime(nekretnina.artikal.timestamp),
                url = nekretnina.artikal.url,
            });
        }

        private void btn_Export_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItems.Count == 0) { AktidoCore.ShowInfo("Niste ništa selektovali u tabeli!"); return; }

            Artikal_MIN selected = (Artikal_MIN)dataGrid.SelectedItem;
            DataTable newTable = AktidoCore.CreateTable(selected);

            foreach (Artikal_MIN row in dataGrid.SelectedItems)
            {
                DataRow datarow = newTable.NewRow();
                newTable.Rows.Add(row.id, row.podkategorija, row.cijena, row.ime_korisnika, row.objavljeno, row.url);
            }

            StringBuilder data = AktidoCore.ConvertDataTableToCsvFile(newTable);
            AktidoCore.DialogSave(data);
        }

        private void btn_CleanDatagrid_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.Items.Count != 0)
            {
                dataGrid.Items.Clear();
                dataGrid.Items.Refresh();
                txt_Rez.Content = "Broj rezultata: ";
            }
            else AktidoCore.ShowInfo("Tabela je već prazna!");
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
