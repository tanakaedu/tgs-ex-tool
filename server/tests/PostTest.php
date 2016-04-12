<?php

namespace PostTest;

use Am1\Attend\CsAttend;
use Am1\Attend\AttendTable;
use Am1\Attend\ClassTable;
use Am1\Attend\CheckParameters;

class DbTest extends \PHPUnit_Framework_TestCase
{
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
     * テスト用のクラスデータを出席受付中にする.
     */
    private function enableTestData()
    {
        // テスト用に最初のデータは必ず今の動作にする
        $data = ClassTable::find(1);
        $data->week = $this->getWeekChar(date('w'));
        $data->sttime = date('H:i:s');
        $data->offminutes = 1;
        $data->save();
    }

    /**
     * テストクラスの出席を削除.
     */
    private function removeTestAttend()
    {
        AttendTable::where('classid', '=', 1)->delete();
    }

    /**
     * 全角数値を半角数値に変換する処理のテスト
     */
    public function testZen2Han()
    {
        $this->assertEquals('0a1b2c3d4e5f6g789aあ9876543210', CheckParameters::Zen2Han('０a１b２c３d４e５f６g７８９aあ9876543210'));
    }

    /**
     * 出席登録テスト
     */
    public function testEntry()
    {
        $this->enableTestData();
        $this->removeTestAttend();
        $beforeCount = AttendTable::where('classid', '=', 1)->count();

        $send = array(
            'uid' => '21531000',
            'card' => '999',
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $json = json_decode($res['response']);

        $this->assertRegExp('/200/', $res['http_response_header'][0]);
        $this->assertEquals('ok', $json->message);
        $this->assertEquals('127.0.0.1', $json->server_ip);
        $this->assertEquals('', $json->name);
        $this->assertEquals($beforeCount + 1, AttendTable::where('classid', '=', 1)->count());
    }

    /**
     * @depend testEntry
     * 同一日の出席は更新しない関数をじかに呼び出す
     */
    public function testEntryBlockInner()
    {
        $att = AttendTable::where('classid', '=', 1)->take(1)->get()[0];
        $this->assertEquals(999, $att->card);
        $beforeCount = AttendTable::where('classid', '=', 1)->count();
        CsAttend::entryAttendProc('21531000', 1);

        $this->assertEquals($beforeCount, AttendTable::where('classid', '=', 1)->count());
        $att = AttendTable::where('classid', '=', 1)->take(1)->get()[0];
        $this->assertEquals(1, $att->card);
    }

    /**
     * @depend testEntry
     * 同一日の出席はカードのみ更新。
     */
    public function testEntryBlock()
    {
        $att = AttendTable::where('classid', '=', 1)->take(1)->get()[0];
        $this->assertEquals(999, $att->card);

        // 登録済みのデータの日付を変更
        $test = AttendTable::where('classid', '=', 1)->take(1)->get()[0];
        $test->enttime = date('Y-m-d H:i:s', time()-60*60);
        $test->save();

        $beforeCount = AttendTable::where('classid', '=', 1)->count();
        $send = array(
            'uid' => '21531000',
            'card' => '1',
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $json = json_decode($res['response']);

        $this->assertEquals($beforeCount, AttendTable::where('classid', '=', 1)->count());
        $att = AttendTable::where('classid', '=', 1)->take(1)->get()[0];
        $this->assertEquals(1, $att->card, 'カード番号の変更をチェック');
        $this->assertNotEquals(date('Y-m-d H:i:s'), $att->enttime, '時間が変更されていないチェック');
    }

    /**
     * @depend testEntry
     * 他の日の出席に対しては、データを追加
     */
    public function testEntryAnotherDate()
    {
        // 登録済みのデータの日付を変更
        $test = AttendTable::where('classid', '=', 1)->take(1)->get()[0];
        $test->enttime = date('Y-m-d H:i:s', time()-60*60*24*7);
        $test->save();

        // 登録
        $beforeCount = AttendTable::where('classid', '=', 1)->count();
        $send = array(
            'uid' => '21531000',
            'card' => '2',
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $json = json_decode($res['response']);

        $this->assertEquals($beforeCount+1, AttendTable::where('classid', '=', 1)->count());
        $att = AttendTable::where('classid', '=', 1)->get()[1];
        $this->assertEquals(2, $att->card);
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
     * 不正な学籍番号
     * エラー
     */
    public function testUIDError() {
        $send = array(
            'uid' => 'abcdef',
            'card' => '100',
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $result = CheckParameters::checkError($res['http_response_header']);

        $this->assertEquals('学籍番号が違います。', $result);
    }

    /**
     * 不正なカード番号
     * エラー
     */
    public function testCardError() {
         $send = array(
             'uid' => '21531999',
             'card' => 'abc',
         );
         $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
         $result = CheckParameters::checkError($res['http_response_header']);

         $this->assertEquals('カード番号が不正です。', $result);

         $send = array(
             'uid' => '21531999',
             'card' => '-1',
         );
         $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
         $result = CheckParameters::checkError($res['http_response_header']);

         $this->assertEquals('カード番号が不正です。', $result);

         $send = array(
             'uid' => '21531999',
             'card' => '1000',
         );
         $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
         $result = CheckParameters::checkError($res['http_response_header']);

         $this->assertEquals('カード番号が不正です。', $result);
     }


    /**
     * 要素不足
     * エラー
     */
    public function testInvalidParameters() {
        $send = array(
            'card' => '1000'
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $result = CheckParameters::checkError($res['http_response_header']);
        $this->assertEquals('パラメータ不足です。', $result, 'uid不足');

        $send = array(
            'uid' => '21531999'
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $result = CheckParameters::checkError($res['http_response_header']);
        $this->assertEquals('パラメータ不足です。', $result, 'uid不足');

        $send = array(
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        $result = CheckParameters::checkError($res['http_response_header']);
        $this->assertEquals('パラメータ不足です。', $result, 'uid不足');
    }

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

        $res = @file_get_contents(
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
