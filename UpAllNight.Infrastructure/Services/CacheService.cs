using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            return cached == null ? null : JsonSerializer.Deserialize<T>(cached);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
            };
            var json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => await _cache.RemoveAsync(key, cancellationToken);

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
            => await _cache.GetStringAsync(key, cancellationToken) != null;

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            // Redis ile kullanıldığında IConnectionMultiplexer ile pattern silme yapılabilir
            // Memory cache için bu özellik sınırlıdır
            return Task.CompletedTask;
        }
    }
}
