using Xunit;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;
using System.ComponentModel;
using Microsoft.VisualBasic;

namespace MssqlTableMigrator.Tests
{
    public class TableMigrationManagerIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly string _sourceConnectionString; 
        private readonly string _destinationConnectionString;

        /// <summary>
        /// <see cref="TableMigrationManagerIntegrationTests"/> クラスの新しいインスタンスを初期化します。
        /// 設定ファイルから接続文字列を読み込みます。
        /// </summary>
        /// <param name="fixture">共通のセットアップ用のDatabaseFixtureのインスタンス。</param>
        public TableMigrationManagerIntegrationTests(DatabaseFixture fixture)
        {
            // appsettings.jsonから設定を読み込む
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            _sourceConnectionString = configuration.GetConnectionString("TestA") ?? throw new InvalidOperationException("Source connection string is not configured.");
            _destinationConnectionString = configuration.GetConnectionString("TestB") ?? throw new InvalidOperationException("Destination connection string is not configured.");
        }
#region DualDbManager
        /// <summary>
        /// マイグレーションマネージャーがソーステーブルからデスティネーションテーブルにデータを正常に転送するかテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestination()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTest1");
            
            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString, _destinationConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString), "Data does not match.");
        }

        /// <summary>
        /// マイグレーションマネージャーがソーステーブルからデスティネーションテーブルにデータを正常に転送するかテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestination_NonTransaction()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTest2");

            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString, _destinationConnectionString))
            {
                migrationManager.Open();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString), "Data does not match.");
        }

        /// <summary>
        /// マイグレーションマネージャーのロールバックテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestination_Rollback()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTest3");

            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString, _destinationConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.RollbackTransaction();
                migrationManager.Close();
            }

            // Assert
            Assert.False(IsExistsTable(_destinationConnectionString,destinationTable),"Table is Exists");
        }
        /// <summary>
        ////ファクトリーで接続文字列を設定し, それを使用するテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestination_connectionWithConfiguration()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTest4");
            TableMigrationManagerFactory.Configuration.SetDefaultConnectionStrings(_sourceConnectionString,_destinationConnectionString);

            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager())
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString), "Data does not match.");
        }

        /// <summary>
        /// テーブルスキーマのコピー
        /// </summary>
        [Fact]
        public void MigrateTable_copySchema()
        {
            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo","DestinationTest5");
            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString,_destinationConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.CopyTableSchema(sourceTable,destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            //Assert
            Assert.True(AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString));
            Assert.Equal(0,GetTableRowCount(_destinationConnectionString,destinationTable));
        }


        /// <summary>
        /// テーブルの存在チェック
        /// </summary>
        [Fact]
        public void MigrateTable_CheckTableExists()
        {


            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo","DestinationTest6");
            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString,_destinationConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.CopyTableSchema(sourceTable,destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            var result = false;
            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString,_destinationConnectionString))
            {
                migrationManager.Open();
                //Assert
                result = migrationManager.CheckTableExists(destinationTable);
                migrationManager.Close();
            }

            Assert.True(result);
        }
        /// <summary>
        /// マイグレーションマネージャーがソーステーブルからデスティネーションテーブルにデータを正常に転送するかテスト
        /// destinationは未指定でソースと同一にする
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestination_nullDestination()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "TestTable");

            using (var migrationManager = TableMigrationManagerFactory.Create.dualDBMigrationManager(_sourceConnectionString, _destinationConnectionString))
            {
                migrationManager.Open();
                migrationManager.MigrateTable(sourceTable);
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_destinationConnectionString), "Data does not match.");
        }

#endregion
#region SingleDbManager

        /// <summary>
        /// マイグレーションマネージャーがソーステーブルからデスティネーションテーブルにデータを正常に転送するかテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestinationInSameDb()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTestSameDB1");

            using (var migrationManager = TableMigrationManagerFactory.Create.singleDBMigrationManager(_sourceConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString), "Data does not match.");
        }

        /// <summary>
        /// マイグレーションマネージャーがソーステーブルからデスティネーションテーブルにデータを正常に転送するかテスト
        /// トランザクションを貼らないテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestinationInSameDb_NonTransaction()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTestSameDB2");

            using (var migrationManager = TableMigrationManagerFactory.Create.singleDBMigrationManager(_sourceConnectionString))
            {
                migrationManager.Open();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString), "Data does not match.");
        }

        /// <summary>
        /// マイグレーションマネージャーのロールバックテスト
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestinationInSameDb_Rollback()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTestSameDB3");

            using (var migrationManager = TableMigrationManagerFactory.Create.singleDBMigrationManager(_sourceConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.RollbackTransaction();
                migrationManager.Close();
            }

            // Assert
            Assert.False(IsExistsTable(_destinationConnectionString,destinationTable),"Table is Exists");
        }
        /// <summary>
        ///ファクトリーでコネクションストリングを設定
        /// </summary>
        [Fact]
        public void MigrateTable_ShouldTransferDataFromSourceToDestinationInSameDb_connectionWithConfiguration()
        {

            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo", "DestinationTestSameDB4");
            TableMigrationManagerFactory.Configuration.SetDefaultConnectionString(_sourceConnectionString);

            using (var migrationManager = TableMigrationManagerFactory.Create.singleDBMigrationManager())
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.MigrateTable(sourceTable, destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            // Assert
            if (!AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString))
            {
                throw new InvalidOperationException("Column structures do not match.");
            }

            Assert.True(IsDataIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString), "Data does not match.");
        }


        /// <summary>
        /// テーブルスキーマのコピー
        /// </summary>
        [Fact]
        public void MigrateTable_copySchemaSameDb()
        {
            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTable = new TableIdentifer("dbo","DestinationTestSameDB5");
            using (var migrationManager = TableMigrationManagerFactory.Create.singleDBMigrationManager(_sourceConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.CopyTableSchema(sourceTable,destinationTable);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            //Assert
            Assert.True(AreColumnsIdentical(sourceTable, destinationTable,_sourceConnectionString,_sourceConnectionString));
            Assert.Equal(0,GetTableRowCount(_sourceConnectionString,destinationTable));
        }

        /// <summary>
        /// テーブルの存在チェック
        /// </summary>
        [Fact]
        public void MigrateTable_CheckTableExistsSameDb()
        {
            var sourceTable = new TableIdentifer("dbo", "TestTable");
            var destinationTableSchema = new TableIdentifer("dbo","DestinationTestSameDB6");
            using (var migrationManager = TableMigrationManagerFactory.Create.singleDBMigrationManager(_sourceConnectionString))
            {
                migrationManager.Open();
                migrationManager.BeginTransaction();
                migrationManager.CopyTableSchema(sourceTable,destinationTableSchema);
                migrationManager.CommitTransaction();
                migrationManager.Close();
            }

            //NOTE:MigrateTable_ShouldTransferDataFromSourceToDestination_connectionWithConfigurationで作成するテーブルを使用する。
            var destinationTable = new TableIdentifer("dbo", "DestinationTestSameDB6");
            var result = false;
            using (var migrationManager2 = TableMigrationManagerFactory.Create.singleDBMigrationManager(_sourceConnectionString))
            {
                migrationManager2.Open();
                //Assert
                result =migrationManager2.CheckTableExists(destinationTable); 
                migrationManager2.Close();
            }
            
            Assert.True(result);
        }

#endregion
        /// <summary>
        /// ソーステーブルとデスティネーションテーブルの列が同一であるかを確認します。
        /// </summary>
        /// <param name="sourceTable">ソーステーブルの識別子。</param>
        /// <param name="destinationTable">デスティネーションテーブルの識別子。</param>
        /// <returns>列が同一であればtrueを返します。それ以外の場合はfalseを返します。</returns>
        private bool AreColumnsIdentical(TableIdentifer sourceTable, TableIdentifer destinationTable,string sourceConnectionString,string destinationConnectionString)
        {
            var sourceColumns = GetColumnDefinitions(sourceConnectionString, sourceTable);
            var destinationColumns = GetColumnDefinitions(destinationConnectionString, destinationTable);

            if (sourceColumns.Count != destinationColumns.Count)
                return false;

            for (int i = 0; i < sourceColumns.Count; i++)
            {
                if (!sourceColumns[i].Equals(destinationColumns[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 指定されたテーブルの列定義をデータベースから取得します。
        /// </summary>
        /// <param name="connectionString">データベースの接続文字列。</param>
        /// <param name="table">テーブルの識別子。</param>
        /// <returns>列定義のリストを返します。</returns>
        private List<string> GetColumnDefinitions(string connectionString, TableIdentifer table)
        {
            var columns = new List<string>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = $@"
                    SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = '{table.Schema}' AND TABLE_NAME = '{table.TableName}'
                    ORDER BY ORDINAL_POSITION";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnDefinition = $"{reader["COLUMN_NAME"]} {reader["DATA_TYPE"]}";

                        if (reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                        {
                            columnDefinition += $"({reader["CHARACTER_MAXIMUM_LENGTH"]})";
                        }
                        else if (reader["NUMERIC_PRECISION"] != DBNull.Value && reader["NUMERIC_SCALE"] != DBNull.Value)
                        {
                            columnDefinition += $"({reader["NUMERIC_PRECISION"]},{reader["NUMERIC_SCALE"]})";
                        }

                        columnDefinition += reader["IS_NULLABLE"].ToString() == "YES" ? " NULL" : " NOT NULL";

                        columns.Add(columnDefinition);
                    }
                }
            }

            return columns;
        }

        /// <summary>
        /// ソーステーブルとデスティネーションテーブルのデータが同一であるかを確認します。
        /// </summary>
        /// <param name="sourceTable">ソーステーブルの識別子。</param>
        /// <param name="destinationTable">デスティネーションテーブルの識別子。</param>
        /// <returns>データが同一であればtrueを返します。それ以外の場合はfalseを返します。</returns>
        private bool IsDataIdentical(TableIdentifer sourceTable, TableIdentifer destinationTable,string sourceConnectionString, string destinationConnectionString)
        {
            var sourceData = LoadDataIntoList(sourceConnectionString, sourceTable);
            var destinationData = LoadDataIntoList(destinationConnectionString, destinationTable);

            if (sourceData.Count != destinationData.Count)
                return false;

            bool dataMatches = true;

            Parallel.ForEach(sourceData, (sourceRow, state, index) =>
            {
                var destinationRow = destinationData[(int)index];
                for (int i = 0; i < sourceRow.Count; i++)
                {
                    if (!object.Equals(sourceRow[i], destinationRow[i]))
                    {
                        dataMatches = false;
                        state.Stop(); // 不一致が見つかった場合、処理を停止します
                        break;
                    }
                }
            });

            return dataMatches;
        }

        /// <summary>
        /// ソーステーブルとデスティネーションテーブルのデータが同一であるかを確認します。
        /// </summary>
        /// <param name="sourceTable">ソーステーブルの識別子。</param>
        /// <param name="destinationTable">デスティネーションテーブルの識別子。</param>
        /// <returns>データが同一であればtrueを返します。それ以外の場合はfalseを返します。</returns>
        private bool IsDataIdenticalSingleConnection(string connectionString,TableIdentifer sourceTable, TableIdentifer destinationTable)
        {
            var sourceData = LoadDataIntoList(connectionString, sourceTable);
            var destinationData = LoadDataIntoList(connectionString, destinationTable);

            if (sourceData.Count != destinationData.Count)
                return false;

            bool dataMatches = true;

            Parallel.ForEach(sourceData, (sourceRow, state, index) =>
            {
                var destinationRow = destinationData[(int)index];
                for (int i = 0; i < sourceRow.Count; i++)
                {
                    if (!object.Equals(sourceRow[i], destinationRow[i]))
                    {
                        dataMatches = false;
                        state.Stop(); // 不一致が見つかった場合、処理を停止します
                        break;
                    }
                }
            });

            return dataMatches;
        }
        /// <summary>
        /// 指定されたテーブルからすべてのデータをリスト形式で読み込みます。
        /// </summary>
        /// <param name="connectionString">データベースの接続文字列。</param>
        /// <param name="table">テーブルの識別子。</param>
        /// <returns>行ごとにオブジェクトのリストを含むリストを返します。</returns>
        private List<List<object>> LoadDataIntoList(string connectionString, TableIdentifer table)
        {
            var data = new List<List<object>>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = $"SELECT * FROM {table.ToString()} ORDER BY ID , SubId";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new List<object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row.Add(fieldValue);
                        }
                        data.Add(row);
                    }
                }
            }

            return data;
        }
    
        /// <summary>
        /// 指定のテーブルが存在するかを確認します。
        /// </summary>
        /// <param name="tableIdentifier"></param>
        /// <returns></returns>
        private bool IsExistsTable(string connectionString,TableIdentifer tableIdentifier)
        {
            var query = @"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
                                 WHERE TABLE_SCHEMA = @SchemaName 
                                 AND TABLE_NAME = @TableName)
                            SELECT 1
                            ELSE
                            SELECT 0";
            using(var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query, connection))
            {   
                command.Parameters.AddWithValue("@SchemaName ",tableIdentifier.Schema);
                command.Parameters.AddWithValue("@TableName ",tableIdentifier.TableName);
                connection.Open();
                
                int result = (int)command.ExecuteScalar();

                return result ==1? true:false;
            }
        }
        /// <summary>
        /// 指定テーブルのカラム数を取得する
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableIdentifer"></param>
        /// <returns></returns>
        private int GetTableRowCount(string connectionString,TableIdentifer tableIdentifer)
        {
            int rowCount;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = $@"SELECT COUNT(*) FROM {tableIdentifer.ToString()}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // クエリを実行し、行数を取得
                    rowCount = (int)command.ExecuteScalar();
                }
            }    
            return rowCount;
        }
    }
}
