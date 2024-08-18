using System;
using System.Data.SqlClient;
using System.Data;
namespace MssqlTableMigrator
{
    /// <summary>
    /// データベース間でテーブルをマイグレーションするための管理クラスです.
    /// </summary>
    public class DualDBTableMigrationManager : BaseTableMigrationManager
    {
        private readonly string _destinationConnectionString;
        private SqlConnection _destinationConnection;
        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="sourceConnectionString">マイグレーション元のDBへの接続文字列</param>
        /// <param name="destinationConnectionString">でシティネーションDBへの接続文字列</param>
        public DualDBTableMigrationManager(string sourceConnectionString, string destinationConnectionString)
            :base(sourceConnectionString)
        {
            _destinationConnectionString = destinationConnectionString ?? throw new ArgumentNullException(nameof(destinationConnectionString));
        }
        /// <summary>
        /// ソースとでシティネーション先のDBへのコネクションを開きます.
        /// </summary>
        public override void Open()
        {
            base.Open();
            _destinationConnection = new SqlConnection(_destinationConnectionString);
            _destinationConnection.Open();
        }
        /// <summary>
        /// ソースとでシティネーション先のDBへのコネクションを閉じます.
        /// </summary>
        public override void Close()
        {
            base.Close();
            _destinationConnection?.Close();
        }
        /// <summary>
        /// ソースデータベースからデスティネーションデータベースへのテーブルのマイグレーションを行います.
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
            if (base._connection == null || _destinationConnection == null)
            {
                throw new InvalidOperationException("Connections must be opened before migrating tables.");
            }

            if (destinationTable is null)
            {
                destinationTable = new TableIdentifer(sourceTable.Schema, sourceTable.TableName);
            }

            CreateTable(sourceTable,destinationTable,_connection,_destinationConnection,_transaction);
            // データの取得と挿入
            CopyData(base._connection, sourceTable, _destinationConnection, destinationTable,_transaction);
            return true;
        }

        /// <summary>
        /// ソーステーブルからデスティネーションテーブルへデータをコピーします.
        /// </summary>
        /// <param name="sourceConnection">
        /// データをコピーするソースデータベースの <see cref="SqlConnection"/> オブジェクト.
        /// </param>
        /// <param name="sourceTable">
        /// データをコピーするソーステーブルを指定する <see cref="TableIdentifer"/> オブジェクト.
        /// </param>
        /// <param name="destinationConnection">
        /// データをコピーするデスティネーションデータベースの <see cref="SqlConnection"/> オブジェクト.
        /// </param>
        /// <param name="destinationTable">
        /// データをコピーするデスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.
        /// </param>
        /// <param name="transaction"></param>
        private void CopyData(SqlConnection sourceConnection, TableIdentifer sourceTable, SqlConnection destinationConnection, TableIdentifer destinationTable,SqlTransaction transaction)
        {
            var selectQuery = $"SELECT * FROM {sourceTable.Schema}.{sourceTable.TableName}";
            using (var selectCommand = new SqlCommand(selectQuery, sourceConnection))
            using (var reader = selectCommand.ExecuteReader())
            {
                using (var bulkCopy = new SqlBulkCopy(destinationConnection,SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = $"{destinationTable.Schema}.{destinationTable.TableName}";
                    bulkCopy.WriteToServer(reader);
                }
            }
        }

        /// <summary>
        /// 指定したテーブルがデスティネーションデータベースに存在するかどうかを確認します。
        /// </summary>
        /// <param name="table">存在を確認するテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <returns>テーブルが存在する場合はtrue、存在しない場合はfalseを返します。</returns>
        public override bool CheckTableExists(TableIdentifer table)
        {
            if (base._connection == null || _destinationConnection == null)
            {
                throw new InvalidOperationException("Connections must be opened before migrating tables.");
            }

            return base.ProcessCheckTableExists(_destinationConnection,table,_transaction);
        }

        /// <summary>
        /// ソーステーブルのスキーマ情報のみをデスティネーションテーブルにコピーします。
        /// </summary>
        /// <param name="sourceTable">スキーマ情報をコピーするソーステーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <param name="destinationTable">スキーマ情報をコピーするデスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト. nullの場合はソーステーブルの名前が使用されます.</param>
        /// <returns>スキーマのコピーが成功した場合はtrue、失敗した場合はfalseを返します。</returns>
        public override bool CopyTableSchema(TableIdentifer sourceTable, TableIdentifer destinationTable = null)
        {
            if (base._connection == null || _destinationConnection == null)
            {
                throw new InvalidOperationException("Connections must be opened before migrating tables.");
            }

            if (destinationTable is null)
            {
                destinationTable = new TableIdentifer(sourceTable.Schema, sourceTable.TableName);
            }
            base.CreateTable(sourceTable,destinationTable,_connection,_destinationConnection,_transaction);

            return true;
        }
        /// <summary>
        /// トランザクションを開始します.
        /// </summary>
        public override void BeginTransaction()
        {
            if (_destinationConnection == null || _destinationConnection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Destination connection is not open.");
            }

            base._transaction = _destinationConnection.BeginTransaction();
        }
        /// <summary>
        /// 現在のトランザクションをコミットします.
        /// </summary>
        public override void CommitTransaction()
        {
            if (base._transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }

            base._transaction.Commit();
            base._transaction = null;
        }
        /// <summary>
        /// 現在のトランザクションをロールバックします.
        /// </summary>
        public override void RollbackTransaction()
        {
            if (base._transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback.");
            }

            base._transaction.Rollback();
            base._transaction = null;
        }        

        /// <summary>
        /// 現在のDBのオブジェクトを破棄します.
        /// </summary>
        public override void Dispose()
        {
            if (base._transaction != null)
            {
                base._transaction.Rollback();
                base._transaction.Dispose();
                base._transaction = null;
            }

            Close();
            base._connection?.Dispose();
            _destinationConnection?.Dispose();

        }



    }
}
