using System;
using System.Data.SQLite;

namespace UpdateInvoiceReferenceCode
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=../../src/server/PigFarmManagement.Server/pigfarm.db";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Update InvoiceReferenceCode with TransactionCode values for all records
                string updateQuery = @"
                    UPDATE Feeds 
                    SET InvoiceReferenceCode = TransactionCode 
                    WHERE TransactionCode IS NOT NULL 
                    AND TransactionCode != '';";
                
                using (var command = new SQLiteCommand(updateQuery, connection))
                {
                    int rowsUpdated = command.ExecuteNonQuery();
                    Console.WriteLine($"Updated {rowsUpdated} rows with InvoiceReferenceCode from TransactionCode");
                }
                
                // Show all column information to debug
                string selectQuery = "PRAGMA table_info(Feeds);";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("\nColumn information:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"Column: {reader["name"]}, Type: {reader["type"]}, NotNull: {reader["notnull"]}, DefaultValue: {reader["dflt_value"]}");
                    }
                }
                
                // Show feed data with column names
                selectQuery = "SELECT Id, TransactionCode, InvoiceReferenceCode FROM Feeds;";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("\nAll feed results:");
                    Console.WriteLine("Id\t\t\t\t\tTransactionCode\tInvoiceReferenceCode");
                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["Id"]}\t{reader["TransactionCode"]}\t{reader["InvoiceReferenceCode"]}");
                    }
                }
            }
        }
    }
}