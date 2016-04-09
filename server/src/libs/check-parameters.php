<?php

namespace Am1\Attend;

/**
 * パラメータの有効性を確認するSlimミドルウェア.
 *
 * @copyright 2016 YuTanaka@AmuseOne
 */
class CheckParameters
{
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
            $response = $response->withStatus(400);
            $response = $response->write(json_encode(['message' => '時間外です。']));

            return $response;
        }

        // uidの適正チェック
        $_POST['uid'] = self::Zen2Han($_POST['uid']);
        if (mb_strlen($_POST['uid']) != $class->uid_keta) {
            // 桁数が違う
            $response = $response->withStatus(400);
            $response = $response->write(json_encode(['message' => '学籍番号が違います。']));

            return $response;
        }

        // カード番号が3桁以内の数値
        if (!is_numeric($_POST['card'])
            ||  (($_POST['card'] - 0) < 0)
            ||  (($_POST['card'] - 0) >= 1000)) {
            // カード番号が不正
            $response = $response->withStatus(400);
            $response = $response->write(json_encode(['message' => 'カード番号が違います。']));
        }

        // 問題がないので処理を進める
        return $next($request, $response);
    }
}
