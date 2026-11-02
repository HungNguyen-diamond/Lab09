LAB 9 - ĐỌC, TÌM KIẾM VÀ LỌC DỮ LIỆU BẰNG SQLDATAREADER
Học phần : Lập trình .NET (IT3212)
Sinh viên: Nguyễn Tiến Hùng
MSSV     : 24000091
Lớp      : Lập trình .NET-1-1-26(N03)

------------------------------------------------------------
1. MÔI TRƯỜNG
------------------------------------------------------------
- Visual Studio, project Windows Forms (.NET Framework 4.8), ngôn ngữ C#.
- SQL Server + SSMS, CSDL QLSinhVien (bảng Khoa, SinhVien).

------------------------------------------------------------
2. CÁCH CHẠY
------------------------------------------------------------
Bước 1. Chuẩn bị CSDL
  - Dùng CSDL QLSinhVien đã có từ các lab trước (29 sinh viên, 3 khoa: OTO, CNTT, KT
    => đủ 3 trang khi hiển thị 10 dòng/trang).
  - Nếu máy chưa có CSDL: chạy tệp database.sql trong SSMS để tạo bảng và dữ liệu mẫu
    (35 sinh viên, 4 trang).
  - Cấu trúc bảng sử dụng:
      Khoa(MaKhoa, TenKhoa)
      SinhVien(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)

Bước 2. Cấu hình kết nối
  - Mở Program.cs, lớp DbConnectionFactory, hằng ConnStr:
      Server=DESKTOP-9D35S54;Database=QLSinhVien;Integrated Security=True;TrustServerCertificate=True;
  - Chạy trên máy khác: thay DESKTOP-9D35S54 bằng tên Server hiển thị trong SSMS
    (ví dụ .\SQLEXPRESS hoặc (localdb)\MSSQLLocalDB).

Bước 3. Chạy
  - Mở Lab09_DataReaderSearch.csproj bằng Visual Studio, nhấn F5.

------------------------------------------------------------
3. CẤU TRÚC MÃ NGUỒN
------------------------------------------------------------
- Program.cs             : điểm vào chương trình + DbConnectionFactory (tạo SqlConnection).
- SinhVienRepository.cs  : toàn bộ truy vấn SQL (SqlDataReader, SQL tham số hóa).
- MainForm.cs            : giao diện (bộ lọc, DataGridView, vùng chi tiết, phân trang,
                           thanh trạng thái) và xử lý sự kiện.
- database.sql           : script tạo CSDL và dữ liệu mẫu (tùy chọn).

------------------------------------------------------------
4. PHẦN ĐÃ HOÀN THÀNH (10/10 BÀI)
------------------------------------------------------------
Bài 1  Nạp SqlDataReader vào DataTable, hiển thị DataGridView
       -> SinhVienRepository.LoadStudents()
Bài 2  Tìm gần đúng theo họ tên (LIKE, thêm % vào giá trị tham số)
       -> SinhVienRepository.SearchByName()
Bài 3  Lọc theo khoa và khoảng điểm (BETWEEN, mọi giá trị là parameter)
       -> SinhVienRepository.FilterByKhoaDiem()
Bài 4  Phân trang OFFSET-FETCH, 10 sinh viên/trang, nút Trước/Sau
       -> SinhVienRepository.GetPage(); trên form dùng Search() có kèm phân trang
Bài 5  Hiển thị chi tiết dòng được chọn, kiểm tra CurrentRow và DBNull
       -> MainForm.dgvSinhVien_SelectionChanged
Bài 6  Tìm kiếm đa tiêu chí: mã, họ tên, khoa, giới tính, khoảng điểm; ô trống = bỏ qua
       -> SinhVienRepository.Search() / BuildWhere()
Bài 7  Sắp xếp theo cột và chiều tăng/giảm; chỉ dùng danh sách cột hợp lệ (whitelist
       SortColumns), tên cột không truyền bằng SqlParameter
Bài 8  Lọc theo tuổi: quy đổi tuổi thành khoảng ngày sinh nên chính xác theo ngày/tháng
       -> BuildWhere()
Bài 9  Định dạng DataGridView: ngày dd/MM/yyyy, điểm 0.00, tiêu đề tiếng Việt;
       điểm < 5 tô đỏ nhạt, điểm >= 8 tô xanh nhạt
       -> MainForm.FormatGrid(), dgvSinhVien_CellFormatting
Bài 10 Xuất CSV đúng tập dữ liệu đang hiển thị trên lưới (trang hiện tại, theo bộ lọc),
       mã hóa UTF-8 có BOM để Excel đọc đúng tiếng Việt
       -> MainForm.ExportCsv()

------------------------------------------------------------
5. DỮ LIỆU THỬ GỢI Ý
------------------------------------------------------------
- Họ tên: nhập "Văn", "Thị"... rồi bấm Tìm kiếm.
- Khoa: chọn Công nghệ thông tin / Kinh tế / ...; điểm từ 5 đến 8.
- Tuổi từ 20 đến 21; giới tính Nam/Nữ.
- Sắp xếp theo Điểm TB, chiều Giảm.
- Trang 2: có dòng điểm thấp (SV019, SV021 - tô đỏ) và điểm cao (SV013, SV014, SV015 - tô xanh).
- Dòng có Email hoặc điểm NULL: chương trình không báo lỗi, ô chi tiết để trống.

------------------------------------------------------------
6. XỬ LÝ LỖI VÀ BẢO MẬT
------------------------------------------------------------
- Mọi giá trị người dùng nhập đều đi qua SqlParameter, không nối chuỗi SQL.
- Kiểm tra dữ liệu nhập: điểm 0-10, tuổi 0-120, giá trị min không lớn hơn max.
- Bắt SqlException khi kết nối hoặc truy vấn lỗi và báo bằng MessageBox.
- Bắt IOException khi xuất CSV (ví dụ tệp đang mở trong Excel).

------------------------------------------------------------
7. SẢN PHẨM NỘP
------------------------------------------------------------
- Thư mục mã nguồn Lab9 (đã xóa thư mục bin, obj), build và chạy được.
- Ảnh chụp kết quả từng bài tập.
- Tệp README.txt này.