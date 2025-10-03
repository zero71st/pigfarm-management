using System;
using System.Data.SQLite;

namespace DirectDatabaseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=../../src/server/PigFarmManagement.Server/pigfarm.db";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Test the exact query that EF Core would run
                string selectQuery = @"
                    SELECT 
                        Id, TransactionCode, InvoiceReferenceCode, ProductCode, ProductName
                    FROM Feeds 
                    WHERE PigPenId = 'bb6b86b3-90b8-4db4-ad80-32f7da4e810a'
                    ORDER BY FeedDate DESC;";
                
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("Direct database query results:");
                    Console.WriteLine("Id\t\t\t\t\tProductCode\tTransactionCode\tInvoiceReferenceCode");
                    while (reader.Read())
                    {
                        var id = reader["Id"]?.ToString() ?? "NULL";
                        var productCode = reader["ProductCode"]?.ToString() ?? "NULL";
                        var transactionCode = reader["TransactionCode"]?.ToString() ?? "NULL";
                        var invoiceRefCode = reader["InvoiceReferenceCode"]?.ToString() ?? "NULL";
                        Console.WriteLine($"{id}\t{productCode}\t{transactionCode}\t{invoiceRefCode}");
                    }
                }
            }
        }
    }
}