using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace 試験登録
{
    public partial class Form1 : Form
    {
        /** 設定ファイル*/
        string sSaveFolder = "";
        /** 送信学籍番号*/
        string sUID = "";

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


            string config = Path.GetDirectoryName(Application.ExecutablePath)+"\\config.dat";
            if (!File.Exists(config))
            {
                MessageBox.Show("実行ファイルと同じ場所にconfig.datがありません。");
                Application.Exit();
                return;
            }

            // 設定読み込み
            sSaveFolder = File.ReadAllText(config);
            sSaveFolder = Path.GetDirectoryName(sSaveFolder.Trim())+"\\";
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
    }
}
