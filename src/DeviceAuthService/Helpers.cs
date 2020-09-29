using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Ltwlf.Azure.B2C
{
    public static class Helpers
    {
        public static async Task<string> GetKeyByPattern(IConnectionMultiplexer muxer, string pattern)
        {
            string key = null;
            var server = muxer.GetServer(muxer.GetEndPoints().Single());
            var enumerator = server.KeysAsync(pattern: pattern).GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                key = enumerator.Current;
                break;
            }

            return key;
        }
        
        public static async Task<T> GetValueByKeyPattern<T>(IConnectionMultiplexer muxer, string pattern)
        {
            var key = await GetKeyByPattern(muxer, pattern);

            var json = muxer.GetDatabase().StringGet(key).ToString();

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}