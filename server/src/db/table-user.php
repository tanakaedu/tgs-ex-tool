<?php

namespace Am1\Attend;

/**
 * ユーザーテーブルの定義.
 */
class UserTable extends \Illuminate\Database\Eloquent\Model
{
    /** テーブル名を定義*/
    protected $table = TABLE_USER;
    /** タイムスタンプを無効化する*/
    public $timestamps = false;
}
