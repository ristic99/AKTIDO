using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {   
        public Options()
        {
            InitializeComponent();

            txtDb.Text = Database.database.Database;
            txtIP.Text = Database.database.Server;
            txtPwd.Password = Database.database.Password;
            txtUser.Text = Database.database.UserID;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCred();
            this.Close();
        }

        private void SaveCred()
        {
            Database.database.UserID = txtUser.Text;
            Database.database.Database = txtDb.Text;
            Database.database.Password = txtPwd.Password;
            Database.database.Server = txtIP.Text;
            AktidoCore.SaveData(new StringBuilder(JsonConvert.SerializeObject(Database.database)), AktidoCore.config_path);
        }
    }
}
