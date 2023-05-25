﻿using crypto;
using Guna.UI2.WinForms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Panel = System.Windows.Forms.Panel;
using KSCS.Class;
using MySql.Data.MySqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using System.Web.UI;
using KSCS.UserControls.MainForm;

namespace KSCS
{
    public partial class MainForm : Form
    {
        public static KLAS klas = new KLAS();

        //달력 관련
        private int year, month;
        public static int static_month, static_year;
        //탭 & 카테고리 관련
        public static string TabName;
        public static Category Category = new Category();
        //스케줄 관련
        public static Dictionary<string, string[]> categoryDict = new Dictionary<string, string[]>(); //category dictionary
        public static List<List<Schedule>> monthScheduleList = new List<List<Schedule>>(); //한달 단위 schedule list

        public static string stdNum = "2019203082";
        readonly MySqlConnection connection = DatabaseConnection.getDBConnection(); //MySQL


        public MainForm()
        {
            InitializeComponent();
            connection.Open();
            LoginForm loginForm = new LoginForm();

            DialogResult Result = loginForm.ShowDialog();
            if (Result == DialogResult.OK)
            {
                //dispalyDate();
                lblStdNum.Text = stdNum;
                LoadMagam();
            }
            else
            {
                Close();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Category.TestCategory();


            //초기 메인 카테고리 설정
            UserMainCategory school = new UserMainCategory();
            school.SetAddMode("학교");
            UserMainCategory personal = new UserMainCategory();
            personal.SetAddMode("개인");
            UserMainCategory Etc = new UserMainCategory();
            Etc.SetAddMode("기타");
            MainCategory.Controls.Add(school);
            MainCategory.Controls.Add(personal);
            MainCategory.Controls.Add(Etc);
            //카테고리 로드
            DisplayCategery();

            //초기 탭 설정 
            TabName = 탭1.Name; //수정되어야함
            탭1.Clicked += ChangeTab;
            탭2.Clicked += ChangeTab;
            탭3.Clicked += ChangeTab;
            탭4.Clicked += ChangeTab;
            탭5.Clicked += ChangeTab;
            //탭 로드
            SetCheckedCategoryByTab();
            탭1.ShowTab();

            //달력
            dispalyDate();
        }

        private async void LoadMagam()
        {
            await klas.LoadMagamData();
            MagamButtonEnable();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            this.Size = new Size(1280, 1080);
        }



        public void InitializeDatabase()
        {
            //쿼리 수정(endDate 까지 포함)
            string selectQuery = string.Format("SELECT * from Schedule JOIN Category ON Schedule.category_id=Category.id JOIN StudentCategory ON StudentCategory.student_id=Schedule.student_id and Schedule.category_id=Category.id and Category.id=StudentCategory.category_id WHERE Schedule.student_id={0} and  (startDate BETWEEN DATE_FORMAT('{1}', '%Y-%m-%d') AND LAST_DAY('{1}') or endDate BETWEEN DATE_FORMAT('{1}', '%Y-%m-%d') AND LAST_DAY('{1}')) ORDER BY startDate ASC;", stdNum, new DateTime(year, month, 1).ToString("yyyy-MM-dd"));
            MySqlCommand cmd = new MySqlCommand(selectQuery, connection);
            MySqlDataReader table = cmd.ExecuteReader();
            monthScheduleList.Clear(); //한달 스케줄 초기화

            //하루 단위 리스트 생성
            for (int i = 0; i < DateTime.DaysInMonth(year, month); i++)
            {
                monthScheduleList.Add(new List<Schedule>());
            }

            while (table.Read())
            {
                Schedule schedule = new Schedule(
                    table["title"].ToString(),
                    table["content"].ToString(),
                    table["place"].ToString(),
                    table["type"].ToString(),
                    DateTime.Parse(table["startDate"].ToString()),
                    DateTime.Parse(table["endDate"].ToString()))
                {
                    id = int.Parse(table["id"].ToString()),
                };

                //startDate와 endDate 일자가 다른 경우도 포함(추가)
                TimeSpan duration = schedule.endDate - schedule.startDate;
                for (int i = 0; i <= duration.Days; i++)
                {
                    if (Convert.ToInt32(schedule.startDate.AddDays(i).ToString("MM")) == MainForm.static_month)
                    {
                        MainForm.monthScheduleList[Convert.ToInt32(schedule.startDate.AddDays(i).ToString("dd")) - 1].Add(schedule);
                    }
                }
            }

            table.Close();
            LoadCategory(); //추가
        }


        //카테고리 함수---------------------------------------------------------------------------------------------------------------------------------------
        private void DisplayCategery()
        {
            foreach (var key in Category.Categories.Keys)
            {
                foreach (var item in Category.Categories[key])
                {
                    UserSubCategory uc = new UserSubCategory();
                    uc.SetBasicMode(item);
                    ((FlowLayoutPanel)((UserMainCategory)MainCategory.Controls[key]).flpSubCategory).Controls.Add(uc);
                }
            }
        }

        //탭 함수-------------------------------------------------------------------------------------------------------------------------------------------
        private void ChangeTab(object sender, EventArgs e)
        {
            /*
             * TODO: 이 부분에 DB에 연결하는 함수 추가 필요
             */
            UserTabButton OldTab = this.Controls[TabName] as UserTabButton;
            UserTabButton btn = sender as UserTabButton;
            TabName = btn.Name;
            OldTab.HideTab();
            SetCheckedCategoryByTab();
        }
        private void SetCheckedCategoryByTab()
        {
            foreach (string key in Category.Categories.Keys)
            {
                FlowLayoutPanel flp = ((UserMainCategory)MainCategory.Controls[key]).flpSubCategory;
                foreach (UserSubCategory category in flp.Controls)
                {
                    category.SetChecked(Category.IsChecked(TabName, category.GetText()));
                }
            }

        }

        private void LoadCategory()
        {
            categoryDict.Clear();
            string selectQuery = string.Format("SELECT * from Category JOIN StudentCategory ON Category.id=StudentCategory.category_id WHERE student_id='{0}';", stdNum);
            MySqlCommand cmd = new MySqlCommand(selectQuery, connection);
            MySqlDataReader table = cmd.ExecuteReader();
            while (table.Read())
            {
                categoryDict.Add(table["type"].ToString(), new string[2] { table["id"].ToString(), table["color"].ToString() });
            }
            table.Close();
        }




        //달력 함수-----------------------------------------------------------------------------------------------------------------------------------------
        private void dispalyDate()
        {
            DateTime now = DateTime.Now; //수정 필요

            year = now.Year;
            month = now.Month;
            createDates();
        }

        private void createDates()
        {
            static_month = month;
            static_year = year;

            InitializeDatabase();

            lblMonth.Text = month.ToString() + "월";
            lblMonth.TextAlign = ContentAlignment.MiddleCenter;

            DateTime startOfMonth = new DateTime(year, month, 1);
            int dates = DateTime.DaysInMonth(year, month);
            int dayOfWeek = Convert.ToInt32(startOfMonth.DayOfWeek.ToString("d")) + 1;
            int index = 0;
            int date = 1;

            foreach (UserDate userDate in flpDays.Controls.OfType<UserDate>())
            {
                if (++index < dayOfWeek) userDate.ChangeBlank();
                else if (date <= dates) userDate.SetDate(date++);
                else userDate.ChangeBlank();

                if (index % 7 == 0) userDate.ChangeColor(Color.Blue);
                else if (index % 7 == 1) userDate.ChangeColor(Color.Red);
            }
        }


        //컨트롤 함수------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        //화면 컨트롤-------------------------------------------
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        //마감 일정 컨트롤---------------------------------------------------------------------------------------------------------------
        private void MagamButtonEnable()
        {
            btnMagam_Click(btnMagam_Task, new EventArgs());
            btnMagam_Quiz.Enabled = true;
            btnMagam_Task.Enabled = true;
            btnMagam_Online.Enabled = true;
            btnMagam_Prjct.Enabled = true;
        }

        //마감 버튼 클릭 시, 각 일정에 따른 KLAS 클래스에 정의 된 Dictionary 접근하여 데이터 확인.
        //@todo: 개인 마감일정 관리 개별로 할지 논의 필요.
        private void btnMagam_Click(object sender, EventArgs e)
        {
            Guna2CircleButton btn = (Guna2CircleButton)sender;
            Panel panel = (Panel)btn.Parent;
            foreach (Guna2CircleButton magamBtn in panel.Controls)
            {
                magamBtn.FillColor = Color.FromArgb(217, 217, 217); ;
            }
            //btn.FillColor = Color.FromArgb(217,217,217);
            panelMagam.Controls.Clear();
            int index = 0;

            Dictionary<string, int[]> MagamLectureDic = new Dictionary<string, int[]>();
            Dictionary<string, DateTime> MagamMinDate = new Dictionary<string, DateTime>();

            foreach (Schedule schedule in klas.KlasSchedule[btn.Name.Substring(9)])
            {
                //총 개수 구하는 부분
                //ex) 몇 개 중
                if (MagamLectureDic.ContainsKey(schedule.content))
                    MagamLectureDic[schedule.content][0] += 1;
                else
                {
                    MagamLectureDic.Add(schedule.content, new int[2]);
                    MagamLectureDic[schedule.content][0] = 1;
                    MagamLectureDic[schedule.content][1] = 0;
                }
                //가장 최근 마감 일정 남은 시간 구하는 부분
                //ex) 몇 일/시간 남았습니다.
                if (MagamMinDate.ContainsKey(schedule.content))
                {
                    if (MagamMinDate[schedule.content] < schedule.endDate) MagamMinDate[schedule.content] = schedule.endDate;
                }
                else MagamMinDate.Add(schedule.content, schedule.endDate);
            }
            foreach (Schedule schedule in klas.KlasSchedule[btn.Name.Substring(9)])
            {
                //가장 최근 마감 일정 갯수 구하는 부분
                //ex) 몇 개가
                if (MagamMinDate[schedule.content] == schedule.endDate)
                {
                    MagamLectureDic[schedule.content][1] += 1;
                }
            }
            foreach (KeyValuePair<string, int[]> items in MagamLectureDic)
            {
                Label lbl = new Label
                {
                    Name = "KLAS_" + btn.Name.Substring(9) + "_" + index.ToString(),
                    Text = items.Key + " " + KLAS.klasMagamNames[btn.Name.Substring(9)] + " " + items.Value[0] + " 개 중 " + items.Value[1] + " 개가 " + Schedule.MagamDateFrom(MagamMinDate[items.Key]) + " 남았습니다.",
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
                };
                lbl.Location = new Point(0, index * (lbl.Height + 3));
                //panelMagam.Controls.Add(lbl);
                if (panel.InvokeRequired) panelMagam.Invoke(new MethodInvoker(delegate { panelMagam.Controls.Add(lbl); }));
                else panelMagam.Controls.Add(lbl);
                index++;
            }
        }


        //달력 컨트롤--------------------------------------------------------------------------------------------------------------------------------------
        private void btnMonth_Click(object sender, EventArgs e)
        {
            Guna2Button btn = sender as Guna2Button;
            switch (btn.Name.Substring(3))
            {
                case "Next":
                    if (month == 12) { month = 1; year++; }
                    else month++;
                    
                    break;
                case "Previsous":
                    if (month == 1) { month = 12; year--; }
                    else month--;
                    
                    break;
            }

            createDates();
        }

        //카테고리 컨트롤------------------------------------------------------------------------------------------------------------------------------------

        private void btnPlusCategory_Click(object sender, EventArgs e)
        {
            UserMainCategory category = new UserMainCategory();
            category.SetNewMode();
            MainCategory.Controls.Add(category);
        }

        ////카테고리 유저 컨트롤------------------------------------------------------------------------------------------------------------------------------------
        private UserCategory draggedUcCategory; // 드래그 중인 카테고리 유저 컨트롤
        private UserCategory cloneUcCategory; // 드래그 중인 카테고리 유저 컨트롤 복사본
        private Point MouseLocation;

        public void UndoCategory()
        {
            this.Controls.Remove(cloneUcCategory);
            cloneUcCategory.Dispose();
            draggedUcCategory.Visible = true;
        }
        private void UcCategory_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            draggedUcCategory = (UserCategory)sender;
            draggedUcCategory.Visible = false;

            // 드래그 중인 버튼의 복사본 생성
            MouseLocation = new Point((Cursor.Position.X - e.X) - Left, (Cursor.Position.Y - e.Y) - Top); // 현제 마우스 위치
            cloneUcCategory = new UserCategory { Location = MouseLocation };
            cloneUcCategory.DragMode(draggedUcCategory.GetText());
            this.Controls.Add(cloneUcCategory);
            flpMainCategory.SendToBack();
            cloneUcCategory.MouseMove += UcCategory_MouseMove;
            cloneUcCategory.MouseClick += UcCategory_MouseClick;
        }

        private void UcCategory_MouseClick(object sender, MouseEventArgs e)
        {
            string NewMainCategory;
            if (MouseLocation.Y < flpMainCategory.Location.Y + flpPersonalCategory.Location.Y)
            {
                //학교
                NewMainCategory = "SchoolCategory";

            }
            else if (MouseLocation.Y < flpMainCategory.Location.Y + flpEtcCategory.Location.Y)
            {
                //개인
                NewMainCategory = "PersonalCategory";
            }
            else
            {
                //기타
                NewMainCategory = "EtcCategory";
            }

            if (NewMainCategory.Length > 0)
            {
                draggedUcCategory.Visible = true;
                string OringMainCategory = Category.SubCategorys[cloneUcCategory.GetText()] as string;
                if (OringMainCategory == NewMainCategory)
                {
                    UndoCategory();
                }
                else
                {
                    this.Controls.Remove(cloneUcCategory);
                    FlowLayoutPanel FlpNewCategory = flpMainCategory.Controls["flp" + NewMainCategory] as FlowLayoutPanel;
                    FlowLayoutPanel FlpOriginCategory = flpMainCategory.Controls["flp" + OringMainCategory] as FlowLayoutPanel;
                    FlpOriginCategory.Controls.Remove(draggedUcCategory);
                    FlpNewCategory.Controls.Add(draggedUcCategory);
                    Category.ChangeParentOfSub(NewMainCategory, cloneUcCategory.GetText());
                    draggedUcCategory = null;
                    cloneUcCategory = null;
                }
            }

        }

        private void UcCategory_MouseMove(object sender, MouseEventArgs e)
        {
            MouseLocation = new Point((Cursor.Position.X - cloneUcCategory.Width / 2) - Left, (Cursor.Position.Y - cloneUcCategory.Height / 2) - Top);
            if ((MouseLocation.X < flpMainCategory.Location.X - 100 || MouseLocation.X > flpMainCategory.Location.X + 130)
                || (MouseLocation.Y < flpMainCategory.Location.Y || MouseLocation.Y > flpMainCategory.Location.Y + 450))
            {
                UndoCategory();
            }
            cloneUcCategory.Location = MouseLocation;
        }

        //추가
        public IEnumerable<UserDate> GetUserDate()
        {
            return flpDays.Controls.OfType<UserDate>();
        }
    }
}
