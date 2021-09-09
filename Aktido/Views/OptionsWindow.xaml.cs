using System.Text;
using System.Windows;
using Aktido.Classes;
using Newtonsoft.Json;

namespace Aktido.Views
{
    public partial class OptionsWindow : Window
    {   
        public OptionsWindow()
        {
            InitializeComponent();

            txtDb.Text = Database.database.Database;
            txtIP.Text = Database.database.Server;
            txtPwd.Password = Database.database.Password;
            txtUser.Text = Database.database.UserID;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Database.database.UserID = txtUser.Text;
            Database.database.Database = txtDb.Text;
            Database.database.Password = txtPwd.Password;
            Database.database.Server = txtIP.Text;

            AktidoCore.SaveData(new StringBuilder(JsonConvert.SerializeObject(Database.database)), Constants.config_path);

            this.Close();
        }
    }
}
