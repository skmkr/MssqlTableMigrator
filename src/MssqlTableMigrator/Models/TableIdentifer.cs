using System;

/// <summary>
/// マイグレーション処理のソース, デスティネーション先のテーブルについての設定クラスです
/// </summary>
public class TableIdentifer
{
    /// <summary>
    /// テーブルのスキーマ名です.
    /// </summary>
    /// <value></value>
    public string Schema { get; set; }
    /// <summary>
    /// テーブル名です.
    /// </summary>
    /// <value></value>
    public string TableName { get; set; }
    /// <summary>
    /// コンストラクター
    /// </summary>
    /// <param name="tableName"></param>
    public TableIdentifer(string tableName)
        : this("dbo", tableName) { }
    /// <summary>
    /// コンストラクター
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="tableName"></param>
    public TableIdentifer(string schema, string tableName)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
    }
    /// <summary>
    /// スキーマ名とテーブル名を連結して提供します.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"[{Schema}].[{TableName}]";
    }
}
