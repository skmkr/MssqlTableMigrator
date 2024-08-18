using System;
namespace MssqlTableMigrator
{
    /// <summary>
    /// <see cref="ITableMigrationManager"/> のインスタンスを生成するためのファクトリークラスです。
    /// </summary>
    public static class TableMigrationManagerFactory
    {

        /// <summary>
        /// ファクトリーの設定を管理するクラスです。
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// 規定のソースDBへの接続文字列
            /// </summary>
            /// <value></value>
            public static string DefaultSourceConnectionString { get; set; }
            /// <summary>
            /// 規定のデスティネーションDBへの接続文字列
            /// </summary>
            /// <value></value>
            public static string DefaultDestinationConnectionString { get; set; }

            /// <summary>
            /// ソース, デスティネーション２つのDBの
            /// 規定の接続文字列の設定
            /// </summary>
            /// <param name="source">ソースDBへの接続文字列</param>
            /// <param name="destination">デスティネーションDBへの接続文字列</param>
            public static void SetDefaultConnectionStrings(string source, string destination)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    throw new ArgumentException("Source connection string cannot be null or whitespace.", nameof(source));
                }

                if (string.IsNullOrWhiteSpace(destination))
                {
                    throw new ArgumentException("Destination connection string cannot be null or whitespace.", nameof(destination));
                }

                DefaultSourceConnectionString = source;
                DefaultDestinationConnectionString = destination;
            }

            /// <summary>
            /// 規定の接続文字列の設定
            /// </summary>
            /// <param name="source">ソースDBへの接続文字列</param>
            public static void SetDefaultConnectionString(string source)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    throw new ArgumentException("Source connection string cannot be null or whitespace.", nameof(source));
                }
                DefaultSourceConnectionString = source;
            }
        }
        /// <summary>
        /// 各マネージャーを作成するクラスです。
        /// </summary>
        public static class Create{
            /// <summary>
            /// 指定された接続文字列を使用して <see cref="ITableMigrationManager"/> のインスタンスを生成します。
            /// </summary>
            /// <param name="sourceConnectionString">ソースデータベースへの接続文字列。</param>
            /// <param name="destinationConnectionString">デスティネーションデータベースへの接続文字列。</param>
            /// <returns>新しい <see cref="ITableMigrationManager"/> のインスタンスを返します。</returns>
            /// <exception cref="ArgumentNullException">接続文字列が null または空白の場合にスローされます。</exception>
            public static ITableMigrationManager dualDBMigrationManager(string sourceConnectionString, string destinationConnectionString)
            {
                if (string.IsNullOrWhiteSpace(sourceConnectionString))
                {
                    throw new ArgumentNullException(nameof(sourceConnectionString), "Source connection string cannot be null or whitespace.");
                }

                if (string.IsNullOrWhiteSpace(destinationConnectionString))
                {
                    throw new ArgumentNullException(nameof(destinationConnectionString), "Destination connection string cannot be null or whitespace.");
                }

                return new DualDBTableMigrationManager(sourceConnectionString, destinationConnectionString);
            }
            /// <summary>
            /// 規定の接続文字列を使用して <see cref="ITableMigrationManager"/> のインスタンスを生成します。
            /// </summary>
            /// <returns>新しい <see cref="ITableMigrationManager"/> のインスタンスを返します。</returns>
            /// <exception cref="ArgumentNullException">規定の接続文字列が null または空白の場合にスローされます。</exception>
            public static ITableMigrationManager dualDBMigrationManager()
            {
                if (string.IsNullOrWhiteSpace(Configuration.DefaultSourceConnectionString))
                {
                    throw new ArgumentNullException(nameof(Configuration.DefaultSourceConnectionString), "Default source connection string is not set.");
                }

                if (string.IsNullOrWhiteSpace(Configuration.DefaultDestinationConnectionString))
                {
                    throw new ArgumentNullException(nameof(Configuration.DefaultDestinationConnectionString), "Default destination connection string is not set.");
                }
                
                return new DualDBTableMigrationManager(Configuration.DefaultSourceConnectionString, Configuration.DefaultDestinationConnectionString);
            }
            /// <summary>
            /// 指定された接続文字列を使用して <see cref="ITableMigrationManager"/> のインスタンスを生成します。
            /// </summary>
            /// <param name="connectionString">
            /// ソースデータベースへの接続文字列。
            /// </param>
            /// <returns>
            /// 新しい <see cref="ITableMigrationManager"/> のインスタンスを返します。
            /// </returns>
            public static ITableMigrationManager singleDBMigrationManager(string connectionString)
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or whitespace.");
                }
                return new SingleDbTableMigrationManager(connectionString);
            }
            /// <summary>
            /// 規定の接続文字列を使用して <see cref="ITableMigrationManager"/> のインスタンスを生成します。
            /// </summary>
            /// <returns>
            /// 新しい <see cref="ITableMigrationManager"/> のインスタンスを返します。
            /// </returns>
            public static ITableMigrationManager singleDBMigrationManager()
            {
                if (string.IsNullOrWhiteSpace(Configuration.DefaultSourceConnectionString))
                {
                    throw new ArgumentNullException(nameof(Configuration.DefaultSourceConnectionString), "Default source connection string is not set.");
                }
                return new SingleDbTableMigrationManager(Configuration.DefaultSourceConnectionString);
            }
        }


    }
}
