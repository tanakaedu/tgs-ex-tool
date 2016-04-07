<?php

namespace PostTest;

use Am1\Attend\ClassTable;

class DbTest extends \PHPUnit_Framework_TestCase
{
    /*
     * 出席登録テスト
     */
    public function testEntry()
    {
        $send = array(
            'uid' => '21531000',
            'card' => '999',
        );
        $res = $this->postUrl('http://0.0.0.0:8080/attend', $send);
        echo 'code = '.$res['http_response_header'][0]."\n";
        echo 'resp = '.$res['response']."\n";
    }

    /**
     * @group target
     * クラス一覧を表示
     */
    public function testShowClass()
    {
        mb_language('ja');
        echo "\n".ClassTable::count()."\n";
        $class = ClassTable::all();
        for ($i = 0; $i < $class->count(); ++$i) {
            echo "\n".mb_convert_encoding($class[$i]->class, 'utf-8', 'auto')."\n";
        }
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
