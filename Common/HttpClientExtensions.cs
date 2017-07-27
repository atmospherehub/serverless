using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
    public static class HttpClientExtensions
    {

        public static async Task<T> GetAsync<T>(this HttpClient self, string requestUri)
            where T : class
        {
            var result = await self.GetAsync(requestUri);
            var payload = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
                throw new HttpRequestException($"Received non successful response[{requestUri}]: [{result.StatusCode}] {payload}");

            return payload.FromJson<T>();
        }
    }
}
