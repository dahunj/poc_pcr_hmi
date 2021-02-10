﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ABI_POC_PCR.SerialComm;
using System.Threading;
using System.Threading.Tasks;
using ScottPlot;
using ABI_POC_PCR.GraphPlot;


//public class ProgressBarEx : ProgressBar { 
//    private SolidBrush brush = null; 
//    public ProgressBarEx() { 
//        this.SetStyle(ControlStyles.UserPaint, true); 
//    } 
//    protected override void OnPaint(PaintEventArgs e) { 
//        if (brush == null || brush.Color != this.ForeColor) 
//            brush = new SolidBrush(this.ForeColor); 
//        Rectangle rec = new Rectangle(0, 0, this.Width, this.Height); 

//        if (ProgressBarRenderer.IsSupported) 
//            ProgressBarRenderer.DrawHorizontalBar(e.Graphics, rec); 
//        rec.Width = (int)(rec.Width * ((double)Value / Maximum)) - 4; 
//        rec.Height = rec.Height - 4; 
//        e.Graphics.FillRectangle(brush, 2, 2, rec.Width, rec.Height); 
//    } 
//}

namespace ABI_POC_PCR
{
    public partial class MainFrm : Form
    {
        Thread th1;

        MotionManager mm = new MotionManager();

        SharedMemory sharedmem = SharedMemory.GetInstance();

        BarcodeProtocol serialBarcode = BarcodeProtocol.GetInstance();

        DateTime testStartTime;
        DateTime testEndTime;

        string test_start_time;
        string test_end_time;
        ListViewItem lvi1;
        ListViewItem lvi3;
        ListViewItem lvi4;

        

        #region[[[[[[[[[기존 변수]]]]]]]]]
        int userMode = 0;
        int processStep = 0;
        int receiveDataStep = 0;                // BASE, 15, 20, 25, ... FiNAL = 1, ..., 8  // Base 측정값이 오면 1, Final 측정값이 오면 8
        static int CH_CNT = 4;
        static int COL_CNT = 28; // base, 15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,45

        PcrProtocol serial = PcrProtocol.GetInstance();
        InfoDialog aboutBox;

        int selectedRowCount = 0;
        private object tb_deID_IDManage;
        public Dictionary<String, String> cmd_dic;


        bool isConnected = false;
        int nProgressBar = 0;

        bool bLoadedRecipe = false; // 레시피를 읽었는가?

        // 로그 파일 생성용
        LogWriter logToFile = new LogWriter();
        bool bSaveLog = true;

        string[] headerName = { "", "", "", "" };
        string[] chamberName = { "", "", "", "" };
        string strData;
        string[,] CH_DATA;
        string[] CH_BASE_DATA;

        bool firstRecv = false;
        int recv_cnt = 0;
        int recv_step = -1;

        // 데이터 버퍼 업데이트 확인용;
        bool bUpdated = false;

        // 시작여부 확인용
        bool b_start_Process = false;
        bool b_check_Door = false;
        static int nTimerNo = 0;


        bool globRecv = false;
        bool b_GUI_Init = false;

        //delegate void GetSerialStringCallback(object obj);
        string waitData;
        string[] procData;

        delegate void SetDataBoxCallback(string str);
        delegate void SetTextBoxCallback(TextBox tb, string str);

        /// <summary>
        /// 0907
        /// 데이터 수신 관련 기존 소스에서 추가
        /// </summary>
        string data_tb;
        public string Data_tb
        {
            get
            {
                return data_tb;
            }
            set
            {
                if (data_tb != value)
                {
                    data_tb = value;
                    //NotifyPropertyChanged("Data_tb");
                }
            }
        }
        #endregion

        ////////////////////////////////////////////

        public MainFrm()
        {
            InitializeComponent();

            //프로그램 실행시 Data 폴더 확인 및 없을경우 Data 폴더 생성
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\data");
            if (!di.Exists) { di.Create(); }
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(Application.StartupPath + @"\log");
            if (!di2.Exists) { di2.Create(); }
            System.IO.DirectoryInfo di3 = new System.IO.DirectoryInfo(Application.StartupPath + @"\report");
            if (!di3.Exists) { di3.Create(); }

            // 초기 컴포넌트 세팅

            // 그리드 해더 이름 읽어와서 설정
            setHeaderName();

            // 그리드 기본 값 설정
            presetDataGrid_Tester(dataGridView1_Tester, chamberName[0]);
            presetDataGrid_Tester(dataGridView2_Tester, chamberName[1]);
            presetDataGrid_Tester(dataGridView3_Tester, chamberName[2]);
            presetDataGrid_Tester(dataGridView4_Tester, chamberName[3]);

            presetDataGrid_Engineer(dataGridView1_Engineer, chamberName[0]);
            presetDataGrid_Engineer(dataGridView2_Engineer, chamberName[1]);
            presetDataGrid_Engineer(dataGridView3_Engineer, chamberName[2]);
            presetDataGrid_Engineer(dataGridView4_Engineer, chamberName[3]);

            presetDataGrid_ct(dataGridView5, chamberName[0]);
            presetDataGrid_ct(dataGridView6, chamberName[1]);
            presetDataGrid_ct(dataGridView7, chamberName[2]);
            presetDataGrid_ct(dataGridView8, chamberName[3]);

            // 진행바 칼라 설정
            progressBar_step.ForeColor = Color.FromArgb(255, 0, 0);
            progressBar_step.BackColor = Color.FromArgb(150, 0, 0);

            // 작업진행상황 초기화
            setProcessMode(0);

            // 유저모드 초기화
            setUserMode(0);

            //////////////////////////////////////////////////////////////////////////////////
            ///// 데이터그리드 업데이트 테스트
            ///
            /*
            // 데이터 그리드 각 열에 대하여 데이터 4개를 변경할 수 있음
            // 전달인자 : 1, 1, 4개 값 --> 챔버1, 1번열, 4개의 값
            setDataGrid_Tester(dataGridView1_Tester, 1, "1", "2", "3", "4");
            setDataGrid_Tester(dataGridView2_Tester, 2, "11", "22", "33", "44");
            setDataGrid_Tester(dataGridView3_Tester, 3, "111", "222", "333", "444");
            setDataGrid_Tester(dataGridView4_Tester, 4, "1111", "2222", "3333", "4444");

            // 데이터 그리드 각 열에 대하여 데이터 4개를 변경할 수 있음
            // 전달인자 : 1, 1, 4개 값 --> 챔버1, 1번열, 4개의 값
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 1, "a1", "a2", "a3", "a4"); 
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 2, "b1", "b2", "b3", "b4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 3, "c1", "c2", "c3", "c4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 4, "d1", "d2", "d3", "d4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 5, "e1", "e2", "e3", "e4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 6, "f1", "f2", "f3", "f4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 7, "g1", "g2", "g3", "g4");
            setDataGrid_Engineer(dataGridView1_Engineer, 1, 8, "h1", "h2", "h3", "h4");

            // 데이터 그리드 각 열에 대하여 데이터 4개를 변경할 수 있음
            // 전달인자 : 1, 1, 4개 값 --> 챔버1, 1번열, 4개의 값
            setDataGrid_Engineer(dataGridView2_Engineer, 1, 1, "a1", "a2", "a3", "a4");
            setDataGrid_Engineer(dataGridView2_Engineer, 1, 2, "b1", "b2", "b3", "b4");
            setDataGrid_Engineer(dataGridView3_Engineer, 1, 3, "c1", "c2", "c3", "c4");
            setDataGrid_Engineer(dataGridView3_Engineer, 1, 4, "d1", "d2", "d3", "d4");
            setDataGrid_Engineer(dataGridView4_Engineer, 1, 5, "e1", "e2", "e3", "e4");
            setDataGrid_Engineer(dataGridView4_Engineer, 1, 6, "f1", "f2", "f3", "f4");
            setDataGrid_Engineer(dataGridView2_Engineer, 1, 7, "g1", "g2", "g3", "g4");
            setDataGrid_Engineer(dataGridView3_Engineer, 1, 8, "h1", "h2", "h3", "h4");
            */

            cmd_dic = new Dictionary<string, string>();

            cmd_dic.Add(":ROUTINE_CYCLE_MAX", "");
            cmd_dic.Add(":OPTIC_OPERATION_KEEPING_TEMP_SEC", "");
            cmd_dic.Add(":OPTIC_NO_OPERATION_KEEPING_TEMP_SEC", "");
            cmd_dic.Add(":DELAY_TIME_BEFORE_OPTING_RUNING", "");
            cmd_dic.Add(":KEEPING_MINUTE_PELTIER_TEMPERATURE", "");
            cmd_dic.Add(":KEEPING_TIME_FOR_HIGH_TEMPERATURE", "");
            cmd_dic.Add(":HEAT_SETPOINT", "");
            cmd_dic.Add(":COOL_SETPOINT", "");
            cmd_dic.Add(":PRE_COND_SETPOINT", "");

            serial.ReceivedEvent += GetSerialString;
            serial.ReceivedRacketEvent += RxPacket_CallBack;

            /// 그리드 배열에 넣은 버퍼 생성  
            CH_DATA = new string[CH_CNT * 4, COL_CNT];  // 16 by 7  --> cycle
            CH_BASE_DATA = new string[CH_CNT * 4];     // 16      --> Base

            ///////////////////////////////////////////////////
            /// 변수들 초기화
            
            _reset_Process();

            /////////////////////////////////////////////////////
            ///// 그리드 배열에 넣은 값 초기화
            //CH_DATA = new string[CH_CNT * 4, COL_CNT];  // 16 by 7 ???
            //CH_BASE_DATA = new string[CH_CNT * 4];     // 16

            //for (int i = 0; i < CH_CNT * 4; i++)
            //{
            //    CH_BASE_DATA[i] = "";
            //    for(int j = 0; j < COL_CNT; j++)
            //    {
            //        CH_DATA[i, j] = "";
            //    }
            //}

           // pb_Logo.Image = System.Drawing.Image.FromFile("data\\logo.bmp");

            Plotter.Init();
            
            //tb_ID_Main.Text = "ABI2";
            //tb_PW_Main.Text = "5344";
            //SetScottPlot();

            serialBarcode.ReceivedEvent += GetSerialStringBarcode;

            

        }

        public void Run()
        {
            /*
            string cTime = DateTime.Now.ToLongTimeString();
            string cDate = DateTime.Now.ToLongDateString();

            if(this.InvokeRequired == true)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.Text = "ABI POC PCR ver. 2.0.0     [" + cDate + " | " + cTime + "]";
                }
                );
                
            }
           

            // 데이터 버퍼가 찼는지 확인하고 업데이트
            nTimerNo++;
            if (nTimerNo % 5 == 0 && processStep == 9 && recv_step > -1) // 5초 마다 업데이트
            {
                nTimerNo = 0;
                btnSetBaseData_Click(null, null);
                btnSetFinalData_Click(null, null);
            }

            if (processStep == 9) // 5초 마다 업데이트
            {
                // 상태바 업데이트 - 시작을 20%에서 시작함
                if (progressBar_Tester.InvokeRequired == true)
                {
                    progressBar_Tester.Invoke((MethodInvoker)delegate
                    {
                        progressBar_Tester.Value = (int)(30 + recv_step * 2);
                    }
                    );

                }

                if (progressBar_Manager.InvokeRequired == true)
                {
                    progressBar_Manager.Invoke((MethodInvoker)delegate
                    {
                        progressBar_Manager.Value = (int)(30 + recv_step * 2);
                    }
                    );
                }
            }

            // 버퍼에 쓰레기가 있을 경우 체크해서 클리어
            if (waitData.Length != 0)
                if (waitData[0].Equals('-') || waitData[0].Equals('<') || waitData[0].Equals('>'))
                    waitData = "";

            if (waitData.Length > 100) waitData = "";


            if (b_start_Process && processStep != 9)
            {
                b_start_Process = false;
                _start_Process();
            }

            if (b_check_Door)
            {
                b_check_Door = false;
                _check_Door();
            }

            if (b_GUI_Init)
            {
                // 검사단계 초기화
                setProcessMode(0);
                setProgressInfo(-1);

                if (btn_Start_Test.InvokeRequired == true)
                {
                    btn_Start_Test.Invoke((MethodInvoker)delegate
                    {
                        btn_Start_Test.Text = "Test Start";
                    }
                    );

                }
                

                b_GUI_Init = false;
            }
            */
        }

        private async void SetScottPlot()
        {
            var inputDataY = new double[] { 1, 4, 9, 16, 25, 19, 22, 30, 1, 4, 9, 16, 25, 19, 22, 30 };
            var inputDataY2 = new double[] { 1, 2, 3, 5, 8, 29, 12, 20, 1, 2, 3, 5, 8, 29, 12, 20 };
            var inputDataY3 = new double[] { 2, 2, 4, 7, 8, 29, 22, 20, 2, 2, 4, 7, 8, 29, 22, 20 };
            var inputDataY4 = new double[] { 6, 3, 3, 7, 8, 20, 12, 24, 6, 3, 3, 7, 8, 20, 12, 24 };

            var progress = new Progress<int>(index =>
            {
                Plotter.ch1DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch1DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch1DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch1DataDic["CY5"].Add(inputDataY4[index]);

                Plotter.ch2DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch2DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch2DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch2DataDic["CY5"].Add(inputDataY4[index]);
                
                Plotter.ch3DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch3DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch3DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch3DataDic["CY5"].Add(inputDataY4[index]);
                
                Plotter.ch4DataDic["FAM"].Add(inputDataY[index]);
                Plotter.ch4DataDic["ROX"].Add(inputDataY2[index]);
                Plotter.ch4DataDic["HEX"].Add(inputDataY3[index]);
                Plotter.ch4DataDic["CY5"].Add(inputDataY4[index]);

                Plotter.UpdatePlot(formsPlot1, "Chamber1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5);
                Plotter.UpdatePlot(formsPlot2, "Chamber2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5);
                Plotter.UpdatePlot(formsPlot3, "Chamber3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5);
                Plotter.UpdatePlot(formsPlot4, "Chamber4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5);
            });

            var plotTask = Task.Run(() => AsyncInputSimulator(progress));
            await plotTask;
        }
        private void AsyncInputSimulator(IProgress<int> progress)
        {
            for (int i = 0; i < 8; i++)
            {
                if (progress != null)
                    progress.Report(i);

                Thread.Sleep(1000);
            }
        }
        private void formsPlot1_MouseMoved(object sender, MouseEventArgs e)
        {
            //사용안함
            //if(ch1FAMHL != null)
            //{
            //    int pixelX = e.X;
            //    int pixelY = e.Y;

            //    double mouseX = formsPlot1.plt.CoordinateFromPixelX(e.Location.X);
            //    double mouseY = formsPlot1.plt.CoordinateFromPixelY(e.Location.Y);

            //    ch1FAMHL.HighlightClear();
            //    var (x, y, index) = ch1FAMHL.HighlightPointNearest(mouseX, mouseY);
                
            //    if(cBoxCh1FAM.Checked)
            //        formsPlot1.Render(skipIfCurrentlyRendering: true);

            //    PointF highlightedPoint = formsPlot1.plt.CoordinateToPixel(x, y);
            //    double dX = e.Location.X - highlightedPoint.X;
            //    double dY = e.Location.Y - highlightedPoint.Y;
            //    double distanceToPoint = Math.Sqrt(dX * dX + dY * dY);

            //    if (distanceToPoint < 10)
            //        tooltip.Show($"{x}, {y}", this,
            //            (int)highlightedPoint.X + formsPlot1.Location.X,
            //            (int)highlightedPoint.Y + formsPlot1.Location.Y + tabControl4.Location.Y);
            //    else
            //        tooltip.Hide(this);
            //}
            

            //(double coordinateX, double coordinateY) = formsPlot1.GetMouseCoordinates();

            //Console.WriteLine($"{e.X:0.000}");
            //XPixelLabel.Text = $"{e.X:0.000}";
            //YPixelLabel.Text = $"{e.X:0.000}";
            //Console.WriteLine($"{coordinateX:0.00000000}");

            //XCoordinateLabel.Text = $"{coordinateX:0.00000000}";
            //YCoordinateLabel.Text = $"{coordinateY:0.00000000}";

            //vLine.position = coordinateX;
            //hLine.position = coordinateY;

            //formsPlot1.Render(skipIfCurrentlyRendering: true);
        }
        private void cBoxPlot_CheckedChanged(object sender, EventArgs e)
        {
            var getCBoxName = (sender as CheckBox).Name;
            Plotter.CheckBoxChecked(getCBoxName, (sender as CheckBox).Checked, formsPlot1, formsPlot2, formsPlot3, formsPlot4);
        }
        private void btnRSTPlot1_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot1);
        }
        private void btnRSTPlot2_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot2);
        }
        private void btnRSTPlot3_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot3);
        }
        private void btnRSTPlot4_Click(object sender, EventArgs e)
        {
            Plotter.ViewInit(formsPlot4);
        }

        #region[[[[[[[[[[기존코드]]]]]]]]]]
        private void setDataGrid_Engineer(DataGridView dgv, int chamberIndex, int columindex, string data1, string data2, string data3, string data4)
        {
            // chamberIndex : 1~4
            // columindex : base~final (1~8)

            // 기존 그리드 값을 읽어온 후 값을 업데이트 하고 다시 그리드 값을 설정하자
            //string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };
          
            string[] d1 = new string[10] { chamberName[chamberIndex - 1], headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d2 = new string[10] { chamberName[chamberIndex - 1], headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d3 = new string[10] { chamberName[chamberIndex - 1], headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d4 = new string[10] { chamberName[chamberIndex - 1], headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            // 데이터 가져오기
            int rowNo = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                rowNo++;
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        if (rowNo == 1) d1[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 2) d2[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 3) d3[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 4) d4[i] = row.Cells[i].Value.ToString();
                    }
                }
            }

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat); int iD1 = (int)toFloat;
            float.TryParse(data2, out toFloat); int iD2 = (int)toFloat;
            float.TryParse(data3, out toFloat); int iD3 = (int)toFloat;
            float.TryParse(data4, out toFloat); int iD4 = (int)toFloat;

            // 데이터 업데이트
            d1[columindex + 1] = iD1.ToString();
            d2[columindex + 1] = iD2.ToString();
            d3[columindex + 1] = iD3.ToString();
            d4[columindex + 1] = iD4.ToString();

            // 새로운 데이터는 추가
            dgv.Rows.Add(d1);
            dgv.Rows.Add(d2);
            dgv.Rows.Add(d3);
            dgv.Rows.Add(d4);

            // 기존 데이터는 삭제
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);


            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
            /*
            if (Plotter.isValueBase)
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                Plotter.isValueBase = false;
            }

            switch (chamberIndex)
            {
                case 1:
                    Plotter.ch1DataDic["FAM"].Add(iD1);
                    Plotter.ch1DataDic["ROX"].Add(iD2);
                    Plotter.ch1DataDic["HEX"].Add(iD3);
                    Plotter.ch1DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot1, "Chamber1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5);
                    break;
                case 2:
                    Plotter.ch2DataDic["FAM"].Add(iD1);
                    Plotter.ch2DataDic["ROX"].Add(iD2);
                    Plotter.ch2DataDic["HEX"].Add(iD3);
                    Plotter.ch2DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot2, "Chamber2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5);
                    break;
                case 3:
                    Plotter.ch3DataDic["FAM"].Add(iD1);
                    Plotter.ch3DataDic["ROX"].Add(iD2);
                    Plotter.ch3DataDic["HEX"].Add(iD3);
                    Plotter.ch3DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot3, "Chamber3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5);
                    break;
                case 4:
                    Plotter.ch4DataDic["FAM"].Add(iD1);
                    Plotter.ch4DataDic["ROX"].Add(iD2);
                    Plotter.ch4DataDic["HEX"].Add(iD3);
                    Plotter.ch4DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot4, "Chamber4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5);
                    break;
                default:
                    break;
            }
            */

        }

        private void setDataGrid_Ct(DataGridView dgv, int chamberIndex, int columindex, string data1, string data2, string data3, string data4)
        {
            // chamberIndex : 1~4
            // columindex : base~final (1~8)

            // 기존 그리드 값을 읽어온 후 값을 업데이트 하고 다시 그리드 값을 설정하자
            //string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };
            string[] d1 = new string[30] { headerName[0],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d2 = new string[30] { headerName[1],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d3 = new string[30] { headerName[2],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] d4 = new string[30] { headerName[3],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            //string[] d1 = new string[10] { chamberName[chamberIndex - 1], headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d2 = new string[10] { chamberName[chamberIndex - 1], headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d3 = new string[10] { chamberName[chamberIndex - 1], headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] d4 = new string[10] { chamberName[chamberIndex - 1], headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            // 데이터 가져오기
            int rowNo = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                rowNo++;
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        if (rowNo == 1) d1[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 2) d2[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 3) d3[i] = row.Cells[i].Value.ToString();
                        if (rowNo == 4) d4[i] = row.Cells[i].Value.ToString();
                    }
                }
            }

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat);
            int iD1 = (int)toFloat;

            float.TryParse(data2, out toFloat);
            int iD2 = (int)toFloat;

            float.TryParse(data3, out toFloat);
            int iD3 = (int)toFloat;

            float.TryParse(data4, out toFloat);
            int iD4 = (int)toFloat;

            // 데이터 업데이트
            d1[columindex + 1] = iD1.ToString();
            d2[columindex + 1] = iD2.ToString();
            d3[columindex + 1] = iD3.ToString();
            d4[columindex + 1] = iD4.ToString();

            // 새로운 데이터는 추가
            dgv.Rows.Add(d1);
            dgv.Rows.Add(d2);
            dgv.Rows.Add(d3);
            dgv.Rows.Add(d4);

            // 기존 데이터는 삭제
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);
            dgv.Rows.RemoveAt(0);


            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }

            if (Plotter.isValueBase)
            {
                Plotter.ResetAllPlots(formsPlot1, formsPlot2, formsPlot3, formsPlot4);
                Plotter.isValueBase = false;
            }

            switch (chamberIndex)
            {
                case 1:
                    Plotter.ch1DataDic["FAM"].Add(iD1);
                    Plotter.ch1DataDic["ROX"].Add(iD2);
                    Plotter.ch1DataDic["HEX"].Add(iD3);
                    Plotter.ch1DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot1, "Chamber1", 0, Plotter.ch1DataDic, cBoxCh1FAM, cBoxCh1ROX, cBoxCh1HEX, cBoxCh1CY5);
                    break;
                case 2:
                    Plotter.ch2DataDic["FAM"].Add(iD1);
                    Plotter.ch2DataDic["ROX"].Add(iD2);
                    Plotter.ch2DataDic["HEX"].Add(iD3);
                    Plotter.ch2DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot2, "Chamber2", 1, Plotter.ch2DataDic, cBoxCh2FAM, cBoxCh2ROX, cBoxCh2HEX, cBoxCh2CY5);
                    break;
                case 3:
                    Plotter.ch3DataDic["FAM"].Add(iD1);
                    Plotter.ch3DataDic["ROX"].Add(iD2);
                    Plotter.ch3DataDic["HEX"].Add(iD3);
                    Plotter.ch3DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot3, "Chamber3", 2, Plotter.ch3DataDic, cBoxCh3FAM, cBoxCh3ROX, cBoxCh3HEX, cBoxCh3CY5);
                    break;
                case 4:
                    Plotter.ch4DataDic["FAM"].Add(iD1);
                    Plotter.ch4DataDic["ROX"].Add(iD2);
                    Plotter.ch4DataDic["HEX"].Add(iD3);
                    Plotter.ch4DataDic["CY5"].Add(iD4);
                    Plotter.UpdatePlot(formsPlot4, "Chamber4", 3, Plotter.ch4DataDic, cBoxCh4FAM, cBoxCh4ROX, cBoxCh4HEX, cBoxCh4CY5);
                    break;
                default:
                    break;
            }

        }
        private void setDataGrid_Engineer2(DataGridView dgv, string chName, int index, string data1, string data2, string data3, string data4, string data5, string data6, string data7, string data8)
        {
            // 현재 줄을 지움
            dgv.Rows.RemoveAt(0);

            string[] data_1 = new string[10] { chName, headerName[index], data1, data2, data3, data4, data5, data6, data7, data8 };

            dgv.Rows.Add(data_1);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }
        private void presetDataGrid_Engineer(DataGridView dgv, string value)
        {
            string[] data2 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data3 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data4 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data5 = new string[10] { value, "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            //string[] data2 = new string[10] { value, headerName[0], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data3 = new string[10] { value, headerName[1], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data4 = new string[10] { value, headerName[2], "0", "0", "0", "0", "0", "0", "0", "0" };
            //string[] data5 = new string[10] { value, headerName[3], "0", "0", "0", "0", "0", "0", "0", "0" };

            dgv.Rows.Add(data2);
            dgv.Rows.Add(data3);
            dgv.Rows.Add(data4);
            dgv.Rows.Add(data5);

            dgv.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }

        private void presetDataGrid_ct(DataGridView dgv, string value)
        {
            string[] data2 = new string[30] { headerName[0],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0","0","0","0", "0", "0" };
            string[] data3 = new string[30] { headerName[1],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data4 = new string[30] { headerName[2],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] data5 = new string[30] { headerName[3],  "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            dgv.Rows.Add(data2);
            dgv.Rows.Add(data3);
            dgv.Rows.Add(data4);
            dgv.Rows.Add(data5);

            dgv.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 18, FontStyle.Bold);

            // 그리드뷰를 설정
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }

        private void setDataGrid_Tester(DataGridView dgv, int chamberIndex, string data1, string data2, string data3, string data4)
        {
            // 현재 줄을 지움
            dgv.Rows.RemoveAt(0);

            // string[] chamberName = { "chamber1", "chamber2", "chamber3", "chamber4" };

            int toInt;
            float toFloat;

            // 숫자 변환
            float.TryParse(data1, out toFloat); int iD1 = (int)toFloat;
            float.TryParse(data2, out toFloat); int iD2 = (int)toFloat;
            float.TryParse(data3, out toFloat); int iD3 = (int)toFloat;
            float.TryParse(data4, out toFloat); int iD4 = (int)toFloat;


            // 다시 업데이트
            string[] data = new string[5] { chamberName[chamberIndex - 1], iD1.ToString(), iD2.ToString(), iD3.ToString(), iD4.ToString() };

            dgv.Rows.Add(data);
            dgv.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
        }
        private void presetDataGrid_Tester(DataGridView dgv, string value)
        {
            string[] data1 = new string[5] { value, "0", "0", "0", "0" };

            dgv.Rows.Add(data1);

            dgv.Font = new Font("문체부 제목 돋음체", 28, FontStyle.Regular);
            dgv.DefaultCellStyle.Font = new Font("문체부 제목 돋음체", 28, FontStyle.Bold);

            dgv.CurrentCell = null;

            dgv.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
        }
        private void setHeaderName()
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\header.info";

            string[] lines = File.ReadAllLines(fileName);

            string temp2 = "", temp0 = "", temp1 = "";
            temp0 = lines[1]; // 검사자용
            temp1 = lines[2]; // 관리자용
            temp2 = lines[0]; // 챔버이름


            char[] sep = { ',' };

            string[] result2 = temp2.Split(sep);
            chamberName[0] = result2[0];
            chamberName[1] = result2[1];
            chamberName[2] = result2[2];
            chamberName[3] = result2[3];

            string[] result0 = temp0.Split(sep);
            dataGridView1_Tester.Columns[0].HeaderText = result0[0];
            dataGridView1_Tester.Columns[1].HeaderText = result0[1];
            dataGridView1_Tester.Columns[2].HeaderText = result0[2];
            dataGridView1_Tester.Columns[3].HeaderText = result0[3];
            dataGridView1_Tester.Columns[4].HeaderText = result0[4];

            dataGridView2_Tester.Columns[0].HeaderText = result0[0];
            dataGridView2_Tester.Columns[1].HeaderText = result0[1];
            dataGridView2_Tester.Columns[2].HeaderText = result0[2];
            dataGridView2_Tester.Columns[3].HeaderText = result0[3];
            dataGridView2_Tester.Columns[4].HeaderText = result0[4];

            dataGridView3_Tester.Columns[0].HeaderText = result0[0];
            dataGridView3_Tester.Columns[1].HeaderText = result0[1];
            dataGridView3_Tester.Columns[2].HeaderText = result0[2];
            dataGridView3_Tester.Columns[3].HeaderText = result0[3];
            dataGridView3_Tester.Columns[4].HeaderText = result0[4];

            dataGridView4_Tester.Columns[0].HeaderText = result0[0];
            dataGridView4_Tester.Columns[1].HeaderText = result0[1];
            dataGridView4_Tester.Columns[2].HeaderText = result0[2];
            dataGridView4_Tester.Columns[3].HeaderText = result0[3];
            dataGridView4_Tester.Columns[4].HeaderText = result0[4];

            headerName[0] = result0[1];
            headerName[1] = result0[2];
            headerName[2] = result0[3];
            headerName[3] = result0[4];


            string[] result1 = temp1.Split(sep);
            for(int i = 0; i < 10; i++)
            {
                dataGridView1_Engineer.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView2_Engineer.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView3_Engineer.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView4_Engineer.Columns[i].HeaderText = result1[i];
            }

            //
            for (int i = 0; i < 29; i++)
            {
                dataGridView5.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 29; i++)
            {
                dataGridView6.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 29; i++)
            {
                dataGridView7.Columns[i].HeaderText = result1[i];
            }
            for (int i = 0; i < 29; i++)
            {
                dataGridView8.Columns[i].HeaderText = result1[i];
            }

            // 그리드 컬럼 우측 정렬
            //            dataGridView4_Engineer.DataGrid

        }
        private void setUserMode(int um)
        {
            userMode = um;

            if (userMode != 0) btn_Login_Main.Text = "LOG OUT";

            tb_Log.Visible = false;

           
            if (userMode == 0)
            {
                tabControl1.SelectedIndex = 0;
                tb_WorkerID_Test.Text = "";
                tb_WorkerName_Test.Text = "";
            }
            // 검사자이면 검사탭으로 이동
            if (userMode == 1)
            {
                tabControl1.SelectedIndex = 1;
            }
            // 관리자이면 관리자탬으로 이동
            else if (userMode == 2)
            {
                tabControl1.SelectedIndex = 2;
            }
            // 엔지니어면 엔지니어탭으로 이동
            else if (userMode == 3)
            {
                tabControl1.SelectedIndex = 3;
                tb_Log.Visible = true;
            }

            //tb_PW_Main.Text = "";
        }
        private void setProcessMode(int pm)
        {
            switch (pm)
            {
                case 0:  // 검사 초기화
                    processStep = 0;
                    progressBar_step.Value = 0 / 8;
                    tb_PatientID_Test.Text = "";
                    tb_CartridgeID_Test.Text = "";
                    tb_SampleID_Test.Text = "";

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;
                        btn_DoorOpen_Test.Enabled = true;

                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = false;
                        btn_SetRecipe_Test.Enabled = false;
                        //btn_Start_Test.Enabled = false;

                        //tb_PatientID_Test.Enabled = false;
                        //tb_SampleID_Test.Enabled = false;
                        //tb_CartridgeID_Test.Enabled = false;

                        btn_OpenDoor_Test.Enabled = true;
                        btn_DoorOpen_Test.Enabled = true;

                    }

                    break;
                case 1:   // 검사를 위해 문을 염
                    processStep = 1;
                    progressBar_step.Value = 100 / 8;
                    break;
                case 2:   // 정의 삭제됨 - 바로 3으로
                    processStep = 2;
                    progressBar_step.Value = 200 / 8;
                    break;
                case 3:   // ID 정보 입력 단계
                    processStep = 3;
                    progressBar_step.Value = 300 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;

                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = false;
                        btn_SetRecipe_Test.Enabled = false;
                        //btn_Start_Test.Enabled = false;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = false;

                    }

                    break;
                case 4:  // ID 정보 입력 끝남
                    processStep = 4;
                    progressBar_step.Value = 400 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;

                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        //btn_Start_Test.Enabled = false;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = false;

                    }

                    break;
                case 5:   // 카트리지 캡 제거 안내
                    processStep = 5;
                    progressBar_step.Value = 500 / 8;
                    break;
                case 6:     // 팁 장착 안내 전
                    processStep = 6;
                    progressBar_step.Value = 600 / 8;
                    break;
                case 7:    // 팁 장착 안내 후
                    processStep = 7;
                    progressBar_step.Value = 700 / 8;
                    break;
                case 8:    // 준비 완료
                    processStep = 8;
                    progressBar_step.Value = 800 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;

                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = false;

                    }
                    break;
                case 9:   // 검사 중
                    processStep = 9;
                    progressBar_step.Value = 800 / 8;

                    // 조건에 따른 버튼 등 활성화
                    if (checkBox2.Checked)
                    {
                        cb_Recipe_Test.Enabled = true;
                        btn_SetRecipe_Test.Enabled = true;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = true;
                        tb_SampleID_Test.Enabled = true;
                        tb_CartridgeID_Test.Enabled = true;

                        btn_OpenDoor_Test.Enabled = true;
                        btn_DoorOpen_Test.Enabled = true;
                    }
                    else
                    {
                        cb_Recipe_Test.Enabled = false;
                        btn_SetRecipe_Test.Enabled = false;
                        btn_Start_Test.Enabled = true;

                        tb_PatientID_Test.Enabled = false;
                        tb_SampleID_Test.Enabled = false;
                        tb_CartridgeID_Test.Enabled = false;

                        btn_OpenDoor_Test.Enabled = false;
                        btn_DoorOpen_Test.Enabled = false;
                    }
                    break;
            }

            //MessageBox.Show(pm.ToString());
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

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

            if (comlist.Length > 0)
            {
                cb_Port_Barcode.Items.AddRange(comlist);
                //제일 처음에 위치한 녀석을 선택
                cb_Port_Barcode.SelectedIndex = 0;
            }

            aboutBox = new InfoDialog();

            //제일 처음에 위치한 녀석을 선택
            tb_Right_IDManage.SelectedIndex = 0;

            // 레시피 파일을 검색해서 리스트를 채움
            SetRecipeCombobox_Test();

            // 라벨 투명하게 설정
            //lb_Setting.BackColor = Color.Transparent;
            //lb_Setting.Parent = progressBar_step;


            // 계정정보 읽기
            btn_Search_IDManage_Click(this, null);

            lb_IC_Result.Text = sharedmem.IC_Result;

            tabPage_tester_Click(); 
            // visible false  tabpage 
            //tabControl1.TabPages.Remove(tp_researcher);
            //tabControl_Tester.TabPages.Remove(tabPage1);
            //tabControl_Tester.TabPages.Remove(tabPage9);

            tabControl_Tester.TabPages.Remove(tabPage_rawData);
            tabControl_admin.TabPages.Remove(tp_Table);

            tabControl_Engineer.TabPages.Remove(tp_diagnosis);

            //dataGridView1.AutoGenerateColumns = false;
            setDiagnosisTable();

            
        }
        bool IsTheSameCellValue(int column, int row)
        {
            DataGridViewCell cell1 = dataGridView1[column, row];
            DataGridViewCell cell2 = dataGridView1[column, row - 1];
            if (cell1.Value == null || cell2.Value == null)
            {
                return false;
            }
            return cell1.Value.ToString() == cell2.Value.ToString();
        }


        private void DataGridView1_CellFormatting1(object sender, System.Windows.Forms.DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == 0)
                return;
            if (IsTheSameCellValue(e.ColumnIndex, e.RowIndex))
            {
                e.Value = "";
                e.FormattingApplied = true;
            }
        }
        
        private void DataGridView1_CellPainting1(object sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
        {
            e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None;
            if (e.RowIndex < 1 || e.ColumnIndex < 1)
                return;
            if (IsTheSameCellValue(e.ColumnIndex, e.RowIndex))
            {
                e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
            }
            else
            {
                e.AdvancedBorderStyle.Top = dataGridView1.AdvancedCellBorderStyle.Top;
            }

        }
        
        private void setDiagnosisTable()
        {
            dataGridView1.ColumnCount = 10;
            dataGridView1.ColumnHeadersVisible = true;

            DataGridViewCellStyle columnHeaderStyle = new DataGridViewCellStyle();

            columnHeaderStyle.BackColor = Color.Beige;
            columnHeaderStyle.Font = new Font("Verdana", 10, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle = columnHeaderStyle;

            //set the column header names.
            dataGridView1.Columns[0].Name = "Fluorescent dye";
            dataGridView1.Columns[1].Name = "Threshold";
            dataGridView1.Columns[2].Name = "TB";
            dataGridView1.Columns[3].Name = "NTM";
            dataGridView1.Columns[4].Name = "RIF";
            dataGridView1.Columns[5].Name = "RIF";
            dataGridView1.Columns[6].Name = "RIF";
            dataGridView1.Columns[7].Name = "RIF";
            dataGridView1.Columns[8].Name = "RIF";
            dataGridView1.Columns[9].Name = "INH";


            //dataGridView1.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;
            

            //populate the rows
            string[] row1 = new string[] { "Tube 1", "FAM", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row2 = new string[] { "Tube 1", "ROX", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row3 = new string[] { "Tube 1", "HEX", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row4 = new string[] { "Tube 1", "Cy5", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row5 = new string[] { "Tube 2", "FAM", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row6 = new string[] { "Tube 2", "ROX", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row7 = new string[] { "Tube 2", "HEX", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row8 = new string[] { "Tube 2", "Cy5", "0", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row9 = new string[] { "Tube 3", "FAM", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row10 = new string[] { "Tube 3", "ROX", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row11 = new string[] { "Tube 3", "HEX", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row12 = new string[] { "Tube 3", "Cy5", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row13 = new string[] { "Tube 4", "FAM", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row14 = new string[] { "Tube 4", "ROX", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row15 = new string[] { "Tube 4", "HEX", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            string[] row16 = new string[] { "Tube 4", "Cy5", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf", "asd", "asfasf" };
            

            object[] rows = new object[] { row1, row2, row3, row4, row5, row6, row7, row8, row9, row10, row11, row12, row13, row14, row15, row16 };

            foreach (string[] rowArray in rows)
            {
                dataGridView1.Rows.Add(rowArray);
            }

            /*
            DataTable table = new DataTable();

            // column을 추가합니다.
            table.Columns.Add("ID", typeof(string));
            table.Columns.Add("제목", typeof(string));
            table.Columns.Add("구분", typeof(string));
            table.Columns.Add("생성일", typeof(string));
            table.Columns.Add("수정일", typeof(string));

            // 각각의 행에 내용을 입력합니다.
            table.Rows.Add("ID 1", "제목 1번", "사용중", "2019/03/11", "2019/03/18");
            table.Rows.Add("ID 2", "제목 2번", "미사용", "2019/03/12", "2019/03/18");
            table.Rows.Add("ID 3", "제목 3번", "미사용", "2019/03/13", "2019/03/18");
            table.Rows.Add("ID 4", "제목 4번", "사용중", "2019/03/14", "2019/03/18");

            // 값들이 입력된 테이블을 DataGridView에 입력합니다.
            dataGridView1.DataSource = table;
            */
            
            
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

        private bool Open_exec_barcode()
        {
            if (isConnected)
            {
                serialBarcode.Close();
                isConnected = false;
                //ConnectChanged(this, EventArgs.Empty);
                return false;
            }
            else
            {
                bool success = false;

                if ((serialBarcode != null) && (serialBarcode.IsConnect() == false))
                {
                    bool tryConnect = false;

                    try
                    {
                        tryConnect = serialBarcode.Connect("COM4"); //cb_Port_Main.SelectedItem.ToString()
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

        private void SetRecipeCombobox_Test()
        {
            cb_Recipe_Eng.Items.Clear();
            cb_Recipe_Test.Items.Clear();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");

            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                if (File.Extension.ToLower().CompareTo(".rcp") == 0)
                {
                    String FileNameOnly = File.Name.Substring(0, File.Name.Length - 4);
                    String FullFileName = File.FullName;

                    //MessageBox.Show(FullFileName + " " + FileNameOnly);
                    cb_Recipe_Eng.Items.Add(FileNameOnly);
                    cb_Recipe_Test.Items.Add(FileNameOnly);
                }
            }
        }
        private void btn_Connect_Main_Click(object sender, EventArgs e)
        {
            // 컴포트 선택 후 연결 버튼 클릭하면 ID, PW 입력창이 활성화됨
            //showLog("장치와 연결 동작");

            //컴포트 연결시도
            bool flag = Open_exec();
            //bool flag = true; //Plot Debug

            //added by dahunj
            setUserMode(3);
            tb_Log.Visible = true;

            //연결에 성공하면
            if (flag)
            {
                tb_ID_Main.Enabled = true;
                tb_PW_Main.Enabled = true;
                btn_Login_Main.Enabled = true;
                btn_Connect_Main.Text = "Disconnect";
            }
            else
            {
                tb_ID_Main.Enabled = false;
                tb_PW_Main.Enabled = false;
                btn_Login_Main.Enabled = false;
                btn_Connect_Main.Text = "Connect";
            }
        }
        private void btn_Login_Main_Click(object sender, EventArgs e)
        {
            if (tb_ID_Main.Text == "ABI" && tb_PW_Main.Text == "5344")
            {
                // 엔지니어 계정 로그인임 --> 계정정보에서도 관리 가능
                setUserMode(3);
                tb_Log.Visible = true;

                tb_WorkerID_Test.Text = "ABI";
                tb_WorkerName_Test.Text = "ABI엔지니어";

                // tb_ID_Main.Text = "";
                tb_PW_Main.Text = "";
                return;
            }
            if (btn_Login_Main.Text == "LOG OUT")
            {
                setUserMode(0);
                btn_Login_Main.Text = "LOGIN";
                MessageBox.Show("로그아웃 했습니다.", "로그아웃 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                tabControl1.SelectedIndex = 0;

                tb_ID_Main.Text = "";
                tb_PW_Main.Text = "";

                return;
            }
            // 로그인 계정을 체크
            string userRight = "NOT";

            // 계정에서 로그인한 계정 유무를 확인함
            // 데이터 비교
            foreach (DataGridViewRow row in dataGridView_Manage.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dataGridView_Manage.Columns.Count; i++)
                    {
                        if (tb_ID_Main.Text == row.Cells[1].Value.ToString() // ID
                            && tb_PW_Main.Text == row.Cells[2].Value.ToString()) // PW
                        {
                            userRight = row.Cells[3].Value.ToString();
                            tb_WorkerID_Test.Text = tb_ID_Main.Text;
                            tb_WorkerName_Test.Text = row.Cells[0].Value.ToString();
                        }
                    }
                }
            }

            switch (userRight)
            {
                case "검사자":
                    //MessageBox.Show("검사자 계정으로 로그인 했습니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(1);
                    break;
                case "관리자":
                    //MessageBox.Show("관리자 계정으로 로그인 했습니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(2);
                    break;
                case "엔지니어":
                    //MessageBox.Show("엔지니어 계정으로 로그인 했습니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(3);
                    break;
                case "NOT":
                    MessageBox.Show("계정 정보 또는 비밀번호가 틀립니다. 관리자에게 문의 바랍니다.", "로그인 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setUserMode(0);
                    break;
            }
        }
        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            // 탭의 이동을 원할 경우 권한을 체크함
            if (userMode == 0)
            {
                tabControl1.SelectedIndex = 0;
            }
            else if (userMode == 1)
            {
                if (tabControl1.SelectedIndex == 3 || tabControl1.SelectedIndex == 2)
                {
                    tabControl1.SelectedIndex = 1;
                }
            }
            else if (userMode == 2)
            {
                if (tabControl1.SelectedIndex == 3)
                {
                    tabControl1.SelectedIndex = 2;
                }
            }

            btn_Search_IDManage_Click(this, null);
        }
        private void tabControl4_Selected(object sender, TabControlEventArgs e)
        {
            // 탭의 이동을 원할 경우 권한을 체크함
            if (userMode == 1)
            {
                tabControl_Tester.SelectedIndex = 0;   // 권한이 없는 검사자는 관리자탭으로 이동 불가
            }
            if (processStep == 3) // 검사를 시작한 경우라면 관리자도 탭을 이동할 수 없음
            {
                // 관리자가 현재 보고 있는 탭을 체크하여 그대로 유지하여야 함
                // todo
            }
        }

        // 검사 준비 ==> 관련 초기화 실시
        private void btn_OpenDoor_Test_Click(object sender, EventArgs e)
        {
            // MCU에 문을 열라고 명령함
            if (!isConnected) {
                // 검사 장비와 연결이 되어야 진행함
                MessageBox.Show("먼저 검사 장비와 연결이 되야 합니다. 연결해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 데이터 버퍼 초기화
            for (int i = 0; i < CH_CNT * 4; i++)
            {
                CH_BASE_DATA[i] = "";
                for (int j = 0; j < COL_CNT; j++)
                {
                    CH_DATA[i, j] = "";
                }
            }

            // 그리드 표시값을 삭제
            btnSetBaseData_Click(null, null);
            btnSetFinalData_Click(null, null);

            setProcessMode(0);

            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_UNLOCK");
            String txt = sb.ToString();
            //if (checkBox1.Checked) 
            serial.SendLine(txt);

            // 문의 락이 풀리면 --> MCU에서 문이 열리는 것을 신호를 받은 후 메시지 출력?
            if (MessageBox.Show("문 상단을 눌러 열어 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
            {// 예 클릭

                setProcessMode(1);
                if (MessageBox.Show("환자 ID, 샘플 ID 및 카트리지 ID 입력 후 검사설정을 해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    // setProcessMode(2);
                    // 샘플 ID와 카트리지 ID 입력 가능 상태로 전환 --> 포커스를 옮김
                    setProcessMode(3);
                    tb_SampleID_Test.Focus();

                }
                else   // 취소
                {
                    //처음으로 돌아감
                    MessageBox.Show("작업 취소로 문을 닫습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    setProcessMode(0);
                }

            }
            else
            { // 취소 클릭
              //처음으로 돌아감
                MessageBox.Show("작업 취소로 문을 닫습니다", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                setProcessMode(0);
            }
            


            /*
             AbortRetryIgnore : 중단, 다시시도, 무시 버튼 표시
        OK : 확인 버튼 표시 
        OKCancel : 확인, 취소 버튼 표시 
        RetryCancel : 다시 시도, 취소 버튼 표시 
        YesNo : 예, 아니요 버튼 표시 
        YesNoCancel : 예, 아니요, 취소 버튼 표시             

        Asterisk : 정보 아이콘 
        Error : 빨간색 원 안 X 표시 
        Exclamation : 경고 아이콘 
        Hand : 손바닥 아이콘 
        Information : 정보 아이콘
        None : 아이콘을 표시하지 않음 
        Question : 물음표 아이콘 
        Stop : 빨간색 원 안 X 표시 
        Warning : 경고 아이콘
             */

        }
        private void btn_SetRecipe_Test_Click(object sender, EventArgs e)
        {
            updatePatientInfo();
            if (cb_Recipe_Test.SelectedIndex == -1)
            {
                MessageBox.Show("검사 항목을 선택 후 확인을 눌러주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else if (processStep != 4)
            {
                MessageBox.Show("환자 ID, 샘플 ID 및 카트리지 ID 입력이 끝나야 검사설정을 할 수 있습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else if (processStep == 4)
            {
                // MCU 설정을 위해 레시피 정보 공유
                cb_Recipe_Eng.SelectedIndex = cb_Recipe_Test.SelectedIndex;  // 인덱스를 동일하게
                btn_LoadRecipe_Eng_Click(null, null); // 레시피를 불러옴
                Apply_exec(null, null);  // MCU에 전체 전송

                if (DialogResult.OK == MessageBox.Show("카트리지 커버 및 캡을 제거해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information))
                {
                    setProcessMode(5);

                    aboutBox.ShowDialog();

                    if (aboutBox.retBtn == 1)
                    {
                        // 확인 --> 계속 진행
                        setProcessMode(6);

                        if (DialogResult.OK == MessageBox.Show("팁을 장착해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information))
                        {
                            setProcessMode(7);

                            if (DialogResult.OK == MessageBox.Show("준비가 끝났습니다. 문을 닫고 [검사 시작]을 눌러 검사를 진행해 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information))
                            {
                                setProcessMode(8);

                            }

                        }
                    }
                    else
                    {
                        // 취소 --> 작업 취소
                        MessageBox.Show("검사 취소함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        setProcessMode(0);
                    }

                }
            }
        }
        private void btn_Start_Test_Click(object sender, EventArgs e)
        {
            //update test infomation
            test_start_time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

            listView_ReportInfo.BeginUpdate();

            lvi1 = new ListViewItem("TB");
            lvi1.SubItems.Add(test_start_time);
            lvi1.SubItems.Add("");

            lvi1.SubItems[0].BackColor = Color.WhiteSmoke;

            lvi1.ImageIndex = 0;
            listView_ReportInfo.Items.Add(lvi1);

            listView_ReportInfo.EndUpdate();

            //updatePatientInfo();

            /*
            listView_ReportInfo.BeginUpdate();

            lvi1 = new ListViewItem("MTBC");
            listView_ReportInfo.Items[0].SubItems[2].Text = " dahunj";
            //lvi1.SubItems.Add(test_start_time);
            //lvi1.SubItems.Add("");

            //lvi1.SubItems[0].BackColor = Color.WhiteSmoke;

            //lvi1.ImageIndex = 0;
            listView_ReportInfo.Items.Add(lvi1);

            listView_ReportInfo.EndUpdate();
            */

            if (processStep == 8)  // ID input complete
            {
                MessageBox.Show("검사를 시작할 수 있나 체크합니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
                serial.SendLine(":check_start");  // 문이 열렸는지 체크 의뢰 
            }
            else if (processStep == 9)
            {
                if (DialogResult.OK == MessageBox.Show("검사가 진행 중입니다. 검사를 멈추겠습니까?", "검사 안내",
                                                       MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
            
                {
                    _reset_Process();
                    MessageBox.Show("검사를 강제 종료하였습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
                }
            }
            else
            {
                MessageBox.Show("검사 준비가 되지 않았습니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void _reset_Process()
        {
            // MCU 장비 초기화
            serial.SendLine(":stop");

            // 새로운 검사를 위한 모든 초기화를 여기에서 수행
            procData = null;
            waitData = "";

            // 데이터 받을 준비되었음
            recv_step = -1;

            
            // 버튼 초기화
            b_GUI_Init = true;

            logToFile.CloseFile();
            bSaveLog = false;

            setProcessMode(0);
        }
        private void _start_Process()
        {

            btn_Start_Test.Text = "Shutdown";//"강제 종료";
            setProcessMode(9);

            bSaveLog = true;
            logToFile.MakeNewFile();

            // 시작 명령 전송
            serial.SendLine(":start");

      
            // 진행바 초기화
            setProgressInfo(-1);

            MessageBox.Show("검사를 시작합니다.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }
        private void _check_Door()
        {
            MessageBox.Show("문이 열려 있습니다. 문을 닫고 [검사 시작]을 다시 눌러 주세요.", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public int id_selectedIndex { get; set; }

        enum ID_INDEX{
            PATIENT_ID = 0,
            SAMPLE_ID,
            CARTRIDGE_ID
        }

        private void tb_PatientID_Test_TextChanged(object sender, EventArgs e)
        {
            var triggerOffCmd = new byte[] { 0x1b, 0x41, 0x31, 0x0d };
            serialBarcode.SendBin(triggerOffCmd);

            if (processStep == 3 && tb_PatientID_Test.Text != "" && tb_CartridgeID_Test.Text != "" && tb_SampleID_Test.Text != "")
            {
                //MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                setProcessMode(4);
            }
        }

      

        private void tb_SampleID_Test_TextChanged(object sender, EventArgs e)
        {
            var triggerOffCmd = new byte[] { 0x1b, 0x41, 0x31, 0x0d };
            serialBarcode.SendBin(triggerOffCmd);


            if (processStep == 3 && tb_PatientID_Test.Text != "" && tb_CartridgeID_Test.Text != "" && tb_SampleID_Test.Text != "")
            {
                //MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                setProcessMode(4);
            }
        }

        private void tb_CartridgeID_Test_TextChanged(object sender, EventArgs e)
        {
            var triggerOffCmd = new byte[] { 0x1b, 0x41, 0x31, 0x0d };
            serialBarcode.SendBin(triggerOffCmd);

            if (processStep == 3 && tb_PatientID_Test.Text != "" && tb_CartridgeID_Test.Text != "" && tb_SampleID_Test.Text != "")
            {
                //MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                setProcessMode(4);
            }
        }

        private void tb_PatientID_Test_CursorChanged(object sender, EventArgs e)
        {
            var triggerCmd = new byte[] { 0x1b, 0x41, 0x32, 0x0d };
            serialBarcode.SendBin(triggerCmd);
            id_selectedIndex = (int)ID_INDEX.PATIENT_ID;

            
        }
        
        private void tb_SampleID_Test_CursorChanged(object sender, EventArgs e)
        {
            var triggerCmd = new byte[] { 0x1b, 0x41, 0x32, 0x0d };
            serialBarcode.SendBin(triggerCmd);
            id_selectedIndex = (int)ID_INDEX.SAMPLE_ID;
        }
        
        private void tb_CartridgeID_Test_CursorChanged(object sender, EventArgs e)
        {
            var triggerCmd = new byte[] { 0x1b, 0x41, 0x32, 0x0d };
            serialBarcode.SendBin(triggerCmd);
            id_selectedIndex = (int)ID_INDEX.CARTRIDGE_ID;
        }

        private void btn_About_Click(object sender, EventArgs e)
        {
            aboutBox.ShowDialog();
            if (aboutBox.retBtn == 1)
            {
                // 확인 --> 계속 진행
                MessageBox.Show("다음으로 진행함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 취소 --> 작업 취소
                MessageBox.Show("검사 취소함", "검사 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            dataGridView1_Engineer.CurrentCell = null;
            dataGridView2_Engineer.CurrentCell = null;
            dataGridView3_Engineer.CurrentCell = null;
            dataGridView4_Engineer.CurrentCell = null;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            
            string cTime = DateTime.Now.ToLongTimeString();
            string cDate = DateTime.Now.ToLongDateString();
            this.Text = "ABI POC PCR ver. 2.0.0     [" + cDate + " | " + cTime + "]";
            
            // 데이터 버퍼가 찼는지 확인하고 업데이트
            nTimerNo++;
            if (nTimerNo % 5 ==0 && processStep == 9 && recv_step > -1) // 5초 마다 업데이트
            {
                nTimerNo = 0;
                btnSetBaseData_Click(null, null);
                btnSetFinalData_Click(null, null);
            }

            if (processStep == 9) // 5초 마다 업데이트
            {                                                                         
                // 상태바 업데이트 - 시작을 20%에서 시작함
                progressBar_Tester.Value = (int)(30 + recv_step * 2);
                progressBar_Manager.Value = (int)(30 + recv_step * 2);
            }

            // 버퍼에 쓰레기가 있을 경우 체크해서 클리어
            if (waitData.Length != 0)
                if (waitData[0].Equals('-') || waitData[0].Equals('<') || waitData[0].Equals('>'))
                    waitData = "";

            if (waitData.Length > 100)   waitData = "";


            if (b_start_Process && processStep != 9) { 
                b_start_Process = false;
                _start_Process();
            }

            if (b_check_Door)
            {
                b_check_Door = false;
                _check_Door();
            }

            if (b_GUI_Init) {
                // 검사단계 초기화
                setProcessMode(0);
                setProgressInfo(-1);
                btn_Start_Test.Text = "Test Start";

                b_GUI_Init = false;
            }
            
        }

        /// <summary>
        // step = 0 ==> base 값 수신 완료
        // step = 1 ==> cycle15 값 수신
        // step = 2 ==> cycle20 값 수신
        // step = 3 ==> cycle25 값 수신
        // step = 4 ==> cycle30 값 수신
        // step = 5 ==> cycle35 값 수신
        // step = 6 ==> cycle40 값 수신
        // step = 7 ==> final 값 수신
        /// </summary>
        /// <param name="step"></param>
        private void setGridValue(int step)
        {
            //CH_DATA[0, 0] = "26.474245632";
            //CH_DATA[0, 1] = "26.652167154";
            //CH_DATA[0, 2] = "26.219731428";
            //CH_DATA[0, 3] = "27.110233116";
            //CH_DATA[0, 4] = "27.211859982";
            //CH_DATA[0, 5] = "26.525208078";
            //CH_DATA[0, 6] = "199.999999999";
            //CH_DATA[1, 0] = "26.881051122";
            //CH_DATA[1, 1] = "25.406120448";
            //CH_DATA[1, 2] = "24.236368398";
            //CH_DATA[1, 3] = "25.914552804";
            //CH_DATA[1, 4] = "25.507747314";
            //CH_DATA[1, 5] = "25.507747314";
            //CH_DATA[1, 6] = "299.999999999";
            //CH_DATA[2, 0] = "18.615896064";
            //CH_DATA[2, 1] = "19.582394382";
            //CH_DATA[2, 2] = "18.615896064";
            //CH_DATA[2, 3] = "18.71752293";
            //CH_DATA[2, 4] = "18.74285514";
            //CH_DATA[2, 5] = "18.234422784";
            //CH_DATA[2, 6] = "399.999999999";
            //CH_DATA[3, 0] = "13.885627392";
            //CH_DATA[3, 1] = "14.267100672";
            //CH_DATA[3, 2] = "14.317765092";
            //CH_DATA[3, 3] = "13.758668316";
            //CH_DATA[3, 4] = "13.809332736";
            //CH_DATA[3, 5] = "13.275270144";
            //CH_DATA[3, 6] = "499.999999999";
            //CH_DATA[4, 0] = "26.906681358";
            //CH_DATA[4, 1] = "26.626834944";
            //CH_DATA[4, 2] = "26.32165632";
            //CH_DATA[4, 3] = "26.906681358";
            //CH_DATA[4, 4] = "26.550540288";
            //CH_DATA[4, 5] = "26.372618766";
            //CH_DATA[4, 6] = "599.999999999";
            //CH_DATA[5, 0] = "26.474245632";
            //CH_DATA[5, 1] = "26.067142116";
            //CH_DATA[5, 2] = "25.635004416";
            //CH_DATA[5, 3] = "26.72846181";
            //CH_DATA[5, 4] = "25.88951862";
            //CH_DATA[5, 5] = "26.169067008";
            //CH_DATA[5, 6] = "699.999999999";
            //CH_DATA[6, 0] = "20.904735744";
            //CH_DATA[6, 1] = "21.260876814";
            //CH_DATA[6, 2] = "19.989199872";
            //CH_DATA[6, 3] = "20.879105508";
            //CH_DATA[6, 4] = "21.057325056";
            //CH_DATA[6, 5] = "20.803108878";
            //CH_DATA[6, 6] = "799.999999999";
            //CH_DATA[7, 0] = "14.394357774";
            //CH_DATA[7, 1] = "13.606079004";
            //CH_DATA[7, 2] = "12.18181275";
            //CH_DATA[7, 3] = "13.73303808";
            //CH_DATA[7, 4] = "13.7837025";
            //CH_DATA[7, 5] = "13.961922048";
            //CH_DATA[7, 6] = "899.999999999";
            //CH_DATA[8, 0] = "139.26307941";
            //CH_DATA[8, 1] = "138.14399178";
            //CH_DATA[8, 2] = "138.83094171";
            //CH_DATA[8, 3] = "137.940738048";
            //CH_DATA[8, 4] = "138.44946843";
            //CH_DATA[8, 5] = "137.533932558";
            //CH_DATA[8, 6] = "999.999999999";
            //CH_DATA[9, 0] = "28.22902272";
            //CH_DATA[9, 1] = "28.483536924";
            //CH_DATA[9, 2] = "28.457906688";
            //CH_DATA[9, 3] = "29.500699662";
            //CH_DATA[9, 4] = "28.25435493";
            //CH_DATA[9, 5] = "28.305317376";
            //CH_DATA[9, 6] = "1099.999999999";
            //CH_DATA[10, 0] = "30.288978432";
            //CH_DATA[10, 1] = "30.009132018";
            //CH_DATA[10, 2] = "29.958169572";
            //CH_DATA[10, 3] = "29.958169572";
            //CH_DATA[10, 4] = "29.805878286";
            //CH_DATA[10, 5] = "29.449737216";
            //CH_DATA[10, 6] = "1199.999999999";
            //CH_DATA[11, 0] = "13.961922048";
            //CH_DATA[11, 1] = "14.49598464";
            //CH_DATA[11, 2] = "14.343395328";
            //CH_DATA[11, 3] = "14.699536398";
            //CH_DATA[11, 4] = "14.49598464";
            //CH_DATA[11, 5] = "14.343395328";
            //CH_DATA[11, 6] = "1299.999999999";
            //CH_DATA[12, 0] = "31.30614117";
            //CH_DATA[12, 1] = "31.229846514";
            //CH_DATA[12, 2] = "31.916498418";
            //CH_DATA[12, 3] = "31.30614117";
            //CH_DATA[12, 4] = "31.713244686";
            //CH_DATA[12, 5] = "31.179182094";
            //CH_DATA[12, 6] = "1399.999999999";
            //CH_DATA[13, 0] = "25.965813276";
            //CH_DATA[13, 1] = "26.245361664";
            //CH_DATA[13, 2] = "26.34698853";
            //CH_DATA[13, 3] = "25.55870976";
            //CH_DATA[13, 4] = "25.838556174";
            //CH_DATA[13, 5] = "26.092772352";
            //CH_DATA[13, 6] = "1499.999999999";
            //CH_DATA[14, 0] = "19.251585522";
            //CH_DATA[14, 1] = "19.404174834";
            //CH_DATA[14, 2] = "18.895742478";
            //CH_DATA[14, 3] = "19.04833179";
            //CH_DATA[14, 4] = "20.59955712";
            //CH_DATA[14, 5] = "18.387012096";
            //CH_DATA[14, 6] = "1599.999999999";
            //CH_DATA[15, 0] = "13.910959602";
            //CH_DATA[15, 1] = "14.419689984";
            //CH_DATA[15, 2] = "14.368727538";
            //CH_DATA[15, 3] = "14.87745792";
            //CH_DATA[15, 4] = "13.529486322";
            //CH_DATA[15, 5] = "15.131972124";
            //CH_DATA[15, 6] = "1699.999999999";

            //CH_BASE_DATA[0] = "126.34698853";
            //CH_BASE_DATA[1] = "225.55870976";
            //CH_BASE_DATA[2] = "326.34698853";
            //CH_BASE_DATA[3] = "3326.34698853";
            //CH_BASE_DATA[4] = "425.55870976";
            //CH_BASE_DATA[5] = "526.34698853";
            //CH_BASE_DATA[6] = "625.55870976";
            //CH_BASE_DATA[7] = "726.34698853";
            //CH_BASE_DATA[8] = "825.55870976";
            //CH_BASE_DATA[9] = "926.34698853";
            //CH_BASE_DATA[10] = "1025.55870976";
            //CH_BASE_DATA[11] = "1126.34698853";
            //CH_BASE_DATA[12] = "125.55870976";
            //CH_BASE_DATA[13] = "1326.34698853";
            //CH_BASE_DATA[14] = "1425.55870976";
            //CH_BASE_DATA[15] = "1526.34698853";

            if (step > 0)  // base 값이 아닌 경우
            {
                DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
                DataGridView[] dgvArr2 = { dataGridView1_Tester, dataGridView2_Tester, dataGridView3_Tester, dataGridView4_Tester };
                DataGridView[] dgvArrCt = { dataGridView5, dataGridView6, dataGridView7, dataGridView8 };

                Plotter.isValueBase = false;

                for (int ch_idx = 0; ch_idx < CH_CNT; ch_idx++) //CH_CNT = 4
                {
                    int idx = ch_idx * 4;
                    //setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, 0, CH_BASE_DATA[idx], CH_BASE_DATA[idx + 1], CH_BASE_DATA[idx + 2], CH_BASE_DATA[idx + 3]);

                    for (int col_idx = 0; col_idx < 27; col_idx++) //col은 데이터 순서 for (int col_idx = 0; col_idx < 27; col_idx++)
                    {
                        // setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, col_idx + 1, CH_DATA[idx, col_idx], CH_DATA[idx+1, col_idx], CH_DATA[idx+2, col_idx], CH_DATA[idx+3, col_idx]);
                        // 챔버인덱스(1~4), 컬럼인덱스(1~8), 데이터4개
                        // 각 사이클 데이터 입력시
                        //setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, col_idx + 1, CH_DATA[idx, col_idx], CH_DATA[idx + 1, col_idx], CH_DATA[idx + 2, col_idx], CH_DATA[idx + 3, col_idx]);
                        setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, col_idx + 1, CH_DATA[idx, col_idx], CH_DATA[idx + 1, col_idx], CH_DATA[idx + 2, col_idx], CH_DATA[idx + 3, col_idx]);
                    }
                }

                if (step == 7 && recv_step == 27) // final의 경우 검사자 그리드 업데이트
                {
                    test_end_time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

                    listView_ReportInfo.BeginUpdate();

                    lvi1 = new ListViewItem("TB");
                    listView_ReportInfo.Items[0].SubItems[2].Text = test_end_time;
                    listView_ReportInfo.Items.Add(lvi1);

                    listView_ReportInfo.EndUpdate();

                    updateAnalyticResult();

                    //UpdateICResult();
                    //CriticalThresholdCalculation();
                    lb_IC_Result.Text = (sharedmem.criticalThreshold).ToString();

                    for (int ch_idx = 0; ch_idx < CH_CNT; ch_idx++)
                    {
                        int idx = ch_idx * 4;
                        // 챔버인덱스(1~4), 데이터4개
                        // 각 사이클 데이터 입력시
                        setDataGrid_Tester(dgvArr2[ch_idx], ch_idx + 1, CH_DATA[idx, 6], CH_DATA[idx + 1, 6], CH_DATA[idx + 2, 6], CH_DATA[idx + 3, 6]);
                    }
                }
            }
            else {  // base 값인 경우

                DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };
                DataGridView[] dgvArrCt = { dataGridView5, dataGridView6, dataGridView7, dataGridView8 };


                Plotter.isValueBase = true;

                for (int ch_idx = 0; ch_idx < CH_CNT; ch_idx++)
                {
                    int idx = ch_idx * 4;
                    // 챔버인덱스(1~4), 컬럼인덱스(1~8), 데이터4개
                    // 각 사이클 데이터 입력시
                    //setDataGrid_Engineer(dgvArr[ch_idx], ch_idx + 1, 0, CH_BASE_DATA[idx], CH_BASE_DATA[idx + 1], CH_BASE_DATA[idx + 2], CH_BASE_DATA[idx + 3]);
                    setDataGrid_Ct(dgvArrCt[ch_idx], ch_idx + 1, 0, CH_BASE_DATA[idx], CH_BASE_DATA[idx + 1], CH_BASE_DATA[idx + 2], CH_BASE_DATA[idx + 3]);
                }
                //CH_BASE_DATA
            }
        }

        /// <summary>
        // step = 0 ==> base 값 수신 완료
        // step = 1 ==> cycle15 값 수신
        // step = 2 ==> cycle20 값 수신
        // step = 3 ==> cycle25 값 수신
        // step = 4 ==> cycle30 값 수신
        // step = 5 ==> cycle35 값 수신
        // step = 6 ==> cycle40 값 수신
        // step = 7 ==> final 값 수신
        /// </summary>
        /// <param name="step"></param>
        private void setProgressInfo(int step)
        {
            if (step == -1)
            {
                progressBar_Tester.Value = 0;  // 상태바만 초기화
                progressBar_Manager.Value = 0;  // 상태바만 초기화

                return;
            }
           
            setGridValue(step);
            
            // 각 단계별 처리할 것이 있으면 여기서 처리
            switch (step)
            {
                case 0:  // base 값 업데이트
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;                                                            
                case 6:
                    break;
                case 7:
                    break;
            }
        }
        private void progressBar_Tester_Click(object sender, EventArgs e)
        {

        }
        private void btn_Regist_IDManage_Click(object sender, EventArgs e)
        {
            // 등록할 내용이 없으면 리턴
            if (tb_Name_IDManage.Text == "" || tb_ID_IDManage.Text == "" || tb_PW_IDManage.Text == "")
            {
                MessageBox.Show("성명, ID, Password 모두 입력하세요.", "계정 등록 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 신규 계정 등록
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) {
                di.Create();
            }
            string fileName = di.ToString() + "\\member.info";

            StreamWriter sw = new StreamWriter(fileName, true);

            string buff = tb_Name_IDManage.Text + "," + tb_ID_IDManage.Text + "," + tb_PW_IDManage.Text + "," + tb_Right_IDManage.SelectedItem;

            sw.WriteLine(buff);

            sw.Close();

            MessageBox.Show("계정이 등록되었습니다.", "계정 등록 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btn_Search_IDManage_Click(this, null);

            tb_Name_IDManage.Text = "";
            tb_ID_IDManage.Text = "";
            tb_PW_IDManage.Text = "";
        }
        private void btn_Remove_IDManage_Click(object sender, EventArgs e)
        {
            if (tb_delName_IDManage.Text == "ABI엔지니어") // 관리자 계정 삭제 불가
            {
                MessageBox.Show("엔지니어 계정은 삭제할 수 없습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else if (tb_delPW_IDManage.Text == "")
            {
                MessageBox.Show("삭제할 계정을 선택 후 암호를 입력해야 삭제할 수 있습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (tb_delPW_IDManage.Text != tb_delPW2_IDManage.Text) {
                MessageBox.Show("암호가 일치해야 삭제할 수 있습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");

                string fileName = di.ToString() + "\\member.info";
                Save_Csv(fileName, dataGridView_Manage, true);

                tb_delName_IDManage.Text = "";
                tb_delID_IDManage.Text = "";
                tb_delPW_IDManage.Text = "";
                tb_delPW2_IDManage.Text = "";
            }
        }
        private void btn_Search_IDManage_Click(object sender, EventArgs e)
        {
            // 계정 조회
            dataGridView_Manage.Rows.Clear();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\member.info";

            string[] lines = File.ReadAllLines(fileName);

            int readNum = 1;
            string temp = "";
            for (int i = 1; i < lines.Length; i++) //데이터가 존재하는 라인일 때에만, label에 출력한다.
            {
                temp = lines[i];

                char[] sep = { ',' };

                string[] result = temp.Split(sep);
                string[] data6 = new string[4] { temp, temp, temp, temp };
                int index = 0;
                foreach (var item in result)
                {
                    data6[index++] = item;
                }

                dataGridView_Manage.Rows.Add(data6);
            }

            for (int i = 0; i < dataGridView_Manage.Rows.Count; i++)
            {
                if (i % 2 != 0)
                {
                    dataGridView_Manage.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dataGridView_Manage.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                }
            }
        }
        private void dataGridView_Manage_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 선택한 셀의 정보를 채움
            if (e.RowIndex == 0 && dataGridView_Manage.RowCount == 1 || dataGridView_Manage.Rows.Count == (e.RowIndex + 1))
                return;
            tb_delName_IDManage.Text = dataGridView_Manage.Rows[e.RowIndex].Cells[0].Value.ToString();
            tb_delID_IDManage.Text = dataGridView_Manage.Rows[e.RowIndex].Cells[1].Value.ToString();
            tb_delPW2_IDManage.Text = dataGridView_Manage.Rows[e.RowIndex].Cells[2].Value.ToString();

            selectedRowCount = e.RowIndex;

        }

        /// <summary>
        /// 계정을 1개 삭제 후 dataGridView 데이터를 파일 내보내기
        /// </summary>
        private void Save_Csv(string fileName, DataGridView dgv, bool header)
        {
            // 현재 선택한 셀의 정보를 삭제 후
            //dataGridView_Manage.Rows.RemoveAt(this.dataGridView_Manage.SelectedRows[0].Index);
            int sIndex = dataGridView_Manage.CurrentCell.RowIndex;

            dataGridView_Manage.Rows.RemoveAt(sIndex);

            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgv.Rows.Count == 0) return;

            // 헤더정보 출력
            if (header)
            {
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    csvExport.Write(dgv.Columns[i].HeaderText);
                    if (i != dgv.Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }

            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        csvExport.Write(row.Cells[i].Value);
                        if (i != dgv.Columns.Count - 1)
                        {
                            csvExport.Write(delimiter);
                        }
                    }
                    csvExport.Write(csvExport.NewLine); // write new line
                }
            }

            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
            MessageBox.Show("계정을 삭제하였습니다.", "계정 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Save_GridCsv(string fileName)
        {
            DataGridView[] dgvArr = { dataGridView1_Engineer, dataGridView2_Engineer, dataGridView3_Engineer, dataGridView4_Engineer };

            // 그리드뷰를 파일로 저장함
            string delimiter = ",";  // 구분자
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter csvExport = new StreamWriter(fs, System.Text.Encoding.UTF8);

            if (dgvArr[0].Rows.Count == 0) return;

            // 헤더정보 출력
            if (true)
            {
                for (int i = 0; i < dgvArr[0].Columns.Count; i++)
                {
                    csvExport.Write(dgvArr[0].Columns[i].HeaderText);
                    if (i != dgvArr[0].Columns.Count - 1)
                    {
                        csvExport.Write(delimiter);
                    }
                }
            }

            csvExport.Write(csvExport.NewLine); // add new line

            // 데이터 출력
            for (int n = 0; n < 4; n++)
            {
                foreach (DataGridViewRow row in dgvArr[n].Rows)
                {
                    if (!row.IsNewRow)
                    {
                        for (int i = 0; i < dgvArr[n].Columns.Count; i++)
                        {
                            csvExport.Write(row.Cells[i].Value);
                            if (i != dgvArr[n].Columns.Count - 1)
                            {
                                csvExport.Write(delimiter);
                            }
                        }
                        csvExport.Write(csvExport.NewLine); // write new line
                    }
                }
            }
            csvExport.Flush(); // flush from the buffers.
            csvExport.Close();
            fs.Close();
        }

        private void btn_NewRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 새로운 레시피를 만들고 싶으면 레시피 콤보박스에 이름을 넣은 후 클릭
            // 레시피 이름 입력 체크
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            string str = cb_Recipe_Eng.Text;

            if (index == -1 && str != "")
            {
                MessageBox.Show("새로운 레시피 파일을 생성했습니다. 설정값을 입력 후 저장해야 합니다.", "레시피 생성 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 새로운 레시피 파일 저장
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
                if (!di.Exists)
                {
                    di.Create();
                }
                string fileName = di.ToString() + "\\" + str + ".rcp";

                // 기존 파일 삭제
                FileInfo fileDel = new FileInfo(fileName);
                if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

                // 새로 저장
                StreamWriter sw = new StreamWriter(fileName, true);

                string buff = tb_PreTemp_Eng.Text + "," + tb_PreHoldSec_Eng.Text + "," + tb_1Temp_Eng.Text + "," + tb_1HoldSec_Eng.Text
                     + "," + tb_2Temp_Eng.Text + "," + tb_2HoldSec_Eng.Text + "," + tb_OCDelaySec_Eng.Text + "," + tb_OCHoldSec_Eng.Text
                     + "," + tb_FianlCycle_Eng.Text;

                sw.WriteLine(buff);

                sw.Close();
                // 콤보 박스 업데이트
                SetRecipeCombobox_Test();
                cb_Recipe_Eng.SelectedIndex = cb_Recipe_Eng.Items.Count - 1;

                bLoadedRecipe = false;
            }
            else if (index == -1 || str == "")
            {
                MessageBox.Show("레시피 이름을 입력한 후 생성해 주세요.", "레시피 생성 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("기존에 존재하는 레시피 이름인지 확인해 주세요.", "레시피 생성 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btn_DelRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 선택한 레시피를 불러옴
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            if (index == -1)
            {
                MessageBox.Show("레시피를 선택해 주세요.", "레시피 삭제 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string str = cb_Recipe_Eng.Text;
            // 현재 레시피 파일을 찾은 후 삭제
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists)
            {
                di.Create();
            }
            string fileName = di.ToString() + "\\" + str + ".rcp";

            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) // 삭제할 파일이 있는지
            {
                if (DialogResult.OK == MessageBox.Show("레시피를 삭제하겠습니까?", "레시피 삭제 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)) {
                    fileDel.Delete(); // 없어도 에러안남

                    // 콤보 박스 업데이트
                    SetRecipeCombobox_Test();
                    cb_Recipe_Eng.SelectedIndex = 0;

                    // 보여주는 값을 초기화
                    tb_PreTemp_Eng.Text = "-";
                    tb_PreHoldSec_Eng.Text = "-";
                    tb_1Temp_Eng.Text = "-";
                    tb_1HoldSec_Eng.Text = "-";
                    tb_2Temp_Eng.Text = "-";
                    tb_2HoldSec_Eng.Text = "-";
                    tb_OCDelaySec_Eng.Text = "-";
                    tb_OCHoldSec_Eng.Text = "-";
                    tb_FianlCycle_Eng.Text = "-";

                    MessageBox.Show("레시피를 삭제하였습니다.", "레시피 삭제 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                    bLoadedRecipe = false;
                }
            }
        }

        private void btn_SaveRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 선택한 레시피를 불러옴
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            if (index == -1)
            {
                MessageBox.Show("레시피를 선택해 주세요.", "레시피 저장 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string str = cb_Recipe_Eng.Text;

            // 새로운 레시피 파일 저장
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\" + str + ".rcp";

            // 기존 파일 삭제
            FileInfo fileDel = new FileInfo(fileName);
            if (fileDel.Exists) fileDel.Delete(); // 없어도 에러안남

            // 새로 저장
            StreamWriter sw = new StreamWriter(fileName, true);

            string buff = tb_PreTemp_Eng.Text + "," + tb_PreHoldSec_Eng.Text + "," + tb_1Temp_Eng.Text + "," + tb_1HoldSec_Eng.Text
                    + "," + tb_2Temp_Eng.Text + "," + tb_2HoldSec_Eng.Text + "," + tb_OCDelaySec_Eng.Text + "," + tb_OCHoldSec_Eng.Text
                    + "," + tb_FianlCycle_Eng.Text;

            sw.WriteLine(buff);
            sw.Close();
            // 콤보 박스 업데이트
            SetRecipeCombobox_Test();

            MessageBox.Show("레시피를 저장하였습니다.", "레시피 저장 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        }

        private void btn_LoadRecipe_Eng_Click(object sender, EventArgs e)
        {
            // 선택한 레시피를 불러옴
            int index = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음
            if (index == -1)
            {
                MessageBox.Show("레시피를 선택해 주세요.", "레시피 읽기 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            cb_Recipe_Test.SelectedIndex = cb_Recipe_Eng.SelectedIndex;    // -1 이면 입력한 것이 없음

            bLoadedRecipe = true;

            string str = cb_Recipe_Eng.Text;

            // 새로운 레시피 파일 저장
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(Application.StartupPath + @"\Data");
            if (!di.Exists) di.Create();

            string fileName = di.ToString() + "\\" + str + ".rcp";

            string[] lines = File.ReadAllLines(fileName);

            string temp = "";
            temp = lines[0]; // 1줄 밖에 없음

            char[] sep = { ',' };

            string[] result = temp.Split(sep);

            tb_PreTemp_Eng.Text = result[0];
            tb_PreHoldSec_Eng.Text = result[1];
            tb_1Temp_Eng.Text = result[2];
            tb_1HoldSec_Eng.Text = result[3];
            tb_2Temp_Eng.Text = result[4];
            tb_2HoldSec_Eng.Text = result[5];
            tb_OCDelaySec_Eng.Text = result[6];
            tb_OCHoldSec_Eng.Text = result[7];
            tb_FianlCycle_Eng.Text = result[8];
        }

        private void btn_MCURead_Eng_Click(object sender, EventArgs e)
        {
            // MCU와 통신하여 설정된 값을 가져와서 보여줌
            /*
            string[] result = { "1", "2", "3", "4", "5", "6", "7", "8", "9"};

            tb_PreTempMCU_Eng.Text = result[0];
            tb_PreHoldSecMCU_Eng.Text = result[1];
            tb_1TempMCU_Eng.Text = result[2];
            tb_1HoldSecMCU_Eng.Text = result[3];
            tb_2TempMCU_Eng.Text = result[4];
            tb_2HoldSecMCU_Eng.Text = result[5];
            tb_OCDelaySecMCU_Eng.Text = result[6];
            tb_OCHoldSecMCU_Eng.Text = result[7];
            tb_FianlCycleMCU_Eng.Text = result[8];
            */
        }

        private void btn_ApplyAll_Eng_Click(object sender, EventArgs e)
        {

        }

        private void tb_PW_Main_KeyUp(object sender, KeyEventArgs e)
        {
            // 엔터키 누를 때 로그인 버튼 클릭
            if (e.KeyCode == Keys.Enter && tb_PW_Main.Text != "") {
                btn_Login_Main_Click(this, null);
            }
        }


        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void tabPage6_Click(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        void Read_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":SHOW_GLOB_VARS");

            String txt = sb.ToString();
            //Apply_txt = txt;
            serial.SendLine(txt);
        }

        void Cycle_exec(object sender, EventArgs e)
        {
            if (tb_FianlCycle_Eng != null && tb_FianlCycle_Eng.Text != "" && tb_FianlCycle_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":FINAL_CYCLE");
                sb.Append(" ");
                sb.Append(tb_FianlCycle_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Operation_exec(object sender, EventArgs e)
        {
            if (tb_OCHoldSec_Eng.Text != "" && tb_OCHoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":OPTIC_OPERATION_KEEPING_TEMP_SEC");
                sb.Append(" ");
                sb.Append(tb_OCHoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void NoOperation_exec(object sender, EventArgs e)
        {
            if (tb_2HoldSec_Eng.Text != "" && tb_2HoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":2nd_STEP_KEEPING_TIME_SEC");
                sb.Append(" ");
                sb.Append(tb_2HoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Before_exec(object sender, EventArgs e)
        {
            if (tb_OCDelaySec_Eng.Text != "" && tb_OCDelaySec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":DELAY_TIME_BEFORE_OPTING_RUNING");
                sb.Append(" ");
                sb.Append(tb_OCDelaySec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Peltier_exec(object sender, EventArgs e)
        {
            if (tb_PreHoldSec_Eng.Text != "" && tb_PreHoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":PRECOND_KEEPING_TIME_MIN");
                sb.Append(" ");
                sb.Append(tb_PreHoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void High_exec(object sender, EventArgs e)
        {
            if (tb_1HoldSec_Eng.Text != "" && tb_1HoldSec_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":1st_STEP_KEEPING_TIME_SEC");
                sb.Append(" ");
                sb.Append(tb_1HoldSec_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Heat_exec(object sender, EventArgs e)
        {
            if (tb_1Temp_Eng.Text != "" && tb_1Temp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":1st_STEP_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_1Temp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Cool_exec(object sender, EventArgs e)
        {
            if (tb_2Temp_Eng.Text != "" && tb_2Temp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":2nd_STEP_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_2Temp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Cond_exec(object sender, EventArgs e)
        {
            if (tb_PreTemp_Eng.Text != "" && tb_PreTemp_Eng.Text != "-")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(":PRE_COND_SETPOINT");
                sb.Append(" ");
                sb.Append(tb_PreTemp_Eng.Text);

                String txt = sb.ToString();
                //Apply_txt = txt;
                serial.SendLine(txt);
            }
        }

        void Apply_exec(object sender, EventArgs e)
        {
            if (cb_Recipe_Eng.SelectedIndex == -1 || bLoadedRecipe == false)
            {
                MessageBox.Show("레시피를 선택 한 후 [Load]를 해야 [전체 전송]이 가능합니다.", "설정 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (tb_OCHoldSec_Eng.Text == "" || tb_OCHoldSec_Eng.Text == "-"
            || tb_FianlCycle_Eng.Text == "" || tb_FianlCycle_Eng.Text == "-"
            || tb_2HoldSec_Eng.Text == "" || tb_2HoldSec_Eng.Text == "-"
            || tb_OCDelaySec_Eng.Text == "" || tb_OCDelaySec_Eng.Text == "-"
            || tb_PreHoldSec_Eng.Text == "" || tb_PreHoldSec_Eng.Text == "-"
            || tb_1HoldSec_Eng.Text == "" || tb_1HoldSec_Eng.Text == "-"
            || tb_1Temp_Eng.Text == "" || tb_1Temp_Eng.Text == "-"
            || tb_2Temp_Eng.Text == "" || tb_2Temp_Eng.Text == "-"
            || tb_PreTemp_Eng.Text == "" || tb_PreTemp_Eng.Text == "-")
            {
                MessageBox.Show("설정한 값 중에 전송이 불가능한 값이 있습니다. 모두 숫자여야 합니다.", "설정 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            cmd_dic[":FINAL_CYCLE"] = tb_FianlCycle_Eng.Text;
            cmd_dic[":OPTIC_OPERATION_KEEPING_TEMP_SEC"] = tb_OCHoldSec_Eng.Text;
            cmd_dic[":2nd_STEP_KEEPING_TIME_SEC"] = tb_2HoldSec_Eng.Text;
            cmd_dic[":DELAY_TIME_BEFORE_OPTING_RUNING"] = tb_OCDelaySec_Eng.Text;
            cmd_dic[":PRECOND_KEEPING_TIME_MIN"] = tb_PreHoldSec_Eng.Text;
            cmd_dic[":1st_STEP_KEEPING_TIME_SEC"] = tb_1HoldSec_Eng.Text;
            cmd_dic[":1st_STEP_SETPOINT"] = tb_1Temp_Eng.Text;
            cmd_dic[":2nd_STEP_SETPOINT"] = tb_2Temp_Eng.Text;
            cmd_dic[":PRE_COND_SETPOINT"] = tb_PreTemp_Eng.Text;

            foreach (String cmd in cmd_dic.Keys)
            {
                if (cmd_dic[cmd] != null && cmd_dic[cmd] != "")
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(cmd);
                    sb.Append(" ");
                    sb.Append(cmd_dic[cmd]);

                    String txt = sb.ToString();
                    //Apply_txt = txt;

                    // 연속 전송시 딜레이를 둠
                    Thread.Sleep(200);
                    serial.SendLine(txt);
                }
            }

            MessageBox.Show("레시피 설정 값을 MCU로 전송했습니다.", "설정 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Read_exec(sender, e);
        }

        void SetSMStroke_exec(object sender, EventArgs e)
        {
            String SelectedItemSM = cb_Motor.SelectedItem.ToString();
            String SelectedItemDirSM = cb_Motor_Dir.SelectedItem.ToString();
            int PulseCountSM = Convert.ToInt32(tb_Pulse_Cnt.Text);
            int PulsePeriodSM = Convert.ToInt32(tb_Pulse_Period.Text);

            Console.WriteLine("SM[{0}] dir:{1}, Count:{2} Peroid:{3}", SelectedItemSM, SelectedItemDirSM, PulseCountSM, PulsePeriodSM);

            if ((PulseCountSM > 9999) || (PulseCountSM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            if ((PulsePeriodSM > 9999) || (PulsePeriodSM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            StringBuilder sb = new StringBuilder();

            sb.Append(":SM");
            sb.Append(SelectedItemSM);

            int dir = 0;
            if (SelectedItemDirSM == "UP (BACKWARD)")
            {
                dir = 1;
            }
            else if(SelectedItemDirSM == "DOWN (FORWARD)")
            {
                dir = 0;
            }
            sb.Append(dir.ToString());
            sb.Append(string.Format("{0:0000}{1:0000}", PulseCountSM, PulsePeriodSM));

            string txt = sb.ToString();
            Console.WriteLine(txt);
            serial.SendLine(txt);
        }

        void SetOMStroke_exec(object sender, EventArgs e)
        {
            String SelectedItemDirOM = cb_OMotor.Text;
            int PulseCountOM = Convert.ToInt32(tb_OPulse_Cnt.Text);
            int PulsePeriodOM = Convert.ToInt32(tb_OPulse_Period.Text);

            Console.WriteLine("OM dir:{0}, Count:{1} Peroid:{2}", SelectedItemDirOM, PulseCountOM, PulsePeriodOM);

            if ((PulseCountOM > 9999) || (PulseCountOM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            if ((PulsePeriodOM > 9999) || (PulsePeriodOM < 0))
            {
                MessageBox.Show("범위 초과(0~9999)");
                return;
            }

            StringBuilder sb = new StringBuilder();

            sb.Append(":OM1");
            int dir = 1;
            if (SelectedItemDirOM == "CLOCK")
            {
                dir = 0;
            }
            sb.Append(dir.ToString());
            sb.Append(string.Format("{0:0000}{1:0000}", PulseCountOM, PulsePeriodOM));

            string txt = sb.ToString();
            Console.WriteLine(txt);
            serial.SendLine(txt);
        }

        void DoorLock_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_UNLOCK");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void DoorUnLock_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_LOCK");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void Fan1On_exec(object sender, EventArgs e)
        {
            serial.SendLine(":forceFan");
        }

        void Fan1Off_exec(object sender, EventArgs e)
        {
            serial.SendLine(":FanOff");
        }

        void Fan2On_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":FAN2_ON");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void Fan2Ooff_exec(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":FAN2_OFF");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        void Optic1_exec(object sender, EventArgs e)
        {
            serial.SendLine(":optic1");
        }

        void Optic2_exec(object sender, EventArgs e)
        {
            serial.SendLine(":optic2");
        }

        void ReadTemp_exec(object sender, EventArgs e)
        {
            serial.SendLine(":readTemp");
        }

        void Feltier_exec(object sender, EventArgs e)
        {
            serial.SendLine(":FELTIER_TEST");
        }

        delegate void Ctr_Involk(Control ctr, String text);
        public void setText_Control(Control ctr, string txtValue)
        {
            if (ctr.InvokeRequired)
            {
                Ctr_Involk Cl = new Ctr_Involk(setText_Control);
                ctr.Invoke(Cl, ctr, txtValue);
            }
            else
            {
                ctr.Text = txtValue;
            }
        }

        void GetSerialStringBarcode(object sender)
        {
            byte[] raw;
            strData = serialBarcode.ReceiveString(out raw);

            if (strData.Contains("\r\n"))
            {
                if(id_selectedIndex == (int)ID_INDEX.PATIENT_ID)
                {
                    setText_Control(tb_PatientID_Test, strData);//txtBarcodeRead
                }
                else if(id_selectedIndex == (int)ID_INDEX.SAMPLE_ID)
                {
                    setText_Control(tb_SampleID_Test, strData);//txtBarcodeRead
                }
                else if(id_selectedIndex == (int)ID_INDEX.CARTRIDGE_ID)
                {
                    setText_Control(tb_CartridgeID_Test, strData);//txtBarcodeRead
                }

            }
        }

        void GetSerialString(object sender)
        {
            byte[] raw;
            strData = serial.ReceiveString(out raw);
            SetDataBox(strData);
            strData = strData.Replace(" ", "");

            if (strData.Contains("<"))
            {
                globRecv = true;
            }

            waitData += strData;

            if (strData.Contains("-"))
            {
                globRecv = false;
            }

            if (!globRecv)
            {
                if (waitData.IndexOf('\n') > -1)
                {
                    procData = waitData.Split('\n');
                    foreach (string tmp in procData)
                    {
                        SetCommRXProc(tmp);
                        waitData = "";
                    }
                }
            }

            if (bSaveLog)
            {
                if ((!string.IsNullOrEmpty(strData)) && (strData.Length > 0))
                {
                    logToFile.Append(strData);
                }
            }
        }

        void GetSerialString4(object sender)
        {
            byte[] raw;
            strData = serial.ReceiveString(out raw);
            SetDataBox(strData);

            strData = strData.Replace(" ", "");
            procData = strData.Split('\n');

            foreach (string tmp in procData)
            {
                string[] arr = tmp.Split('=');
                if (arr.Length > 1)
                {
                    if (arr[1] != null && arr[1] != "")
                    {
                        SetCommRXProc(tmp);
                        waitData = "";
                    }
                    else
                    {
                        waitData += tmp;
                    }
                }
                else
                {
                    waitData += tmp;
                }
            }

            SetCommRXProc(waitData);
        }

        void SetDataBox(string str)
        {
            if (this.InvokeRequired)
            {
                SetDataBoxCallback sdb = new SetDataBoxCallback(SetDataBox);
                this.Invoke(sdb, new object[] { str });
            }
            else
            {
                if (!firstRecv)
                {
                    tb_Data.Text = "";
                    tb_Log.Text = "";

                    firstRecv = true;
                }

                tb_Data.AppendText(str + "\r");
                tb_Data.Select(tb_Data.Text.Length, 0);
                tb_Data.ScrollToCaret();
                tb_Log.AppendText(str + "\r");
                tb_Log.Select(tb_Log.Text.Length, 0);
                tb_Log.ScrollToCaret();
            }
        }

        public void showLog(string str)
        {
            if (this.InvokeRequired)
            {
                SetDataBoxCallback sdb = new SetDataBoxCallback(SetDataBox);
                this.Invoke(sdb, new object[] { str });
            }
            else
            {
                tb_Data.Text += Environment.NewLine;
                tb_Data.AppendText(str);
//                tb_Data.Text += Environment.NewLine;
                tb_Data.Select(tb_Data.Text.Length, 0);
                tb_Data.ScrollToCaret();
                tb_Log.Text += Environment.NewLine;
                tb_Log.AppendText(str);
  //              tb_Log.Text += Environment.NewLine;
                tb_Log.Select(tb_Log.Text.Length, 0);
                tb_Log.ScrollToCaret();
            }
        }

        public void SetCommRXProc(string str)
        {
            str = str.Replace(" ", "");
            //str = str.Replace("-", "");
            //str = str.Replace(">", "");
            //str = str.Replace("<", "");

            string[] commVar = str.Split('=');
            if (commVar.Length > 1)             
            {
                if (commVar[0].Equals("g_FINAL_CYCLE"))
                {
                    //tb_FianlCycleMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_FianlCycleMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_OPTIC_OPERATION_KEEPING_TEMP_SEC"))
                {
                    //tb_OCHoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OCHoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_2nd_STEP_KEEPING_TIME_SEC"))
                {
                    //tb_2HoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_2HoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_DELAY_TIME_BEFORE_OPTING_RUNING"))
                {
                    //tb_OCDelaySecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_OCDelaySecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_PRECOND_KEEPING_TIME_MIN"))
                {
                    //tb_PreHoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_PreHoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_1st_STEP_KEEPING_TIME_SEC"))
                {
                    //tb_1HoldSecMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_1HoldSecMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_1st_STEP_SETPOINT"))
                {
                    //tb_1TempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_1TempMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_2nd_STEP_SETPOINT"))
                {
                    //tb_2TempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_2TempMCU_Eng, commVar[1]);
                }
                else if (commVar[0].Equals("g_PRE_COND_SETPOINT"))
                {
                    //tb_PreTempMCU_Eng.Text = commVar[1];
                    SetTextBox(tb_PreTempMCU_Eng, commVar[1]);
                }
            }

            if (str.Equals("g_check_door\n") || str.Equals("g_check_door"))
            {
                //_check_Door();
                b_check_Door = true;
            }
            else if (str.Equals("g_ok_door\n") || str.Equals("g_ok_door"))
            {
                //_start_Process();
                b_start_Process = true;
            }

        }
        void SetTextBox(TextBox tb, string str)
        {
            if (this.InvokeRequired)
            {
                SetTextBoxCallback stb = new SetTextBoxCallback(SetTextBox);
                this.Invoke(stb, new object[] { tb, str });
            }
            else
            {
                tb.Text = str;
            }
        }

        public void SetCommRXProc2(string str)
        {
            str = str.Replace(" ", "");

            if (str.Equals("check_door\n"))
            {
                _check_Door();
            }
            else if (str.Equals("ok_door\n"))
            {
                _start_Process();
            }
        }


        public void SetCommRXProc3(string str)
        {
            str = str.Replace(" ", "");

            string[] commVar = str.Split('=');
            if (commVar[0].Equals("FINAL_CYCLE"))
            {
                tb_FianlCycleMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_FianlCycleMCU_Eng.Text.Split('\n');
                tb_FianlCycleMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("OPTIC_OPERATION_KEEPING_TEMP_SEC"))
            {
                tb_OCHoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_OCHoldSecMCU_Eng.Text.Split('\n');
                tb_OCHoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("2nd_STEP_KEEPING_TIME_SEC"))
            {
                tb_2HoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_2HoldSecMCU_Eng.Text.Split('\n');
                tb_2HoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("DELAY_TIME_BEFORE_OPTING_RUNING"))
            {
                tb_OCDelaySecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_OCDelaySecMCU_Eng.Text.Split('\n');
                tb_OCDelaySecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("PRECOND_KEEPING_TIME_MIN"))
            {
                tb_PreHoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_PreHoldSecMCU_Eng.Text.Split('\n');
                tb_PreHoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("1st_STEP_KEEPING_TIME_SEC"))
            {
                tb_1HoldSecMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_1HoldSecMCU_Eng.Text.Split('\n');
                tb_1HoldSecMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("1st_STEP_SETPOINT"))
            {
                tb_1TempMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_1TempMCU_Eng.Text.Split('\n');
                tb_1TempMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("2nd_STEP_SETPOINT"))
            {
                tb_2TempMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_2TempMCU_Eng.Text.Split('\n');
                tb_2TempMCU_Eng.Text = commVar2[0];
            }
            else if (commVar[0].Equals("PRE_COND_SETPOINT"))
            {
                tb_PreTempMCU_Eng.Text = commVar[1];
                string[] commVar2 = tb_PreTempMCU_Eng.Text.Split('\n');
                tb_PreTempMCU_Eng.Text = commVar2[0];
            }
            else if (str.Equals("check_door\n") || str.Equals("check_door"))
            {
                _check_Door();
            }
            else if (str.Equals("ok_door\n") || str.Equals("ok_door"))
            {
                _start_Process();
            }
        }


        void RxPacket_CallBack(object sender)
        {
            if (sender is PcrRxProcess rx)
            {
                Pcr_Packet packet = rx.rxPacket;
                //recv_cnt++;
                //if (recv_cnt == 16)
                //{
                //    recv_cnt = 0;
                //    recv_step++;
                //}
                switch (packet.SubCode)
                {
                    ///////////////////////////////////////////////////////////1
                    // base 값 16개
                    case Optic_SubItemCode.SFC_OPT_BASE_1CH_TB:                             // BASE 챔버1 FAM 1
                        CH_BASE_DATA[0] = packet.data_float.ToString();
                        showLog("BASE 챔버1 FAM 수신완료");
                        recv_step = 0;   // base값이 들어오기 시작함
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_1CH_IC:                             // BASE 챔버1 ROX 2
                        CH_BASE_DATA[1] = packet.data_float.ToString();
                        showLog("BASE 챔버1 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_1CH_E1D1:                          // BASE 챔버1 HEX 15
                        CH_BASE_DATA[2] = packet.data_float.ToString();
                        showLog("BASE 챔버1 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_1CH_E2D2:                          // BASE 챔버1 CY5 16
                        CH_BASE_DATA[3] = packet.data_float.ToString();
                        showLog("BASE 챔버1 CY5 수신완료");
                        showLog("base 값을 모두 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_2CH_NTM:                            // BASE 챔버2 FAM 5
                        CH_BASE_DATA[4] = packet.data_float.ToString();
                        showLog("BASE 챔버2 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_2CH_IC:                             // BASE 챔버2 ROX 6
                        CH_BASE_DATA[5] = packet.data_float.ToString();
                        showLog("BASE 챔버2 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_2CH_E1D1:                          // BASE 챔버2 HEX 3
                        CH_BASE_DATA[6] = packet.data_float.ToString();
                        showLog("BASE 챔버2 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_2CH_E2D2:                          // BASE 챔버2 CY5 4
                        CH_BASE_DATA[7] = packet.data_float.ToString();
                        showLog("BASE 챔버2 CY5 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_3CH_E1D1:                           // BASE 챔버3 FAM 9
                        CH_BASE_DATA[8] = packet.data_float.ToString();
                        showLog("BASE 챔버3 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_3CH_E2D2:                           // BASE 챔버3 ROX 10
                        CH_BASE_DATA[9] = packet.data_float.ToString();
                        showLog("BASE 챔버3 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_3CH_E1D1:                          // BASE 챔버3 HEX 7
                        CH_BASE_DATA[10] = packet.data_float.ToString();
                        showLog("BASE 챔버3 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_3CH_E2D2:                          // BASE 챔버3 CY5 8
                        CH_BASE_DATA[11] = packet.data_float.ToString();
                        showLog("BASE 챔버3 CY5 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_4CH_E1D1:                           // BASE 챔버4 FAM 13
                        CH_BASE_DATA[12] = packet.data_float.ToString();
                        showLog("BASE 챔버4 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_BASE_4CH_E2D2:                           // BASE 챔버4 ROX 14
                        CH_BASE_DATA[13] = packet.data_float.ToString();
                        showLog("BASE 챔버4 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_4CH_E1D1:                          // BASE 챔버4 HEX 11
                        CH_BASE_DATA[14] = packet.data_float.ToString();
                        showLog("BASE 챔버4 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_BASE_4CH_E2D2:                          // BASE 챔버4 CY5 12
                        CH_BASE_DATA[15] = packet.data_float.ToString();
                        showLog("BASE 챔버4 CY5 수신완료");
                        break;
                    ///////////////////////////////////////////////////////////1
                    // cycle 값 16개
                    case Optic_SubItemCode.SFC_OPT_VAL_1CH_TB:                        //============> opt1 para1   // 15cycle 챔버1 FAM
                        CH_DATA[0, recv_step] = packet.data_float.ToString();
                        showLog("챔버1 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_1CH_IC:                      //============> opt1 para2      // 15cycle 챔버1 ROX
                        CH_DATA[1, recv_step] = packet.data_float.ToString();
                        showLog("챔버1 ROX 수신완료");
                       
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_1CH_E1D1:                           // 15cycle 첌버1 HEX
                        CH_DATA[2, recv_step] = packet.data_float.ToString();
                        showLog("챔버1 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_1CH_E2D2:                           // 15cycle 챔버1 CY5  =========> 20사이클 마지막 step2, step4, step6
                        CH_DATA[3, recv_step] = packet.data_float.ToString();
                        showLog("챔버1 CY5 수신완료");
                        if (recv_step % 2 == 1 ) 
                        {
                            recv_step++;  // 최종 끝나면 recv_step=7 이됨 ==> 종료처리
                        }
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_2CH_NTM:                             // 15cycle 챔버2 FAM
                        CH_DATA[4, recv_step] = packet.data_float.ToString();
                        showLog("챔버2 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_2CH_IC:                              // 15cycle 챔버2 ROX
                        CH_DATA[5, recv_step] = packet.data_float.ToString();
                        showLog("챔버2 ROX 수신완료");
                        
                        break;

                    case Optic_SubItemCode.SFC_OPT2_VAL_2CH_E1D1:                           // 15cycle 챔버2 HEX              
                        CH_DATA[6, recv_step] = packet.data_float.ToString();
                        showLog("챔버2 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_2CH_E2D2:                           // 15cycle 챔버2 CY5  =========> 15사이클 마지막  step1, step3, step5, step7
                        CH_DATA[7, recv_step] = packet.data_float.ToString();
                        showLog("챔버2 CY5 수신완료");
                        if (recv_step % 2 == 0 ) //if (recv_step == 0 || recv_step == 2 || recv_step == 4 || recv_step == 6)
                        {
                            recv_step++;  // 최종 끝나면 recv_step=7 이됨 ==> 종료처리
                        }
                        if ( (checkBox1.Checked && recv_step == 1) || recv_step == 27)  // 분석 프로세스가 정상적으로 종료됨  // 체크할 경우는 15사이클 후 종료
                        {
                            showLog("분석 프로세스가 정상 종료됨");
                            _endProcess();
                        }
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_3CH_E1D1:                            // 15cycle 챔버3 FAM
                        CH_DATA[8, recv_step] = packet.data_float.ToString();
                        showLog("챔버3 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_3CH_E2D2:                            // 15cycle 챔버3 ROX
                        CH_DATA[9, recv_step] = packet.data_float.ToString();
                        showLog("챔버3 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_3CH_E1D1:                   //============> opt2 para1    // 15cycle 챔버3 HEX
                        CH_DATA[10, recv_step] = packet.data_float.ToString();
                        showLog("챔버3 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_3CH_E2D2:
                        CH_DATA[11, recv_step] = packet.data_float.ToString();      //============> opt2 para2     // 15cycle 챔버3 CY5
                        showLog("챔버3 CY5 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_4CH_E1D1:                            // 15cycle 챔버4 FAM
                        CH_DATA[12, recv_step] = packet.data_float.ToString();
                        showLog("챔버4 FAM 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT_VAL_4CH_E2D2:                            // 15cycle 챔버4 ROX
                        CH_DATA[13, recv_step] = packet.data_float.ToString();
                        showLog("챔버4 ROX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_4CH_E1D1:                           // 15cycle 챔버4 HEX
                        CH_DATA[14, recv_step] = packet.data_float.ToString();
                        showLog("챔버4 HEX 수신완료");
                        break;
                    case Optic_SubItemCode.SFC_OPT2_VAL_4CH_E2D2:                           // 15cycel 챔버4 CY5
                        CH_DATA[15, recv_step] = packet.data_float.ToString();
                        showLog("챔버4 VY5 수신완료");
                        break;
                    ///////////////////////////////////////////////////////////1
                    case Optic_SubItemCode.SFC_OPT_TEST:
                        Console.WriteLine("{0}", packet.ToString());
                        break;
                }


            }
        }

        private void _endProcess()
        {
            /////////////////////////////////////////////////////
            /// 정상적인 프로세스 종료에 따른 후속 작업
            /// 
            MessageBox.Show("검사가 끝났습니다. 결과를 확인해 주세요.", "검사 종료 안내", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 결과 인쇄(pdf)
            btn_Print_Click_1(null, null);

            // 관련 변수들 초기화
            _reset_Process();
        }


        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics; 
            
            // 배경 이미지를 인쇄
            System.Drawing.Image img = System.Drawing.Image.FromFile("data\\ABI출력폼.bmp");
            Point loc = new Point(0, 0);
            e.Graphics.DrawImage(img, loc);


            ////////////////////////////////////////////////////////////////////////////
            /// 검사 정보
            float x_org = 210.0F;
            float y_org = 289.0F;
            float x_offset = 340.0F;
            float y_offset = 31.0F;

            // 검사 시작시간 인쇄
            myStringPrint(g, "2020년9월2일 오후 6:12", x_org, y_org);

            // 검사 종료시간 인쇄
            myStringPrint(g, "2020년9월2일 오후 8:22", x_org + x_offset, y_org);

            // 검사 담당자 
            myStringPrint(g, "홍길동", x_org, y_org + y_offset);

            // 피검사자ID 담당자 
            myStringPrint(g, "피검사자ID", x_org + x_offset, y_org + y_offset);

            // 담당자ID 
            myStringPrint(g, "담당자ID", x_org, y_org + y_offset * 2);

            // 샘플ID 담당자 
            myStringPrint(g, "샘플ID", x_org + x_offset, y_org + y_offset * 2);

            // 검사종류 
            myStringPrint(g, "검사종류", x_org, y_org + y_offset * 3);

            // 카트리지ID 담당자 
            myStringPrint(g, "카트리지ID", x_org + x_offset, y_org + y_offset * 3);

            ////////////////////////////////////////////////////////////////////////////
            /// 분석결과
            // 챔버1
            x_org = 210.0F;
            y_org = 584.0F;
            x_offset = 136.0F;
            y_offset = 31.0F;

            myStringPrint(g, "FAM", x_org + 10, y_org);
            myStringPrint(g, "FAM", x_org + x_offset / 2 + 10, y_org);

            myStringPrint(g, "ROX", x_org + 10 + x_offset, y_org);
            myStringPrint(g, "ROX", x_org + x_offset / 2 + 10 + x_offset, y_org);

            myStringPrint(g, "HEX", x_org + 10 + x_offset * 2, y_org);
            myStringPrint(g, "HEX", x_org + x_offset / 2 + 10 + x_offset * 2, y_org);

            myStringPrint(g, "CY5", x_org + 10 + x_offset * 3, y_org);
            myStringPrint(g, "CY5", x_org + x_offset / 2 + 10 + x_offset * 3, y_org);

            // 챔버2
            x_org = 210.0F;
            y_org = 584.0F + 155.0F;
            x_offset = 136.0F;
            y_offset = 31.0F;

            myStringPrint(g, "FAM", x_org + 10, y_org);
            myStringPrint(g, "FAM", x_org + x_offset / 2 + 10, y_org);

            myStringPrint(g, "ROX", x_org + 10 + x_offset, y_org);
            myStringPrint(g, "ROX", x_org + x_offset / 2 + 10 + x_offset, y_org);

            myStringPrint(g, "HEX", x_org + 10 + x_offset * 2, y_org);
            myStringPrint(g, "HEX", x_org + x_offset / 2 + 10 + x_offset * 2, y_org);

            myStringPrint(g, "CY5", x_org + 10 + x_offset * 3, y_org);
            myStringPrint(g, "CY5", x_org + x_offset / 2 + 10 + x_offset * 3, y_org);


            // 챔버3
            x_org = 210.0F;
            y_org = 584.0F + 155.0F * 2;
            x_offset = 136.0F;
            y_offset = 31.0F;

            myStringPrint(g, "FAM", x_org + 10, y_org);
            myStringPrint(g, "FAM", x_org + x_offset / 2 + 10, y_org);

            myStringPrint(g, "ROX", x_org + 10 + x_offset, y_org);
            myStringPrint(g, "ROX", x_org + x_offset / 2 + 10 + x_offset, y_org);

            myStringPrint(g, "HEX", x_org + 10 + x_offset * 2, y_org);
            myStringPrint(g, "HEX", x_org + x_offset / 2 + 10 + x_offset * 2, y_org);

            myStringPrint(g, "CY5", x_org + 10 + x_offset * 3, y_org);
            myStringPrint(g, "CY5", x_org + x_offset / 2 + 10 + x_offset * 3, y_org);


            // 챔버4
            x_org = 210.0F;
            y_org = 584.0F + 155.0F * 3;
            x_offset = 136.0F;
            y_offset = 31.0F;

            myStringPrint(g, "FAM", x_org + 10, y_org);
            myStringPrint(g, "FAM", x_org + x_offset / 2 + 10, y_org);

            myStringPrint(g, "ROX", x_org + 10 + x_offset, y_org);
            myStringPrint(g, "ROX", x_org + x_offset / 2 + 10 + x_offset, y_org);

            myStringPrint(g, "HEX", x_org + 10 + x_offset * 2, y_org);
            myStringPrint(g, "HEX", x_org + x_offset / 2 + 10 + x_offset * 2, y_org);

            myStringPrint(g, "CY5", x_org + 10 + x_offset * 3, y_org);
            myStringPrint(g, "CY5", x_org + x_offset / 2 + 10 + x_offset * 3, y_org);
                    }


        private void myStringPrint(Graphics g, string str, float x, float y)
        {

            PointF drawPoint = new PointF(x, y);
            using (Font font = new Font("문체부 제목 돋음체", 12))
            //using (SolidBrush drawBrush = new SolidBrush(Color.Black))
            using (SolidBrush drawBrush = new SolidBrush(Color.DarkGray))
            {
                g.DrawString(str, font, drawBrush, drawPoint);
            }

        }


        private void btn_Print_Click_1(object sender, EventArgs e)
        {
            string dt = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            // 결과 pdf 인쇄
            string filePath = Application.StartupPath + @"\report";
            filePath += "/PCR " + dt + ".pdf";
            
            printDocument1.DocumentName = "PCR report printing...";
            printDocument1.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            printDocument1.PrinterSettings.PrintFileName = filePath;
            printDocument1.PrinterSettings.PrintToFile = true;
            printDocument1.Print(); // 인쇄 시작

            // 결과 csv 저장
            string filePath2 = Application.StartupPath + @"\log";
            filePath2 += "/PCR " + dt + ".csv";

            Save_GridCsv(filePath2);
        }

        private void tb_Data_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnSetBaseData_Click(object sender, EventArgs e)
        {
            setGridValue(0);
        }

        private void btnSet15Data_Click(object sender, EventArgs e)
        {
            setGridValue(1);
        }

        private void btnSet20Data_Click(object sender, EventArgs e)
        {
            setGridValue(2);
        }

        private void btnSet25Data_Click(object sender, EventArgs e)
        {
            setGridValue(3);
        }

        private void btnSet30Data_Click(object sender, EventArgs e)
        {
            setGridValue(4);
        }

        private void btnSet35Data_Click(object sender, EventArgs e)
        {
            setGridValue(5);
        }

        private void btnSet40Data_Click(object sender, EventArgs e)
        {
            setGridValue(6);
        }

        private void btnSetFinalData_Click(object sender, EventArgs e)
        {
            setGridValue(7);
        }

        private void btn_DoorOpen_Test_Click(object sender, EventArgs e)
        {
            // 검사 준비 중의 경우에는 검사를 초기화 할지 확인
            if (processStep > 0)
            {
                // 확인
                if (MessageBox.Show("문을 엽니다. \n검사 준비를 초기화하려면 [예], \n검사를 계속 하려면 [아니오]를 누르세요.", "문열림 안내", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    // 검사 초기화
                    setProcessMode(0);
                }
                else   // 문만 열었다가 계속 검사할 경우
                {
                }
            } 

            // 검사창 도어 오픈
            StringBuilder sb = new StringBuilder();
            sb.Append(":DOOR_UNLOCK");

            String txt = sb.ToString();
            serial.SendLine(txt);
        }

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 검사 중이면 검사를 종료해야 종료 가능
            if (processStep == 9)
            {
                MessageBox.Show("검사 중에는 프로그램을 종료할 수 없습니다.", "종료 안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;

                return;
            }

            // 종료 확인
            if (MessageBox.Show("프로그램을 종료하시겠습니까?", "종료 안내", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                // 종료 
                e.Cancel = false;
            }
            else   // 취소
            {
                e.Cancel = true;
            }
        }

        private void seqTestBtn_Click(object sender, EventArgs e)
        {
            //serial.SendLine( mm.moveSM(1, 0, 500, 800) );
            mm.moveSM(1, 0, 500, 800);
        }

        private void tabPage_tester_Click()
        {
            //test info. list view 
            listView_ReportInfo.View = View.Details;           //컬럼형식으로 변경

            listView_ReportInfo.FullRowSelect = true;          //Row 전체 선택

            listView_ReportInfo.Columns.Add("Test Name", 433);
            listView_ReportInfo.Columns.Add("Start Time", 433);        //컬럼추가
            listView_ReportInfo.Columns.Add("End Time ", 433);

            //test 
            listView_inspectorInfo.View = View.Details;           //컬럼형식으로 변경
            listView_inspectorInfo.FullRowSelect = true;          //Row 전체 선택

            listView_inspectorInfo.Columns.Add("Inspector Name", 250);
            listView_inspectorInfo.Columns.Add("Inspector ID ", 250);        //컬럼추가


            //test 
            listView_PatientInfo.View = View.Details;           //컬럼형식으로 변경
            listView_PatientInfo.FullRowSelect = true;          //Row 전체 선택

            listView_PatientInfo.Columns.Add("Patient ID", 250);
            listView_PatientInfo.Columns.Add("Sample ID ", 250);        //컬럼추가
            listView_PatientInfo.Columns.Add("Cartridge ID ", 250);        //컬럼추가

            //test 
            listView_Result.View = View.Details;           //컬럼형식으로 변경
            listView_Result.FullRowSelect = true;          //Row 전체 선택

            listView_Result.Columns.Add("T B", 433);
            listView_Result.Columns.Add("N T M", 433);        //컬럼추가
            listView_Result.Columns.Add("R I F", 433);        //컬럼추가
            //listView_Result.Columns.Add("I N H", 250);        //컬럼추가

            DataAddToList();
            //updatePatientInfo();
        }
        private void tabPage_tester_Click(object sender, EventArgs e)
        {
            
        }

        private void tabPage_Result_Click(object sender, EventArgs e)
        {
            

        }

        private void DataAddToList()
        {
            /*
            listView_ReportInfo.BeginUpdate();

            ListViewItem lvi1 = new ListViewItem("MTBC");
            //lvi1.SubItems.Add("2020-00-00 00:00:00");
            //lvi1.SubItems.Add("2020-00-00 00:00:00");

            lvi1.SubItems[0].BackColor = Color.WhiteSmoke;
            
            lvi1.ImageIndex = 0;
            listView_ReportInfo.Items.Add(lvi1);
            
            listView_ReportInfo.EndUpdate();
            */
            //*************************************************************
            listView_inspectorInfo.BeginUpdate();
            ListViewItem lvi2 = new ListViewItem(sharedmem.userName);
            lvi2.SubItems.Add(sharedmem.userID);
            lvi2.SubItems[0].BackColor = Color.WhiteSmoke;
            //lvi2.SubItems.Add("임시1");
            //lvi2.SubItems.Add("인천광역시 부평구");
            lvi2.ImageIndex = 0;
            listView_inspectorInfo.Items.Add(lvi2);

            listView_inspectorInfo.EndUpdate();
            
            //****************************************************************

        }

        public void CriticalThresholdCalculation()
        {
            //Ct calculation
            string[] value_Dye = new string[26] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            double[] value_Dye_Slope = new double[25] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            double[] second_Slope = new double[24] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };

            double tempMax = 0;
            int tempMax_Index = 0;

            for (int i = 2; i < 27; i++)
            {
                value_Dye[i] = dataGridView5.Rows[0].Cells[i].FormattedValue.ToString();
            }
            for (int j = 0; j < 25; j++)
            {
                value_Dye_Slope[j] = Convert.ToDouble(value_Dye[j + 1]) - Convert.ToDouble(value_Dye[j]);
            }
            for (int j = 0; j < 24; j++)
            {
                second_Slope[j] = value_Dye_Slope[j + 1] - value_Dye_Slope[j];
                if( second_Slope[j] > second_Slope[j-1] && j != 0)
                {
                    tempMax = second_Slope[j];
                    sharedmem.criticalThreshold = j;

                }
            }
        }

        public void updateAnalyticResult()
        {
            //****************************************************************
            int[] probe = new int[8]{ 0, 0, 0, 0, 0, 0, 0, 0 };
            
            //probe 1~8 data get from dataGridView
            for (int i = 0; i < 0; i++)
            {
                if (i % 2 == 0)
                {
                    probe[i] = Int32.Parse(dataGridView1_Tester.Rows[0].Cells[1].FormattedValue.ToString());
                }
                else
                {
                    probe[i] = Int32.Parse(dataGridView1_Tester.Rows[0].Cells[4].FormattedValue.ToString());
                }
            }

            //CriticalThresholdCalculation();
            //dataGridView1_Tester.Rows[0].Cells[1].FormattedValue.ToString()



            //Analysis Start 
            listView_Result.BeginUpdate();
            lvi4 = new ListViewItem("");

            //MTBC analysis
            if ( (probe[0] > 2000) && (probe[1] > 1000) && (probe[2] > 2000))
            {
                lvi4 = new ListViewItem("");
                listView_Result.Items[0].SubItems[0].Text = "POSITIVE";
                //listView_Result.Items[0].SubItems[1].Text = "";
            }
            else
            {
                listView_Result.Items[0].SubItems[0].Text = "NEGATIVE";
                listView_Result.Items[0].SubItems[2].Text = "N/A";
            }

            //NTM analysis
            if( (probe[0] < 1000) && (probe[1] > 2000) && ( probe[2] > 2000))
            {
                listView_Result.Items[0].SubItems[1].Text = "POSITIVE";
            }
            else
            {
                listView_Result.Items[0].SubItems[1].Text = "NEGATIVE";
            }

            //RIF analysis
            int j = 0;
            for (int i = 3; i < 8; i++)
            {
                if ( probe[i] > 2000 )
                {
                    j++;
                }
            }

            if(j >= 3)
            {
                listView_Result.Items[0].SubItems[2].Text = "POSITIVE";
            }
            else
            {
                listView_Result.Items[0].SubItems[2].Text = "NEGATIVE";
            }
            
            //INH analysis

            listView_Result.EndUpdate();


        }

        public void initializeAnalyticResult()
        {
            listView_Result.BeginUpdate();
            lvi4 = new ListViewItem("POS");
            lvi4.SubItems.Add("NEG");
            lvi4.SubItems.Add("Susceptible");
            //lvi2.SubItems.Add("임시1");
            //lvi2.SubItems.Add("인천광역시 부평구");
            lvi4.ImageIndex = 0;
            listView_Result.Items.Add(lvi4);

            listView_Result.EndUpdate();

        }

        public void initializePatientInfo()
        {
            listView_PatientInfo.BeginUpdate();
            lvi3 = new ListViewItem(" ");

            listView_PatientInfo.Items[0].SubItems[0].Text = "";
            listView_PatientInfo.Items[0].SubItems[1].Text = "";
            listView_PatientInfo.Items[0].SubItems[2].Text = "";

            lvi3.SubItems[0].BackColor = Color.WhiteSmoke;
            //lvi2.SubItems.Add("임시1");
            //lvi2.SubItems.Add("인천광역시 부평구");
            lvi3.ImageIndex = 0;
            listView_PatientInfo.Items.Add(lvi3);

            listView_PatientInfo.EndUpdate();
        }

        public void updatePatientInfo()
        {
            listView_PatientInfo.BeginUpdate();
            lvi3 = new ListViewItem(tb_PatientID_Test.Text);
            lvi3.SubItems.Add(tb_SampleID_Test.Text);
            lvi3.SubItems.Add(tb_CartridgeID_Test.Text);
            lvi3.SubItems[0].BackColor = Color.WhiteSmoke;
            //lvi2.SubItems.Add("임시1");
            //lvi2.SubItems.Add("인천광역시 부평구");
            lvi3.ImageIndex = 0;
            listView_PatientInfo.Items.Add(lvi3);

            listView_PatientInfo.EndUpdate();

        }

        private void testButton_Click(object sender, EventArgs e)
        {
        
            MessageBox.Show(dataGridView5.Rows[0].Cells[1].FormattedValue.ToString());

        
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //update test infomation
            test_start_time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

            listView_ReportInfo.BeginUpdate();

            lvi1 = new ListViewItem("TB");
            lvi1.SubItems.Add(test_start_time);
            lvi1.SubItems.Add("");

            lvi1.SubItems[0].BackColor = Color.WhiteSmoke;

            lvi1.ImageIndex = 0;
            listView_ReportInfo.Items.Add(lvi1);

            listView_ReportInfo.EndUpdate();
        }

        private void btn_Connect_Barcode_Click(object sender, EventArgs e)
        {
            // 컴포트 선택 후 연결 버튼 클릭하면 ID, PW 입력창이 활성화됨
            //showLog("장치와 연결 동작");

            //컴포트 연결시도
            bool flag = Open_exec_barcode();
            
            //연결에 성공하면
           
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void formsPlot1_Load(object sender, EventArgs e)
        {

        }
    }
    #endregion
    
}