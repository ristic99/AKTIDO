using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Aktido.Classes;
using Newtonsoft.Json;

namespace Aktido.Views
{
	
    public partial class ArtikalWindow : Window
    {
        Nekretnina nekretnina;

        public ArtikalWindow(int id)
        {
            InitializeComponent();
            nekretnina = JsonConvert.DeserializeObject<Nekretnina>(Database.DBLoadNekretnina(id));

            txtKorisnik.Text = "Korisnik: " + nekretnina.artikal.korisnik.ime;
            txtObjavljeno.Text = "Objavljeno: " + AktidoCore.UnixTimestampToDateTime(nekretnina.artikal.timestamp);
            txtGrad.Text = "Grad: " + nekretnina.artikal.grad;
            txtCijena.Text = "Cijena: " + nekretnina.artikal.cijena;
            txtNaslov.Text = nekretnina.artikal.naslov;

            List<Osobine> osobine = new List<Osobine>();
            osobine = nekretnina.artikal.osobine;
            
            foreach(Osobine osobina in osobine)
                listBox_Osobine.Items.Add(new TextBlock { FontSize = 14, Text = osobina.naziv + ": " + osobina.vrijednost });

            textBox.Text = nekretnina.artikal.detaljni_opis;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnLink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("www.olx.ba/artikal/" + nekretnina.artikal.id);
        }
    }
}
