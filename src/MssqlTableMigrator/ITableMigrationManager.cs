using System;

namespace MssqlTableMigrator
{
    /// <summary>
    /// データベース間でテーブルをマイグレーションするための管理クラスのインターフェースです。
    /// </summary>
    public interface ITableMigrationManager : IDisposable
    {
        /// <summary>
        /// ソースデータベースとデスティネーションデータベースへの接続を開きます.
        /// </summary>
        void Open();

        /// <summary>
        /// ソースデータベースとデスティネーションデータベースへの接続を閉じます.
        /// </summary>
        void Close();

        /// <summary>
        /// トランザクションを開始します.
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 現在のトランザクションをコミットします.
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// 現在のトランザクションをロールバックします.
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// ソースデータベースからデスティネーションデータベースへのテーブルのマイグレーションを行います.
        /// </summary>
        /// <param name="sourceTable">
        /// マイグレーションするテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.
        /// </param>
        /// <param name="destinationTable">
        /// デスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.nullの場合はソーステーブルの名前が使用されます.
        /// </param>
        /// <returns>
        /// マイグレーションが成功した場合はtrueを返します.失敗した場合はfalseを返します.
        /// </returns>
        bool MigrateTable(TableIdentifer sourceTable, TableIdentifer destinationTable = null);


        /// <summary>
        /// 指定したテーブルがデスティネーションデータベースに存在するかどうかを確認します。
        /// </summary>
        /// <param name="table">存在を確認するテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <returns>テーブルが存在する場合はtrue、存在しない場合はfalseを返します。</returns>
        bool CheckTableExists(TableIdentifer table);

        /// <summary>
        /// ソーステーブルのスキーマ情報のみをデスティネーションテーブルにコピーします。
        /// </summary>
        /// <param name="sourceTable">スキーマ情報をコピーするソーステーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <param name="destinationTable">スキーマ情報をコピーするデスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト. nullの場合はソーステーブルの名前が使用されます.</param>
        /// <returns>スキーマのコピーが成功した場合はtrue、失敗した場合はfalseを返します。</returns>
        bool CopyTableSchema(TableIdentifer sourceTable, TableIdentifer destinationTable = null);

    }
}
