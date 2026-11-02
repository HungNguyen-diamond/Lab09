-- Chạy trong SSMS trước khi chạy ứng dụng
IF DB_ID('QLSinhVien') IS NULL CREATE DATABASE QLSinhVien;
GO
USE QLSinhVien;
GO
IF OBJECT_ID('SinhVien') IS NOT NULL DROP TABLE SinhVien;
IF OBJECT_ID('Khoa') IS NOT NULL DROP TABLE Khoa;
GO
CREATE TABLE Khoa(
    MaKhoa  VARCHAR(10)   PRIMARY KEY,
    TenKhoa NVARCHAR(100) NOT NULL);
CREATE TABLE SinhVien(
    MaSV     VARCHAR(10)   PRIMARY KEY,
    HoTen    NVARCHAR(100) NOT NULL,
    NgaySinh DATE          NULL,
    GioiTinh BIT           NOT NULL,          -- 1 = Nam, 0 = Nữ
    MaKhoa   VARCHAR(10)   NOT NULL REFERENCES Khoa(MaKhoa),
    DiemTB   DECIMAL(4,2)  NULL,
    Email    NVARCHAR(100) NULL);
GO
INSERT Khoa VALUES ('CNTT',N'Công nghệ thông tin'),('KT',N'Kinh tế'),('NN',N'Ngoại ngữ');

DECLARE @i INT = 1;
WHILE @i <= 35   -- 35 dòng => 4 trang (10 dòng/trang)
BEGIN
    INSERT SinhVien(MaSV,HoTen,NgaySinh,GioiTinh,MaKhoa,DiemTB,Email)
    VALUES(
        'SV' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3),
        CHOOSE(@i%6+1,N'Nguyễn',N'Trần',N'Lê',N'Phạm',N'Hoàng',N'Vũ') + N' '
          + CHOOSE(@i%4+1,N'Văn',N'Thị',N'Minh',N'Đức') + N' '
          + CHOOSE(@i%7+1,N'An',N'Bình',N'Cường',N'Dung',N'Hùng',N'Lan',N'Mai'),
        DATEADD(DAY, @i*60, '1999-01-01'),
        @i % 2,
        CHOOSE(@i%3+1,'CNTT','KT','NN'),
        CAST(3 + ((@i*37) % 70) / 10.0 AS DECIMAL(4,2)),
        'sv' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3) + '@eaut.edu.vn');
    SET @i += 1;
END
-- Một dòng có NULL để kiểm tra xử lý DBNull
UPDATE SinhVien SET Email = NULL, DiemTB = NULL WHERE MaSV = 'SV035';
GO



USE QLSinhVien;
DELETE FROM SinhVien WHERE MaSV LIKE 'L9%';
SELECT COUNT(*) FROM SinhVien;   -- kết quả phải là 29