using System;
using Microsoft.Data.Sqlite;

// Simple test to check if CostDiscountPrice column exists and can store data
var connectionString = @"Data Source=D:\dz Projects\PigFarmManagement\src\server\PigFarmManagement.Server\pigfarm.db";

using var connection = new SqliteConnection(connectionString);
connection.Open();

// Check if CostDiscountPrice column exists
var checkCommand = new SqliteCommand("PRAGMA table_info(Feeds);", connection);
using var reader = checkCommand.ExecuteReader();

Console.WriteLine("Feeds table columns:");
while (reader.Read())
{
    var name = reader.GetString(1);
    var type = reader.GetString(2);
    Console.WriteLine($"  {name}: {type}");
}
reader.Close();

// Check existing data for CostDiscountPrice - look for recently imported data
var dataCommand = new SqliteCommand(@"
    SELECT Id, ProductName, CostDiscountPrice, UnitPrice, TransactionCode, CreatedAt 
    FROM Feeds 
    WHERE CreatedAt >= datetime('now', '-1 hour')
    ORDER BY CreatedAt DESC 
    LIMIT 10;", connection);
using var dataReader = dataCommand.ExecuteReader();

Console.WriteLine("\nRecently imported feeds with CostDiscountPrice:");
while (dataReader.Read())
{
    var id = dataReader.GetString(0);
    var name = dataReader.GetString(1);
    var costDiscount = dataReader.IsDBNull(2) ? 0 : dataReader.GetDecimal(2);
    var unitPrice = dataReader.IsDBNull(3) ? 0 : dataReader.GetDecimal(3);
    var transactionCode = dataReader.IsDBNull(4) ? "NULL" : dataReader.GetString(4);
    var createdAt = dataReader.GetString(5);
    Console.WriteLine($"  {transactionCode}: {name} - CostDiscountPrice: {costDiscount:C}, UnitPrice: {unitPrice:C}, Created: {createdAt}");
}
dataReader.Close();

connection.Close();