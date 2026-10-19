using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace VCheckViewer_Others.Services
{
    public class APIService
    {
        //private string url = "https://localhost:7247/"; // local API IIS
        private string url = "http://localhost:8080/"; // local API WPF

        public async Task<bool> TestConnection()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

                    var resp = await client.GetAsync(url + "Api/testconnection");
                    string content = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine(content);

                    if (resp.StatusCode.ToString() == "OK")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
