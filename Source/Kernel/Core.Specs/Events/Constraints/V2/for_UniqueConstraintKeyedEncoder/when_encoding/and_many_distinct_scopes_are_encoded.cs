// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_encoding;

public class and_many_distinct_scopes_are_encoded : Specification
{
    const int Scopes = UniqueConstraintKeyedEncoder.ScopeCacheLimit * 5;
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    int _cached;
    int _mismatches;

    void Because()
    {
        var encoder = new UniqueConstraintKeyedEncoder(_key);
        for (var index = 0; index < Scopes; index++)
        {
            var scope = new ResolvedConstraintScope(EventStreamId: $"stream-{index}");
            if (encoder.EncodeScope(scope) != UniqueConstraintScopeToken.Encode(_key, scope))
            {
                _mismatches++;
            }
        }
        _cached = encoder.CachedScopeCount;
    }

    [Fact] void should_keep_the_cache_within_its_bound() => _cached.ShouldBeLessThanOrEqual(UniqueConstraintKeyedEncoder.ScopeCacheLimit);
    [Fact] void should_still_return_correct_tokens() => _mismatches.ShouldEqual(0);
}
