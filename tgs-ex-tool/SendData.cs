using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 試験登録
{
    public class SendData {
        static List<SendDataItem> lists = new List<SendDataItem>();
        /** 連続で記録できないミリ秒*/
        const long ADD_MIN_INTERVAL = 300;
        /** 時間計測*/
        static Stopwatch sw = null;
        /** TCP送信クラス*/
        static TcpClient tcpClient = new TcpClient();

        /** データを登録する。一定時間以内の場合は古いデータを破棄する*/
        public static void Add(byte[] scr, byte[] copy) {
            if (sw == null)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            // 時間をチェック
            if (lists.Count > 0)
            {
                if (sw.ElapsedMilliseconds < lists[lists.Count-1].recTime + ADD_MIN_INTERVAL)
                {
                    // 最後のデータを削除
                    lists.RemoveAt(lists.Count - 1);
                }
            }
            // データを追加
            lists.Add(new SendDataItem(scr, copy, sw.ElapsedMilliseconds));
        }

        /** 送信処理*/
        public static bool Send(string ip)
        {
            // データがあれば送信
            if (lists.Count > 0) {
                SendDataItem item = lists[0];
                if (!tcpClient.SendTcp(ip, "scr.png", item.scrShot))
                {
                    return false;
                }
                if (!tcpClient.SendTcp(ip, "copy.txt", item.copyText))
                {
                    return false;
                }
                // 成功したので、最初のデータを削除
                lists.RemoveAt(0);
                return true;
            }

            return false;
        }
    }

    /** 送信するデータを保持するためのクラス*/
    class SendDataItem
    {
        public SendDataItem(byte[] scr, byte[] copy, long tm)
        {
            scrShot = scr;
            copyText = copy;
            recTime = tm;
        }
        public byte[] scrShot { get; set; }
        public byte[] copyText { get; set; }
        /** 記録したストップウォッチ時間*/
        public long recTime { get; set; }
        /**
         * データをTCPで送信
         * @return true=成功 / false=失敗
         */
        public bool Send(TcpClient client, string ip)
        {
            if (!client.SendTcp(ip, "scr.png", scrShot)) return false;
            if (!client.SendTcp(ip, "copy.txt", copyText)) return false;
            return true;
        }
    }

}
