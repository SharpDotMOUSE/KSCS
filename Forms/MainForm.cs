﻿using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;
using KSCS.Class;
using static KSCS.Class.KSCS_static;
using System.Net.Sockets;
using Socket;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using KSCS.UserControls.MainForm;
using System.Threading;
using KSCS.Forms;
using System.Security;
using System.IO;
using Microsoft.Win32;
using MySqlX.XDevAPI;

namespace KSCS
{
    public partial class MainForm : Form
    {

        //스케줄 관련
        public MainForm()
        {
            InitializeComponent();
        }
        static bool isShareSchedule = false;
        TcpListener listener;
        bool isListen;
        SocketClient s_client = null;
        private Guna2CircleButton clickMagamBtn;
        public static FlowLayoutPanel flowLayoutPanelLable;

        private async void MainForm_Load(object sender, EventArgs e)
        {

            flowLayoutPanelLable = flpLabel;

            KLAS.initializeKLAS();
            LoginForm loginForm = new LoginForm();
            DialogResult Result = loginForm.ShowDialog();
            if (Result == DialogResult.OK)
            {
                //초기 사이즈 및 위치 설정
                this.Size = new Size(1360, 960);

                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(
                   (Screen.PrimaryScreen.Bounds.Width - this.Size.Width) / 2,
                   (Screen.PrimaryScreen.Bounds.Height - this.Size.Height) / 2
                );

                await KLAS.LoadMagamData();
                MagamButtonEnable();

                lblStdNum.Text = stdNum;
                //초기 메인 카테고리 설정
                Database.ReadCategoryList();
                Database.ReadTabAndCategory();
                foreach (string Main in category.Categories.Keys)
                {
                    UserMainCategory category = new UserMainCategory();
                    category.SetAddMode(Main);
                    panelMainCategory.Controls.Add(category);
                }


                //초기 탭 설정 
                TabAll.Clicked += ChangeTab;
                Tab1.Clicked += ChangeTab;
                Tab2.Clicked += ChangeTab;
                Tab3.Clicked += ChangeTab;
                Tab4.Clicked += ChangeTab;
                //btnSharing.Clicked += CreateSharing;
                TabSharing.Clicked += btnShare_Click;
                //btnSharing.DoubleClicked += CreateSharing;
                setTab();


                //오늘의 일정

                //달력 (탭 위에 위치 -> 현재)
                dispalyDate();
                DisplayCategery();

                //탭 로드
                UpdateTab();
                TabAll.ShowTab();



            }
            else
                Close();
        }

        private void setTab()
        {
            List<string> tabNameList = Database.ReadTab();
            TabAll.Name = tabNameList[0];
            Tab1.Name = tabNameList[1];
            Tab2.Name = tabNameList[2];
            Tab3.Name = tabNameList[3];
            Tab4.Name = tabNameList[4];
        }


        //private void MainForm_Resize(object sender, EventArgs e)
        //{
        //    this.Size = new Size(1340, 960);
        //    this.MaximumSize = new Size(1340, 960);
        //    this.MinimumSize = new Size(1340, 960);
        //}


        //카테고리 함수---------------------------------------------------------------------------------------------------------------------------------------
        private void DisplayCategery()
        {
            foreach (var key in category.Categories.Keys)
            {
                foreach (var item in category.Categories[key])
                {
                    UserSubCategory uc = new UserSubCategory();
                    uc.SetBasicMode(item);
                    uc.setMain(key);
                    ((FlowLayoutPanel)((UserMainCategory)panelMainCategory.Controls[key]).flpSubCategory).Controls.Add(uc);
                }
            }
        }

        //실시간 함수---------------------------------------------------------------------------------------------------------------------------------------
        private void SharingSubCategorySet(bool enable)
        {
            foreach (var key in category.Categories.Keys)
            {
                ((UserMainCategory)panelMainCategory.Controls[key]).SetSharing(enable);
                foreach (var item in category.Categories[key])
                {
                    ((FlowLayoutPanel)((UserMainCategory)panelMainCategory.Controls[key]).flpSubCategory).Controls[item].Visible = !enable;
                    if (enable)
                    {
                        SharingCategory.Add(item, false);
                        UserSharingSubCategory ucSharing = new UserSharingSubCategory();
                        ucSharing.shareCategory = LoadScheduleOnSharing;
                        ucSharing.SetBasicMode(item);
                        ucSharing.setMain(key);
                        ((FlowLayoutPanel)((UserMainCategory)panelMainCategory.Controls[key]).flpSubCategory).Controls.Add(ucSharing);
                    }
                    else
                    {
                        UserSharingSubCategory ucSharing = ((FlowLayoutPanel)((UserMainCategory)panelMainCategory.Controls[key]).flpSubCategory).Controls["Sharing" + item] as UserSharingSubCategory;
                        ((FlowLayoutPanel)((UserMainCategory)panelMainCategory.Controls[key]).flpSubCategory).Controls.Remove(ucSharing);
                    }
                }
            }
        }

        private void LoadScheduleOnSharing(object sender, EventArgs e)
        {
            List<string> categories=new List<string>();
            foreach (KeyValuePair<string, bool> category in SharingCategory)
            {
                if (category.Value == true)
                {
                    categories.Add(category.Key);
                }
            }
            monthScheduleList.Clear(); //한달 스케줄 초기화
            Database.ReadShareScheduleList(stdNum, categories);
            Schedule.LoadTotalScheduleList();
            CreateSharingSchedule();
        }
        private void EnterSharingTab(bool enable)
        {
            if (enable)
            {
                UserTabButton OldTab = this.Controls[TabName] as UserTabButton;
                OldTab.HideTab();
                flowLayoutPanelLable.Controls.Clear();
                TabName = TabSharing.Name;
                TabSharing.ShowTab();
                SharingCategory = new Dictionary<string, bool>();
                SharingSubCategorySet(enable);
                AllowOrRequestForm.EnableTab = TabEnableHadler;
                UserSharingAddButton.Exit = ExitSharingHandler;
                UserSharingAddButton.EnableTab = TabEnableHadler;

            }
            else
            {
                TabSharing.HideTab();
                SharingCategory = null;
                isListen = false;
                isShareSchedule = false;
                AllowOrRequestForm.EnableTab = null;
                UserSharingAddButton.EnableTab = null;
                UserSharingAddButton.Exit = null;
                s_client = null;

                SharingSubCategorySet(enable);
                listener.Stop();
            }
        }

        public void TabEnableHadler(object sender, EventArgs e)
        {
            EnableTab(true);
        }

        public void EnableTab(bool enable)
        {
            Tab1.Enabled = !enable;
            Tab2.Enabled = !enable;
            Tab3.Enabled = !enable;
            Tab4.Enabled = !enable;
            TabAll.Enabled = !enable;
        }

        public void ExitSharingHandler(object sender, EventArgs e)
        {
            
            EnableTab(false);
            TabAll.ShowTab();
            ChangeTab(TabAll, e);
        }

        public void ChangeSharingMemberLableStatus(string stdNum, bool enable)
        {
            Invoke((MethodInvoker)(() =>
            {
                    ((UserMemberStatus)flowLayoutPanelLable.Controls[stdNum])?.SetStatus(enable);
            }));
        }

        //탭 함수-------------------------------------------------------------------------------------------------------------------------------------------
        private void ChangeTab(object sender, EventArgs e)
        {
            if (TabName != TabSharing.Name)
            {
                UserTabButton OldTab = this.Controls[TabName] as UserTabButton;
                OldTab.HideTab();
            }
            else
            {
                EnterSharingTab(false);
            }

            UserTabButton btn = sender as UserTabButton;
            TabName = btn.Name;
            ChangeShareSchedule();
            LoadMainForm(); //추가
            UpdateTab();
        }

        private void SetCheckedCategoryByTab()
        {
            flpLabel.Controls.Clear();
            foreach (string key in category.Categories.Keys)
            {
                FlowLayoutPanel flp = ((UserMainCategory)panelMainCategory.Controls[key]).flpSubCategory;
                foreach (UserSubCategory subCategory in flp.Controls)
                {
                    Color subColor = category.GetColor(subCategory.GetText());
                    if (TabName != TabAll.Name)
                    {
                        bool check = category.IsChecked(TabName, subCategory.GetText());
                        subCategory.SetCheckedEnable(true);
                        subCategory.SetChecked(check);
                    }
                    else
                    {
                        subCategory.SetCheckedEnable(false);
                        subCategory.SetChecked(true);
                    }

                    subCategory.SetColor(subColor);
                }
            }
        }
        public void UpdateTab()
        {
            Task.Run(() => this.Invoke(new Action(delegate ()
            {
                SetCheckedCategoryByTab();
                CreateSchedule();
                DisplayToday();
            })));
        }




        //달력 함수-----------------------------------------------------------------------------------------------------------------------------------------
        private void dispalyDate()
        {
            DateTime now = DateTime.Now;

            year = now.Year;
            month = now.Month;
            createDates();
        }

        private void createDates()
        {
            //Database.ReadScheduleList();
            if (!isShareSchedule)
            {
                Database.ReadTabScheduleList();
                Schedule.ReadTabKlasSchedule(); //추가
            }

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

        //카테고리/탭 호출용
        public static void CreateSchedule()
        {
            Database.ReadTabScheduleList();
            Schedule.ReadTabKlasSchedule();

            DateTime startOfMonth = new DateTime(year, month, 1);
            int dates = DateTime.DaysInMonth(year, month);
            int dayOfWeek = Convert.ToInt32(startOfMonth.DayOfWeek.ToString("d")) + 1;
            int index = 0;
            int date = 1;

            foreach (UserDate userDate in Application.OpenForms.OfType<MainForm>().FirstOrDefault().GetUserDate())
            {
                if (++index < dayOfWeek) userDate.ChangeBlank();
                else if (date <= dates) userDate.SetDate(date++);
                else userDate.ChangeBlank();

                if (index % 7 == 0) userDate.ChangeColor(Color.Blue);
                else if (index % 7 == 1) userDate.ChangeColor(Color.Red);
            }
        }

        //실시간 일정 공유 호출용
        public static void CreateSharingSchedule() //학번
        {

            DateTime startOfMonth = new DateTime(2023, 6, 1);
            int dates = DateTime.DaysInMonth(2023, 6);
            int dayOfWeek = Convert.ToInt32(startOfMonth.DayOfWeek.ToString("d")) + 1;
            int index = 0;
            int date = 1;

            foreach (UserDate userDate in Application.OpenForms.OfType<MainForm>().FirstOrDefault().GetUserDate())
            {
                if (++index < dayOfWeek) userDate.ChangeBlank();
                else if (date <= dates) userDate.SetShareDate(date++);
                else userDate.ChangeBlank();

                if (index % 7 == 0) userDate.ChangeColor(Color.Blue);
                else if (index % 7 == 1) userDate.ChangeColor(Color.Red);
            }
        }

        //마감일정 함수-----------------------------------------------------------------------------------------------------------------------------------------
        public void InitializeMagam()
        {
            string[] MagamKLASName = { "btnMagam_Online", "btnMagam_Quiz", "btnMagam_Task", "btnMagam_Prjct" };
            DateTime now = DateTime.Now;

            foreach (string MagamName in MagamKLASName)
            {
                DateTime min = DateTime.MaxValue;
                TimeSpan time;

                foreach (Schedule schedule in KlasSchedule[MagamName.Substring(9)])
                {
                    //최소 날짜 구하기
                    if (schedule.endDate < min) min = schedule.endDate;
                }

                if (min != DateTime.MaxValue)
                {
                    time = min - now;
                    if (time.Days < 3) ((Guna2CircleButton)panelMagamBtns.Controls[MagamName]).FillColor = Color.Red;
                    else if (time.Days < 7) ((Guna2CircleButton)panelMagamBtns.Controls[MagamName]).FillColor = Color.Yellow;
                    else ((Guna2CircleButton)panelMagamBtns.Controls[MagamName]).FillColor = Color.Green;
                }
            }
        }

        private Color ChangeColor(Color color)
        {
            if (color == Color.Red) return Color.DarkRed;
            else if (color == Color.Green) return Color.DarkGreen;
            else if (color == Color.Yellow) return Color.FromArgb(204, 204, 0);
            else if (color == Color.DarkRed) return Color.Red;
            else if (color == Color.DarkGreen) return Color.Green;
            else if (color == Color.FromArgb(204, 204, 0)) return Color.Yellow;
            else if (color == Color.Gray) return Color.Gainsboro;
            else return Color.Gray;
        }

        //오늘의 일정 함수
        public static void DisplayToday()
        {
            Panel panel = (Panel)Application.OpenForms.OfType<MainForm>().FirstOrDefault().panelToday;
            panel.Controls.Clear();
            int index = 0;

            foreach (Schedule schedule in monthScheduleList[DateTime.Now.Day - 1])
            {
                Label lbl = new Label
                {
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
                };
                lbl.Text = schedule.startDate.ToString("HH:mm") + " ~ " + schedule.endDate.ToString("HH:mm") + " " + schedule.title;
                lbl.Location = new Point(0, index * (lbl.Height + 3));
                if (panel.InvokeRequired) panel.Invoke(new MethodInvoker(delegate { panel.Controls.Add(lbl); }));
                else panel.Controls.Add(lbl);
                index++;
            }
            Application.OpenForms.OfType<MainForm>().FirstOrDefault().Refresh();
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
            btnMagam_Click(btnMagam_Online, new EventArgs());
            btnMagam_Quiz.Enabled = true;
            btnMagam_Online.Enabled = true;
            btnMagam_Task.Enabled = true;
            btnMagam_Prjct.Enabled = true;
            InitializeMagam();
        }

        //마감 버튼 클릭 시, 각 일정에 따른 KLAS 클래스에 정의 된 Dictionary 접근하여 데이터 확인.
        //@todo: 개인 마감일정 관리 개별로 할지 논의 필요.
        private void btnMagam_Click(object sender, EventArgs e)
        {
            //이전 컬러 수정
            if (clickMagamBtn != null) clickMagamBtn.FillColor = ChangeColor(clickMagamBtn.FillColor);
            //현제 클릭 마감 버튼 수정
            clickMagamBtn = (Guna2CircleButton)sender;
            Panel panel = (Panel)clickMagamBtn.Parent;
            clickMagamBtn.FillColor = ChangeColor(clickMagamBtn.FillColor);

            panelMagam.Controls.Clear();
            int index = 0;

            Dictionary<string, int[]> MagamLectureDic = new Dictionary<string, int[]>();
            Dictionary<string, DateTime> MagamMinDate = new Dictionary<string, DateTime>();

            // KLAS 요소 중 남은 요소가 없을시 문구 출력
            if (KlasSchedule[clickMagamBtn.Name.Substring(9)].Count < 1 && clickMagamBtn.Name.Substring(9) != "Personal")
            {
                Label lbl = new Label
                {
                    Name = "KLAS_" + clickMagamBtn.Name.Substring(9) + "_" + index.ToString(),
                    Text = "남은 " + KLAS.klasMagamNames[clickMagamBtn.Name.Substring(9)] + "가 없습니다!!",
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
                };
                lbl.Location = new Point(0, index * (lbl.Height + 3));
                //panelMagam.Controls.Add(lbl);
                if (panel.InvokeRequired) panelMagam.Invoke(new MethodInvoker(delegate { panelMagam.Controls.Add(lbl); }));
                else panelMagam.Controls.Add(lbl);
                index++;
            }
            else
            {

                foreach (Schedule schedule in KlasSchedule[clickMagamBtn.Name.Substring(9)])
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
                foreach (Schedule schedule in KlasSchedule[clickMagamBtn.Name.Substring(9)])
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
                        Name = "KLAS_" + clickMagamBtn.Name.Substring(9) + "_" + index.ToString(),
                        Text = items.Key + " " + KLAS.klasMagamNames[clickMagamBtn.Name.Substring(9)] + " " + items.Value[0] + " 개 중 " + items.Value[1] + " 개가 " + Schedule.MagamDateFrom(MagamMinDate[items.Key]) + " 남았습니다.",
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


        }

        //달력 컨트롤--------------------------------------------------------------------------------------------------------------------------------------
        private void btnMonth_Click(object sender, EventArgs e)
        {
            switch (((Guna2Button)sender).Name.Substring(3))
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
            panelMainCategory.Controls.Add(category);
        }

        public IEnumerable<UserDate> GetUserDate()
        {
            return flpDays.Controls.OfType<UserDate>();
        }


        //각 맴버들 초대
        public void InvitieShareSchedule(object sender, EventArgs e)
        {

            listener.Stop();
            isListen = false;
            Database.DeleteAddress();
            
            btnUserSharingAddButton.ChangeStatus(true);

            sharingMember.Add(stdNum);
            List<string> todoLink = sharingMember.ToList();
            todoLink.Remove(stdNum);

            foreach (string memberNum in sharingMember)
            {
                UserMemberStatus memberStatus = new UserMemberStatus();
                memberStatus.SetName(memberNum);
                memberStatus.SetColor(testStdNumColor[memberNum]);
                if (memberNum == stdNum)
                    memberStatus.SetStatus(true);
                
                flowLayoutPanelLable.Controls.Add(memberStatus);
            }


            s_client.addressDict = Database.GetAddress(sharingMember);
            //Init 데이터 생성
            s_client.InviteClass = new Invite
            {
                Type = (int)PacketType.INVITE,
                members = sharingMember,
                todoLink = todoLink,
                boss = stdNum,
            };
            s_client.inviteAllMembers();
            btnSettingComplete.Enabled = true;
        }
        
        private void OnInvite(string boss)
        {
            Invoke((MethodInvoker)(() =>
            {
                AllowOrRequestForm allowOrRequestForm = new AllowOrRequestForm();
                allowOrRequestForm.lbl_StudentNumber.Text = boss;
                allowOrRequestForm.TopMost = true;
                foreach (string memberNum in s_client.InviteClass.members)
                {
                    UserMemberStatus memberStatus = new UserMemberStatus();
                    memberStatus.SetName(memberNum);
                    memberStatus.SetColor(testStdNumColor[memberNum]);
                    if (memberNum == stdNum)
                        memberStatus.SetStatus(true);
                    flowLayoutPanelLable.Controls.Add(memberStatus);
                }
                allowOrRequestForm.RefuseConnect+= refusetInvite;
                DialogResult = allowOrRequestForm.ShowDialog();
                if (DialogResult == DialogResult.OK)
                {
                    btnSettingComplete.Enabled = true;
                }
            }));
        }

        public void refusetInvite(object sender, EventArgs e)
        {
            listener.Stop();
            isListen = false;
            listener.Start();
            isListen = true;
            flowLayoutPanelLable.Controls.Clear();
            s_client = null;
        }

        public void ConnectClient(string sender, List<string> todo, string type)
        {
            Invoke(new MethodInvoker(delegate ()
            {
                MessageBox.Show(type + " 성공!\r\n"
                    + "\r\n 연결된 사람 : " + sender
                    + "\r\n todo : " + string.Join(",", todo.Select(std => string.Format("'{0}'", std))));
            }));
        }

        public void ShowMessage(string message)
        {
            Invoke(new MethodInvoker(delegate ()
            {
                MessageBox.Show(message);
            }));
        }

        public void LoadAddress()
        {
            if (s_client != null)
            {
                Invoke((MethodInvoker)(() =>
                {
                    s_client.addressDict = Database.GetAddress(s_client.InviteClass.members);

                }));
            }
        }

        public void applyShareSchedule(string stdNum, List<string> categoryList)
        {
            Invoke((MethodInvoker)(() =>
            {
                monthScheduleList.Clear(); //한달 스케줄 초기화
                Database.ReadShareScheduleList(stdNum, categoryList);
                Schedule.LoadTotalScheduleList();
                CreateSharingSchedule();
            }));

        }
        async public Task EnterShareSchedule()
        {
            Database.SetAddress();
            listener = new TcpListener(IPAddress.Any, 7777);
            listener.Start();
            isListen = true;
            Console.WriteLine("Start Listener");
            s_client = new SocketClient(stdNum);
            s_client.OnConnect += new SocketClient.ConnectClientHandler(ConnectClient);
            s_client.OnLoadAddress += new SocketClient.LoadAddress(LoadAddress);
            s_client.OnMessage += new SocketClient.MessageHandler(ShowMessage);
            s_client.OnInvite += new SocketClient.InvitationMessageHandler(OnInvite);
            s_client.OnSendCategories += new SocketClient.SendCategoryHandler(applyShareSchedule);
            s_client.OnStatusChange += new SocketClient.StatusChangeHandler(ChangeSharingMemberLableStatus);
            while (isListen)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false); //주최자
                    Task.Run(() => s_client.readStreamData(client));
                }
                catch (SocketException se)
                {
                    Trace.WriteLine(string.Format("EnterShareSchedule - SocketException : {0}", se.Message));
                    isListen = false;
                    listener.Stop();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("EnterShareSchedule - Exception : {0}", ex.Message));
                    isListen = false;
                    listener.Stop();
                }
            }
        }

        public void ChangeShareSchedule()
        {
            btnSettingComplete.Visible = isShareSchedule;
            btnSettingComplete.Enabled = false;
            btnUserSharingAddButton.Visible = isShareSchedule;

            //btnUserSharingAddButton.Enabled = !isShareSchedule;
        }


        //실시간 일정 공유 참가 : 현재 클릭
        public void btnShare_Click(object sender, EventArgs e)
        {
            if (!isShareSchedule)
            {
                isShareSchedule = true;

                ChangeShareSchedule();
                btnUserSharingAddButton.CreateSharing = InvitieShareSchedule;
                monthScheduleList.Clear();
                for (int i = 0; i < DateTime.DaysInMonth(year, month); i++)
                {
                    monthScheduleList.Add(new List<Schedule>());
                }
                createDates();

                EnterSharingTab(true);

                Task.Run(() => EnterShareSchedule());
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (listener != null)
            {
                this.listener.Stop();
            }
            if (s_client != null && s_client.clientSocketDict.Count > 0)
            {
                foreach (KeyValuePair<string, TcpClient> keyValue in s_client.clientSocketDict)
                    keyValue.Value.Close();
            }

            Database.DeleteAddress();
        }
        public static void LoadMainForm()
        {
            if (!isShareSchedule)
            {
                Database.ReadTabScheduleList();
                Schedule.ReadTabKlasSchedule(); //추가
            }

            DateTime startOfMonth = new DateTime(year, month, 1);
            int dates = DateTime.DaysInMonth(year, month);
            int dayOfWeek = Convert.ToInt32(startOfMonth.DayOfWeek.ToString("d")) + 1;
            int index = 0;
            int date = 1;

            foreach (UserDate userDate in Application.OpenForms.OfType<MainForm>().FirstOrDefault().GetUserDate())
            {
                if (++index < dayOfWeek) userDate.ChangeBlank();

                else if (date <= dates) userDate.SetDate(date++);
                else userDate.ChangeBlank();

                if (index % 7 == 0) userDate.ChangeColor(Color.Blue);
                else if (index % 7 == 1) userDate.ChangeColor(Color.Red);
            }
        }

        //보낼 때
        private void btnSettingComplete_Click(object sender, EventArgs e)
        {
            List<string> categories = new List<string>();
            foreach (KeyValuePair<string, bool> category in SharingCategory)
            {
                if (category.Value == true)
                {
                    categories.Add(category.Key);
                }
            }
            
            s_client.sendCategoryList(categories);
        }

        private void btn_logout_Click(object sender, EventArgs e)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"KSCS");
            }
            catch (Exception ex) { }
            this.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
