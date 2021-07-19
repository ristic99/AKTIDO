using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            {
                TextBlock txtNaziv = new TextBlock();
                TextBlock txtVrijendost = new TextBlock();

                txtNaziv.FontSize = 14;
                txtNaziv.Text = osobina.naziv + ": " + osobina.vrijednost;
                listBox_Osobine.Items.Add(txtNaziv);
            }

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
