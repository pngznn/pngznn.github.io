using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient; //Bước 0

namespace UngDungQLBH
{
    public partial class frmBan_Them : Form
    {
        string sCon = "Data Source=LAPTOP-7VPG5DKP\\PXPN;Initial Catalog=QLBH;Integrated Security=True;Encrypt=False";
        DataTable dsChiTiet = new DataTable();
        public frmBan_Them()
        {
            InitializeComponent();
        }

        //Các hàm
        //a. Load lại bảng datagridview1
        private void LoadData()
        {
            SqlConnection con = new SqlConnection(sCon);
            try
            {
                con.Open();
                string query = @"
                        SELECT BAN.MAHD, BAN.NGAY, BAN.TONGTIEN, BAN.PTTT, BAN.TONGSL, 
                               BAN.PHUTHU, BAN.MAKH, BAN.MANV,
                               CT.MAMON, CT.SOLUONG
                        FROM BAN
                        JOIN CHITIET_BAN CT ON BAN.MAHD = CT.MAHD";

                SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                dataGridView1.DataSource = dataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //b. Xóa các dữ liệu trong textbox (quay về ban đầu)
        private void ClearForm()
        {
            txtMaHD.Text = "";
            txtMaKH.Text = "";
            txtMaNV.Text = "";
            txtTongtien.Text = "";
            txtTongSL.Text = "";
            txtPhuThu.Text = "";
            dtpNgay.Value = DateTime.Now;
            cboPTTT.SelectedIndex = -1; //Không chọn giá trị nào trong ComboBox
            cboMon.SelectedIndex = -1;
            txtSoLuong.Text = "";
            txtSoTienGiam.Text = "";
            txtThanhTienSauKM.Text = "";
            dsChiTiet.Clear();
            dgvChiTietBan.DataSource = null;

            txtMaHD.Enabled = true; // Cho phép nhập lại mã hóa đơn
        }

        private void frmBan_FormClosing(object sender, FormClosingEventArgs e)
        {
            MessageBox.Show("Cảm ơn! Hẹn gặp lại lần sau!", "Thông báo");
        }

        private void txtTieuDe_Click(object sender, EventArgs e)
        {

        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            DialogResult ret = MessageBox.Show("Bạn có chắc chắn muốn thêm hóa đơn này không?", "Thông báo", MessageBoxButtons.OKCancel);
            if (ret == DialogResult.OK)
            {
                // Kiểm tra đầu vào
                if (string.IsNullOrWhiteSpace(txtMaHD.Text))
                {
                    MessageBox.Show("Vui lòng nhập mã hóa đơn.");
                    return;
                }

                if (dsChiTiet.Rows.Count == 0)
                {
                    MessageBox.Show("Vui lòng thêm ít nhất một món hàng.");
                    return;
                }

                // Tạo bản sao DataTable chỉ chứa 2 cột MAMON và SOLUONG
                DataTable dsChiTietTruyen = new DataTable();
                dsChiTietTruyen.Columns.Add("MAMON", typeof(string));
                dsChiTietTruyen.Columns.Add("SOLUONG", typeof(int));

                foreach (DataRow row in dsChiTiet.Rows)
                {
                    dsChiTietTruyen.Rows.Add(row["MAMON"], row["SOLUONG"]);
                }

                using (SqlConnection con = new SqlConnection(sCon))
                {
                    try
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("sp_KiemTra_Ban", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Truyền tham số vào thủ tục
                        cmd.Parameters.AddWithValue("@MAHD", txtMaHD.Text);
                        cmd.Parameters.AddWithValue("@MAKH", txtMaKH.Text);
                        cmd.Parameters.AddWithValue("@MANV", txtMaNV.Text);
                        cmd.Parameters.AddWithValue("@PTTT", cboPTTT.SelectedItem?.ToString() ?? "Tiền mặt");

                        // Tham số dạng bảng
                        SqlParameter tvpParam = cmd.Parameters.AddWithValue("@DS_CT_BAN", dsChiTietTruyen);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "dbo.DS_CT_BAN_TYPE";

                        cmd.ExecuteNonQuery();
                        // Sau khi thêm hóa đơn xong, gọi khuyến mãi
                        SqlCommand cmdKM = new SqlCommand("sp_AP_DUNG_KHUYEN_MAI", con);
                        cmdKM.CommandType = CommandType.StoredProcedure;
                        cmdKM.Parameters.AddWithValue("@MAHD", txtMaHD.Text.Trim());

                        // Thêm các tham số OUTPUT để lấy số tiền giảm và thành tiền sau KM
                        SqlParameter tienGiamParam = new SqlParameter("@TIENGIAM", SqlDbType.Decimal);
                        tienGiamParam.Direction = ParameterDirection.Output;
                        cmdKM.Parameters.Add(tienGiamParam);

                        SqlParameter thanhTienSauKMParam = new SqlParameter("@THANHTIEN_SAUKM", SqlDbType.Decimal);
                        thanhTienSauKMParam.Direction = ParameterDirection.Output;
                        cmdKM.Parameters.Add(thanhTienSauKMParam);


                        cmdKM.ExecuteNonQuery();

                        // Hiển thị lên TextBox
                        txtSoTienGiam.Text = string.Format("{0:N0}", tienGiamParam.Value);
                        txtThanhTienSauKM.Text = string.Format("{0:N0}", thanhTienSauKMParam.Value);

                        MessageBox.Show("Tạo hóa đơn thành công!");
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi không xác định:\n" + ex.Message, "Lỗi");
                    }

                    con.Close();
                }
            }
        }

        private void frmBan_Load(object sender, EventArgs e)
        {
            //Bước 1: KHởi tạo kết nối
            SqlConnection con = new SqlConnection(sCon);
            try
            {
                con.Open();
                string sQuery = "select * from BAN";
                SqlDataAdapter adapter = new SqlDataAdapter(sQuery, con);
                DataSet ds = new DataSet();
                adapter.Fill(ds, "Hóa đơn bán hàng");
                dataGridView1.DataSource = ds.Tables["Hóa đơn bán hàng"];

                SqlCommand cmdMon = new SqlCommand("select MAMON, TENMON from MON", con);
                SqlDataReader reader = cmdMon.ExecuteReader();
                DataTable dtMon = new DataTable();
                dtMon.Load(reader);
                cboMon.DataSource = dtMon;
                cboMon.DisplayMember = "TENMON";
                cboMon.ValueMember = "MAMON";

                cboPTTT.Items.Add("Chuyển khoản");
                cboPTTT.Items.Add("Tiền mặt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL:\n" + ex.Message, "Lỗi");
            }

            dsChiTiet.Columns.Add("MAMON", typeof(string));
            dsChiTiet.Columns.Add("TENMON", typeof(string));
            dsChiTiet.Columns.Add("SOLUONG", typeof(int));
            dsChiTiet.Columns.Add("THANHTIEN", typeof(decimal));

            con.Close();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Chỉ xử lý nếu không phải tiêu đề
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                txtMaHD.Text = dataGridView1.Rows[e.RowIndex].Cells["MaHD"].Value.ToString();
                txtMaKH.Text = dataGridView1.Rows[e.RowIndex].Cells["MaKH"].Value.ToString();
                txtMaNV.Text = dataGridView1.Rows[e.RowIndex].Cells["MaNV"].Value.ToString();
                txtTongtien.Text = row.Cells["TongTien"].Value.ToString();
                txtTongSL.Text = row.Cells["TongSL"].Value.ToString();
                txtPhuThu.Text = row.Cells["PhuThu"].Value.ToString(); 
                txtSoTienGiam.Text = row.Cells["SoTienGiam"].Value.ToString(); //
                txtThanhTienSauKM.Text = row.Cells["ThanhTienSauKM"].Value.ToString(); //

                dtpNgay.Value = Convert.ToDateTime(dataGridView1.Rows[e.RowIndex].Cells["Ngay"].Value);

                // Kiểu chuỗi - ComboBox
                cboPTTT.SelectedItem = row.Cells["PTTT"].Value?.ToString();

                txtMaHD.Enabled = false; //Không cho sửa mã hóa đơn
            }
        }
        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private decimal LayGiaBanMon(string maMon)
        {
            SqlConnection con = new SqlConnection(sCon);
            con.Open();
            SqlCommand cmd = new SqlCommand("select GIABAN from MON where MAMON = @MAMON", con);
            cmd.Parameters.AddWithValue("@MAMON", maMon);
            object result = cmd.ExecuteScalar();
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        private void CapNhatTongTienVaSL()
        {
            int tongSL = 0;
            decimal tongTien = 0;

            foreach (DataRow row in dsChiTiet.Rows)
            {
                tongSL += Convert.ToInt32(row["SOLUONG"]);
                tongTien += Convert.ToDecimal(row["THANHTIEN"]);
            }

            txtTongSL.Text = tongSL.ToString();
            txtTongtien.Text = tongTien.ToString("N0");
        }

        private void btnThemMon_Click(object sender, EventArgs e)
        {
            if (cboMon.SelectedValue == null || string.IsNullOrEmpty(txtSoLuong.Text)) return;

            string maMon = cboMon.SelectedValue.ToString();
            string tenMon = cboMon.Text;
            int soLuong = int.Parse(txtSoLuong.Text);
            decimal donGia = LayGiaBanMon(maMon);
            decimal thanhTien = soLuong * donGia;

            dsChiTiet.Rows.Add(maMon, tenMon, soLuong, thanhTien);
            dgvChiTietBan.DataSource = dsChiTiet;

            CapNhatTongTienVaSL();
        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {
            txtMaHD.ReadOnly = false;
            string key = txtMaHD.Text.Trim();

            if (string.IsNullOrEmpty(key))
            {
                LoadData(); // Hiển thị lại toàn bộ đơn hàng
                return;
            }

            using (SqlConnection con = new SqlConnection(sCon))
            {
                try
                {
                    con.Open();

                    string query = @"
                        SELECT BAN.MAHD, BAN.NGAY, BAN.TONGTIEN, BAN.PTTT, BAN.TONGSL, 
                               BAN.PHUTHU, BAN.MAKH, BAN.MANV,
                               CT.MAMON, CT.SOLUONG
                        FROM BAN
                        JOIN CHITIET_BAN CT ON BAN.MAHD = CT.MAHD
                        WHERE BAN.MAHD LIKE @key";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@key", "%" + key + "%");

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        dataGridView1.DataSource = dt;
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy đơn!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tìm kiếm: " + ex.Message);
                }
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            DialogResult ret = MessageBox.Show("Bạn có chắc chắn muốn xóa hóa đơn này không?", "Thông báo", MessageBoxButtons.OKCancel);
            if (ret == DialogResult.OK)
            {
                using (SqlConnection connection = new SqlConnection(sCon))
                {
                    connection.Open();
                    string query = "DELETE FROM BAN WHERE MAHD = @MAHD";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@MAHD", txtMaHD.Text);

                    command.ExecuteNonQuery();
                }
                LoadData();
            }
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            DialogResult ret = MessageBox.Show("Bạn có chắc chắn muốn sửa hóa đơn này không?", "Thông báo", MessageBoxButtons.OKCancel);
            if (ret == DialogResult.OK)
            {
                using (SqlConnection con = new SqlConnection(sCon))
                {
                    try
                    {
                        con.Open();
                        string query = @"UPDATE BAN 
                                         SET MAKH = @MAKH, MANV = @MANV, 
                                             NGAY = @NGAY, PTTT = @PTTT 
                                         WHERE MAHD = @MAHD;

                                         UPDATE CHITIET_BAN 
                                         SET MAMON = @MAMON, SOLUONG = @SOLUONG
                                         WHERE MAHD = @MAHD";

                        SqlCommand command = new SqlCommand(query, con);

                        command.Parameters.AddWithValue("@MAHD", txtMaHD.Text);
                        command.Parameters.AddWithValue("@MAKH", txtMaKH.Text);
                        command.Parameters.AddWithValue("@MANV", txtMaNV.Text);
                        command.Parameters.AddWithValue("@PTTT", cboPTTT.SelectedItem?.ToString() ?? "Tiền mặt");

                        command.Parameters.AddWithValue("@MAMON", cboPTTT.SelectedItem?.ToString() ?? "Latte");
                        command.Parameters.AddWithValue("@SOLUONG", txtSoLuong.Text);
                        command.Parameters.AddWithValue("@NGAY", dtpNgay.Value);
                        command.ExecuteNonQuery();

                        MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi cập nhật: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
