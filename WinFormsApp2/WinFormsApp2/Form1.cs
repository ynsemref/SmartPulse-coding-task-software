using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private async void button1_ClickAsync(object sender, EventArgs e)
        {



            try
            {
                // API'den verileri al
                var client = new HttpClient();
                var response = await client.GetAsync("https://seffaflik.epias.com.tr/transparency/service/market/intra-day-trade-history?endDate=2022-02-07&startDate=2022-02-07");

                var jsonString = await response.Content.ReadAsStringAsync();

                // Verileri DataGridView'e y�kle
                var dataTable = new DataTable();
                dataTable.Columns.Add("Id", typeof(string));
                dataTable.Columns.Add("Date", typeof(DateTime));
                dataTable.Columns.Add("Contract", typeof(string));
                dataTable.Columns.Add("Price", typeof(decimal));
                dataTable.Columns.Add("Quantity", typeof(decimal));

                var data = JObject.Parse(jsonString)["body"]["intraDayTradeHistoryList"];
                foreach (var item in data)
                {
                    if (((string)item["conract"]).StartsWith("PB"))
                        continue;



                    dataTable.Rows.Add(
                        item["id"].ToString(),
                        DateTime.ParseExact(item["date"].ToString(), "d.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                        item["conract"].ToString(),
                        decimal.Parse(item["price"].ToString()),
                        decimal.Parse(item["quantity"].ToString()) / 10
                    );

                }
                // Gruplama i�lemi
                var groupedData = dataTable.AsEnumerable()
                    .GroupBy(row => row.Field<string>("Contract"))
                    .Select(g => new
                    {
                        Contract = g.Key,
                        TotalQuantity = g.Sum(row => row.Field<decimal>("Quantity")),
                        TotalPrice = g.Sum(row => row.Field<decimal>("Price") * row.Field<decimal>("Quantity") / 10),
                        WeightedAveragePrice = g.Sum(row => row.Field<decimal>("Price") * row.Field<decimal>("Quantity") / 10) / g.Sum(row => row.Field<decimal>("Quantity"))
                    })
                    .ToList();

                // S�tunlar� DataGridView'e ekleme
                dataTable.Columns.Add("Toplam ��lem Miktar�", typeof(decimal));
                dataTable.Columns.Add("Toplam ��lem Tutar�", typeof(decimal));
                dataTable.Columns.Add("A��rl�kl� Ortalama Fiyat", typeof(decimal));

                foreach (DataRow row in dataTable.Rows)
                {
                    var contract = row.Field<string>("Contract");
                    var group = groupedData.Find(g => g.Contract == contract);

                    if (group != null)
                    {
                        row.SetField("Toplam ��lem Miktar�", group.TotalQuantity);
                        row.SetField("Toplam ��lem Tutar�", group.TotalPrice);
                        row.SetField("A��rl�kl� Ortalama Fiyat", group.WeightedAveragePrice);
                    }
                }

                // DataGridView'e DataSource olarak dataTable'� ata
                dataGridView1.DataSource = dataTable;


                dataGridView1.DataSource = dataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }




        }

    }
}