using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// http://www.atmarkit.co.jp/fdotnet/dotnettips/848cbviewer/cbviewer.html
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace 試験登録
{
    public class ClipbardEventArgs : EventArgs
    {
        private string text;

        public string Text
        {
            get { return this.text; }
        }

        public ClipbardEventArgs(string str)
        {
            this.text = str;
        }
    }

    public delegate void cbEventHandler(
    object sender, ClipbardEventArgs ev);

    [System.Security.Permissions.PermissionSet(
        System.Security.Permissions.SecurityAction.Demand,
        Name = "FullTrust")]
    internal class MyClipboardViewer : NativeWindow
    {
        [DllImport("user32")]
        public static extern IntPtr SetClipboardViewer(
            IntPtr hWndNewViewer);
        [DllImport("user32")]
        public static extern bool ChangeClipboardChain(
                IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32")]
        public extern static int SendMessage(
                IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;
        private IntPtr nextHandle;

        private Form parent;
        public event cbEventHandler ClipboardHandler;

        public MyClipboardViewer(Form f)
        {
            f.HandleCreated
                        += new EventHandler(this.OnHandleCreated);
            f.HandleDestroyed
                        += new EventHandler(this.OnHandleDestroyed);
            this.parent = f;
        }

        internal void OnHandleCreated(object sender, EventArgs e)
        {
            AssignHandle(((Form)sender).Handle);
            // ビューアを登録
            nextHandle = SetClipboardViewer(this.Handle);
        }

        internal void OnHandleDestroyed(object sender, EventArgs e)
        {
            // ビューアを解除
            bool sts = ChangeClipboardChain(this.Handle, nextHandle);
            ReleaseHandle();
        }

        protected override void WndProc(ref Message msg)
        {
            switch (msg.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    // データを列挙
                    IDataObject data = Clipboard.GetDataObject();
                    string[] formats = data.GetFormats();
                    string res = "";
                    bool isRecord = false;
                    for (int i = 0; i < formats.Length; i++)
                    {
                        // EnhancedMetafileがあれば記録
                        if (formats[i].IndexOf("EnhancedMetafile") != -1)
                        {
                            isRecord = true;
                        }
                        if (i > 0)
                        {
                            res += ",";
                        }
                        res += formats[i];
                    }

                    if (Clipboard.ContainsText())
                    {
                        // クリップボードの内容がテキストの場合のみ
                        if (ClipboardHandler != null)
                        {
                            res += " : "+Clipboard.GetText()+"\r\n";
                            isRecord = true;
                        }
                    }
                    // クリップボードの内容を取得してハンドラを呼び出す
                    if (isRecord)
                    {
                        ClipboardHandler(this,
                            new ClipbardEventArgs(res));
                    }


                    if ((int)nextHandle != 0)
                        SendMessage(
                            nextHandle, msg.Msg, msg.WParam, msg.LParam);
                    break;

                // クリップボード・ビューア・チェーンが更新された
                case WM_CHANGECBCHAIN:
                    if (msg.WParam == nextHandle)
                    {
                        nextHandle = (IntPtr)msg.LParam;
                    }
                    else if ((int)nextHandle != 0)
                        SendMessage(
                            nextHandle, msg.Msg, msg.WParam, msg.LParam);
                    break;
            }
            base.WndProc(ref msg);
        }
    }
}
