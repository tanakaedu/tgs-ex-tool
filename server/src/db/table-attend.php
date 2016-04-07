<?php

namespace Am1\Attend;

/**
 * 出席テーブルの定義.
 */
class AttendTable extends \Illuminate\Database\Eloquent\Model
{
    /** テーブル名を定義*/
    protected $table = TALBE_ATTEND;
    /** タイムスタンプを無効化する*/
    public $timestamps = false;
}
