using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;

namespace ChromeHistoryBrowser
{
    public partial class Form1 : Form
    {
        private SQLiteConnection historyFile;
        private DirectoryInfo chromeDir;
        private const string appPath = "\\Google\\Chrome\\User Data\\Default";
        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            this.textBox1.KeyPress += TextBox1_KeyPress;
        }

        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 13)
            {
                updateRecords();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(historyFile.State == ConnectionState.Open)
            {
                historyFile.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers(); // Close 이후 메모리 상에서 없어지기 전까지 대기해야 아래 열었던 복사본을 삭제할 수 있음.
            } 

            if (File.Exists(chromeDir.FullName + appPath + "\\History_copy"))
            {
                File.Delete(chromeDir.FullName + appPath + "\\History_copy");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            chromeDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            if(!File.Exists(chromeDir.FullName + appPath + "\\History"))
            {
                MessageBox.Show("정보를 찾을 수 없습니다.");
                Application.Exit();
                return;
            }

            File.Copy(chromeDir.FullName + appPath + "\\History", chromeDir.FullName + appPath + "\\History_copy",true); // History는 쓰이고 있으므로 복사 후 사용한다.

            historyFile = new SQLiteConnection("Data Source=" + chromeDir.FullName + appPath + "\\History_copy;Version=3;");

            historyFile.Open();

            updateRecords();
        }

        /// <summary>
        /// 특정 날짜와 검색 키워드로 
        /// </summary>
        private void updateRecords()
        {
            string searchKeyword = textBox1.Text, searchQuery = "";
            DateTime searchDate = dateTimePicker1.Value;
            SQLiteCommand sqlQuery;
            SQLiteDataReader sqlRows;
            ListViewItem newRow;

            if (historyFile.State != ConnectionState.Open)
            {
                MessageBox.Show("검색 기록 DB가 열리지 않았습니다.");
                return;
            }

            listView1.Items.Clear();

            searchQuery = dateQuery(searchDate);

            if (searchKeyword.Trim().Length > 0)
            {
                if (checkBox1.Checked)
                {
                    searchQuery = searchQuery + " AND (url LIKE '%" + searchKeyword + "%' or title LIKE '%" + searchKeyword + "%')";
                }
                else
                {
                    searchQuery = "  (url LIKE '%" + searchKeyword + "%' or title LIKE '%" + searchKeyword + "%')";
                }
            }

            using (sqlQuery = new SQLiteCommand("SELECT datetime(last_visit_time/1000000-11644473600,'unixepoch','localtime') lctime, title, url FROM urls WHERE " + searchQuery + " ORDER BY last_visit_time DESC", historyFile)) { 

                sqlRows = sqlQuery.ExecuteReader();

                while (sqlRows.Read())
                {
                    newRow = new ListViewItem(new String[3] { sqlRows["lctime"].ToString(), sqlRows["url"].ToString(), sqlRows["title"].ToString() });
                    listView1.Items.Add(newRow);
                }

                sqlRows.Close();
            }
        }

        private string dateQuery(DateTime? dt)
        {
            long sTime, eTime;
            if (!dt.HasValue) {
                dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            } else {
                dt = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day);
            }

            sTime = ((DateTimeOffset)dt).ToUnixTimeSeconds();
            eTime = ((DateTimeOffset)dt.Value.AddSeconds(86399)).ToUnixTimeSeconds();

            return " (last_visit_time/1000000-11644473600) between " + sTime.ToString() + " and " + eTime.ToString() + "  ";
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            updateRecords();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            updateRecords();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
