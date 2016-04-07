<?php
/**
 * C#製の出席システム用の処理クラス
 */

namespace Am1\Attend;

use Illuminate\Database\Capsule\Manager as Capsule;

class CsAttend {
    /** 自分のインスタンス*/
    public static $me = null;
    /** Illuminate Databaseのオブジェクト*/
    public static $capsule = null;
    /** セッティングを記録*/
    protected $settings;

    /**
     * コンストラクタ。Illuminate Databaseの接続を開始.
     * @param array $set dbにデータベースへの接続パラメータ配列を設定した配列
     */
    public function __construct($set)
    {
        $this->settings = $set;

        if (self::$capsule == null) {
            self::$capsule = new Capsule();

            self::$capsule->addConnection($set['db']);
            self::$capsule->setAsGlobal();
            self::$capsule->bootEloquent();
        }
    }

    /**
     * 本クラスを起動させる
     * @param array $set dbにデータベースへの接続パラメータ配列を設定した配列
     */
    public static function boot($set) {
        self::$me = new CsAttend($set);
        self::$me->settings = $set;
    }

    /**
     * 指定の学籍番号とカード番号で出席登録処理を行う
     * @param string $uid 学籍番ごう
     * @param number $card カード(IPアドレスの下３桁)
     */
    public static function entryAttend($uid, $card) {
        if (is_null(self::$me)) {
            echo "\nこの関数を実行する前に、bootで初期設定を渡してください。\n";
            return;
        }

        self::$me->entryAttendProc($uid, $card);
    }

    public function entryAttendProc($uid, $card) {
        
    }
}
