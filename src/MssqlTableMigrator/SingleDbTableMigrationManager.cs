using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data;
namespace MssqlTableMigrator
{
    /// <summary>
    /// 単一のDB内でテーブルマイグレーションするための管理クラスです.
    /// </summary>
    public class SingleDbTableMigrationManager : BaseTableMigrationManager
    {
        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="connectionString"></param>
        public SingleDbTableMigrationManager(string connectionString)
            : base(connectionString)
        {}
        /// <summary>
        /// テーブルのマイグレーションを行います.
        /// </summary>
        /// <param name="sourceTable">
        /// マイグレーションするテーブルを指定する <see cref="TableIdentifer"/>オブジェクト.
        /// </param>
        /// <param name="destinationTable">
        /// デスティネーションテーブルを指定する <see cref="TableIdentifer"/>オブジェクト. nullの場合はソーステーブルの名前が使用されます.
        /// </param>
        /// <returns>
        /// マイグレーションが成功した場合はtrueを返します.失敗した場合はfalseを返します.
        /// </returns>
        public override bool MigrateTable(TableIdentifer sourceTable, TableIdentifer destinationTable = null)
        {
            if (base._connection == null)
            {
                throw new InvalidOperationException("Connections must be opened before migrating tables.");
            }

            if (destinationTable is null)
            {
                destinationTable = new TableIdentifer(sourceTable.Schema, sourceTable.TableName);
            }
            
            CreateTable(sourceTable,destinationTable,base._connection,base._connection,base._transaction);
            // データの取得と挿入
            CopyData(base._connection, sourceTable, destinationTable,_transaction);
            return true;
        }
        /// <summary>
        /// ソーステーブルからデスティネーションテーブルへデータをコピーします.
        /// </summary>
        /// <param name="connection">
        /// データをコピーするソースデータベースの <see cref="SqlConnection"/> オブジェクト.
        /// </param>
        /// <param name="sourceTable">
        /// データをコピーするソーステーブルを指定する <see cref="TableIdentifer"/> オブジェクト.
        /// </param>
        /// <param name="destinationTable">
        /// データをコピーするデスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.
        /// </param>
        /// <param name="transaction"></param>
        private void CopyData(SqlConnection connection, TableIdentifer sourceTable, TableIdentifer destinationTable,SqlTransaction transaction)
        {
            var insertQuery = $@"
                INSERT INTO {destinationTable.ToString()}
                SELECT * FROM {sourceTable.ToString()}";

            using (var insertCommand = new SqlCommand(insertQuery, connection,transaction))
            {
                insertCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 指定したテーブルがデスティネーションデータベースに存在するかどうかを確認します。
        /// </summary>
        /// <param name="table">存在を確認するテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <returns>テーブルが存在する場合はtrue、存在しない場合はfalseを返します。</returns>
        public override bool CheckTableExists(TableIdentifer table)
        {
            if (base._connection == null)
            {
                throw new InvalidOperationException("Connections must be opened before migrating tables.");
            }

            return base.ProcessCheckTableExists(_connection,table,_transaction);
        }

        /// <summary>
        /// ソーステーブルのスキーマ情報のみをデスティネーションテーブルにコピーします。
        /// </summary>
        /// <param name="sourceTable">スキーマ情報をコピーするソーステーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <param name="destinationTable">スキーマ情報をコピーするデスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト. nullの場合はソーステーブルの名前が使用されます.</param>
        /// <returns>スキーマのコピーが成功した場合はtrue、失敗した場合はfalseを返します。</returns>
        public override bool CopyTableSchema(TableIdentifer sourceTable, TableIdentifer destinationTable = null)
        {

            if (base._connection == null)
            {
                throw new InvalidOperationException("Connections must be opened before migrating tables.");
            }

            if (destinationTable is null)
            {
                destinationTable = new TableIdentifer(sourceTable.Schema, sourceTable.TableName);
            }

            base.CreateTable(sourceTable,destinationTable,_connection,_connection,_transaction);
            return true;
        }
    }
}