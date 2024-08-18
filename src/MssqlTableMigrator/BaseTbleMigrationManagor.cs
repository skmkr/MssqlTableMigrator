using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data;
using System.Transactions;
namespace MssqlTableMigrator
{
    /// <summary>
    /// データベース間でテーブルをマイグレーションするための管理クラスです.
    /// </summary>
    public abstract class BaseTableMigrationManager : ITableMigrationManager
    {
#region Field
        /// <summary>
        /// 接続文字列
        /// </summary>
        protected readonly string _connectionString;
        /// <summary>
        /// 接続object
        /// </summary>
        protected SqlConnection _connection;
        /// <summary>
        /// トランザクション
        /// </summary>
        protected SqlTransaction _transaction;
#endregion
        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="connectionString">マイグレーションDBへの接続文字列</param>
        public BaseTableMigrationManager(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }
#region Main
#region InInterface
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
        public abstract bool MigrateTable(TableIdentifer sourceTable, TableIdentifer destinationTable = null);


        /// <summary>
        /// ソーステーブルのスキーマ情報のみをデスティネーションテーブルにコピーします。
        /// </summary>
        /// <param name="sourceTable">スキーマ情報をコピーするソーステーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <param name="destinationTable">スキーマ情報をコピーするデスティネーションテーブルを指定する <see cref="TableIdentifer"/> オブジェクト. nullの場合はソーステーブルの名前が使用されます.</param>
        /// <returns>スキーマのコピーが成功した場合はtrue、失敗した場合はfalseを返します。</returns>
        public abstract bool CopyTableSchema(TableIdentifer sourceTable, TableIdentifer destinationTable = null);        

        /// <summary>
        /// 指定したテーブルがデスティネーションデータベースに存在するかどうかを確認します。
        /// </summary>
        /// <param name="table">存在を確認するテーブルを指定する <see cref="TableIdentifer"/> オブジェクト.</param>
        /// <returns>テーブルが存在する場合はtrue、存在しない場合はfalseを返します。</returns>
        public abstract bool CheckTableExists(TableIdentifer table);



#endregion
#region protected,private
        /// <summary>
        /// テーブル作成SQL文を作成します
        /// </summary>
        /// <param name="connection">
        /// データをコピーするソースデータベースの <see cref="SqlConnection"/> オブジェクト.
        /// </param>
        /// <param name="sourceTable">
        /// マイグレーションするテーブルの<see cref="TableIdentifer"/>オブジェクト.
        /// </param>
        /// <param name="destinationTable">
        /// デスティネーションテーブルの<see cref="TableIdentifer"/>オブジェクト.
        /// </param>
        /// <param name="transaction">connectionに対する<see cref="SqlTransaction"/>オブジェクト</param>
        /// <returns>SQL文字列</returns>
        protected virtual string GetCreateTableScript(SqlConnection connection, TableIdentifer sourceTable, TableIdentifer destinationTable,SqlTransaction transaction =null)
        {
            var createTableScript = new System.Text.StringBuilder();
            createTableScript.AppendLine($"CREATE TABLE {destinationTable} (");

            var columnsQuery = $@"
                SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.COLUMN_DEFAULT, c.IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_NAME = @tablename AND c.TABLE_SCHEMA = @tableschema";
            //Console.WriteLine($"get columninfoquery:{columnsQuery}");
            using (var columnsCommand = new SqlCommand(columnsQuery, connection))
            {
                //シングルDBの場合はトランザクション実行中の可能性がありその場合はselect文の発行でもトランザクションが必要なので設定する
                if (transaction != null && connection == transaction.Connection)
                {
                    columnsCommand.Transaction = transaction;
                }
                columnsCommand.Parameters.AddWithValue("@tablename", sourceTable.TableName);
                columnsCommand.Parameters.AddWithValue("@tableschema", sourceTable.Schema);
                using (var reader = columnsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader["COLUMN_NAME"];
                        var dataType = reader["DATA_TYPE"];
                        var isNullable = reader["IS_NULLABLE"].ToString() == "YES";
                        var characterMaxLength = reader["CHARACTER_MAXIMUM_LENGTH"];
                        var numericPrecision = reader["NUMERIC_PRECISION"];
                        var defaultValue = reader["COLUMN_DEFAULT"];
                        var numericScale = reader["NUMERIC_SCALE"];

                        createTableScript.Append($"{columnName} {dataType}");

                        //text,int型は

                        if (characterMaxLength != DBNull.Value)
                        {
                            //text型は未指定とする
                            if (!(dataType.ToString().ToLower() == "text"))
                            {
                                createTableScript.Append($"({characterMaxLength})");
                            }
                        }
                        else if (numericPrecision != DBNull.Value && numericScale != DBNull.Value)
                        {
                            //int型はサイズ指定しない
                            if (dataType.ToString().ToLower() != "int")
                            {
                                createTableScript.Append($"({numericPrecision}, {numericScale})");
                            }
                        }
                        //初期値の設定
                        if (defaultValue != DBNull.Value)
                        {   
                            createTableScript.Append(" DEFAULT ");
                            createTableScript.Append(RemoveOuterParentheses(defaultValue.ToString()));
                        }

                        if (!isNullable)
                        {
                            createTableScript.Append(" NOT NULL");
                        }

                        createTableScript.Append(", ");
                    }
                }
            }

            createTableScript.Length -= 2; // Remove the trailing comma and space
            createTableScript.AppendLine(");");

            return createTableScript.ToString();
        }

        /// <summary>
        /// 存在する場合、一番外側の括弧を削除します.
        /// </summary>
        /// <param name="input">
        /// 処理する <see cref="string"/> 文字列
        /// </param>
        /// <returns>
        /// 括弧を外した文字列 <see cref="string"/>.
        /// </returns>
        protected string RemoveOuterParentheses(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input; // もとの入力がnullまたは空の場合、そのまま返す
            }
            input = input.Trim();
            // 最初の文字が開きカッコで、最後の文字が閉じカッコであるかをチェック
            if (input.Length > 1 && input[0] == '(' && input[input.Length - 1] == ')')
            {
                // 先頭と末尾の文字を削除した部分文字列を返す
                return input.Substring(1, input.Length - 2).Trim();
            }

            return input; // 条件を満たさない場合は元の文字列をそのまま返す
        }

        /// <summary>
        /// 指定されたテーブルのインデックス情報のリストを取得します.
        /// </summary>
        /// <param name="connection">
        /// インデックス情報を取得するために使用する <see cref="SqlConnection"/> オブジェクト.
        /// </param>
        /// <param name="sourceTable">
        /// インデックス情報を取得する対象の <see cref="TableIdentifer"/> オブジェクト.
        /// </param>
        /// <param name="transaction">connectionに対する<see cref="SqlTransaction"/>オブジェクト</param>
        /// <returns>
        /// テーブル上の各インデックスに関する詳細を含む <see cref="List{IndexInfo}"/>.
        /// </returns>
        protected virtual List<IndexInfo> GetIndexInfos(SqlConnection connection, TableIdentifer sourceTable, SqlTransaction transaction = null)
        {
            var indexes = new List<IndexInfo>();
                 
            string query = @"
                SELECT
                    i.name AS IndexName,
                    i.type_desc AS IndexType,
                    i.is_primary_key AS IsPrimaryKey,
                    c.name AS ColumnName
                FROM
                    sys.indexes i
                    INNER JOIN sys.index_columns ic ON i.index_id = ic.index_id
                    AND i.object_id = ic.object_id
                    INNER JOIN sys.columns c ON ic.column_id = c.column_id
                    AND ic.object_id = c.object_id
                    INNER JOIN sys.objects o ON o.object_id = i.object_id
                WHERE
                    o.name = @TableName
                    AND SCHEMA_NAME(o.schema_id) = @SchemaName
                ORDER BY
                    i.name, ic.key_ordinal";
            using (var command = new SqlCommand(query, connection))
            {
                //singleDBでトランザクションが貼られているとSELECT文の発行の場合でもTransactionを貼る必要がある
                if (transaction != null && connection == transaction.Connection)
                {
                    command.Transaction = transaction;
                }
                command.Parameters.AddWithValue("@TableName", sourceTable.TableName);
                command.Parameters.AddWithValue("@SchemaName", sourceTable.Schema);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    string currentIndex = null;
                    IndexInfo indexInfo = null;

                    while (reader.Read())
                    {
                        string indexName = reader.GetString(0);
                        string indexType = reader.GetString(1);
                        bool isPrimaryKey = reader.GetBoolean(2);
                        string columnName = reader.GetString(3);

                        if (currentIndex != indexName)
                        {
                            if (indexInfo != null)
                            {
                                indexes.Add(indexInfo);
                            }

                            indexInfo = new IndexInfo
                            {
                                Name = indexName,
                                Type = indexType,
                                IsPrimaryKey = isPrimaryKey,
                                Columns = new List<string>()
                            };

                            currentIndex = indexName;
                        }

                        indexInfo.Columns.Add(columnName);
                    }

                    if (indexInfo != null)
                    {
                        indexes.Add(indexInfo);
                    }
                }
            }

            return indexes;
        }

        /// <summary>
        /// インデックス情報を作成する
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="destinationTable"></param>
        /// <param name="indexes"></param>
        /// <param name="transaction"></param> 
        protected void CreateIndexes(SqlConnection connection, TableIdentifer destinationTable,List<IndexInfo> indexes,SqlTransaction transaction)
        {
            var isValidTransaction = false;
            if (transaction != null && connection == transaction.Connection)
            {
                isValidTransaction = true;
            }
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            //destinationTableのObject_idを取得する
            string objid = null;
            var getObjidQuery = $"SELECT object_id FROM sys.objects as o WHERE o.name = @tableName ";
            using (SqlCommand command = new SqlCommand(getObjidQuery, connection))
            {
                if (isValidTransaction)
                {
                    command.Transaction = transaction;
                }
                command.Parameters.AddWithValue("@tableName",destinationTable.TableName);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        objid = reader["object_id"].ToString();
                    }
                }
            }

            foreach (var index in indexes)
            {
                var indexType = index.Type == "CLUSTERED" ? "CLUSTERED" : "NONCLUSTERED";
                var columns = string.Join(", ", index.Columns);

                string createIndexQuery;
                if (index.IsPrimaryKey)
                {   
                    var indexName = $"PK_{objid}_{timestamp}";
                    // 主キーとして作成
                    createIndexQuery = $@"
                        ALTER TABLE {destinationTable.ToString()}
                        ADD CONSTRAINT {indexName} PRIMARY KEY {indexType} ({columns})";
                }
                else
                {
                    var indexName = $"IX_{objid}_{timestamp}";
                    // 通常のインデックス作成
                    createIndexQuery = $@"
                        CREATE {indexType} INDEX {indexName} 
                        ON {destinationTable.ToString()} ({columns})";
                }
                using (SqlCommand command = new SqlCommand(createIndexQuery, connection))
                {
                    if (isValidTransaction)
                    {
                        command.Transaction = transaction;
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// CheckTableExistsの実行実体
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="table"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        protected bool ProcessCheckTableExists(SqlConnection connection , TableIdentifer table,SqlTransaction transaction)
        {
            var query = @"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
                                 WHERE TABLE_SCHEMA = @SchemaName 
                                 AND TABLE_NAME = @TableName)
                            SELECT 1
                            ELSE
                            SELECT 0";
            using (var command = new SqlCommand(query, connection))
            {   
                if (transaction != null && connection == transaction.Connection)
                {
                    command.Transaction = transaction;
                }
                command.Parameters.AddWithValue("@SchemaName ",table.Schema);
                command.Parameters.AddWithValue("@TableName ",table.TableName);
                
                int result = (int)command.ExecuteScalar();

                return result ==1? true:false;
            }
        }

        /// <summary>
        /// スキーマ作成処理
        /// </summary>
        /// <param name="sourceTable"></param>
        /// <param name="destinationTable"></param>
        /// <param name="sourceConnection"></param>
        /// <param name="destinationConnection"></param>
        /// <param name="transaction"></param>
        protected void CreateTable(TableIdentifer sourceTable, TableIdentifer destinationTable,SqlConnection sourceConnection, SqlConnection destinationConnection, SqlTransaction transaction)
        {
            // テーブルスキーマ取得
            //dualの場合はソースとデスティネーションが別DBなので, sourceConnectionとGetIndexInfosではトランザクションはいらないが, Singleの場合は必要になる
            var createTableScript = GetCreateTableScript(sourceConnection, sourceTable, destinationTable,transaction);
            //対象は必ずデスティネーションConnectionとなる
            using (var createTableCommand = new SqlCommand(createTableScript, destinationConnection,transaction))
            {
                if (transaction != null && destinationConnection == transaction.Connection)
                {
                    createTableCommand.Transaction = transaction;
                }
                createTableCommand.ExecuteNonQuery();
            }
            //主キーとインデックス取得
            var indexinfos = GetIndexInfos(sourceConnection,sourceTable,transaction);
            CreateIndexes(destinationConnection,destinationTable,indexinfos,transaction);
        }
#endregion
#endregion
#region DBState
        /// <summary>
        /// DBへのコネクションを開きます.
        /// </summary>
        public virtual void Open()
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }
        /// <summary>
        /// DBへのコネクションを閉じます.
        /// </summary>
        public virtual void Close()
        {
            _connection?.Close();
        }
        /// <summary>
        /// トランザクションを開始します.
        /// </summary>
        public virtual void BeginTransaction()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Destination connection is not open.");
            }

            _transaction = _connection.BeginTransaction();
        }
        /// <summary>
        /// 現在のトランザクションをコミットします.
        /// </summary>
        public virtual void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }

            _transaction.Commit();
            _transaction = null;
        }
        /// <summary>
        /// 現在のトランザクションをロールバックします.
        /// </summary>
        public virtual void RollbackTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback.");
            }

            _transaction.Rollback();
            _transaction = null;
        }        

        /// <summary>
        /// 現在のDBのオブジェクトを破棄します.
        /// </summary>
        public virtual void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }

            Close();
            _connection?.Dispose();

        }
#endregion
    }

}