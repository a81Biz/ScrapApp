using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://localhost/wp-json/wc/v3/";
    private const string ConsumerKey = "ck_7e7b334d5ac5d958e286c30db2a9326c4d332b67";
    private const string ConsumerSecret = "cs_70c81322464a22edb7e1003b912e61fba9c3a15f";

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(300)
        };
    }

    public async Task<T> DataAsync<T>(HttpMethod method, string controller, string action = "", object? body = null)
    {
        try
        {
            // 🔹 1️⃣ Construcción de la URL
            string url = string.IsNullOrWhiteSpace(action)
                ? $"{BaseUrl}{controller}"
                : $"{BaseUrl}{controller}/{action}";

            HttpRequestMessage request = new HttpRequestMessage(method, url);

            // 🔹 2️⃣ Generar `Authorization` dinámicamente en Base64
            string credentials = $"{ConsumerKey}:{ConsumerSecret}";
            string base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

            // 🔹 3️⃣ Si hay un cuerpo, se serializa a JSON con Newtonsoft.Json
            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                string jsonBody = JsonConvert.SerializeObject(body, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore // Evita enviar valores `null`
                });

                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            // 🔹 4️⃣ Enviar la solicitud
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            // 🔹 5️⃣ Manejo de errores en API
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error en la solicitud HTTP: {response.StatusCode} - {responseString}");
                return default!;
            }

            if (string.IsNullOrWhiteSpace(responseString))
            {
                Console.WriteLine("⚠️ Advertencia: La respuesta de la API está vacía.");
                return default!;
            }

            // 🔹 6️⃣ Deserializar respuesta JSON con Newtonsoft.Json
            return JsonConvert.DeserializeObject<T>(responseString)!;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ Error en la solicitud HTTP: {ex.Message}");
            return default!;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ Error al deserializar la respuesta: {ex.Message}");
            return default!;
        }
    }
}

