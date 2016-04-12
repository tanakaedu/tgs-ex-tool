<?php
/**
 * C#製の出席システム用の処理クラス.
 */
namespace Am1\Attend;

use Illuminate\Database\Capsule\Manager as Capsule;
class CsAttend
{
    /** 自分のインスタンス*/
    public static $me = null;
    /** Illuminate Databaseのオブジェクト*/
    public static $capsule = null;
    /** セッティングを記録*/
    protected $settings;

    /**
     * コンストラクタ。Illuminate Databaseの接続を開始.
     *
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
     * 本クラスを起動させる.
     *
     * @param array $set dbにデータベースへの接続パラメータ配列を設定した配列
     */
    public static function boot($set)
    {
        self::$me = new self($set);
        self::$me->settings = $set;
    }

    /**
     * 現在の日時からクラスを判定して、データを返す.
     */
    public static function getClassData()
    {
        $arYoubi = array('日', '月', '火', '水', '木', '金', '土');
        $params = [];

        $select = 'select * from am1_att_class where semester=?';
        $select .= ' and week=?';
        $select .= ' and ((sttime<=now()';
        $select .= ' and sttime>=time(timestampadd(MINUTE, -offminutes, now())))';
        $select .= ' or (started<=now()';
        $select .= ' and started>=timestampadd(MINUTE, -offminutes, now())))';

        // 前期後期の判定(4月から8月が前期)
        $now = getdate();
        if (($now['mon'] >= 4) && ($now['mon'] <= 8)) {
            $params[] = self::toDB('前期');
        } else {
            $params[] = self::toDB('後期');
        }

        // 曜日を指定
        $params[] = self::toDB($arYoubi[$now['wday']]);

        // 抽出
        $datas = Capsule::select($select, $params);

        // 見つからない時はfalseを返す
        if (count($datas) == 0) {
            return false;
        }

        // 見つかっていれば最初のデータを返す
        return $datas[0];
    }

    /**
     * DBで利用する日本語にエンコード.
     */
    public static function toDB($in)
    {
        mb_language('ja');

        return mb_convert_encoding($in, 'euc-jp', 'auto');
    }

    /**
     * 指定のDBから取り出した文字列をUTF-8に変換.
     */
    public static function fromDB($in)
    {
        mb_language('ja');

        return mb_convert_encoding($in, 'utf-8', 'auto');
    }

    /**
     * 指定の学籍番号とカード番号で出席登録処理を行う.
     *
     * @param string $uid  学籍番ごう
     * @param number $card カード(IPアドレスの下３桁)
     */
    public static function entryAttend($uid, $card)
    {
        if (is_null(self::$me)) {
            echo "\nこの関数を実行する前に、bootで初期設定を渡してください。\n";

            return;
        }

        self::$me->entryAttendProc($uid, $card);
    }

    /**
     * 現在のUIDの名前を取得。見つからない場合は空文字列を返す.
     */
    public static function getName()
    {
        $user = UserTable::where('uid', '=', $_POST['uid']);
        if ($user->count() == 0) {
            return '';
        }

        return self::fromDB($user->take(1)->get()[0]->name);
    }

    /**
     * 現在の日時からクラスを特定して、そのクラスのサーバーIPを返す
     * 見つからない場合はfalse.
     */
    public static function getServerIP()
    {
        $class = self::getClassData();
        if ($class === false) {
            return false;
        }

        return self::fromDB($class->localserver);
    }

    /**
     * 指定の学籍番号とカード番号で出席を登録
     * 見つからない場合はfalse.
     */
    public static function entryAttendProc($uid, $card)
    {
        $class = self::getClassData();
        if ($class === false) {
            return false;
        }

        // 同一日のデータがあるかを確認
        $select = "select id from am1_att_attend where";
        $select .= " uid=? and";
        $select .= " classid=? and";
        $select .= " date(enttime)=date(now())";
        $attend = Capsule::select(
            $select,
            [
                $uid,
                $class->id
            ]
        );

        // 既存のデータがある場合は、カード番号を上書き
        if (count($attend) > 0) {
            $update = AttendTable::where('id', '=', $attend[0]->id)->take(1)->get()[0];
            $update->card = $card;
            $update->save();
        }
        else {
            // 既存データがないので新規登録
            $newattend = new AttendTable;
            $newattend->uid = $uid;
            $newattend->classid = $class->id;
            $newattend->card = $card;
            $newattend->enttime = date('Y-n-j H:i:s');
            $newattend->save();
        }
    }
}
