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

        public Form1()
        {
            InitializeComponent();
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
    }
}
