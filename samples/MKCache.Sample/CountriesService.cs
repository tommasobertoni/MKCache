using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MKCache.Sample
{
    internal class CountriesService
    {
        private int _resolutionsCount;

        public int ResolutionsCount => _resolutionsCount;

        public async Task<Country> ResolveAsync(string isoCode)
        {
            await Task.Delay(500);
            return Resolve(isoCode);
        }

        public Country Resolve(string isoCode)
        {
            Interlocked.Increment(ref _resolutionsCount);

            return isoCode switch
            {
                "US" => new Country { Id = 1, Name = "United States of America", ISOCode = isoCode },
                "NO" => new Country { Id = 2, Name = "Norway", ISOCode = isoCode },
                _ => new Country { Id = GetId(isoCode), Name = $"Country: {isoCode}", ISOCode = isoCode }
            };
        }

        private static int GetId(string isoCode)
        {
            int id = isoCode.Select(x => x - 'A' + 1).Aggregate((x, y) => x + y);
            return id;
        }
    }
}
