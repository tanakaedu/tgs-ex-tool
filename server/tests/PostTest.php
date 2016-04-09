<?php

namespace PostTest;

use Am1\Attend\CsAttend;
use Am1\Attend\ClassTable;
use Am1\Attend\CheckParameters;

class DbTest extends \PHPUnit_Framework_TestCase
{
    /**
     * @group target
     * 全角数値を半角数値に変換する処理のテスト
     */
    public function testZen2Han() {
        $this->assertEquals("0a1b2c3d4e5f6g789aあ9876543210", CheckParameters::Zen2Han("０a１b２c３d４e５f６g７８９aあ9876543210"));
    }

    /**
     * @group target
     * 出席登録テスト
     */
    public function testEntry()
    {
        $this->enableTestData();

        $send = array(
            'uid' => '21531000',
            'card' => '999',
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $json = json_decode($res['response']);

        $this->assertRegExp("/200/", $res['http_response_header'][0]);
        $this->assertEquals('ok', $json->message);
        $this->assertEquals('127.0.0.1', $json->server_ip);
    }

    /**
     * DBの操作テスト.
     */
    public function testDB()
    {
        $this->enableTestData();

        $this->assertFalse(CsAttend::getClassData());
    }

    /**
     * クラス一覧を表示.
     */
    public function testShowClass()
    {
        echo "\n".ClassTable::count()."\n";
        $class = ClassTable::all();
        for ($i = 0; $i < $class->count(); ++$i) {
            echo "\n";
            echo CsAttend::fromDB($class[$i]->class).',';
            echo CsAttend::fromDB($class[$i]->week).',';
            echo CsAttend::fromDB($class[$i]->semester)."\n";
        }
    }

    /**
     * @param number $wn date("w")の値。0(日曜日)-6(土曜日)
     *
     * @return DB用にエンコードした曜日文字列
     */
    private function getWeekChar($wn)
    {
        $wk = ['日', '月', '火', '水', '木', '金', '土'];

        return CsAttend::toDB($wk[$wn]);
    }

    /**
     * テスト用のクラスデータを出席受付中にする
     */
    private function enableTestData() {
        // テスト用に最初のデータは必ず今の動作にする
        $data = ClassTable::find(1);
        $data->week = $this->getWeekChar(date('w'));
        $data->sttime = date('H:i:s');
        $data->offminutes = 1;
        $data->save();
    }


    /*
     * 登録済みのUIDで同じcardに登録
     * 変更なし
     */

    /*
     * 登録済みのUIDで、別のcardに登録
     * カード番号を変更
     * カードの重複チェックは省く
     */

    /*
     * 不正な学籍番号
     * エラー
     */

    /*
     * 不正なカード番号
     * エラー
     */

    /*
     * 要素不足
     * エラー
     */

    /**
     * @param string $url       送信先のURL
     * @param array  $sendarray 送信する連想配列
     *
     * @return array response=戻ってきたページの情報 / http_response_header=レスポンスヘッダ
     */
    private function postUrl($url, $sendarray)
    {
        $data_url = http_build_query($sendarray);
        $data_len = strlen($data_url);

        $res = file_get_contents(
            $url,
            false,
            stream_context_create(array(
                'http' => array(
                    'method' => 'POST',
                    'header' => "Content-Type: application/x-www-form-urlencoded\r\nContent-Length: $data_len\r\n",
                    'content' => $data_url,
                ),
            ))
        );

        return array(
            'response' => $res,
            'http_response_header' => $http_response_header,
        );
    }
}
