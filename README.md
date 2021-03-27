# MKCache

[![Nuget](https://img.shields.io/nuget/v/MKCache)](https://www.nuget.org/packages/MKCache)
[![netstandard2.0](https://img.shields.io/badge/netstandard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support)
<br/>
[![CI](https://img.shields.io/github/workflow/status/tommasobertoni/MKCache/CI/main)](https://github.com/tommasobertoni/MKCache/actions?query=workflow%3ACI+branch%3Amain)
[![Coverage](https://img.shields.io/coveralls/github/tommasobertoni/MKCache/main)](https://coveralls.io/github/tommasobertoni/MKCache?branch=main)
<br/>
[![License MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Almost called *yamc*, this is *yet another memory cache*.
<br />
This library is a thin layer of abstraction over `Microsoft.Extensions.Caching.Memory.MemoryCache` (.NET Standard 2.0) that **allows to cache an element using more than one key**.
<br/>
It does that by creating a different memory cache for each key of an element, so by caching the item multiple times.
<br/>
`MKCache` can also be used as a single-key cache.

## Scenario

Given the following type:

```csharp
class Country
{
    // Id unique in the database.
    public int Id { get; set; }

    public string Name { get; set; }

    // ISO 3166-2 standard, unique identifier.
    public string ISOCode { get; set; }
}
```

allow the cache consumers to find the cached item either by `Id` or `ISOCode`,
since they both identify uniquely the entity, and **their values don't overlap**,
meaning that no `Id` will never equal to an `ISOCode`.

## Usage

#### with a single key

```csharp
// This cache doesn't have any multi-key logic.
// It behaves exactly like a MemoryCache.
var cache = new MKCache<Country>();
```

#### with one or more specific keys

```csharp
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
Assert(countryFoundById != null);

var countryFoundByISO = cache.Get("US");
Assert(countryFoundByISO != null);

Assert(countryFoundById == countryFoundByISO);
```

## Reusing running asynchronous fetchers

It may happen that the cache is requested to resolve an item with the same key multiple times *concurrently*.
<br/>
If the cache doesn't have a reference to the item yet, it would cause it to execute as many item-resolution delegates (fetchers)
as the cache invocations.

In order to mitigate this, `MKCache` reuses the tasks created by the delegates, which are stored in a `ConcurrentDictionary`.

```csharp
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

cache.ReuseRunningAsyncFetchers = false;

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
```

This won't *ensure* that two or more concurrent requests with the same key will never be executed,
because **there's no lock** in play, but in general it will greatly improve the use of resources
and performances, proportionally to the amount of "twin" requests executed concurrently.

This behavior can be disabled by setting `cache.ReuseRunningAsyncFetchers = false;` (default is `true`).
