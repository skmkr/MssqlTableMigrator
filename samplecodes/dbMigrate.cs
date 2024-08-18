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

            var sourceTable = new TableIdentifier("SourceSchema", "SourceTableName");
            var destinationTable = new TableIdentifier("DestinationSchema", "DestinationTableName");
            
            if (manager.CheckTableExists(destinationTable))
            {
                throw new InvalidOperationException($"Already Exists {destinationTable.tostring()}");
            }
            
            manager.BeginTransaction();

            bool result = manager.CopyTableSchema(sourceTable, destinationTable);
            
            if (result)
            {
                manager.CommitTransaction();
                Console.WriteLine("CopySchema succeeded.");
            }
            else
            {
                manager.RollbackTransaction();
                Console.WriteLine("CopySchema failed.");
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