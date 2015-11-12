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
        // 送信回数
        int Ser = 0;

        // UDP
        IPAddress localAddress = IPAddress.Parse("127.0.0.1");
        int localPort = 60000;
        IPEndPoint localEP;
        UdpClient udp;
        
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

        public Form1()
        {
            InitializeComponent();            
        }

        /** メンバーを更新する*/
        void updateMember()
        {
            // 既存のテキストボックスを
        }

        // 各列の配置
        AnchorStyles[] anc = {
            AnchorStyles.Right,
            AnchorStyles.Left,
            AnchorStyles.Left,
            AnchorStyles.Right,
            AnchorStyles.Left,
            AnchorStyles.Left
                        };

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

        /** 起動時処理*/
        private void Form1_Load(object sender, EventArgs e)
        {
            // タイトルの作成
            tableLayoutPanel1.RowCount = 1;
            string [] LABEL = {"通し番号", "学籍番号", "IP", "コピペ", "切断", "パス"};
            for (int i=0 ; i<LABEL.Length ; i++) {
                Label lbl = new Label();
                lbl.AutoSize = true;
                lbl.Text = LABEL[i];
                labels.Add(lbl);
                tableLayoutPanel1.Controls.Add(labels[labels.Count-1], i,0);
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
            localEP = new IPEndPoint(localAddress, localPort);
            udp = new UdpClient(localEP);
            
            // 受信を開始
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), udp);

            // ポーリングの開始
            timer1.Enabled = true;
        }

        /** 受信コールバック*/
        public void ReceiveCallback(IAsyncResult ar)
        {
            if (udp == null) return;
            IPEndPoint ipAny = new IPEndPoint(IPAddress.Any, localPort);
            Byte[] dat = udp.EndReceive(ar, ref ipAny);
            string recv = System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat);

            // 解体

            // 設定
            testData[0,5] = recv;
            printRow(0, testData);

            // 非同期開始
            udp.BeginReceive(ReceiveCallback, udp);
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

            udp.Close();
            udp = null;
        }

        /** ポーリング*/
        private void timer1_Tick(object sender, EventArgs e)
        {
            Ser++;
            Text = "試験サーバー(" + Ser + ")";
            Application.DoEvents();

            // ブロードキャストで送信
            Byte[] dat =
                System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes("call" + Ser);
            udp.Send(dat, dat.GetLength(0));
        }

    }
}
