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
            new string[] {"21531999","124.124.124.124","1","o","path2"},
            new string[] {"21531000","123.123.123.123","0","o","path"}
        };

        /** 見出し用ラベル*/
        List<Label[]> labelsHeader = new List<Label[]>();
        /** ラベルリスト*/
        List<Label[]> labels = new List<Label[]>();

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

        /** メッセージ用フォーム*/
        MessageForm messageForm = new MessageForm();

        /** コンストラクタ*/
        public Form1()
        {
            InitializeComponent();
        }

        /** 
         * 指定の文字列配列の行を新しく作成して、テーブルの最後に追加する
         * @param string[5] indata 学籍番号からパスまでのデータ。インデックスは含まない
         */
        void addMember(string[] indata)
        {
            int row = labels.Count;
            // ラベルを作成して記録しておく
            string[] datas = {
                                 ""+(row+1),
                                 indata[0],
                                 indata[1],
                                 indata[2],
                                 indata[3],
                                 indata[4]
                             };

            labels.Add(createLabels(datas));
            // テーブルに追加
            for (int i = 0; i < (int)COL.MAX; i++)
            {
                tableLayoutPanel1.Controls.Add(labels[row][i], i, row+1);
                tableLayoutPanel1.Controls[(row+1)*((int)COL.MAX)+i].Anchor = anc[i];
            }
        }

        /**
         * 指定の文字列から、ラベルの配列を作成した返す
         */
        Label[] createLabels(string[] datas)
        {
            Label[] ret = new Label[datas.Length+1];
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
            labelsHeader.Add(createLabels(LABEL));

            for (int i = 0; i < LABEL.Length; i++)
            {
                tableLayoutPanel1.Controls.Add(labelsHeader[0][i], i, 0);
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
                sortLabels();
            }

            // UDP起動
            udp = new UdpClient(localPort);
            udp.DontFragment = true;    // 断片化を防ぐ
            udp.EnableBroadcast = true; // ブロードキャスト許可

            // 受信を開始
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), udp);

            // ポーリングの開始
            timer1.Enabled = true;

            // メッセージ用フォーム表示
            messageForm.Show();
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

        /**
         * 指定のユーザー番目の列文字を返す
         * @return 文字列
         */
        string getLabelsText(int idx, COL col)
        {
            return labels[idx][(int)col].Text;
        }

        /** 指定のユーザー番目の指定の列に文字を設定する*/
        void setLabelsText(int idx, COL col, string data)
        {
            labels[idx][(int)col].Text = data;
        }

        /** 指定のIPが既存のラベルに含まれているかを確認して、インデックスを返す
         * @return -1=見つからない / 0以上はメンバーのインデックス
         */
        int getUserIndexWithIP(string ip)
        {
            for (int i = 0; i < labels.Count-1; i++)
            {
                if (ip == getLabelsText(i,COL.IP))
                {
                    // 見つかった
                    return i;
                }
            }
            return -1;
        }

        /** 受信コールバック*/
        public void ReceiveCallback(IAsyncResult ar)
        {
            if (udp == null || isClose) return;
            IPEndPoint remoteEP = null;
            Byte[] dat = udp.EndReceive(ar, ref remoteEP);
            string remoteIP = remoteEP.Address.ToString();
            string recv = System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat);

            // コンマで分解
            string[] recvs = recv.Split(new char[] { ',' });
            string[] rowstrings = new string[(int)COL.MAX];

            // 登録済みか確認
            int idx = getUserIndexWithIP(remoteIP);

            // シャットダウン
            if (recvs[0] == "shutdown")
            {
                if (idx != -1)
                {
                    setLabelsText(idx, COL.ALERT, "x");
                    setLabelsText(idx, COL.UID, rowstrings[1]);
                    // ソート
                    sortLabels();
                }
            }
            // データを受信
            else if (recvs.Length == 4) {
                if (idx == -1) {
                    // 新規なので登録
                    rowstrings[0] = recvs[1]+"("+recvs[0]+")";
                    rowstrings[1] = remoteIP;
                    rowstrings[2] = recvs[2];
                    rowstrings[3] = "o";
                    rowstrings[4] = recvs[3];
                    addMember(rowstrings);
                }
                else {
                    // 既存のメンバー
                    setLabelsText(idx,COL.IP,remoteIP);
                    setLabelsText(idx,COL.UID,rowstrings[1]+"("+rowstrings[0]+")");
                    setLabelsText(idx,COL.ALERT,"o");
                    setLabelsText(idx,COL.COUNT,rowstrings[2]);
                    setLabelsText(idx,COL.PATH,rowstrings[3]);
                }
                // ソート
                sortLabels();
            }
            // それ以外
            else {
                // 呼び出し以外の時はエラー
                if (recvs[0] != "call")
                {
                    messageForm.sMes += remoteIP + " : ";
                    for (int i = 0; i < recvs.Length; i++)
                    {
                        if (i > 0)
                        {
                            messageForm.sMes += ",";
                        }
                        messageForm.sMes += recvs[i];
                    }
                    messageForm.sMes += "\r\n";
                }
            }

            // 非同期開始
            udp.BeginReceive(ReceiveCallback, udp);
        }

        /** 更新されたメンバーを表示*/
        /*
        void printMember()
        {
            tableLayoutPanel1.Visible = false;
            for (int i = 0 ; i < (labels.Count/(int)COL.MAX)-1 ; i++)
            {
                if (getLabelsText(i,COL.UPDATE).Length > 0)
                {
                    setLabelsText(i,COL.UPDATE,"");
                    // 行を出力
                    printRow(i, mems);
                }
            }
            tableLayoutPanel1.Visible = true;
        }*/

        /** 行を表示*/
        /*
        void printRow(int row, string[][] mems)
        {
            // データ
            for (int i = 0; i <mems[0].Length; i++)
            {
                labels[row + 1][i+1].Text = mems[row][i];
            }
        }
         */

        /**
         * ラベルを並び替える
         */
        void sortLabels()
        {
            // 文字列をコピー
            List<string[]> clone = new List<string[]>();
            for (int i = 0; i < labels.Count; i++)
            {
                string[] lines = new string[(int)COL.MAX];
                for (int j = 0; j < (int)COL.MAX; j++)
                {
                    lines[j] = labels[i][j].Text;
                }
                clone.Add(lines);
            }

            // ソート
            clone.Sort((a, b) => int.Parse(a[(int)COL.UID])- int.Parse(b[(int)COL.UID]));
            // ラベルの中身を入れ替える
            for (int i=0 ; i<labels.Count ;i++) {
                if (labels[i][(int)COL.IP].Text != clone[i][(int)COL.IP])
                {
                    for (int j = 1; j < (int)COL.MAX; j++)
                    {
                        labels[i][j].Text = clone[i][j];
                    }
                }
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
            //printRow(0, testData);

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
                System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes("call," + Ser);
            udp.Send(dat, dat.Length, "255.255.255.255", localPort);
        }
    }
}
