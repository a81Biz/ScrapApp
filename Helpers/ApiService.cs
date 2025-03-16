using System;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScraperApp.Models;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://localhost/wp-json/wc/v3/";
    private const string ConsumerKey = "***";
    private const string ConsumerSecret = "***";

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

    public async Task<ApiResponse<T>> DataAsyncResponse<T>(HttpMethod method, string controller, string action = "", object? body = null)
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
                var filteredBody = FilterNullAndEmptyValues(body);

                string jsonBody = JsonConvert.SerializeObject(filteredBody, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            // 🔹 4️⃣ Enviar la solicitud y obtener la respuesta
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            // 🔹 5️⃣ Manejo de errores
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error en la solicitud HTTP: {response.StatusCode} - {responseString}");
                return new ApiResponse<T>
                {
                    StatusCode = response.StatusCode,
                    ErrorMessage = responseString
                };
            }

            // 🔹 6️⃣ Deserializar respuesta si es exitosa
            T data = JsonConvert.DeserializeObject<T>(responseString)!;

            return new ApiResponse<T>
            {
                StatusCode = response.StatusCode,
                Data = data
            };
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ Error en la solicitud HTTP: {ex.Message}");
            return new ApiResponse<T>
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ErrorMessage = ex.Message
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ Error al deserializar la respuesta: {ex.Message}");
            return new ApiResponse<T>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ErrorMessage = ex.Message
            };
        }
    }

    private static object FilterNullAndEmptyValues(object obj)
    {
        if (obj == null) return null;

        // 🔹 Convierte el objeto en un diccionario de propiedades
        var json = JObject.FromObject(obj);

        // 🔹 Filtra propiedades con valores `null`, `""`, o listas vacías
        var filteredJson = new JObject(
            json.Properties().Where(p =>
                p.Value.Type != JTokenType.Null &&                 // Excluir `null`
                (p.Value.Type != JTokenType.String || p.Value.ToString().Trim() != "") &&  // Excluir cadenas vacías
                (p.Value.Type != JTokenType.Array || p.Value.Any()) // Excluir listas vacías
            )
        );

        return filteredJson.ToObject(obj.GetType()); // 🔹 Convierte el JSON filtrado de vuelta al objeto original
    }
}

