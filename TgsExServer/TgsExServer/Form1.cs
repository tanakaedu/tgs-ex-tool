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
        // デバッグデータの利用
        const bool USE_DEBUG = true;

        // ポーリング間隔
        const long PORING_MSEC = 3000;
        // カウンタ
        int iPoringCount = 0;

        // 送信回数
        int Ser = 0;

        // UDP
        int localPort = 60000;
        UdpClient udp;

        /** 終了*/
        bool isClose = false;
        
        /** テストデータ*/
        string [][] testData ={
            new string[] {"21531000","123.123.123.123","0","o","path"},
            new string[] {"21531999","124.124.124.124","1","o","path2"}
        };

        /** ラベルリスト*/
        List<Label[]> labels = new List<Label[]>();
        /** 更新行フラグ*/
        List<bool> updates = new List<bool>();

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

        /** 指定の文字列配列の行を新しく作成して、テーブルの最後に追加する
         */
        void addMember(string[] indata)
        {
            int row = labels.Count;
            // ラベルを作成して記録しておく
            string[] datas = {
                                 ""+row,
                                 indata[0],
                                 indata[1],
                                 indata[2],
                                 indata[3],
                                 indata[4]
                             };

            labels.Add(createLabels(datas));
            // 更新フラグを追加
            updates.Add(true);
            // テーブルに追加
            for (int i = 0; i < (int)COL.MAX; i++)
            {
                int w = tableLayoutPanel1.Controls.Count;
                tableLayoutPanel1.Controls.Add(labels[row][i], i, row);
                tableLayoutPanel1.Controls[row*(int)COL.MAX+i].Anchor = anc[i];
            }
        }

        /**
         * 指定の文字列から、ラベルの配列を作成した返す
         */
        Label[] createLabels(string[] datas)
        {
            Label[] ret = new Label[datas.Length];
            for (int i = 0; i < datas.Length; i++)
            {
                ret[i] = new Label();
                ret[i].AutoSize = true;
                ret[i].Text = datas[i];
            }

            return ret;
        }

        /** 起動時処理*/
        private void Form1_Load(object sender, EventArgs e)
        {
            // タイトルの作成
            tableLayoutPanel1.RowCount = 1;
            string[] LABEL = { "通し番号", "学籍番号", "IP", "コピペ", "接続", "パス" };
            labels.Add(createLabels(LABEL));

            for (int i = 0; i < LABEL.Length; i++)
            {
                tableLayoutPanel1.Controls.Add(labels[0][i], i, 0);
                // 自動調整
                tableLayoutPanel1.ColumnStyles[i] = new ColumnStyle(SizeType.AutoSize);
            }

            // テストデータを利用する
            if (USE_DEBUG)
            {
                // テストデータを登録する
                for (int i = 0; i < testData.GetLength(0); i++)
                {
                    addMember(testData[i]);
                }
            }

            // 表示
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

        /**
         * 指定のIP文字列を数値化して返す
         */
        long ip2long(string ip)
        {
            string[] ips = ip.Split(new char[] { '.' });
            return long.Parse(ips[0]) * 0x1000000L
                + long.Parse(ips[1]) * 0x10000L
                + long.Parse(ips[2]) * 0x100L
                + long.Parse(ips[3]);
        }

        /** 受信コールバック*/
        public void ReceiveCallback(IAsyncResult ar)
        {
            if (udp == null || isClose) return;
            IPEndPoint remoteEP = null;
            Byte[] dat = udp.EndReceive(ar, ref remoteEP);
            string remoteIP = remoteEP.Address.ToString();
            string recv = remoteIP+":"+System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat);

            // 既存のメンバーかをIPで確認
            int lbidx = -1;
            for (int i = 0; i < labels.Count; i++)
            {
                if (remoteIP == labels[i][(int)COL.IP].Text)
                {
                    lbidx = i;
                    break;
                }
            }

            // 見つからなかった時は、新規に追加する


            // コンマで分解
            string[] recvs = recv.Split(new char[] { ',' });

            // シャットダウンか
            if (recvs[0] == "shutdown")
            {

            }


            // 仮出力
            testData[0][4] = recv;

            // 非同期開始
            udp.BeginReceive(ReceiveCallback, udp);
        }

        /** メンバーを表示*/
        void printMember(string [][] mems)
        {
            tableLayoutPanel1.Visible = false;
            for (int i = 0 ; i < mems.GetLength(0); i++)
            {
                if (updates[i])
                {
                    updates[i] = false;
                    // 行を出力
                    printRow(i, mems);
                }
            }
            tableLayoutPanel1.Visible = true;
        }

        /** 行を表示*/
        void printRow(int row, string[][] mems)
        {
            // データ
            for (int i = 0; i <mems[0].Length; i++)
            {
                labels[row + 1][i+1].Text = mems[row][i];
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
