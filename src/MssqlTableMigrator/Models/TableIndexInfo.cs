using System;
using System.Collections.Generic;

/// <summary>
/// テーブルのインデックス情報を扱うクラスです.
/// </summary>
public class IndexInfo
{
    /// <summary>
    /// インデックス名
    /// </summary>
    /// <value></value>
    public string Name { get; set; }
    /// <summary>
    /// インデックス種別 CLUSTER | NONCLUSTER
    /// </summary>
    /// <value></value>
    public string Type { get; set; }
    /// <summary>
    /// 主キーかどうか
    /// </summary>
    /// <value></value>
    public bool IsPrimaryKey { get; set; }
    /// <summary>
    /// 対象の列名リスト
    /// </summary>
    /// <value></value>
    public List<string> Columns { get; set; }
}