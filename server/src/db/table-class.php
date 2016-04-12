<?php

namespace Am1\Attend;

/**
 * クラステーブルの定義.
 */
class ClassTable extends \Illuminate\Database\Eloquent\Model
{
    /** テーブル名を定義*/
    protected $table = TABLE_CLASS;
    /** タイムスタンプを無効化する*/
    public $timestamps = false;
}
