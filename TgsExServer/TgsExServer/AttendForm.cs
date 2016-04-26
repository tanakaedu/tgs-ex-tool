using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TgsExServer
{
    public partial class AttendForm : Form
    {
        /** 座席コード*/
        private int [,] CHAIR_CODE = new int[,] {
            {6,5,4,3,2,1},
            {12,11,10,9,8,7},
            {18,17,16,15,14,13},
            {24,23,22,21,20,19},
            {30,29,28,27,26,25},
            {36,35,34,33,32,31},
            {-1,-1,40,39,38,37},
        };
   
        public const int DEFAULT_FONT_SIZE = 34;

        /** デフォルトのフォントサイズ*/
        private int fontSize = DEFAULT_FONT_SIZE;
        /** 先頭座席*/
        private int firstIP = 101;
        /** 描画するラベルのリスト*/
        List<Label[]> drawLabels = null;

        public AttendForm()
        {
            InitializeComponent();
        }

        /**
         * テキストの大きさを変更する
         */
        public void changeTextSize(int size)
        {
            fontSize = size;
            setMember();
        }

        private void AttendForm_Load(object sender, EventArgs e)
        {
            /*
            webBrowser1.DocumentText = 
               "<html><body>Please enter your name:<br/>" +
               "<input type='text' name='userName'/><br/>" +
               "<a href='http://www.microsoft.com'>continue</a>" +
               "</body></html>";

            
            webBrowser1.Navigating +=
                new WebBrowserNavigatingEventHandler(webBrowser1_Navigating);
             */
            setMember(null);
        }

        public void setMember()
        {
            setMember(drawLabels);
        }

        /**
         * 受信データから出席者一覧を表示
         */
        public void setMember(List<Label[]> labels)
        {
            string[] COLOR = { "#fff", "#dee", "black", "black", "#888", "#688"};
            drawLabels = labels;
            string html = "<html>";
            html += "<head><style type='text/css'>";
            html += "tr { font-size: "+fontSize+"; text-align: center; }";
            html += "</style>";
            html += "<body><font size='large'><table border='1'>";
            html += "<tr><td colspan='8'>ホワイトボード</td></tr>";

            // データがない時は処理なし
            if ((labels != null) && (labels.Count > 0))
            {
                // 縦方向
                for (int y = 0; y < 7; y++)
                {
                    html += "<tr>";
                    // 横方向
                    for (int x = 0; x < 8; x++)
                    {
                        bool match = false;
                        if (x == 2 || x == 5)
                        {
                            html += "<td style='background-color: "+COLOR[2+(y&1)]+"'>| |</td>";
                            continue;
                        }
                        // インデックスを求める
                        int idx = x < 2 ? x : x < 5 ? x - 1 : x - 2;
                        for (int lbl = 0; lbl < labels.Count; lbl++)
                        {
                            // IPを取得
                            string[] ips = labels[lbl][2].Text.Split(new char[] { '.' });
                            int chnum = int.Parse(ips[3]) - (firstIP-1);
                            if (chnum == CHAIR_CODE[y, idx])
                            {
                                html += "<td style='background-color: "+COLOR[y&1]+"'>" + labels[lbl][1].Text + "</td>";
                                match = true;
                                break;
                            }
                        }
                        // 見つからなかった
                        if (!match)
                        {
                            if (CHAIR_CODE[y, idx] == -1)
                            {
                                html += "<td style='background-color: black'>-</td>";
                            }
                            else
                            {
                                html += "<td style='background-color: " + COLOR[4+(y & 1)] + "'>-</td>";
                            }
                        }
                    }
                    html += "</tr>";
                }
            }

            webBrowser1.DocumentText = html+"</table></font></body></html>";
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            System.Windows.Forms.HtmlDocument document =
                this.webBrowser1.Document;

            if (document != null && document.All["userName"] != null &&
                String.IsNullOrEmpty(
                document.All["userName"].GetAttribute("value")))
            {
                e.Cancel = true;
                System.Windows.Forms.MessageBox.Show(
                    "You must enter your name before you can navigate to " +
                    e.Url.ToString());
            }
        }

        /**
         * 先頭座席を変更
         */
        public void changeZaseki(int w)
        {
            firstIP = w;
            setMember();
        }
    }
}
