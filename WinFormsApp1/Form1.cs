using Npgsql;
using System.Data;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {

        private string connectionString = "Host=localhost;Port=5432;Username=mustafa;Password=102030;Database=TokenMinted";

        private NpgsqlConnection connection;

        public Form1()
        {
            InitializeComponent();
            InitializePostgresListener();
        }

        private async void Form1_LoadAsync(object sender, EventArgs e)
        {
            LoadData();
            await ListenData();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Uygulama kapatýldýðýnda PostgreSQL dinlemeyi kapat
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        private void InitializePostgresListener()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using (var cmd = new NpgsqlCommand("LISTEN accounts_changed", connection))
            {
                cmd.ExecuteNonQuery();
            }

            connection.Notification += new NotificationEventHandler(OnPostgresNotification);
        }

        private void OnPostgresNotification(object sender, NpgsqlNotificationEventArgs e)
        {
            string payload = e.Payload;

            // Assuming payload is in JSON format, parse it and add to ListView
            try
            {
                // Parse JSON
                // Adjust this part based on your actual JSON structure
                var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<MinterObjGeneral>(payload);
                Console.WriteLine(jsonObject.ToString());
                // Add individual properties to ListView columns
                ListViewItem item = new ListViewItem(new string[]
                {
                    jsonObject.Record.minter.ToString(),
                    jsonObject.Record.TokenId.ToString(),
                    jsonObject.Record.PricePaid.ToString()
                });

                listView1.Invoke((MethodInvoker)(() => listView1.Items.Add(item)));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing JSON payload: " + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(this.connectionString))
                {
                    connection.Open();

                    // PostgreSQL sorgusu
                    string query = "SELECT * FROM mustafa";
                    listView1.View = View.Details;
                    listView1.Columns.Add("Minter");
                    listView1.Columns.Add("Token ID");
                    listView1.Columns.Add("Price Paid");
                    listView1.GridLines = true;

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            // ListView temizle
                            listView1.Items.Clear();

                            while (reader.Read())
                            {
                                listView1.Items.Add(new ListViewItem(new string[] { reader["minter"].ToString(), reader["tokenid"].ToString(), reader["pricepaid"].ToString() }));

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private async Task ListenData()
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            conn.Notification += new NotificationEventHandler(OnPostgresNotification);

            using (var cmd = new NpgsqlCommand("LISTEN accounts_changed", conn))
            {
                cmd.ExecuteNonQuery();
            }

            while (true)
                await conn.WaitAsync(); // wait for events
        }

        //this.ListenData();
    }
    }

public class MinterObjGeneral
{
    public string Operation { get; set; }
    public MinterObj Record { get; set; }
}
public class MinterObj
{
    public string ID { get; set; }
    public string minter { get; set; }
    public string TokenId { get; set; }
    public string PricePaid { get; set; }
}