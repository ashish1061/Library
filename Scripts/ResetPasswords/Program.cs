using Microsoft.Data.SqlClient;
using BCrypt.Net;

var connectionString = "Server=172.20.20.210;Database=Library;User Id=sa;Password=Tango!@12345;Encrypt=False;";
var defaultPassword = "Library@123";
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

Console.WriteLine($"Default password: {defaultPassword}");
Console.WriteLine($"BCrypt hash: {hashedPassword}");
Console.WriteLine();

using var db = new SqlConnection(connectionString);
await db.OpenAsync();

// Update ALL employees to the hashed default password
var cmd = new SqlCommand("UPDATE Employee SET password = @Password", db);
cmd.Parameters.AddWithValue("@Password", hashedPassword);

var rowsAffected = await cmd.ExecuteNonQueryAsync();

Console.WriteLine($"Successfully updated {rowsAffected} employees to default password 'Library@123'.");
Console.WriteLine("All users will be forced to change their password on first login.");
