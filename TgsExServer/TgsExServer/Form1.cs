using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace TgsExServer
{
    public partial class Form1 : Form
    {
        // ポーリング間隔
        const long PORING_MSEC = 3000;
        // カウンタ
        int iPoringCount = 0;

        // 送信回数
        int Ser = 0;

        // UDP
        IPAddress localAddress = IPAddress.Parse("127.0.0.1");
        int localPort = 60000;
        IPEndPoint localEP;
        UdpClient udp;

        /** 終了*/
        bool isClose = false;
        
        /** テストデータ*/
        string [,] testData = new string[,]{
            {"21531000","123.123.123.123","0","x","path"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
            {"21531999","124.124.124.124","1","x","path2"},
        };
        /** ラベルリスト*/
        List<Label> labels = new List<Label>();
        /** 列名*/
        enum COL
        {
            INDEX,
            UID,
            IP,
            COUNT,
            ALERT,
            PATH,
            MAX
        };

        // 各列の配置
        AnchorStyles[] anc = {
            AnchorStyles.Right,
            AnchorStyles.Left,
            AnchorStyles.Left,
            AnchorStyles.Right,
            AnchorStyles.Left,
            AnchorStyles.Left
                        };

        /** コンストラクタ*/
        public Form1()
        {
            InitializeComponent();            
        }

        /** 起動時処理*/
        private void Form1_Load(object sender, EventArgs e)
        {
            // タイトルの作成
            tableLayoutPanel1.RowCount = 1;
            string[] LABEL = { "通し番号", "学籍番号", "IP", "コピペ", "切断", "パス" };
            for (int i = 0; i < LABEL.Length; i++)
            {
                Label lbl = new Label();
                lbl.AutoSize = true;
                lbl.Text = LABEL[i];
                labels.Add(lbl);
                tableLayoutPanel1.Controls.Add(labels[labels.Count - 1], i, 0);
                // パス以外
                if (i < LABEL.Length - 1)
                {
                    //tableLayoutPanel1.ColumnStyles[i] = new ColumnStyle(SizeType.Absolute, lbl.Width * 2f);
                    tableLayoutPanel1.ColumnStyles[i] = new ColumnStyle(SizeType.AutoSize);
                }
                else
                {
                    tableLayoutPanel1.ColumnStyles[i] = new ColumnStyle(SizeType.AutoSize);
                }
            }

            // テスト表示
            printMember(testData);

            // UDP起動
            udp = new UdpClient(localPort);
            udp.DontFragment = true;    // 断片化を防ぐ
            udp.EnableBroadcast = true; // ブロードキャスト許可

            // 受信を開始
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), udp);

            // ポーリングの開始
            timer1.Enabled = true;
        }

        /** 受信コールバック*/
        public void ReceiveCallback(IAsyncResult ar)
        {
            if (udp == null || isClose) return;
            IPEndPoint remoteEP = null;
            Byte[] dat = udp.EndReceive(ar, ref remoteEP);
            string remoteIP = remoteEP.Address.ToString();
            string recv = remoteIP+":"+System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat);

            // 分析


            // 仮出力
            testData[0, 4] = recv;

            // 非同期開始
            udp.BeginReceive(ReceiveCallback, udp);
        }

        /** メンバーを表示*/
        void printMember(string [,] mems)
        {
            tableLayoutPanel1.Visible = false;
            for (int i = 0,lbidx=(int)COL.MAX ; i < mems.GetLength(0); i++,lbidx+=(int)COL.MAX)
            {
                for (int j = 0; j < (int)COL.MAX; j++)
                {
                    // ラベルが不足していたら作成
                    if (labels.Count < lbidx+(int)COL.MAX)
                    {
                        Label lbl = new Label();
                        lbl.AutoSize = true;
                        labels.Add(lbl);
                        tableLayoutPanel1.Controls.Add(labels[labels.Count-1], j, i+1);
                        tableLayoutPanel1.Controls[labels.Count-1].Anchor = anc[j];
                    }
                }

                // 行を出力
                printRow(i, mems);
            }
            tableLayoutPanel1.Visible = true;

        }

        /** 行を表示*/
        void printRow(int row, string[,] mems)
        {
            int lbidx = (row+1) * (int)COL.MAX;
            // インデックス
            labels[lbidx].Text = (row + 1).ToString();

            // データ
            for (int i = 0; i <mems.GetLength(1); i++)
            {
                labels[lbidx + i + 1].Text = mems[row, i];
            }
        }

        /** サイズ変更*/
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            tableLayoutPanel1.Width = ClientSize.Width;
        }

        /** 閉じる*/
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Enabled = false;

            isClose = true;
            udp.Close();
            udp = null;
        }

        /** ポーリング*/
        private void timer1_Tick(object sender, EventArgs e)
        {
            // データ更新
            printRow(0, testData);

            // ポーリングするか
            iPoringCount++;
            if (iPoringCount * timer1.Interval < PORING_MSEC)
            {
                return;
            }

            // ポーリング開始
            iPoringCount = 0;
            Ser++;
            Text = "試験サーバー(" + Ser + ")";
            Application.DoEvents();

            // ブロードキャストで送信
            Byte[] dat =
                System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes("call" + Ser);
            udp.Send(dat, dat.Length, "255.255.255.255", localPort);
        }
    }
}
