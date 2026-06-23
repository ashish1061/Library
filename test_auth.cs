using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true };
        using var client = new HttpClient(handler);
        
        var loginBody = new StringContent("{"email": "admin@jindalstainless.com", "password": "admin"}", Encoding.UTF8, "application/json");
        var loginRes = await client.PostAsync("https://localhost:7001/api/auth/login", loginBody);
        var loginStr = await loginRes.Content.ReadAsStringAsync();
        Console.WriteLine("Login: " + loginRes.StatusCode);
        
        if (loginRes.IsSuccessStatusCode)
        {
            var token = System.Text.Json.JsonDocument.Parse(loginStr).RootElement.GetProperty("accessToken").GetString();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var changeBodyCorrect = new StringContent("{"OldPassword": "admin", "NewPassword": "newpass123"}", Encoding.UTF8, "application/json");
            var changeResCorrect = await client.PostAsync("https://localhost:7001/api/auth/change-password", changeBodyCorrect);
            Console.WriteLine("Change (correct): " + changeResCorrect.StatusCode);
            Console.WriteLine("Response Body: " + await changeResCorrect.Content.ReadAsStringAsync());
            
            // Revert password
            var revertBody = new StringContent("{"OldPassword": "newpass123", "NewPassword": "admin"}", Encoding.UTF8, "application/json");
            await client.PostAsync("https://localhost:7001/api/auth/change-password", revertBody);
        }
    }
}
