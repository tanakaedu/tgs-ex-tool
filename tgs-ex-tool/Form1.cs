// http://dobon.net/vb/dotnet/graphics/screencapture.html
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace 試験登録
{
    public partial class Form1 : Form
    {
        /** 設定ファイル*/
        string sSaveFolder = "";
        /** 送信学籍番号*/
        string sUID = "";
        /** UDPクライアント*/
        UdpClient client = null;
        /** UDPポート*/
        const int CLIENT_PORT = 60001;
        /** サーバーポート*/
        const int SERVER_PORT = 60000;
        /** サーバーIP*/
        string remoteIP = "";
        /** 閉じる処理*/
        bool isClose = false;
        /** コピーペースト回数*/
        int iCopyPasteCount = 0;

        /** イベントハンドら*/
        private MyClipboardViewer viewer;

        public Form1()
        {
            viewer = new MyClipboardViewer(this);
            // イベントハンドラの登録
            viewer.ClipboardHandler += this.OnClipBoardChanged;
            InitializeComponent();
        }

        // クリップボードにテキストがコピーされると呼び出される
        private void OnClipBoardChanged(object sender, ClipbardEventArgs args)
        {
            // 保存先確認
            string path = getSavePath();
            if (path.Length == 0) return;

            // フォルダー作成
            Directory.CreateDirectory(path);

            // テキスト保存
            File.WriteAllText(path + "\\copy.txt", args.Text);
            // スクリーンショット保存
            ScreenShot(path + "\\scr.png");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 多重起動チェック
            if (System.Diagnostics.Process.GetProcessesByName(
                System.Diagnostics.Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                // すでに起動
                MessageBox.Show("起動済みです。");
                Application.Exit();
                return;
            }
        }

        /** 受信データのインデックス*/
        enum RECV
        {
            MES,
            SER,
            PATH,
            MAX
        }

        /** 受信コールバック*/
        public void ReceiveCallback(IAsyncResult ar)
        {
            if (client == null || isClose) return;

            IPEndPoint remoteEP = null;
            Byte[] dat = client.EndReceive(ar, ref remoteEP);
            remoteIP = remoteEP.Address.ToString();
            string recv = System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat);

            // コンマで分解
            string[] recvs = recv.Split(new char[] { ',' });

            // 呼び出しか確認
            if ((recvs[0] == "call") && (recvs.Length == (int)RECV.MAX))
            {
                // 保存先パスを受け取る
                sSaveFolder = recvs[(int)RECV.PATH];

                // サーバー相手に送り返す
                Byte[] send =
                    System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes(
                    recvs[(int)RECV.SER] + ","
                    + sUID+","
                    + iCopyPasteCount);
                client.Send(send, send.Length, remoteIP,SERVER_PORT);
            }

            // 非同期開始
            client.BeginReceive(ReceiveCallback, client);
        }

        /** ボタンが押された*/
        private void btnEntry_Click(object sender, EventArgs e)
        {
            // 全角だったら半角に変換して記録
            sUID = convZen2Han(textUID.Text);
            // メッセージ
            MessageBox.Show(sUID + "：登録しました。");

            // フォームを最小化
            this.WindowState = FormWindowState.Minimized;

            // ボタン名変更
            btnEntry.Text = "学籍番号を変更";

            // タスクバーから隠す
            this.ShowInTaskbar = false; 

            // 受信の開始
            if (client == null)
            {
                client = new UdpClient(CLIENT_PORT);
                client.DontFragment = true;
                client.EnableBroadcast = true;
                // 受信開始
                client.BeginReceive(ReceiveCallback, client);
            }
        }

        /** 全角＞半角数字変換*/
        private string convZen2Han(string str)
        {
            string zen = "０１２３４５６７８９";
            string han = "0123456789";
            string ret = "";
            for (int i = 0; i < str.Length; i++)
            {
                string s = str.Substring(i, 1);
                int idx = zen.IndexOf(s);
                if (idx == -1)
                {
                    ret += s;
                }
                else
                {
                    ret += han.Substring(idx, 1);
                }
            }
            return ret;
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            // フォームを復帰
            this.WindowState = FormWindowState.Normal;
        }

        /** 数値8桁入るまで無効*/
        private void textUID_TextChanged(object sender, EventArgs e)
        {
            btnEntry.Enabled = false;

            if (textUID.Text.Length > 0)
            {
                string han = convZen2Han(textUID.Text);
                try
                {
                    int iuid = int.Parse(han);

                    // 桁数チェック
                    if (iuid >= 20000000)
                    {
                        btnEntry.Enabled = true;
                    }
                }
                catch (Exception ee)
                {
                    ee.ToString();
                }
            }
        }

        /** フォームを閉じる処理*/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null && !isClose && remoteIP.Length > 0)
            {
                Byte[] send =
                    System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes(
                    "shutdown,"
                    + sUID);
                client.Send(send, send.Length, remoteIP, SERVER_PORT);
            }
        }

        /** パス文字列を生成
         * 受け取った保存先 > 学籍番号 > 日時
         * @return 空=無効 / パス文字列
         */
        string getSavePath()
        {
            if (sSaveFolder.Length == 0)
            {
                return "";
            }
            string time = DateTime.Now.ToLongTimeString().Replace(':','-');
            string path = sSaveFolder + sUID + "\\" + time;
            for (int i = 0; i<10 ; i++)
            {
                if (!Directory.Exists(path+"-"+i)) {
                    return path + "-" + i;
                }
            }

            // 1秒で10個以上は無効
            return "";
        }


        private const int SRCCOPY = 13369376;
        private const int CAPTUREBLT = 1073741824;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr hDestDC,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hSrcDC,
            int xSrc,
            int ySrc,
            int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd,
            ref  RECT lpRect);

        /** 指定のパスにスクリーンショットを保存*/
        void ScreenShot(string path)
        {
            //アクティブなウィンドウのデバイスコンテキストを取得
            IntPtr hWnd = GetForegroundWindow();
            IntPtr winDC = GetWindowDC(hWnd);
            //ウィンドウの大きさを取得
            RECT winRect = new RECT();
            GetWindowRect(hWnd, ref winRect);
            

            //Bitmapの作成
            Bitmap bmp = new Bitmap(winRect.right - winRect.left,
                winRect.bottom - winRect.top);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);

            //Graphicsのデバイスコンテキストを取得
            IntPtr hDC = g.GetHdc();
            //Bitmapに画像をコピーする
            BitBlt(hDC, 0, 0, bmp.Width, bmp.Height,
                winDC, 0, 0, SRCCOPY);
            //解放
            g.ReleaseHdc(hDC);
            g.Dispose();
            ReleaseDC(hWnd, winDC);

            // 保存
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
