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
using System.Threading;     //匯入多執行緒功能函數
using System.Collections;   //匯入集合物件功能

namespace Go_Server
{
    public partial class Form1 : Form
    {
        Socket T;           //通訊物件
        TcpListener Server;//伺服端網路監聽器(相當於電話總機)
        Socket Client;//給客戶用的連線物件(相當於電話分機)
        Thread Th_Svr;//伺服器監聽用執行緒(電話總機開放中)
        Thread Th_Clt;//客戶用的通話執行緒(電話分機連線中)
        Hashtable HT = new Hashtable();//客戶名稱與通訊物件的集合(雜湊表)(key:Name, Socket)

        Random rnd = new Random(Guid.NewGuid().GetHashCode()); //建立亂數抽牌
        int check, re_chcek = 0;  //檢查牌組是否重複
        int remain_card = 52; //剩餘手牌
        int zero_count = 0;  //確認牌組中多少牌已被抽走
        int game_point = 0;  //累積點數
        int[] card_deck = new int[52]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,
             30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52};
        int[] card = new int[5];

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = ShowIP(); //呼叫函數找本機IP
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

        public Form1()
        {
            InitializeComponent();
            this.textBox2.Text = "2013";
            this.textBox3.Text = "0";
            
        }
        private void ServerSub()
        {
            //Server IP 和 Port
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            Server = new TcpListener(EP);
            Server.Start(100);
            while (true)
            {
                Client = Server.AcceptSocket();
                Th_Clt = new Thread(Listen); //建立監聽這個客戶連線的獨立執行緒
                Th_Clt.IsBackground = true; //設定為背景執行緒
                Th_Clt.Start(); //開始執行緒的運作
                
            }
        }
        //#endregion
        //#region 監聽客戶訊息的程式
        private void Listen()
        {
            Socket sck = Client;  //複製Client通訊物件到個別客戶專用物件Sck
            Thread Th = Th_Clt;   //複製執行緒Th_Clt到區域變數Th
            while (true) //持續監聽客戶傳來的訊息
            {
                
                try //用 Sck 來接收此客戶訊息，inLen 是接收訊息的 Byte 數目
                {
                    byte[] B = new byte[1023];                            //建立接收資料用的陣列，長度須大於可能的訊息
                    int inLen = sck.Receive(B);                           //接收網路資訊(Byte陣列)
                    string Msg = Encoding.Default.GetString(B, 0, inLen); //翻譯實際訊息(長度inLen)
                    string Cmd = Msg.Substring(0, 1);                     //取出命令碼 (第一個字)
                    string Str = Msg.Substring(1);
                    string ShowCard;
                    int receive_card = 0; //負責接收手牌點數
                    int change_card = 0;  //負責接收手牌位置 用以發新牌
                    //取出命令碼之後的訊息(user name)
                    switch (Cmd)//依據命令碼執行功能
                    {
                        case "0"://有新使用者上線：新增使用者到名單中
                            HT.Add(Str, sck); //連線加入雜湊表，Key:使用者，Value:連線物件(Socket)
                            listBox1.Items.Add(Str); //加入上線者名單
                            SendAll(OnlineList()); //將目前上線人名單回傳剛剛登入的人(包含他自己)
                            break;
                        case "9": //使用者登出
                            game_point = 0;
                            textBox3.Text = "" + game_point; //點數歸0
                            HT.Remove(Str);             //移除使用者名稱為Name的連線物件
                            listBox1.Items.Remove(Str); //自上線者名單移除Name
                            SendAll(OnlineList()); //將目前上線人名單回傳剛剛登入的人(不包含他自己)
                            Th.Abort();                 //結束此客戶的監聽執行緒
                            Reset_card_deck();
                            break;
                        case "R": //接收出牌訊息
                            receive_card = Int32.Parse(Msg.Substring(1)); //接收client端傳送的手牌點數
                            //MessageBox.Show("" + receive_card);
                            Check_deck(); //檢查牌組
                            Count_points(receive_card); //計算點數
                            break;
                        case "H": //接收牌型訊息 替client端換一張新牌
                            change_card = Int32.Parse(Str.Substring(0,1)); //接收client手牌位置
                            String player = Str.Substring(4);
                            Check_deck();   //檢查牌組
                            Change_card(change_card,player); //傳送新牌給玩家
                            //接收client端打的牌的編號 然後廣播給所有client端
                            ShowCard = Str.Substring(2,2);
                            SendAll("D" + ShowCard);
                            break;
                        case "G": //開局發牌給玩家
                            Check_deck();
                            Shuffle(Str);
                            break;
                    }
                    SendAll("s" + textBox3.Text); //廣播訊息

                }
                catch (Exception)
                {
                    //有錯誤時忽略，通常是客戶端無預警強制關閉程式，測試階段常發生
                }
                
            }
        }
        private string OnlineList()
        {
            string L = "L"; //代表線上名單的命令碼(字頭)
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                L += listBox1.Items[i]; //逐一將成員名單加入L字串
                //不是最後一個成員要加上","區隔
                if (i < listBox1.Items.Count - 1) { L += ","; }
            }
            return L;
        }
        private void SendAll(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str); //訊息轉譯為Byte陣列
            foreach (Socket s in HT.Values) s.Send(B, 0, B.Length, SocketFlags.None); //傳送資料
        }
        //#endregion

        private void SendTo(string Str ,string User) //傳送訊息給指定使用者
        {
            byte[] B = Encoding.Default.GetBytes(Str); //訊息轉譯為Byte陣列
            Socket Sck = (Socket)HT[User]; //取出發送對象User的通訊物件
            Sck.Send(B, 0, B.Length, SocketFlags.None); //發送訊息
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.ExitThread();//關閉所有執行緒
        }
        private void Button1_Click_1(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;    //忽略跨執行緒處理的錯誤(允許跨執行緒存取變數)
            Th_Svr = new Thread(ServerSub);             //宣告監聽執行緒(副程式ServerSub)
            Th_Svr.IsBackground = true;                 //設定為背景執行緒
            Th_Svr.Start();                             //啟動監聽執行緒
            button1.Enabled = false;                    //讓按鍵無法使用(不能重複啟動伺服器)
        }

        private void Reset_card_deck()
        {
            card_deck = new int[52]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,
            30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52}; //牌組重設為52張
            for (int i = 0; i < card.Length; i++) //將手上有的牌從牌組刪除
            {
                re_chcek = card[i];
                card_deck[re_chcek - 1] = 0;
            }
            Check_deck(); //顯示剩餘牌組
        }

        //開局發牌
        private void Shuffle(string player)
        {
            //int i = 0;
            card = new int[5]; //5張手牌
            //MessageBox.Show("" + card.Length);
            for(int i=0;i<card.Length;i++)
            {
            RE:
                card[i] = rnd.Next(1, 53); //亂數抽牌
                //檢查是否抽到重覆 若重複就重抽
                check = card[i];
                if (card_deck[check - 1] == 0) 
                {
                    goto RE;
                }
                else
                {
                    card_deck[check - 1] = 0; //將該張牌從牌組中刪除(避免重複);
                    if (card[i] < 10)
                    {
                        SendTo("c" + "0" + Convert.ToString(card[i]), player); //傳送手牌訊息給client
                    }
                    else
                    {
                        SendTo("c" + Convert.ToString(card[i]),player);
                    }
                }
            }
        }
        //檢查剩餘牌組 若為0 就重制牌組
        private void Check_deck()
        {
            remain_card = 52;
            zero_count = 0;
            for (int i = 0; i < card_deck.Length; i++)
                if (card_deck[i] == 0)
                {
                    zero_count++;
                }
            remain_card -= zero_count; 
            if (zero_count >= 52)
            {
                Reset_card_deck();
            }
        }
        //出完牌重新抽牌
        private void Change_card(int n,String User)
        {
        retake:
            card[n] = rnd.Next(1, 53); //亂數抽牌
            check = card[n];
            if (card_deck[check - 1] == 0) //檢查是否重複抽牌
            {
                goto retake;
            }
            else
                card_deck[check - 1] = 0;
                if (card[n] < 10)
                {
                    SendTo("H" + "0" + Convert.ToString(card[n]),User); //傳送H控制碼+新牌給單一玩家
                }
                else
                {
                    SendTo("H" + Convert.ToString(card[n]), User); //傳送H控制碼+新牌給單一玩家
            }

        }
        //判斷牌型 計算點數
        private void Count_points(int point)
        {
            switch (point)
            {
                //打出 K 點數變成99
                case 99:
                    game_point = 99;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 黑桃Ace 點數歸零
                case 0:
                    game_point = 0;
                    textBox3.Text = "" + game_point;
                    break;
                //打出Ace 點數+1
                case 1:
                    game_point += 1;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 2  點數+2
                case 2:
                    game_point += 2;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 3 點數+3
                case 3:
                    game_point += 3;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 4 點數+4
                case 4:
                    game_point += 4;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 5 點數+5
                case 5:
                    game_point += 5;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 6 點數+6
                case 6:
                    game_point += 6;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 7 點數+7
                case 7:
                    game_point += 7;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 8 點數+8
                case 8:
                    game_point += 8;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 9 點數+9
                case 9:
                    game_point += 9;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 10 選擇點數+10
                case 10:
                    game_point += 10;
                    textBox3.Text = "" + game_point;
                    break;
                //打出 10 選擇點數-10
                case -10:
                    game_point -= 10;
                    if (game_point < 0)
                    {
                        game_point = 0;
                    }
                    textBox3.Text = "" + game_point;
                    break;
                //打出 J Pass
                case 11:
                    textBox3.Text = "" + game_point;
                    break;
                //打出 Q 選擇點數+20
                case 20:
                    game_point += 20;
                    textBox3.Text = "" + game_point;
                    break;
               
                //打出 Q 選擇點數-20
                case -20:
                    game_point -= 20;
                    if (game_point < 0)
                    {
                        game_point = 0;
                    }
                    textBox3.Text = "" + game_point;
                    break;
            }
            Reset();
        }

        //判斷勝負 重製牌局
        private void Reset()
        {
            textBox3.Text = "" + game_point;
            if (game_point > 99)
            {
                SendAll("s" + textBox3.Text); //傳送結束訊息給client 
            }
        }
    }
}
