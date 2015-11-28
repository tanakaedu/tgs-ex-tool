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
using System.IO;

namespace TgsExServer
{
    public partial class Form1 : Form
    {
        public static Form1 form1;

        // デバッグデータの利用
        const bool USE_DEBUG = false;

        // ポーリング間隔
        const long PORING_MSEC = 30000;
        // カウンタ
        int iPoringCount = 0;

        // 送信回数
        int Ser = 0;

        /** UDPポート*/
        const int CLIENT_PORT = 60001;
        /** サーバーポート*/
        const int SERVER_PORT = 60000;
        /** UDPクライアント*/
        UdpClient udp = null;

        /** 終了*/
        bool isClose = false;
        
        /** テストデータ*/
        string [][] testData ={
            new string[] {"21531999","124.124.124.124","1","o"},
            new string[] {"21531000","123.123.123.123","0","o"}
        };

        /** 見出し用ラベル*/
        List<Label[]> labelsHeader = new List<Label[]>();
        /** ラベルリスト*/
        List<Label[]> labels = new List<Label[]>();
        /** 受信したデータ*/
        List<string[]> recvDatas = new List<string[]>();

        /** 列名*/
        enum COL
        {
            INDEX,
            UID,
            IP,
            COUNT,
            ALERT,
            MAX
        };

        // 各列の配置
        AnchorStyles[] anc = {
            AnchorStyles.Right,
            AnchorStyles.Left,
            AnchorStyles.Left,
            AnchorStyles.Right,
            AnchorStyles.Left
                        };

        /** TCP通信用クラス*/
        TcpServer tcpServer = null;

        /** コンストラクタ*/
        public Form1()
        {
            form1 = this;
            InitializeComponent();
        }

        /** 
         * 指定の文字列配列の行を新しく作成して、テーブルの最後に追加する
         * @param string[4] indata 学籍番号からALERTまでのデータ。インデックスは含まない
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
                                 indata[3]
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
            // 設定読み込み
            string config = Path.GetDirectoryName(Application.ExecutablePath) + "\\config.dat";
            if (!File.Exists(config))
            {
                textBox1.Text = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + textBox1.Text+"\\";
                MessageBox.Show("実行ファイルと同じ場所にconfig.datがないのでデフォルトフォルダに保存します。");
            }
            else
            {
                textBox1.Text = File.ReadAllText(config);
            }
            textBox1.Text = Path.GetDirectoryName(textBox1.Text.Trim()) + "\\";

            // タイトルの作成
            tableLayoutPanel1.RowCount = 1;
            string[] LABEL = { "通し番号", "学籍番号", "IP", "コピペ", "接続" };
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
            for (int i = 0; i < labels.Count; i++)
            {
                if (ip == getLabelsText(i,COL.IP))
                {
                    // 見つかった
                    return i;
                }
            }
            return -1;
        }

        /** 指定のIPに対応するユーザーの学籍番号を返す
         */
        public string getUIDWithIP(string ip)
        {
            int idx = getUserIndexWithIP(ip);
            if (idx == -1)
            {
                return "";
            }
            return getLabelsText(idx, COL.UID);
        }

        /** 受信コールバック*/
        public void ReceiveCallback(IAsyncResult ar)
        {
            if (udp == null || isClose) return;
            IPEndPoint remoteEP = null;
            Byte[] dat = udp.EndReceive(ar, ref remoteEP);
            string recv = System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat);
            string [] recvs = recv.Split(new char[] { ',' });
            string [] recvsip = new string[recvs.Length+1];
            Array.Copy(recvs,recvsip,recvs.Length);
            recvsip[recvs.Length] = remoteEP.Address.ToString();

            // IPを追加したデータを登録
            recvDatas.Add(recvsip) ;

            // 非同期開始
            udp.BeginReceive(ReceiveCallback, udp);
        }

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
            if (udp != null)
            {
                udp.Close();
            }
            udp = null;
        }

        /** 受信データの処理とポーリング*/
        private void timer1_Tick(object sender, EventArgs e)
        {
            // 受信データの処理
            procRecvs();

            // ポーリングするか
            iPoringCount++;
            if (iPoringCount * timer1.Interval < PORING_MSEC)
            {
                return;
            }

            sendCall();
        }

        /**
         * クライアント呼び出しを送信する
         */
        void sendCall()
        {
            // ポーリング開始
            iPoringCount = 0;
            Ser++;
            Text = "試験サーバー(" + Ser + ")";
            Application.DoEvents();

            // ブロードキャストで送信
            Byte[] dat =
                System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes("call," + Ser + "," + textBox1.Text);
            udp.Send(dat, dat.Length, "255.255.255.255", CLIENT_PORT);
        }

        /** 受信データを処理*/
        void procRecvs()
        {
            string[] rowstrings = new string[(int)COL.ALERT];
            bool isChange = false;

            while (recvDatas.Count > 0) {
                // 登録済みか確認
                int idx = getUserIndexWithIP(recvDatas[0][recvDatas[0].Length-1]);

                // シャットダウン
                if (recvDatas[0][0] == "shutdown")
                {
                    if (idx != -1)
                    {
                        setLabelsText(idx, COL.ALERT, "x");
                        // 学籍番号を登録
                        setLabelsText(idx, COL.UID, recvDatas[0][1]);
                        // 変更フラグ
                        isChange = true;
                    }
                }
                // データを受信(受信データ+IP)
                else if (recvDatas[0].Length == (int)(COL.ALERT))
                {
                    if (idx == -1)
                    {
                        // 新規なので登録
                        rowstrings[0] = recvDatas[0][1];    // UID
                        rowstrings[1] = recvDatas[0][3];    // IP
                        rowstrings[2] = recvDatas[0][2];    // コピペ回数
                        rowstrings[3] = "o(" + recvDatas[0][0] + ")";   // 通信(シリアル)
                        addMember(rowstrings);
                    }
                    else
                    {
                        // 既存のメンバー
                        setLabelsText(idx, COL.IP, recvDatas[0][3]);    // IP
                        setLabelsText(idx, COL.UID, recvDatas[0][1]);
                        setLabelsText(idx, COL.ALERT, "o" + "(" + recvDatas[0][0] + ")");
                        setLabelsText(idx, COL.COUNT, recvDatas[0][2]);
                    }
                    // 更新
                    isChange = true;
                }
                // それ以外
                else
                {
                    // 呼び出し以外の時はエラー
                    if (recvDatas[0][0] != "call")
                    {
                        textBox2.Text += recvDatas[0][3] + " : ";
                        for (int i = 0; i < recvDatas[0].Length; i++)
                        {
                            if (i > 0)
                            {
                                textBox2.Text += ",";
                            }
                            textBox2.Text += recvDatas[0][i];
                        }
                        textBox2.Text += "\r\n";
                    }
                }

                // 処理したデータを削除
                recvDatas.RemoveAt(0);
            }

            // 変更があったらソート実行
            if (isChange)
            {
                sortLabels();
            }
        }

        /** 開始*/
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "開始")
            {
                // UDP起動
                udp = new UdpClient(SERVER_PORT);
                udp.DontFragment = true;    // 断片化を防ぐ
                udp.EnableBroadcast = true; // ブロードキャスト許可

                // 受信を開始
                udp.BeginReceive(new AsyncCallback(ReceiveCallback), udp);

                // ボタン変更
                button1.Text = "再送";

                // ポーリングの開始
                timer1.Enabled = true;

                // TCP開始
                if (tcpServer == null)
                {
                    tcpServer = new TcpServer();
                    tcpServer.StartServer(textBox2);
                }
            }

            // 送信実行
            sendCall();
        }

        /** フォームを閉じる*/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // TCPを閉じる
            if (tcpServer != null)
            {
                tcpServer.closeTcpListener();
                tcpServer = null;
            }
        }
    }
}
