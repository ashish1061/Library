using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (a,b,c,d) => true };
        var client = new HttpClient(handler);
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.Add("Content-Type", "image/png");
        content.Add(fileContent, "file", "test.png");
        try {
            var response = await client.PostAsync("https://localhost:7002/api/books/upload-cover", content);
            Console.WriteLine(response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }
}
