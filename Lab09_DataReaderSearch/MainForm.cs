using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Lab09_DataReaderSearch
{
    public class MainForm : Form
    {
        private readonly SinhVienRepository repo = new SinhVienRepository();

        // Vùng bộ lọc
        private TextBox txtLocMa, txtLocTen, txtDiemMin, txtDiemMax, txtTuoiMin, txtTuoiMax;
        private ComboBox cboKhoa, cboGioi, cboSort, cboDir;
        private Button btnTim, btnLamMoi, btnTruoc, btnSau, btnXuat;
        private Label lblTrang;

        // Lưới + vùng chi tiết + thanh trạng thái
        private DataGridView dgvSinhVien;
        private TextBox txtMaSV, txtHoTen, txtNgaySinh, txtKhoaCT, txtDiemTB, txtEmail;
        private ToolStripStatusLabel lblTrangThai;

        // Trạng thái phân trang
        private SearchCriteria criteria = new SearchCriteria();
        private int page = 1, totalPages = 1, totalRows = 0;
        private const int PageSize = 10;
        private DataTable current;

        public MainForm()
        {
            BuildUi();
            Load += MainForm_Load;
        }

        // =============================== GIAO DIỆN ===============================
        private void BuildUi()
        {
            Text = "Lab 9 - Đọc, tìm kiếm và lọc dữ liệu bằng SqlDataReader";
            Width = 1100; Height = 720;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            // --- vùng bộ lọc ---
            FlowLayoutPanel pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8),
                BackColor = Color.WhiteSmoke
            };
            txtLocMa = Tb(80); txtLocTen = Tb(140);
            cboKhoa = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cboGioi = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cboGioi.Items.AddRange(new object[] { "Tất cả", "Nam", "Nữ" });
            cboGioi.SelectedIndex = 0;
            txtDiemMin = Tb(45); txtDiemMax = Tb(45); txtTuoiMin = Tb(40); txtTuoiMax = Tb(40);
            cboSort = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (string k in SinhVienRepository.SortColumns.Keys) cboSort.Items.Add(k);
            cboSort.SelectedIndex = 0;
            cboDir = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cboDir.Items.AddRange(new object[] { "Tăng", "Giảm" });
            cboDir.SelectedIndex = 0;

            btnTim = new Button { Text = "Tìm kiếm", AutoSize = true };
            btnLamMoi = new Button { Text = "Làm mới", AutoSize = true };
            btnXuat = new Button { Text = "Xuất CSV", AutoSize = true };
            btnTim.Click += (s, e) => DoSearch(true, true);
            btnLamMoi.Click += BtnLamMoi_Click;
            btnXuat.Click += BtnXuat_Click;

            pnlFilter.Controls.AddRange(new Control[] {
                Lb("Mã SV"), txtLocMa, Lb("Họ tên"), txtLocTen, Lb("Khoa"), cboKhoa,
                Lb("Giới tính"), cboGioi, Lb("Điểm từ"), txtDiemMin, Lb("đến"), txtDiemMax,
                Lb("Tuổi từ"), txtTuoiMin, Lb("đến"), txtTuoiMax,
                Lb("Sắp xếp"), cboSort, cboDir, btnTim, btnLamMoi, btnXuat });

            // Enter ở ô nhập = tìm kiếm
            AcceptButton = btnTim;

            // --- thanh phân trang ---
            FlowLayoutPanel pnlPaging = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight
            };
            btnTruoc = new Button { Text = "< Trước", AutoSize = true };
            btnSau = new Button { Text = "Sau >", AutoSize = true };
            lblTrang = new Label { AutoSize = true, Padding = new Padding(10, 6, 10, 0) };
            btnTruoc.Click += (s, e) => { if (page > 1) { page--; DoSearch(false, false); } };
            btnSau.Click += (s, e) => { if (page < totalPages) { page++; DoSearch(false, false); } };
            pnlPaging.Controls.AddRange(new Control[] { btnTruoc, lblTrang, btnSau });

            // --- vùng chi tiết ---
            GroupBox grpDetail = new GroupBox { Text = "Chi tiết sinh viên", Dock = DockStyle.Bottom, Height = 110 };
            txtMaSV = RoTb(); txtHoTen = RoTb(); txtNgaySinh = RoTb();
            txtKhoaCT = RoTb(); txtDiemTB = RoTb(); txtEmail = RoTb();
            TableLayoutPanel tl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2, Padding = new Padding(6)
            };
            for (int i = 0; i < 6; i++)
                tl.ColumnStyles.Add(new ColumnStyle(i % 2 == 0 ? SizeType.AutoSize : SizeType.Percent, 33));
            tl.Controls.Add(Lb("Mã SV"), 0, 0);   tl.Controls.Add(txtMaSV, 1, 0);
            tl.Controls.Add(Lb("Họ tên"), 2, 0);  tl.Controls.Add(txtHoTen, 3, 0);
            tl.Controls.Add(Lb("Ngày sinh"), 4, 0); tl.Controls.Add(txtNgaySinh, 5, 0);
            tl.Controls.Add(Lb("Khoa"), 0, 1);    tl.Controls.Add(txtKhoaCT, 1, 1);
            tl.Controls.Add(Lb("Điểm TB"), 2, 1); tl.Controls.Add(txtDiemTB, 3, 1);
            tl.Controls.Add(Lb("Email"), 4, 1);   tl.Controls.Add(txtEmail, 5, 1);
            grpDetail.Controls.Add(tl);

            // --- thanh trạng thái ---
            StatusStrip status = new StatusStrip();
            lblTrangThai = new ToolStripStatusLabel("Sẵn sàng");
            status.Items.Add(lblTrangThai);

            // --- DataGridView ---
            dgvSinhVien = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, RowHeadersVisible = false
            };
            dgvSinhVien.SelectionChanged += dgvSinhVien_SelectionChanged;
            dgvSinhVien.CellFormatting += dgvSinhVien_CellFormatting;

            // Thứ tự add: control Fill phải add đầu tiên để nằm dưới cùng thứ tự dock
            Controls.Add(dgvSinhVien);
            Controls.Add(pnlPaging);
            Controls.Add(grpDetail);
            Controls.Add(pnlFilter);
            Controls.Add(status);
        }

        private static Label Lb(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        }
        private static TextBox Tb(int w) { return new TextBox { Width = w }; }
        private static TextBox RoTb() { return new TextBox { ReadOnly = true, Dock = DockStyle.Fill }; }

        // =============================== SỰ KIỆN ===============================
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                DataTable khoa = repo.LoadKhoa();
                DataRow all = khoa.NewRow();
                all["MaKhoa"] = "";
                all["TenKhoa"] = "Tất cả";
                khoa.Rows.InsertAt(all, 0);
                cboKhoa.DataSource = khoa;
                cboKhoa.DisplayMember = "TenKhoa";
                cboKhoa.ValueMember = "MaKhoa";
                DoSearch(true, true);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Không kết nối được CSDL:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblTrangThai.Text = "Lỗi kết nối CSDL";
            }
        }

        private void BtnLamMoi_Click(object sender, EventArgs e)
        {
            txtLocMa.Clear(); txtLocTen.Clear(); txtDiemMin.Clear(); txtDiemMax.Clear();
            txtTuoiMin.Clear(); txtTuoiMax.Clear();
            cboKhoa.SelectedIndex = 0; cboGioi.SelectedIndex = 0;
            cboSort.SelectedIndex = 0; cboDir.SelectedIndex = 0;
            DoSearch(true, true);
        }

        /// <summary>readCriteria = true: đọc lại điều kiện từ form; false: dùng điều kiện đã lưu (khi chuyển trang).</summary>
        private void DoSearch(bool resetPage, bool readCriteria)
        {
            if (readCriteria)
            {
                SearchCriteria c;
                if (!ReadCriteria(out c)) return;
                criteria = c;
            }
            if (resetPage) page = 1;

            try
            {
                bool asc = cboDir.SelectedIndex == 0;
                current = repo.Search(criteria, cboSort.Text, asc, page, PageSize, out totalRows);
                totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)PageSize));
                if (page > totalPages)
                {
                    page = totalPages;
                    current = repo.Search(criteria, cboSort.Text, asc, page, PageSize, out totalRows);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Lỗi truy vấn dữ liệu:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dgvSinhVien.DataSource = current;
            FormatGrid();
            lblTrang.Text = "Trang " + page + "/" + totalPages;
            btnTruoc.Enabled = page > 1;
            btnSau.Enabled = page < totalPages;
            lblTrangThai.Text = "Tìm thấy " + totalRows + " sinh viên";
            if (current.Rows.Count == 0) ClearDetail();
        }

        /// <summary>Đọc và kiểm tra điều kiện; ô trống = bỏ qua tiêu chí.</summary>
        private bool ReadCriteria(out SearchCriteria c)
        {
            c = new SearchCriteria();
            c.MaSV = txtLocMa.Text;
            c.HoTen = txtLocTen.Text;
            c.MaKhoa = Convert.ToString(cboKhoa.SelectedValue);
            c.GioiTinh = cboGioi.SelectedIndex;

            decimal? dmin, dmax; int? tmin, tmax;
            if (!TryDecimal(txtDiemMin, "Điểm từ", out dmin)) return false;
            if (!TryDecimal(txtDiemMax, "Điểm đến", out dmax)) return false;
            if (!TryInt(txtTuoiMin, "Tuổi từ", out tmin)) return false;
            if (!TryInt(txtTuoiMax, "Tuổi đến", out tmax)) return false;

            if (dmin.HasValue && dmax.HasValue && dmin > dmax)
            { Warn("Điểm tối thiểu không được lớn hơn điểm tối đa.", txtDiemMin); return false; }
            if (tmin.HasValue && tmax.HasValue && tmin > tmax)
            { Warn("Tuổi tối thiểu không được lớn hơn tuổi tối đa.", txtTuoiMin); return false; }

            c.DiemMin = dmin; c.DiemMax = dmax; c.TuoiMin = tmin; c.TuoiMax = tmax;
            return true;
        }

        private bool TryDecimal(TextBox tb, string name, out decimal? value)
        {
            value = null;
            string s = tb.Text.Trim().Replace(',', '.');
            if (s.Length == 0) return true;
            decimal d;
            if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d) || d < 0 || d > 10)
            { Warn(name + " phải là số từ 0 đến 10.", tb); return false; }
            value = d; return true;
        }

        private bool TryInt(TextBox tb, string name, out int? value)
        {
            value = null;
            string s = tb.Text.Trim();
            if (s.Length == 0) return true;
            int n;
            if (!int.TryParse(s, out n) || n < 0 || n > 120)
            { Warn(name + " phải là số nguyên từ 0 đến 120.", tb); return false; }
            value = n; return true;
        }

        private void Warn(string msg, Control focus)
        {
            MessageBox.Show(msg, "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focus.Focus();
        }

        // =============================== BÀI 9: ĐỊNH DẠNG LƯỚI ===============================
        private void FormatGrid()
        {
            SetHeader("MaSV", "Mã SV"); SetHeader("HoTen", "Họ tên"); SetHeader("NgaySinh", "Ngày sinh");
            SetHeader("GioiTinh", "Giới tính"); SetHeader("TenKhoa", "Khoa");
            SetHeader("DiemTB", "Điểm TB"); SetHeader("Email", "Email");

            dgvSinhVien.Columns["NgaySinh"].DefaultCellStyle.Format = "dd/MM/yyyy";
            dgvSinhVien.Columns["DiemTB"].DefaultCellStyle.Format = "0.00";
            dgvSinhVien.Columns["DiemTB"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSinhVien.EnableHeadersVisualStyles = false;
            dgvSinhVien.ColumnHeadersDefaultCellStyle.BackColor = Color.SteelBlue;
            dgvSinhVien.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSinhVien.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            dgvSinhVien.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void SetHeader(string col, string text)
        {
            dgvSinhVien.Columns[col].HeaderText = text;
        }

        // Điểm < 5: đỏ nhạt; điểm >= 8: xanh nhạt
        private void dgvSinhVien_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !dgvSinhVien.Columns.Contains("DiemTB")) return;
            object v = dgvSinhVien.Rows[e.RowIndex].Cells["DiemTB"].Value;
            if (v == null || v == DBNull.Value) return;
            decimal d = Convert.ToDecimal(v);
            if (d < 5m) e.CellStyle.BackColor = Color.MistyRose;
            else if (d >= 8m) e.CellStyle.BackColor = Color.Honeydew;
        }

        // =============================== BÀI 5: CHI TIẾT DÒNG CHỌN ===============================
        private void dgvSinhVien_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSinhVien.CurrentRow == null) return;
            DataGridViewRow row = dgvSinhVien.CurrentRow;
            if (!dgvSinhVien.Columns.Contains("MaSV")) return;

            // Convert.ToString(DBNull) trả về "" nên an toàn với giá trị NULL
            txtMaSV.Text = Convert.ToString(row.Cells["MaSV"].Value);
            txtHoTen.Text = Convert.ToString(row.Cells["HoTen"].Value);
            txtKhoaCT.Text = Convert.ToString(row.Cells["TenKhoa"].Value);
            txtEmail.Text = Convert.ToString(row.Cells["Email"].Value);

            object ns = row.Cells["NgaySinh"].Value;
            txtNgaySinh.Text = (ns == null || ns == DBNull.Value) ? "" : Convert.ToDateTime(ns).ToString("dd/MM/yyyy");

            object diem = row.Cells["DiemTB"].Value;
            txtDiemTB.Text = (diem == null || diem == DBNull.Value) ? "(chưa có)" : Convert.ToDecimal(diem).ToString("0.00");

            lblTrangThai.Text = "Đang chọn: " + txtMaSV.Text;
        }

        private void ClearDetail()
        {
            txtMaSV.Clear(); txtHoTen.Clear(); txtNgaySinh.Clear();
            txtKhoaCT.Clear(); txtDiemTB.Clear(); txtEmail.Clear();
        }

        // =============================== BÀI 10: XUẤT CSV ===============================
        private void BtnXuat_Click(object sender, EventArgs e)
        {
            if (current == null || current.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv", FileName = "KetQuaTimKiem.csv"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    ExportCsv(dlg.FileName);
                    lblTrangThai.Text = "Đã xuất " + current.Rows.Count + " dòng ra " + dlg.FileName;
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Không ghi được tệp (đang mở bằng chương trình khác?):\n" + ex.Message,
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>Xuất đúng tập đang hiển thị trên lưới, UTF-8 có BOM để Excel đọc đúng tiếng Việt.</summary>
        private void ExportCsv(string path)
        {
            StringBuilder sb = new StringBuilder();
            string[] headers = new string[dgvSinhVien.Columns.Count];
            for (int i = 0; i < headers.Length; i++) headers[i] = Csv(dgvSinhVien.Columns[i].HeaderText);
            sb.AppendLine(string.Join(",", headers));

            foreach (DataRow r in current.Rows)
            {
                string[] cells = new string[current.Columns.Count];
                for (int i = 0; i < cells.Length; i++)
                {
                    object v = r[i];
                    string s;
                    if (v == DBNull.Value) s = "";
                    else if (v is DateTime) s = ((DateTime)v).ToString("dd/MM/yyyy");
                    else if (v is decimal) s = ((decimal)v).ToString("0.00", CultureInfo.InvariantCulture);
                    else s = Convert.ToString(v);
                    cells[i] = Csv(s);
                }
                sb.AppendLine(string.Join(",", cells));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static string Csv(string s)
        {
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
