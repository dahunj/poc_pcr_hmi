﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ABI_POC_PCR.SerialComm;

namespace ABI_POC_PCR
{
    public partial class LogIn : MetroFramework.Forms.MetroForm
    {

        SharedMemory sharedmem = SharedMemory.GetInstance();
        PcrProtocol serial = PcrProtocol.GetInstance();

        bool isConnected = false;

        public LogIn()
        {
            InitializeComponent();

            //tb_LoginPW.BackColor = Color.LightPink;
            //tb_LoginID.BackColor = Color.LightPink;
            //btnLogin.BackColor = Color.LightPink; 
            //tb_LoginPW.Enabled = false;
            //tb_LoginID.Enabled = false;
            //btnLogin.Enabled = false;
        }

        private void LogIn_Load(object sender, EventArgs e)
        {
            // combo_comList = 콤보 박스
            //COM Port 리스트 얻어 오기
            string[] comlist = System.IO.Ports.SerialPort.GetPortNames();

            //COM Port가 있는 경우에만 콤보 박스에 추가.
            if (comlist.Length > 0)
            {
                cb_Port_Main.Items.AddRange(comlist);
                //제일 처음에 위치한 녀석을 선택
                cb_Port_Main.SelectedIndex = 0;
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\member.info";

            string[] lines = File.ReadAllLines(fileName);

            int readNum = 1;
            string temp = "";
            for (int i = 1; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i]; // name, ID, PW, athority

                char[] sep = { ',' };

                string[] result = temp.Split(sep);
                string[] data6 = new string[4] { temp, temp, temp, temp };
                int index = 0;
                foreach (var item in result)
                {
                    data6[index++] = item;
                }
                if(data6[1] == tb_LoginID.Text &&
                    data6[2] == tb_LoginPW.Text)
                {
                    sharedmem.userName = data6[0];
                    sharedmem.userID = tb_LoginID.Text;
                    sharedmem.userPW = tb_LoginPW.Text;

                    //if (tb_LoginID.Text == "ABI" && tb_LoginPW.Text == "5344")
                    //{
                    // 엔지니어 계정 로그인임 --> 계정정보에서도 관리 가능

                    //sharedmem.userName =   
                    this.Visible = false;
                    //this.Close();
                    //Home dlg = new Home();
                    MainFrm dlg = new MainFrm();
                    dlg.Owner = this;
                    dlg.ShowDialog();
                    //dlg.Owner = this;
                    //Owner.Show();

                    //}

                    ////this.Visible = false;
                    ////Home dlg = new Home();
                    ////dlg.Owner = this;
                    //home.ShowDialog();
                    //this.Close();
                }
                //dataGridView_Manage.Rows.Add(data6);
            }         
        }

        private void btn_Connect_Main_Click(object sender, EventArgs e)
        {
            // 컴포트 선택 후 연결 버튼 클릭하면 ID, PW 입력창이 활성화됨
            //showLog("장치와 연결 동작");

            //컴포트 연결시도
            bool flag = Open_exec();


            //added by dahunj
            //tb_Log.Visible = true;

            //연결에 성공하면
            if (flag)
            {
                tb_LoginPW.BackColor = Color.WhiteSmoke;
                tb_LoginID.BackColor = Color.WhiteSmoke;

                tb_LoginPW.Enabled = true;
                tb_LoginID.Enabled = true;
                btnLogin.Enabled = true;
                //tb_ID_Main.Enabled = true;
                //tb_PW_Main.Enabled = true;
                //btn_Login_Main.Enabled = true;
                btn_Connect_Main.Text = "Disconnect";
            }
            else
            {
                tb_LoginPW.BackColor = Color.LightPink;
                tb_LoginID.BackColor = Color.LightPink;
                btnLogin.BackColor = Color.LightPink;
                tb_LoginPW.Enabled = false;
                tb_LoginID.Enabled = false;
                btnLogin.Enabled = false;
                //tb_ID_Main.Enabled = false;
                //tb_PW_Main.Enabled = false;
                //btn_Login_Main.Enabled = false;
                btn_Connect_Main.Text = "Connect";
            }
        }

        private bool Open_exec()
        {
            if (isConnected)
            {
                serial.Close();
                isConnected = false;
                //ConnectChanged(this, EventArgs.Empty);
                return false;
            }
            else
            {
                bool success = false;

                if ((serial != null) && (serial.IsConnect() == false))
                {
                    bool tryConnect = false;

                    try
                    {
                        tryConnect = serial.Connect(cb_Port_Main.SelectedItem.ToString());
                    }
                    catch
                    { }

                    if (tryConnect)
                    {
                        if (true)
                        {
                            //Open_cmd.CanSet = false;
                            isConnected = true;
                            success = true;
                            return true;
                        }
                    }
                }

                if (success != true)
                {

                }
                else
                {

                }

                return false;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
    }
}
