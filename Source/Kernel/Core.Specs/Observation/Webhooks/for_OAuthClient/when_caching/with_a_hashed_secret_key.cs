// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_a_hashed_secret_key : given.a_controlled_oauth_client
{
    string _secretHash;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        await _client.AcquireToken(_authorization);
        var cache = (IEnumerable)typeof(OAuthClient).GetField("_tokens", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_client)!;
        var entry = cache.Cast<object>().Single();
        var key = entry.GetType().GetProperty("Key")!.GetValue(entry)!;
        _secretHash = (string)key.GetType().GetProperty("SecretHash")!.GetValue(key)!;
    }

    [Fact] void should_key_the_cache_with_the_sha256_hex_digest() => _secretHash.ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_authorization.ClientSecret.Value))));
}
