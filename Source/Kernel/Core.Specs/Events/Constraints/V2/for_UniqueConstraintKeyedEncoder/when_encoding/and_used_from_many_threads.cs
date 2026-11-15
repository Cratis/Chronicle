// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_encoding;

public class and_used_from_many_threads : Specification
{
    const int Iterations = 2000;
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    int _mismatches;

    void Because()
    {
        var encoder = new UniqueConstraintKeyedEncoder(_key);
        Parallel.For(0, Iterations, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
        {
            UniqueConstraintComponent[] components = [new($"value-{index}"), new(index, "int32")];
            var scope = new ResolvedConstraintScope(EventStreamId: $"stream-{index}");
            var valueMatches = encoder.EncodeValue(components).AsT0.Value == UniqueConstraintEncoder.Encode(_key, components).AsT0.Value;
            var scopeMatches = encoder.EncodeScope(scope).Value == UniqueConstraintScopeToken.Encode(_key, scope).Value;
            if (!valueMatches || !scopeMatches)
            {
                Interlocked.Increment(ref _mismatches);
            }
        });
    }

    [Fact] void should_match_the_static_encoder_on_every_thread() => _mismatches.ShouldEqual(0);
}
