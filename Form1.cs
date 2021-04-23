using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;           //匯入網路通訊協定相關函數
using System.Net.Sockets;   //匯入網路插座功能函數
using System.Threading;//匯入多執行緒功能函數

namespace Go_Client
{
    public partial class Form1 : Form
    {
        //公用變數
        Socket T;           //通訊物件
        string User;        //使用者
        Thread Th;//網路監聽執行緒
        int STEP_count = 0;
        int show_step = 0;
        int[] card = new int[5];
        int card_isclick = 0; //檢測第幾張牌被點擊
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = ShowIP(); //呼叫函數找本機IP
            this.textBox2.Text = "2013";
            this.textBox3.Text = "enter your name";
            button_start.Enabled = false;
            button2.Enabled = false;
            pictureBox1.Enabled = false;
            pictureBox2.Enabled = false;
            pictureBox3.Enabled = false;
            pictureBox4.Enabled = false;
            pictureBox5.Enabled = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        //找本機IP
        private string ShowIP()
        {
            string hn = Dns.GetHostName();
            IPAddress[] ip = Dns.GetHostEntry(hn).AddressList; //取得本機IP陣列
            foreach (IPAddress it in ip)
            {
                if (it.AddressFamily == AddressFamily.InterNetwork)
                {
                    return it.ToString();//如果是IPv4回傳此IP字串
                }
            }
            return ""; //找不到合格IP回傳空字串
        }

        private void Listen()
        {
            EndPoint ServerEP = (EndPoint)T.RemoteEndPoint; //Server 的 EndPoint
            byte[] B = new byte[1023]; //接收用的 Byte 陣列
            int inLen = 0; //接收的位元組數目
            int n = 0;
            int Pic = 0;
            string Msg; //接收到的完整訊息
            string St; //命令碼
            string Str; //訊息內容(不含命令碼)
            
            while (true)//無限次監聽迴圈
            {
                try
                {
                    inLen = T.ReceiveFrom(B, ref ServerEP);//收聽資訊並取得位元組數
                }
                catch (Exception)//產生錯誤時
                {
                    T.Close();//關閉通訊器
                    listBox1.Items.Clear();//清除線上名單
                    MessageBox.Show("伺服器斷線了！");//顯示斷線
                    button1.Enabled = true;//連線按鍵恢復可用
                    Th.Abort();//刪除執行緒
                }
                Msg = Encoding.Default.GetString(B, 0, inLen); //解讀完整訊息
                St = Msg.Substring(0, 1); //取出命令碼 (第一個字)
                Str = Msg.Substring(1); //取出命令碼之後的訊息
                switch (St)//依命令碼執行功能
                {
                    case "L"://接收線上名單
                        listBox1.Items.Clear(); //清除名單
                        string[] M = Str.Split(','); //拆解名單成陣列
                        for (int i = 0; i < M.Length; i++)
                        {
                            listBox1.Items.Add(M[i]); //逐一加入名單
                        }
                        break;
                    case "s"://傳送score訊息給所有人
                        int GameOver = Int32.Parse(Str);
                        textBox5.Text = Str;
                        if (GameOver > 99) //如果點數超過99 顯示遊戲結束 玩家登出
                        {
                            DialogResult Result = MessageBox.Show("請按確定結束遊戲", "點數爆掉 遊戲結束", MessageBoxButtons.OK);
                            if (Result == DialogResult.OK)
                            {
                                GameOver = 0;
                                Send("9" + User);
                                T.Close();
                                textBox5.Text = ""+ 0; //歸0
                                label_show_next.Text = "";
                                show_step = 0;
                                pictureBox1.Image = null;
                                pictureBox2.Image = null;
                                pictureBox3.Image = null;
                                pictureBox4.Image = null;
                                pictureBox5.Image = null;
                                pictureBox6.Image = null;
                                pictureBox1.Enabled = false;
                                pictureBox2.Enabled = false;
                                pictureBox3.Enabled = false;
                                pictureBox4.Enabled = false;
                                pictureBox5.Enabled = false;
                                button_start.Enabled = false;
                                button2.Enabled = false;
                            }
                        }
                        break;
                    case "c"://開局發牌 
                        if (n == 0)
                        {
                            card[0] = Int32.Parse(Str.Substring(0, 2)); //接收第一張牌  起始位置0 長度2
                            pictureBox1.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[0] + ".jpg"); //顯示第一張牌
                            n++;
                            break;
                        }
                        if (n == 1)
                        {
                            card[1] = Int32.Parse(Str.Substring(0, 2)); //接收第二張牌  起始位置0 長度2
                            pictureBox2.Image = Image.FromFile(Application.StartupPath+ "/poker_image/" + card[1] + ".jpg"); //顯示第二張牌
                            n++;
                        }
                        if (n == 2)
                        {
                            card[2] = Int32.Parse(Str.Substring(3, 2)); //接收第三張牌  
                            pictureBox3.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[2] + ".jpg"); //顯示第三張牌                       
                            n++;
                        }
                        if (n == 3)
                        {
                            card[3] = Int32.Parse(Str.Substring(6, 2)); //接收第四張牌
                            pictureBox4.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[3] + ".jpg"); //顯示第四張牌
                            n++;
                        }
                        if (n == 4)
                        {
                            card[4] = Int32.Parse(Str.Substring(9, 2)); //接收第五張牌
                            pictureBox5.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[4] + ".jpg"); //顯示第五張牌
                            n = 0;
                        }
                        pictureBox1.Enabled = true;
                        pictureBox2.Enabled = true;
                        pictureBox3.Enabled = true;
                        pictureBox4.Enabled = true;
                        pictureBox5.Enabled = true;
                        break;
                        
                    case "H": //出牌後要求重抽一張新牌 
                        STEP_count = 0;
                        if (card_isclick == 1)
                        {
                            card[0] = Int32.Parse(Msg.Substring(1,2));
                            pictureBox1.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[0] + ".jpg"); //顯示第一張牌
                        }
                        if (card_isclick == 2)
                        {
                            card[1] = Int32.Parse(Msg.Substring(1, 2));
                            pictureBox2.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[1] + ".jpg"); //顯示第二張牌                         
                        }
                        if (card_isclick == 3)
                        {
                            card[2] = Int32.Parse(Msg.Substring(1, 2));
                            pictureBox3.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[2] + ".jpg"); //顯示第三張牌
                        }
                        if (card_isclick == 4)
                        {
                            card[3] = Int32.Parse(Msg.Substring(1, 2));
                            pictureBox4.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[3] + ".jpg"); //顯示第四張牌
                        }
                        if (card_isclick == 5)
                        {
                            card[4] = Int32.Parse(Msg.Substring(1, 2));
                            pictureBox5.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + card[4] + ".jpg"); //顯示第五張牌
                        }
                        Pic = Int32.Parse(Msg.Substring(4, 2)); //接收server廣播給所有client的牌
                        pictureBox6.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + Pic + ".jpg"); //顯示前一玩家打出的牌
                        break;
                    case "D": //client端接收server廣播其他client打出的牌
                        Pic = Int32.Parse(Msg.Substring(1, 2)); //接收server廣播給所有client的牌
                        pictureBox6.Image = Image.FromFile(Application.StartupPath + "/poker_image/" + Pic + ".jpg"); //顯示前一玩家打出的牌
                        show_step++;
                        STEP_count++;
                        if (STEP_count == 3)  
                        {
                            
                            pictureBox1.Enabled = true;
                            pictureBox2.Enabled = true;
                            pictureBox3.Enabled = true;
                            pictureBox4.Enabled = true;
                            pictureBox5.Enabled = true;
                            if (show_step >= 3)
                            {
                                label_show_next.Text = "輪到你出牌囉";
                            }
                        }
                        else
                        {
                            if (show_step >= 3)
                            {
                                label_show_next.Text = "請等待其他玩家出牌";
                            }  
                        }
                        break;
                }
            }
        }
        private void Send(string Str)
        {
            try
            {
                byte[] B = Encoding.Default.GetBytes(Str);     //翻譯字串Str為Byte陣列B
                int ret = T.Send(B, 0, B.Length, SocketFlags.None);      //使用連線物件傳送資料
            }
            catch { }
        }
       

        private void Button1_Click(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false; //忽略跨執行緒處理的錯誤(允許跨執行緒存取變數)
            string IP = textBox1.Text;                                  //伺服器IP
            if(IP == "" )
            {
                MessageBox.Show("please enter Server IP");
                return;
            }
            int Port = int.Parse(textBox2.Text);                        //伺服器Port
            try
            {
                User = textBox3.Text;  //使用者名稱
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port);  //伺服器的連線端點資訊
                //建立可以雙向通訊的TCP連線
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);                                     
                T.Connect(EP);           //連上伺服器的端點EP(類似撥號給電話總機)
                Th = new Thread(Listen); //建立監聽執行緒
                Th.IsBackground = true; //設定為背景執行緒
                Th.Start(); //開始監聽
                Send("0" + User);               //連線後隨即傳送自己的名稱給伺服器
            }
            catch (Exception)
            {
                MessageBox.Show("無法連上伺服器！"); //連線失敗時顯示訊息
                return;
            }
            button1.Enabled = false; //讓連線按鍵失效，避免重複連線
            button2.Enabled = true;  //允許玩家登出
            button_start.Enabled = true;
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            if (button1.Enabled == false)
            {
                Send("9" + User); //傳送自己的離線訊息給伺服器
                T.Close();        //關閉網路通訊器T
                textBox5.Text = "" + 0; //點數歸0
                label_show_next.Text = "";
                show_step = 0;
                button1.Enabled = true;
                button2.Enabled = false;
                button_start.Enabled = false;
                pictureBox1.Image = null;
                pictureBox2.Image = null;
                pictureBox3.Image = null;
                pictureBox4.Image = null;
                pictureBox5.Image = null;
                pictureBox6.Image = null;
                pictureBox1.Enabled = false;
                pictureBox2.Enabled = false;
                pictureBox3.Enabled = false;
                pictureBox4.Enabled = false;
                pictureBox5.Enabled = false;
                
            }
        }
        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
        //出牌後回傳server牌組編號
        private void PictureBox1_Click(object sender, EventArgs e) //手牌1
        {
            card_isclick = 1; //第一張牌被點擊
            Judge_cardType(card[0]); //判定牌型
            ChangeCard(0); //要求換新牌
            ReturnCard(card[0]); //將打出的牌傳給server端 
            Step();
            label_show_next.Text = "請等待其他玩家出牌";
        }
        private void PictureBox2_Click(object sender, EventArgs e) //手牌2
        {           
            card_isclick = 2; //第二張牌被點擊
            Judge_cardType(card[1]);
            ChangeCard(1);
            ReturnCard(card[1]);
            Step();
            label_show_next.Text = "請等待其他玩家出牌";
        }
        private void PictureBox3_Click(object sender, EventArgs e) //手牌3
        {
            card_isclick = 3; //第三張牌被點擊
            Judge_cardType(card[2]);
            ChangeCard(2);
            ReturnCard(card[2]);
            Step();
            label_show_next.Text = "請等待其他玩家出牌";
        }
        private void PictureBox4_Click(object sender, EventArgs e) //手牌4
        {
            card_isclick = 4; //第四張牌被點擊
            Judge_cardType(card[3]);
            ChangeCard(3);
            ReturnCard(card[3]);
            Step();
            label_show_next.Text = "請等待其他玩家出牌";
        }

        private void PictureBox5_Click(object sender, EventArgs e) //手牌5
        {
            card_isclick = 5; //第五張牌被點擊
            Judge_cardType(card[4]);
            ChangeCard(4);
            ReturnCard(card[4]);
            Step();
            label_show_next.Text = "請等待其他玩家出牌";
        }
        
        //判斷牌型回傳給server
        private void Judge_cardType(int card_type)
        {
            int point = card_type % 13; //判斷手牌類型
            int game_point=0; 
            switch (point)
            {
                //打出 K 點數變成99
                case 0:
                    game_point = 99;
                    Send("R" + game_point); //回傳server點數訊息
                    break;
                //打出 Ace 點數歸零
                case 1:
                    if (card_type == 40) //判定是否是黑桃ACE
                    {
                        game_point = 0;
                    }
                    else
                        game_point = 1;
                    Send("R" + game_point);
                    break;
                //打出 2  點數+2
                case 2:
                    game_point = 2;
                    Send("R" + game_point);
                    break;
                //打出 3 點數+3
                case 3:
                    game_point = 3;
                    Send("R" + game_point);
                    break;
                //打出 4 點數+4
                case 4:
                    game_point = 4;
                    Send("R" + game_point);
                    break;
                //打出 5 點數+5
                case 5:
                    game_point = 5;
                    Send("R" + game_point);
                    break;
                //打出 6 點數+6
                case 6:
                    game_point = 6;
                    Send("R" + game_point);
                    break;
                //打出 7 點數+7
                case 7:
                    game_point = 7;
                    Send("R" + game_point);
                    break;
                //打出 8 點數+8
                case 8:
                    game_point = 8;
                    Send("R" + game_point);
                    break;
                //打出 9 點數+9
                case 9:
                    game_point = 9;
                    Send("R" + game_point);
                    break;
                //打出 10 點數+10或-10
                case 10: //跳出視窗讓玩家可以選擇要+10或減10
                    DialogResult Select10Type = MessageBox.Show("確定=增加 取消=減少", "請選擇增加或減少", MessageBoxButtons.OKCancel);
                    if (Select10Type == DialogResult.OK)
                    {
                        game_point = 10;
                        
                    }
                    else if (Select10Type == DialogResult.Cancel)
                    {
                        game_point = -10;
                        
                    }
                    Send("R" + game_point);
                    break;
                //打出 J Pass
                case 11:
                    game_point = 11;
                    Send("R" + game_point);
                    break;
                //打出 Q 點數+20或-20
                case 12: //跳出視窗讓玩家可以選擇要+20或減20
                    DialogResult SelectQType = MessageBox.Show("確定=增加 取消=減少", "請選擇增加或減少", MessageBoxButtons.OKCancel);
                    if (SelectQType == DialogResult.OK)
                    {
                        game_point = 20;
                    }
                    else if (SelectQType == DialogResult.Cancel)
                    {
                        game_point = -20;
                    }
                    Send("R" + game_point);
                    break;
            } 
        }
        private void ChangeCard(int n) //回傳server 第幾張手牌被打出去
        {
            Send("H" + n );
        }
        private void ReturnCard(int n)
        {
            if (n < 10)
            {
                Send("D" + "0" + n + User); //回傳server出了哪張牌 用來公告給其他client端
            }
            else
                Send("D" + n + User); //回傳server出了哪張牌 用來公告給其他client端
        }

        private void Button_start_Click(object sender, EventArgs e) //開始遊戲
        {
            Send("G" + User);
            button_start.Enabled = false;
        }

        private void Step()
        {
            pictureBox1.Enabled = false;
            pictureBox2.Enabled = false;
            pictureBox3.Enabled = false;
            pictureBox4.Enabled = false;
            pictureBox5.Enabled = false;   
        }
    }
}
