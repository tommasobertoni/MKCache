using System;
using System.Linq;
using System.Threading.Tasks;
using static System.Diagnostics.Trace;

namespace MKCache.Sample
{
    class Program
    {
        private static readonly CountriesService _countriesService = new();

        static async Task Main()
        {
            var cache = new MKCache<Country>(
                // Cache the items using both
                // their Id and their ISOCode.
                c => c.Id,
                c => c.ISOCode);

            // The delegate that will retrieve the country,
            // if it's not found in the cache.
            async static Task<Country> countryResolver()
            {
                return await _countriesService.ResolveAsync("US");
            }

            var countryCacheExpiration = TimeSpan.FromMinutes(30);

            // Set the item in cache,
            // fetched via the countryResolver delegate.
            var country = await cache.GetOrCreateAsync(
                "US",
                countryResolver,
                countryCacheExpiration);

            // Now the country can be found in the cache
            // using both its Id and its ISOCode

            var countryFoundById = cache.Get(country.Id);
            Assert(countryFoundById is not null);

            var countryFoundByISO = cache.Get("US");
            Assert(countryFoundByISO is not null);

            Assert(countryFoundById == countryFoundByISO);

            cache.Clear();

            // Reusing running asynchronous fetchers

            var people = new[]
            {
                new Person { Name = "Thomas", CountryISOCode = "US", },
                new Person { Name = "Astrid", CountryISOCode = "NO", },
                new Person { Name = "Elizabeth", CountryISOCode = "US", },
            };

            // The country "US" will be requested two times,
            // and if the cache doesn't hold the country's reference
            // _countriesService.ResolveAsync("US") would be invoked two times
            // if it wasn't for the ReuseRunningAsyncFetchers property.

            cache.ReuseRunningAsyncFetchers = true;

            var allCountriesTasks = people.Select(async p =>
            {
                return await cache.GetOrCreateAsync(
                    p.CountryISOCode,
                    () => _countriesService.ResolveAsync(p.CountryISOCode),
                    TimeSpan.FromMinutes(30));
            });

            var allCountries = await Task.WhenAll(allCountriesTasks);
            var allCountryNames = allCountries.Select(c => c.Name);
            var uniqueCountryNames = allCountryNames.Distinct();
        }
    }
}
