
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
namespace 試験登録
{
    class TcpClient
    {
        const int TCP_PORT = 60100;

        /**
         * スクリーンショットを確保してTCP/IPで送信
         */
        public void SendScreenShot(string ip, string fname) {
            byte[] scshot = Capture();
            SendTcp(ip, fname, scshot);
        }

        /**
         * 送信
         * @param string ipOrHost サーバーのIP
         * @param string ファイル名
         * @param byte[] 送信するデータ
         */
        public void SendTcp(string ipOrHost, string fname, byte[] data)
        {
            Encoding enc = Encoding.UTF8;

            System.Net.Sockets.TcpClient tcp = new System.Net.Sockets.TcpClient(ipOrHost, TCP_PORT);
            Console.WriteLine("サーバー({0}:{1})と接続しました({2}:{3})。",
            ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Address,
            ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Port,
            ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address,
            ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Port);

            //NetworkStreamを取得する
            System.Net.Sockets.NetworkStream ns = tcp.GetStream();

            //読み取り、書き込みのタイムアウトを10秒にする
            //デフォルトはInfiniteで、タイムアウトしない
            //(.NET Framework 2.0以上が必要)
            ns.ReadTimeout = 10000;
            ns.WriteTimeout = 10000;

            //サーバーにデータを送信する
            //データを送信する
            byte[] sendBytes = getSendData(fname, data);
            ns.Write(sendBytes, 0, sendBytes.Length);

            //サーバーから送られたデータを受信する
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] resBytes = new byte[256];
            int resSize = 0;
            do
            {
                //データの一部を受信する
                resSize = ns.Read(resBytes, 0, resBytes.Length);
                //Readが0を返した時はサーバーが切断したと判断
                if (resSize == 0)
                {
                    Console.WriteLine("サーバーが切断しました。");
                    break;
                }
                //受信したデータを蓄積する
                ms.Write(resBytes, 0, resSize);
                //まだ読み取れるデータがあるか、データの最後が\nでない時は、
                // 受信を続ける
            } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');
            //受信したデータを文字列に変換
            string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();
            //末尾の\nを削除
            resMsg = resMsg.TrimEnd('\n');
            Console.WriteLine(resMsg);

            //閉じる
            ns.Close();
            tcp.Close();
            Console.WriteLine("切断しました。");

            Console.ReadLine();
        }

        /**
         * スクリーンショットして、PNGのバイト配列を返す
         */
        byte[] Capture()
        {
            //Bitmapの作成
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //画面全体をコピーする
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), bmp.Size);
            //解放
            g.Dispose();

            //表示
            MemoryStream mem = new MemoryStream();
            bmp.Save(mem, System.Drawing.Imaging.ImageFormat.Png);
            byte[] dt = mem.ToArray();
            bmp.Dispose();
            return dt;
        }

        /**
         * 送信するデータをパッケージしてbyte配列にして返す
         * 0.int ファイルの全容量(big endian)
         * 4.byte ファイル名(UTF-8)のバイト数
         * ファイル名
         * データ配列 
         */
        byte[] getSendData(string fname, byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            // ファイル名の生成
            byte[] arrayfname = Encoding.UTF8.GetBytes(fname);
            // 全体サイズ(4byte+1byte+ファイル名+データ)
            int size = 4+1+arrayfname.Length+data.Length;
            // データ容量
            ms.WriteByte((byte)((size >> 24) & 0xff));
            ms.WriteByte((byte)((size >> 16) & 0xff));
            ms.WriteByte((byte)((size >> 8) & 0xff));
            ms.WriteByte((byte)((size) & 0xff));
            // ファイル名容量
            ms.WriteByte((byte)arrayfname.Length);
            // ファイル名
            ms.Write(arrayfname, 0, arrayfname.Length);
            // データ
            ms.Write(data, 0, data.Length);

            return ms.ToArray();
        }

    }
}
