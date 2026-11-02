using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Lab09_DataReaderSearch
{
    /// <summary>Tiêu chí tìm kiếm đa tiêu chí. Giá trị null/rỗng = bỏ qua tiêu chí đó.</summary>
    public class SearchCriteria
    {
        public string MaSV;
        public string HoTen;
        public string MaKhoa;          // rỗng = tất cả khoa
        public int GioiTinh;           // 0 = tất cả, 1 = Nam, 2 = Nữ
        public decimal? DiemMin, DiemMax;
        public int? TuoiMin, TuoiMax;
    }

    public class SinhVienRepository
    {
        private const string SelectCols = @"
            SELECT sv.MaSV, sv.HoTen, sv.NgaySinh,
                   CASE sv.GioiTinh WHEN 1 THEN N'Nam' ELSE N'Nữ' END AS GioiTinh,
                   k.TenKhoa, sv.DiemTB, sv.Email
            FROM SinhVien sv JOIN Khoa k ON sv.MaKhoa = k.MaKhoa";

        // Bài 7: danh sách cột hợp lệ (whitelist) - tên cột KHÔNG đi qua SqlParameter
        public static readonly Dictionary<string, string> SortColumns = new Dictionary<string, string>
        {
            { "Mã SV",     "sv.MaSV" },
            { "Họ tên",    "sv.HoTen" },
            { "Ngày sinh", "sv.NgaySinh" },
            { "Khoa",      "k.TenKhoa" },
            { "Điểm TB",   "sv.DiemTB" }
        };

        // ---------- Bài 1: nạp SqlDataReader vào DataTable ----------
        public DataTable LoadStudents()
        {
            DataTable table = new DataTable();
            using (SqlConnection conn = DbConnectionFactory.Create())
            using (SqlCommand cmd = new SqlCommand(SelectCols, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader()) table.Load(reader);
            }
            return table;
        }

        // ---------- Danh sách khoa cho ComboBox (đọc bằng SqlDataReader) ----------
        public DataTable LoadKhoa()
        {
            DataTable table = new DataTable();
            using (SqlConnection conn = DbConnectionFactory.Create())
            using (SqlCommand cmd = new SqlCommand("SELECT MaKhoa, TenKhoa FROM Khoa ORDER BY TenKhoa", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader()) table.Load(reader);
            }
            return table;
        }

        // ---------- Bài 2: tìm gần đúng theo họ tên ----------
        public DataTable SearchByName(string keyword)
        {
            string sql = @"SELECT MaSV, HoTen, MaKhoa, DiemTB
                           FROM SinhVien
                           WHERE HoTen LIKE @Keyword
                           ORDER BY HoTen";
            DataTable table = new DataTable();
            using (SqlConnection conn = DbConnectionFactory.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value =
                    "%" + (keyword ?? "").Trim() + "%";
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader()) table.Load(reader);
            }
            return table;
        }

        // ---------- Bài 3: lọc theo khoa và khoảng điểm ----------
        public DataTable FilterByKhoaDiem(string maKhoa, decimal min, decimal max)
        {
            string sql = @"SELECT MaSV, HoTen, MaKhoa, DiemTB
                           FROM SinhVien
                           WHERE DiemTB BETWEEN @Min AND @Max";
            if (!string.IsNullOrEmpty(maKhoa)) sql += " AND MaKhoa=@MaKhoa";

            DataTable table = new DataTable();
            using (SqlConnection conn = DbConnectionFactory.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Min", SqlDbType.Decimal).Value = min;
                cmd.Parameters.Add("@Max", SqlDbType.Decimal).Value = max;
                if (!string.IsNullOrEmpty(maKhoa))
                    cmd.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = maKhoa;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader()) table.Load(reader);
            }
            return table;
        }

        // ---------- Bài 4: phân trang OFFSET-FETCH ----------
        public DataTable GetPage(int pageIndex, int pageSize)
        {
            string sql = @"SELECT MaSV, HoTen, MaKhoa, DiemTB
                           FROM SinhVien
                           ORDER BY MaSV
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            DataTable table = new DataTable();
            using (SqlConnection conn = DbConnectionFactory.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageIndex - 1) * pageSize;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader()) table.Load(reader);
            }
            return table;
        }

        // ---------- Bài 6 + 7 + 8 (+ 4): tìm đa tiêu chí, sắp xếp, lọc tuổi, phân trang ----------
        public DataTable Search(SearchCriteria c, string sortKey, bool ascending,
                                int pageIndex, int pageSize, out int totalRows)
        {
            string orderCol;
            if (sortKey == null || !SortColumns.TryGetValue(sortKey, out orderCol)) orderCol = "sv.MaSV";
            string dir = ascending ? "ASC" : "DESC";
            string orderBy = orderCol + " " + dir + (orderCol == "sv.MaSV" ? "" : ", sv.MaSV");

            DataTable table = new DataTable();
            using (SqlConnection conn = DbConnectionFactory.Create())
            {
                conn.Open();

                // Đếm tổng số dòng thỏa điều kiện (để tính số trang)
                using (SqlCommand cmdCount = new SqlCommand(null, conn))
                {
                    string where = BuildWhere(c, cmdCount);
                    cmdCount.CommandText =
                        "SELECT COUNT(*) FROM SinhVien sv JOIN Khoa k ON sv.MaKhoa = k.MaKhoa" + where;
                    totalRows = Convert.ToInt32(cmdCount.ExecuteScalar());
                }

                // Lấy dữ liệu trang hiện tại
                using (SqlCommand cmd = new SqlCommand(null, conn))
                {
                    string where = BuildWhere(c, cmd);
                    cmd.CommandText = SelectCols + where +
                        " ORDER BY " + orderBy +
                        " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                    cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (pageIndex - 1) * pageSize;
                    cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                    using (SqlDataReader reader = cmd.ExecuteReader()) table.Load(reader);
                }
            }
            return table;
        }

        /// <summary>Xây mệnh đề WHERE; mọi giá trị đều là parameter.</summary>
        private static string BuildWhere(SearchCriteria c, SqlCommand cmd)
        {
            StringBuilder sb = new StringBuilder(" WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(c.MaSV))
            {
                sb.Append(" AND sv.MaSV LIKE @MaSV");
                cmd.Parameters.Add("@MaSV", SqlDbType.VarChar, 10).Value = "%" + c.MaSV.Trim() + "%";
            }
            if (!string.IsNullOrWhiteSpace(c.HoTen))
            {
                sb.Append(" AND sv.HoTen LIKE @HoTen");
                cmd.Parameters.Add("@HoTen", SqlDbType.NVarChar, 100).Value = "%" + c.HoTen.Trim() + "%";
            }
            if (!string.IsNullOrEmpty(c.MaKhoa))
            {
                sb.Append(" AND sv.MaKhoa = @MaKhoa");
                cmd.Parameters.Add("@MaKhoa", SqlDbType.VarChar, 10).Value = c.MaKhoa;
            }
            if (c.GioiTinh != 0)
            {
                sb.Append(" AND sv.GioiTinh = @GioiTinh");
                cmd.Parameters.Add("@GioiTinh", SqlDbType.Bit).Value = (c.GioiTinh == 1);
            }
            if (c.DiemMin.HasValue)
            {
                sb.Append(" AND sv.DiemTB >= @DiemMin");
                SqlParameter p = cmd.Parameters.Add("@DiemMin", SqlDbType.Decimal);
                p.Precision = 4; p.Scale = 2; p.Value = c.DiemMin.Value;
            }
            if (c.DiemMax.HasValue)
            {
                sb.Append(" AND sv.DiemTB <= @DiemMax");
                SqlParameter p = cmd.Parameters.Add("@DiemMax", SqlDbType.Decimal);
                p.Precision = 4; p.Scale = 2; p.Value = c.DiemMax.Value;
            }

            // Bài 8: lọc theo tuổi - quy đổi tuổi thành khoảng ngày sinh để tính chính xác
            //  - Tuổi >= TuoiMin  <=>  NgaySinh <= hôm nay - TuoiMin năm
            //  - Tuổi <= TuoiMax  <=>  NgaySinh >  hôm nay - (TuoiMax+1) năm
            DateTime today = DateTime.Today;
            if (c.TuoiMin.HasValue)
            {
                sb.Append(" AND sv.NgaySinh <= @NgaySinhTo");
                cmd.Parameters.Add("@NgaySinhTo", SqlDbType.Date).Value = today.AddYears(-c.TuoiMin.Value);
            }
            if (c.TuoiMax.HasValue)
            {
                sb.Append(" AND sv.NgaySinh > @NgaySinhFrom");
                cmd.Parameters.Add("@NgaySinhFrom", SqlDbType.Date).Value = today.AddYears(-(c.TuoiMax.Value + 1));
            }
            return sb.ToString();
        }
    }
}
