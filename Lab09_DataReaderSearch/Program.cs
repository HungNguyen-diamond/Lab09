using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Lab09_DataReaderSearch
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public static class DbConnectionFactory
    {
        private const string ConnStr =
            @"Server=DESKTOP-9D35S54;Database=QLSinhVien;Integrated Security=True;TrustServerCertificate=True;";

        public static SqlConnection Create()
        {
            return new SqlConnection(ConnStr);
        }
    }
}