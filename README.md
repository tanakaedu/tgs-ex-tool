# tgs-ex-tool「試験登録」
コピーを検出して、コピーした文面、スクリーンショットをサーバーに送信して、サーバー側の指定のフォルダーに保存する。

# やること
- 設定ファイルを読み込む(保存先のパス)
- 学籍番号の登録。管理ツールで動作を確認する
- 指定のパスに学籍番号フォルダーを作成
- コピーが発生したら、コピーした時の画面をキャプチャして、クリップボードの内容と時間を配列に登録
  - 前回と指定時間(0.5秒程度)以内だった場合は、前のデータは破棄する
  - クライアントでタイマーで配列にデータがあったらサーバーにTCP/IPで送信
    - 2byteでテキスト文字数 テキスト文字列 ファイルバイナリ
- サーバーでTCP/IPのデータが着信したら、指定のパスに書き出す
  - 学籍番号/日時-シリアル番号/scr.png , copy.txt

# 参考URL
- [atmarkit クリップボードからデータを受け取るには？](http://www.atmarkit.co.jp/fdotnet/dotnettips/152getclipbrd/getclipbrd.html)
- [DOBON.NET 画面をキャプチャする](http://dobon.net/vb/dotnet/graphics/screencapture.html)
- [Microsoft Developer Network. 非同期なサーバーのソケットの例](https://msdn.microsoft.com/ja-jp/library/fx6588te(v=vs.110).aspx)
- [DOBON.NET TCPクライアント・サーバープログラムを作成する](http://dobon.net/vb/dotnet/internet/tcpclientserver.html)

