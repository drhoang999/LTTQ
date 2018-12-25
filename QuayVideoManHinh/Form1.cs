using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Data.SqlClient;
using System.Resources;
using System.Xml.Linq; //xml
using System.Diagnostics;//file
using AForge.Video;
using AForge.Video.FFMPEG;
using System.Runtime.InteropServices;//DllImport

namespace VietCam
{
    public partial class Form1 : Form
    {
     
        public Form1()
        {
            InitializeComponent();
        }
        #region Khai báo thư viện
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        const int MYACTION_HOTKEY_ID = 1;
      
        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public int[] Key = new int[] { 2, 3};
        #endregion


        #region Khởi tạo và hủy Hotkey
        private void _KhoiTaoPhimNong()
        {
            try
            {
                RegisterHotKey(this.Handle, Key[0], (int)KeyModifier.None, Keys.F8.GetHashCode());
                RegisterHotKey(this.Handle, Key[1], (int)KeyModifier.None, Keys.F9.GetHashCode());

                #region Van chua dung toi
                //RegisterHotKey(this.Handle, id, (int)KeyModifier.Shift, Keys.B.GetHashCode());       // Register Shift + B as global hotkey. 
                //RegisterHotKey(this.Handle, id, (int)KeyModifier.Control, Keys.C.GetHashCode());       // Register Control + C as global hotkey. 
                //RegisterHotKey(this.Handle, id, 6, Keys.D.GetHashCode());       // Register Control + Shift + D as global hotkey. 
                #endregion
            }
            catch (Exception)
            {

            }
        }

        private void _HuyPhimNong()
        {
            try
            {
                UnregisterHotKey(this.Handle, Key[0]);
                UnregisterHotKey(this.Handle, Key[1]);
              
            }
            catch (Exception)
            {

            }
        }
        #endregion


        #region Sử dụng Hotkey
        protected override void WndProc(ref Message m)
        {
            try
            {
                base.WndProc(ref m);

                if (m.Msg == 0x0312)
                {
                 
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  
                    KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);      
                    int id = m.WParam.ToInt32();                                      

                    switch (id)
                    {
                        case 2:
                            {
                                Quay();
                                break;
                            }
                        case 3:
                            {
                                camerapic_Click(this, new EventArgs());
                                break;
                            }
                    
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion


        #region vùng biến



        private Size s;
        Bitmap bt;

        VideoFileWriter writer = new VideoFileWriter();
        string AdrVideo;
        string AdrImg;


        private string NameApp = "VietCam";
        private string NameVideo;
        private string NameImg;
        const byte FPS = 10;
        private bool flagQuay = false;

        private string path;

        #endregion
 

        #region Xử lý quay và chụp
        private void ptrRec_Click(object sender, EventArgs e)//Nhấn nút rec
        {
            Quay();

        }
        //Sự kiện nhất nút Stop
        private void ptrStop_Click(object sender, EventArgs e)
        {
            Quay();
        }
        //nút Pause quay
        private void picPause_Click(object sender, EventArgs e)
        {
            picPause.Visible = false; //ẩn nút pause
            picContinue.Visible = true; //hiện nút tiếp tục
            timer1.Stop();
            timer3.Stop();
        }
        //Nút tiếp tục quay
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            picPause.Visible = true; //hiện nút pause
            picContinue.Visible = false; //ẩn nút tiếp tục
            timer1.Start();
            timer3.Start();

        }

        //Chụp ảnh màn
        private void TakeImage(string adr)
        {
            AllScreen().Save(adr, System.Drawing.Imaging.ImageFormat.Png);
        }
        //Sự kiện chụp màn hình
        private void camerapic_Click(object sender, EventArgs e)
        {
            //Tên Image
            NameImg = NameApp + "-Image-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "-"
                 + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond
                 + @".jpg";

            try
            {
                if (Directory.Exists(AdrImg))//Kiểm tra thư mục lưu file tồn tại không
                {
                    AddImageXml(AdrImg, NameImg);

                    TakeImage(AdrImg + @"/" + NameImg);

                    loadXml(false);//load lại ds ảnh

                    DialogResult rs = MessageBox.Show("Đã chụp màn hình, mở xem ngay?", "VietCam", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    if (rs == DialogResult.Yes)
                    {
                        OpenFile(AdrImg + @"/" + NameImg);
                    }
                }else
                {
                    MessageBox.Show("Thư mục lưu ảnh không tồn tại, mời chọn thư mục khác!");
                }


            }
            catch (Exception ec)
            {
                MessageBox.Show(ec.Message);
            }
        }
        //Sự kiện nhất nút Rec
        private void Quay()
        {
            //Tên video ban đầu
            NameVideo = NameApp + "-Video-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "-"
                 + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond
                 + @".mp4";
           string filename = AdrVideo + @"\" + NameVideo;

            if (flagQuay == false) // Nếu chưa quay thì bắt đầu quay
            {
                

                if (Directory.Exists(AdrVideo))//Kiểm tra thư mục lưu file tồn tại không
                {
                    if (File.Exists(filename)) //File không tồn tại thì thêm mới
                    {
                        MessageBox.Show("File đã tồn tại mời đổi tên khác trong phần Setting!");
                    }
                    else
                    {


                        if (System.IO.File.Exists(filename))
                            System.IO.File.Delete(filename);


                        ptrRec.Visible = false; //Ẩn nút rec
                        ptrStop.Visible = true; //Hiện nút stop
                        picPause.Visible = true; //Hiện nút pause
                        flagQuay = true;//đang quay

                        AddVideoXml(AdrVideo, NameVideo); // thêm video vào danh sách

                       

                        writer.Open(filename, s.Width, s.Height,FPS, VideoCodec.MPEG4, 5000000);
                        timer1.Interval = 90;

                        timer1.Start();//ghi
                        timer3.Start(); //đếm


                    }

                }
                else
                {
                    MessageBox.Show("Thư mục lưu video không tồn tại, mời chọn thư mục khác!");
                }
            }else // nếu đang quay thì xử lý dừng

            {

                lbTime.Text = "00:00:00";
                ptrRec.Visible = true; //Hiện nút rec
                ptrStop.Visible = false; //Ẩn nút stop
                flagQuay = flag;//dừng quay

                //đóng quay video
                writer.Close();


                picPause.Visible = false; //ẩn nút pause
                timer1.Stop();//ghi
                timer3.Stop(); //đếm

                loadXml(true);//load lại ds video

                //cập nhật thông số đếm
                i = 0; j = 0; k = 0; m = 0; n = 0; l = 0;
                labe1 = "0";
                labe2 = "0";
                labe3 = "0";
                labe4 = "0";
                labe5 = "0";
                labe6 = "0";

               
                DialogResult rs = MessageBox.Show("Đã lưu video, mở xem ngay?", "VietCam", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if(rs == DialogResult.Yes)
                {
                    OpenFile(lvVideo.Items[lvVideo.Items.Count-1].SubItems[1].Text + "\\" + lvVideo.Items[lvVideo.Items.Count-1].Text);
                }
            }
        }
        //Timer1
        private void timer1_Tick(object sender, EventArgs e)
        {
            writer.WriteVideoFrame(AllScreen());

        }
        #endregion


        #region Khởi động
        //Hàm trả về 1 ảnh 
        private Bitmap AllScreen()
        {

            Graphics g = Graphics.FromImage(bt);

            g.CopyFromScreen(0, 0, 0, 0, s);

            return bt;
        }
        //Timer ghi hình vào home
        private void timer2_Tick(object sender, EventArgs e)
        {
            Screen.Image = AllScreen();
        }
        //Khởi động
        private void Form1_Load(object sender, EventArgs e)
        {
            AdrVideo = Properties.Settings.Default.AdrVideo;
            AdrImg = Properties.Settings.Default.AdrImg;

            if (AdrVideo == "NULL" || AdrImg == "NULL") //mới cài vietcam
            {
                Properties.Settings.Default.AdrVideo = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                Properties.Settings.Default.AdrImg = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                Properties.Settings.Default.Save();

                AdrVideo = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                AdrImg = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
            txtAdrVideo.Text = AdrVideo;
            txtAdrImg.Text = AdrImg;

            _KhoiTaoPhimNong();

            timer2.Start();

              float i = (int)System.Windows.SystemParameters.PrimaryScreenWidth;

           s = new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);

            bt = new Bitmap(
              s.Width,
              s.Height,
              PixelFormat.Format32bppRgb);

            loadXml(true);//Load  list video

            loadXml(false);//Load list image

         

        }
        //Giao diện Chỉnh sửa nút tabcontrol
        private void tabControl1_DrawItem_2(object sender, DrawItemEventArgs e)
        {


            Graphics g = e.Graphics;

            Brush _textBrush;

            // Lấy vật phẩm từ bộ sưu tập.

            TabPage _tabPage = tabControl1.TabPages[e.Index];

            // Nhận giới hạn thực sự cho hình chữ nhật tab.


            Rectangle _tabBounds = tabControl1.GetTabRect(e.Index);

            SolidBrush color = new SolidBrush(Color.FromArgb(75, 179, 104));


            g.FillRectangle(color, e.Bounds);


            if (e.State == DrawItemState.Selected)

            {


                // Vẽ một màu nền khác và không vẽ hình chữ nhật tiêu điểm.

                _textBrush = new SolidBrush(Color.White);

                color = new SolidBrush(Color.FromArgb(56, 160, 107));

                g.FillRectangle(color, e.Bounds);

            }

            else

            {

                _textBrush = new System.Drawing.SolidBrush(Color.White);


            }



            //Sử dụng phông chữ của riêng chúng tôi.

            Font _tabFont = new Font("Arial", (float)13.0);



            // Vẽ chuỗi. Căn giữa văn bản.

            StringFormat _stringFlags = new StringFormat();

            _stringFlags.Alignment = StringAlignment.Center;

            _stringFlags.LineAlignment = StringAlignment.Center;

            g.DrawString(_tabPage.Text, _tabFont, _textBrush, _tabBounds, new StringFormat(_stringFlags));


        }
        #endregion


        #region File và Folder

        //Nút chọn folder video khác
        private void btnSelectAdrVideo_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txtAdrVideo.Text;

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                AdrVideo = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.AdrVideo = AdrVideo;
                Properties.Settings.Default.Save();
                txtAdrVideo.Text = AdrVideo;
            }

        }

        //Nút chọn folder ảnh khác
        private void btnSelectAdrImg_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txtAdrImg.Text;

            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                AdrImg = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.AdrImg = AdrImg;
                Properties.Settings.Default.Save();
                txtAdrImg.Text = AdrImg;


            }

        }
        //Nút Open folder video
        private void btnOpenFolderVideo_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(AdrVideo);
            }
            catch
            {
                MessageBox.Show("Thư mục video không tồn tại hoặc đã bị xóa, mời chọn lại thư mục khác!");
            }
        }

        //Nút Open folder ảnh
        private void btnOpenFolderImage_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(AdrImg);
            }
            catch
            {
                MessageBox.Show("Thư mục ảnh không tồn tại hoặc đã bị xóa, mời chọn lại thư mục khác!");
            }
        }

        //Thêm video vào XML
        private void AddVideoXml(string path, string name)
        {
            try
            {
                string pathvideo = "ListVideo.xml";
                XDocument XML = XDocument.Load(pathvideo);
                XElement newx =

                new XElement("Data",
                new XElement("Path", path)

                );

                newx.SetAttributeValue("ID", name);

                XML.Element("Datas").Add(newx);
                XML.Save(pathvideo);

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        //Thêm ảnh vào XML
        private void AddImageXml(string path, string name)
        {
            try
            {
                string pathimage = "ListImage.xml";
                XDocument XML = XDocument.Load(pathimage);
                XElement newx =
                     new XElement("Data",
                     new XElement("Path", path)
                );
                newx.SetAttributeValue("ID", name);

                XML.Element("Datas").Add(newx);
                XML.Save(pathimage);

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        //Xóa list xml ảnh
        private void btnDeleteA_Click(object sender, EventArgs e)
        {
            if (lvImage.SelectedItems.Count > 0)
            {

                DeleteFile(lvImage.SelectedItems[0].SubItems[1].Text + "\\" + lvImage.SelectedItems[0].Text);
                DeleteXml(lvImage.SelectedItems[0].Text, false);
            }
            else
            {
                MessageBox.Show("Chọn một file để xóa");
            }


        }
        //Hoàm xóa file và xóa list xml
        private void DeleteXml(string ID, bool k) //k=true=> Video
        {

            if (k == true)
                path = "ListVideo.xml";
            else
                path = "ListImage.xml";

            try
            {

                XDocument testXML = XDocument.Load(path);
                XElement cStudent =
                    testXML.Descendants("Data").Where(c => c.Attribute("ID").Value.Equals(ID)).FirstOrDefault();

                cStudent.Remove();
                testXML.Save(path);
                loadXml(true); //load ds video
                loadXml(false); // load ds anh

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        //Mo
        private void btnOpenA_Click(object sender, EventArgs e)
        {
            if (lvImage.SelectedItems.Count > 0)
            {


                OpenFile(lvImage.SelectedItems[0].SubItems[1].Text + "\\" + lvImage.SelectedItems[0].Text);
            }
            else
            {
                MessageBox.Show("Chọn một file để mở");
            }
        }

        private void btnDeleteVD_Click(object sender, EventArgs e)
        {

            if (lvVideo.SelectedItems.Count > 0)
            {
                DeleteFile(lvVideo.SelectedItems[0].SubItems[1].Text + "\\" + lvVideo.SelectedItems[0].Text);
                DeleteXml(lvVideo.SelectedItems[0].Text, true);
            }
            else
            {
                MessageBox.Show("Chọn một file để xóa");
            }


        }
        //Hàm mở file
        private void OpenFile(string file)
        {
            try
            {
                Process.Start(file);
            }
            catch
            {
                MessageBox.Show("File đã bị xóa!");
            }
        }
        //Hàm xóa file
        private void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    MessageBox.Show("File đã bị xóa!");
                }
            }
            else
            {
                MessageBox.Show("File đã bị xóa từ trước!");
            }
        }
        //Load data
        private void loadXml(bool k) //k=true->video
        {
            if (k == true)
            {
                path = "ListVideo.xml";

                try
                {
                    lvVideo.Items.Clear();
                    DataSet dataSet = new DataSet(); //khởi tạo data xml 
                    dataSet.ReadXml(path);

                    DataTable dt = new DataTable();
                    dt = dataSet.Tables["Data"];

                    int i = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        lvVideo.Items.Add(dr["ID"].ToString());
                        lvVideo.Items[i].SubItems.Add(dr["Path"].ToString());
                        i++;
                    }

                }
                catch (Exception)
                {

                }

            }
            else
            {
                path = "ListImage.xml";

                try
                {
                    lvImage.Items.Clear();
                    DataSet dataSet = new DataSet();
                    dataSet.ReadXml(path);
                    DataTable dt = new DataTable();
                    dt = dataSet.Tables["Data"];
                    int i = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        lvImage.Items.Add(dr["ID"].ToString());
                        lvImage.Items[i].SubItems.Add(dr["Path"].ToString());
                        i++;
                    }
                }
                catch (Exception)
                {

                }
            }


        }

        //Open File
        private void button5_Click(object sender, EventArgs e)
        {


            if (lvVideo.SelectedItems.Count > 0)
            {


                OpenFile(lvVideo.SelectedItems[0].SubItems[1].Text + "\\" + lvVideo.SelectedItems[0].Text);
            }
            else
            {
                MessageBox.Show("Chọn một file để mở");
            }


        }
        #endregion


        #region Chức năng phụ
        //Ẩn ứng dụng xuống icon thông báo
        private void button3_Click(object sender, EventArgs e) //bấm nút nhỏ ẩn ứng dụng
        {

            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(500, "Đã ẩn VietCam!", "F8 Bắt đầu/Dừng quay\r\nF9 Chụp màn hình", ToolTipIcon.Info);
            this.Hide();

        }
        //Thoát ứng dụng
        private void button1_Click(object sender, EventArgs e)
        {
            _HuyPhimNong();
            Application.Exit();
        }
        //Ẩn xuống Minimized
        private void button2_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
        //Icon ẩn
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e) //double vào icon ẩn
        {
            notifyIcon1.Visible = false;
            this.Show();
            WindowState = FormWindowState.Normal;
        }

       
        //Thanh công cụ cho icon ẩn
        private void openToolStripMenuItem_Click(object sender, EventArgs e) //nút mở trong icon
        {
            notifyIcon1.Visible = false;
            this.Show();
            WindowState = FormWindowState.Normal;
        }
        //chọn Thoát
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        //Chọn quay
        private void recordToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Quay();

            contextMenuStrip1.Items[1].Visible = false;
            contextMenuStrip1.Items[2].Visible = true;
        }
        //Chọn dừng quay
        private void dừngQuayVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Quay();

            contextMenuStrip1.Items[1].Visible = true;
            contextMenuStrip1.Items[2].Visible = false;
        }
        //Chọn chụp màn hình
        private void captureImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camerapic_Click(this, new EventArgs());
        }

        //Kéo khung
        //x y vị trí nhấn chuột. flag kiểm tra đang nhấn chuột
        bool flag = false;
        int x, y;
        private void panel3_MouseMove(object sender, MouseEventArgs e)
        {
            if (flag == true)
            {
                this.SetDesktopLocation(Cursor.Position.X - x, Cursor.Position.Y - y);
            }
        }
        //Nhấn chuột vào khung kéo
        private void panel3_MouseDown(object sender, MouseEventArgs e)
        {
            flag = true;
            x = e.X;
            y = e.Y;

        }
        //Nhấc chuột
        private void panel3_MouseUp(object sender, MouseEventArgs e)
        {
            flag = false;
        }
        //Chạy bộ đếm thời gian 
        int i = 0, j = 0, k = 0, m = 0, n = 0, l = 0;
        string labe1 = "0";
        string labe2 = "0";
        string labe3 = "0";
        string labe4 = "0";
        string labe5 = "0";
        string labe6 = "0";

        private void tgian(ref int t1, ref int t2, ref string lb1, ref string lb2)
        {
            t1 = 0;
            t2++;
            lb1 = t2.ToString();
            lb2 = t1.ToString();
        }
        private string tgian1()
        {
            string x = "";
            i = i + 1;
            labe6 = i.ToString();
            if (i == 10)
            {
                tgian(ref i, ref j, ref labe5, ref labe6);

            }
            if (j == 6)
            {
                tgian(ref j, ref k, ref labe4, ref labe5);

            }

            if (k == 10)
            {
                tgian(ref k, ref m, ref labe3, ref labe4);

            }
            if (m == 6)
            {
                tgian(ref m, ref n, ref labe2, ref labe3);

            }
            if (n == 10)
            {

                tgian(ref n, ref l, ref labe1, ref labe2);

            }
            x = labe1 + labe2 + ":" + labe3 + labe4 + ":" + labe5 + labe6;
            return x;
        }
        //Timer3 chạy bộ đếm
        private void timer3_Tick(object sender, EventArgs e)
        {
            lbTime.Text = tgian1();
        }
        #endregion

     
    }
}
