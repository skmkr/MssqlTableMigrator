using MssqlTableMigrator;

class Program
{
    static void Main(string[] args)
    {
        var sourceConnectionString = "SourceConnectionStringHere";
        var destinationConnectionString = "DestinationConnectionStringHere";

        TableMigrationManagerFactory.Configuration.SetDefaultConnectionStrings(sourceConnectionString,destinationConnectionString);
        
        using var manager = TableMigrationManagerFactory.Create.dualDBMigrationManager();
        
        manager.Open();
        
        try
        {
            var sourceTable = new TableIdentifier("SourceSchema", "SourceTableName");
            
            if (manager.CheckTableExists(sourceTable))
            {
                throw new InvalidOperationException($"Already Exists {destinationTable.tostring()}");
            }
            
            manager.BeginTransaction();

            bool result = manager.CopyTableSchema(sourceTable);
            
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