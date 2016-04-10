<?php

namespace Am1\Attend;

/**
 * パラメータの有効性を確認するSlimミドルウェア.
 *
 * @copyright 2016 YuTanaka@AmuseOne
 */
class CheckParameters
{
    const ERROR_NO_CLASS = 0;
    const ERROR_INVALID_UID = 1;
    const ERROR_INVALID_CARD = 2;
    private static $ERRORS = [
        '出席の受け付け時間ではありません。',
        '学籍番号が違います。',
        'カード番号が不正です。'
    ];

    /**
     * 全角の数値を半角の数値に変換する.
     */
    public static function Zen2Han($in)
    {
        $zen = '０１２３４５６７８９';
        $han = '0123456789';
        mb_language('ja');
        $res = '';
        for ($i = 0; $i < mb_strlen($in); ++$i) {
            $idx = mb_strpos($zen, mb_substr($in, $i, 1));
            if ($idx === false) {
                $res .= mb_substr($in, $i, 1);
            } else {
                $res .= substr($han, $idx, 1);
            }
        }

        return $res;
    }

    /**
     * パラメータのuidとcardの適正チェックを行う。
     * 不合格の場合は、次へは行かず、HTTPステータス400(Bad Request)を
     * 返して、JSONでエラーを返して終了.
     */
    public function __invoke($request, $response, $next)
    {
        // クラスの特定
        $class = CsAttend::getClassData();
        if ($class == false) {
            // クラスが見つからない
            return $response->withStatus(400)
                ->withHeader('X-Status-Reason', self::ERROR_NO_CLASS);
        }

        // uidの適正チェック
        $_POST['uid'] = self::Zen2Han($_POST['uid']);
        if (mb_strlen($_POST['uid']) != $class->uid_keta) {
            // 桁数が違う
            return $response->withStatus(400)
                ->withHeader('X-Status-Reason', self::ERROR_INVALID_UID);
        }

        // カード番号が3桁以内の数値
        if (!is_numeric($_POST['card'])
            ||  (($_POST['card'] - 0) < 0)
            ||  (($_POST['card'] - 0) >= 1000)) {
            // カード番号が不正
            return $response->withStatus(400)
                ->withHeader('X-Status-Reason', self::ERROR_INVALID_CARD);
        }

        // 問題がないので処理を進める
        return $next($request, $response);
    }

    /**
     * レスポンスヘッダーの配列からエラーの理由を返す
     * @param array $res レスポンスヘッダー配列
     * @return true=エラーなし / string=エラーの原因
     */
    public static function checkError($res) {
        if (preg_match('/200/', $res[0]) === 1) {
            return true;
        }

        for($i=1 ; $i<count($res) ; $i++) {
            if (preg_match('/^X-Status-Reason/', $res[$i]) === 1) {
                $sent = explode(' ', $res[$i]);
                return self::$ERRORS[$sent[1]-0];
            }
        }

        return "不明なエラーです。";
    }
}
