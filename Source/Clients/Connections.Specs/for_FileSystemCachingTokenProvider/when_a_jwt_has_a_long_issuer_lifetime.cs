// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_a_jwt_has_a_long_issuer_lifetime : given.a_caching_provider
{
    DateTimeOffset _latestExpiry;
    DateTimeOffset _expiry;

    void Establish() => _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(JwtExpiringIn(TimeSpan.FromHours(2)));

    async Task Because()
    {
        await _provider.GetAccessToken();
        _latestExpiry = DateTimeOffset.UtcNow.AddMinutes(55);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(_cacheFile));
        _expiry = document.RootElement.GetProperty("Expiry").GetDateTimeOffset();
    }

    [Fact] void should_cap_the_expiry_at_the_configured_lifetime() => (_expiry <= _latestExpiry).ShouldBeTrue();
}
