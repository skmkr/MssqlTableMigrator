
# MssqlTableMigrator

`MssqlTableMigrator` は、データベース間でテーブルを移行するためのクラスライブラリです。このライブラリは、SQL Serverデータベース間のテーブルマイグレーション, スキーマコピーをサポートします。

## 特徴


- データベース間でテーブル移行
- 同一データベースの場合も使用可能
- トランザクションによるデータ一貫性の確保
- 自動的なインデックスの再作成

## インストール

NuGetパッケージとしてインストールできます。

```bash
dotnet add package MssqlTableMigrator
```

または、`Package Manager Console` を使用してインストールします。

```powershell
Install-Package MssqlTableMigrator
```

## 使い方

以下は、`MssqlTableMigrator` を使用して異なるデータベース間でテーブルを移行する方法の簡単な例です。  
他のサンプルについてはsamplecodesを参照してください
```csharp
using MssqlTableMigrator;

class Program
{
    static void Main(string[] args)
    {
        var sourceConnectionString = "SourceConnectionStringHere";
        var destinationConnectionString = "DestinationConnectionStringHere";

        using var manager = TableMigrationManagerFactory.Create.dualDBMigrationManager(sourceConnectionString, destinationConnectionString);
        
        manager.Open();
        
        try
        {
            manager.BeginTransaction();

            var sourceTable = new TableIdentifier("SourceSchema", "SourceTableName");
            var destinationTable = new TableIdentifier("DestinationSchema", "DestinationTableName");
            
            bool result = manager.MigrateTable(sourceTable, destinationTable);
            
            if (result)
            {
                manager.CommitTransaction();
                Console.WriteLine("Migration succeeded.");
            }
            else
            {
                manager.RollbackTransaction();
                Console.WriteLine("Migration failed.");
            }
        }
        catch (Exception ex)
        {
            manager.RollbackTransaction();
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            manager.Close();
        }
    }
}
```

## APIリファレンス

### `DualDBTableMigrationManager` `SingleDBTableMigrationManager`

- **`Open()`**: ソースとデスティネーションの接続を開きます。
- **`Close()`**: ソースとデスティネーションの接続を閉じます。
- **`BeginTransaction()`**: デスティネーションのトランザクションを開始します。
- **`CommitTransaction()`**: デスティネーションのトランザクションをコミットします。
- **`RollbackTransaction()`**: デスティネーションのトランザクションをロールバックします。
- **`MigrateTable(TableIdentifier sourceTable, TableIdentifier destinationTable = null)`**: テーブルを移行します。
    - **`destinationTable`** がnullの場合は **`sourceTable`** と同じ値を使用します※`DualDBTableMigrationManager` の場合のみ`SingleDBTableMigrationManager` ではエラーとなります。
- **`CheckTableExists(TableIdentifer table)`** : デスティネーションデータベースにテーブルが存在するかをチェックします
- **`CopyTableSchema(TableIdentifier sourceTable, TableIdentifier destinationTable = null)`**: テーブルのスキーマのみをコピーします。

### `TableMigrationManagerFactory`

#### `Configuration`

- **`SetDefaultConnectionStrings(source, destination)`** : ソースとデスティネーションの接続文字列の規定値を設定します。`DualDBTableMigrationManager` 用です
- **`SetDefaultConnectionString(source)`** : 接続文字列の規定値を設定します。`SingleDBTableMigrationManager` 用です


#### `Create`

- **`DualDBTableMigrationManager(sourceConnectionString,destinationConnectionString)`** : `DualDBTableMigrationManager` インスタンスを作成します。
- **`DualDBTableMigrationManager()`** : `Configuration` クラスで設定した規定の設定を使用して `DualDBTableMigrationManager` インスタンスを作成します。
- **`SingleDBTableMigrationManager(ConnectionString)`** : `SingleDBTableMigrationManager` インスタンスを作成します。
- **`SingleDBTableMigrationManager()`** : `Configuration` クラスで設定した規定の設定を使用して `SingleDBTableMigrationManager` インスタンスを作成します。


## ライセンス

このプロジェクトはMITライセンスの下でライセンスされています。詳細については、[LICENSE](./LICENSE)ファイルを参照してください。
