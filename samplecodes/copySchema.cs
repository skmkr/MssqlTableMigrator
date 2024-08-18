using MssqlTableMigrator;

class Program
{
    static void Main(string[] args)
    {
        var connectionString = "ConnectionStringHere";

        using var manager = TableMigrationManagerFactory.Create.singleDBMigrationManager(connectionString);
        
        manager.Open();
        
        try
        {

            var sourceTable = new TableIdentifier("SourceSchema", "SourceTableName");
            var destinationTable = new TableIdentifier("DestinationSchema", "DestinationTableName");
            
            if (manager.CheckTableExists(destinationTable))
            {
                throw new InvalidOperationException($"Already Exists {destinationTable.tostring()}");
            }

            manager.BeginTransaction();
            
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