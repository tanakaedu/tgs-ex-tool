using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Windows.Forms;


/**
 * 1)TcpServerをnewする
 * 2)終了時は、closeTcpListenerを呼び出す
 */

namespace TgsExServer
{
    class TcpServer 
    {
        // ListenするIPポート
        const int TCP_PORT = 60100;
        // TCPサーバー
        TcpListener listener;
        // ループ
        bool isLoop = true;
        TextBox textStatus = null;

        /**
         * サーバーを開始
         */
        public void StartServer(TextBox tb) {
            textStatus = tb;

            IPAddress ipAddr = IPAddress.Any;
            // TcpListener
            listener = new TcpListener(ipAddr, TCP_PORT);

            // 開始
            listener.Start();

            // 接続開始
            WaitConnect();
        }

        /**
         * 受信データからファイル名を取り出す
         */
        string getFileName(MemoryStream mem)
        {
            // ファイル名のバイト数を取り出す
            int size = mem.ReadByte() & 0xff;
            byte[] bt = new byte[size];
            mem.Read(bt, 0, size);
            return Encoding.UTF8.GetString(bt);
        }

        /** パス文字列を生成
         * 受け取った保存先 > 学籍番号 > 日時
         * @return 空=無効 / パス文字列
         */
        string getSavePath(string ip)
        {
            if (Form1.form1.textBox1.Text.Length == 0)
            {
                return "";
            }
            string time = DateTime.Now.ToLongTimeString().Replace(':', '-');
            string sUID = Form1.form1.getUIDWithIP(ip);
            if (sUID.Length == 0)
            {
                textStatus.Text += "[" + ip + "]は未登録のIPです。\r\n";
                return "";
            }
            string path = Form1.form1.textBox1.Text + sUID + @"\" + time;
            for (int i = 0; i < 100; i++)
            {
                if (!Directory.Exists(path + "-" + i))
                {
                    return path + "-" + i;
                }
            }

            // 1秒で100個以上は無効
            return "";
        }

        /**
         * 受信データをからデータ本体を取り出す
         */
        byte[] getData(MemoryStream mem)
        {
            byte [] dt = new byte[mem.Length-mem.Position];
            mem.Read(dt, 0, dt.Length);
            return dt;
        }

        /**
         * クライアントからの接続を待機する
         */
        async void WaitConnect()
        {
            while (isLoop)
            {
                // 接続要求を受け入れる
                Task<TcpClient> taskClient = listener.AcceptTcpClientAsync();
                if (taskClient.Status == TaskStatus.Faulted) break;
                TcpClient client = await taskClient;
                string ip = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                textStatus.Text += "クライアント(" +ip
                    + ":" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port + ")と接続しました。\r\n";

                // NetworkStreamを取得
                NetworkStream stream = client.GetStream();

                // タイムアウトを10秒に設定
                stream.ReadTimeout = 10000;
                stream.WriteTimeout = 10000;

                // 受信データ容量を確認
                int fileSize = (stream.ReadByte() << 24) + (stream.ReadByte() << 16) + (stream.ReadByte() << 8) + stream.ReadByte();
                // サイズが異常な時はエラーにしておく
                if ((fileSize < 0) || (fileSize > 10000000))
                {
                    textStatus.Text += "ファイルサイズが異常なのでキャンセルします。\r\n";
                    stream.Close();
                    client.Close();
                    continue;
                }

                // クライアントからのデータを受け取る
                bool disconnected = false;
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                int resSize = 0;
                do
                {
                    // データの一部を受信
                    resSize = stream.Read(resBytes, 0, resBytes.Length);
                    // Readが0の時は切断
                    if (resSize == 0)
                    {
                        disconnected = true;
                        textStatus.Text += "クライアントが切断しました。\r\n";
                        break;
                    }
                    // 受信したデータを蓄積
                    ms.Write(resBytes, 0, resSize);
                }
                while (stream.DataAvailable || (ms.Length<fileSize-4));

                // 受信データを保存
                ms.Position = 0;
                string fname = getSavePath(ip);
                if (fname.Length == 0)
                {
                    textStatus.Text += "保存先が無効です。\r\n";
                    ms.Close();
                    stream.Close();
                    client.Close();
                    continue;
                }
                if (!Directory.Exists(fname))
                {
                    Directory.CreateDirectory(fname);
                }
                fname += @"\"+getFileName(ms);
                byte [] savedata = getData(ms);
                File.WriteAllBytes(fname, savedata);
                ms.Close();

                // 出力履歴
                textStatus.Text += fname+":"+savedata.Length + "bytes\r\n";

                if (!disconnected)
                {
                    // クライアントにデータ送信
                    string sendMsg = savedata.Length.ToString();
                    byte[] sendBytes = Encoding.UTF8.GetBytes(sendMsg + '\n');
                    stream.Write(sendBytes, 0, sendBytes.Length);
                    textStatus.Text += "send "+sendMsg+"bytes\r\n";
                }

                // 閉じる
                stream.Close();
                client.Close();
                textStatus.Text += "クライアントとの接続を閉じました。\r\n";
            }
            // リスナを閉じる
            closeTcpListener();
        }

        /** リスナーを閉じる*/
        public void closeTcpListener()
        {
            isLoop = false;
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
        }
    }
}
