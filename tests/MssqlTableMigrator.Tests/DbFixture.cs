using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace MssqlTableMigrator.Tests
{
    public class DatabaseFixture : IDisposable
    {
        private readonly string _destinationConnectionString;
        private readonly string _sourceConnectionString;
        public DatabaseFixture()
        {
                        var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            _destinationConnectionString = configuration.GetConnectionString("TestB") ?? throw new InvalidOperationException("Destination connection string is not configured.");
            _sourceConnectionString = configuration.GetConnectionString("TestA")?? throw new InvalidOperationException("Source connection string is not configured.");
            //Console.WriteLine(_destinationConnectionString);
            // テスト前の初期化処理
            CleanupTables();
        }
        /// <summary>
        /// マイグレーション先のDBでテストで使用するテーブルの削除
        /// </summary>
        public void CleanupTables()
        {   
            var desitinationTableNameListTestB = new List<string>{"DestinationTest1","DestinationTest2","DestinationTest3","DestinationTest4","DestinationTest5","TestTable"};
            var desitinationTableNameListTestA = new List<string>{"DestinationTestSameDB1","DestinationTestSameDB2","DestinationTestSameDB3","DestinationTestSameDB4","DestinationTestSameDB5","DestinationTestSameDB6"};

            MightDropTables(_destinationConnectionString,desitinationTableNameListTestB);
            MightDropTables(_sourceConnectionString,desitinationTableNameListTestA);
            
        }

        public void Dispose()
        {
            // フィクスチャの後始末が必要な場合にここで行う
        }
        /// <summary>
        /// テーブル削除
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="targetTableNames"></param>
        private void MightDropTables(string connectionString, List<string> targetTableNames)
        {
             var schema = "dbo";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var tableName in targetTableNames)
                {
                    var targetTableFullName = $"{schema}.{tableName}";
                    var deleteQuery = $"IF OBJECT_ID('{targetTableFullName}', 'U') IS NOT NULL DROP TABLE {targetTableFullName};";
                    using (var command = new SqlCommand(deleteQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    
                }
            }
        }
    }
}
