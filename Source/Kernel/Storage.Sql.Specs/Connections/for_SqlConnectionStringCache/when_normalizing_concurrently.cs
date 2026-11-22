// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache;

public class when_normalizing_concurrently : given.a_cache
{
    string[] _shared;
    string[] _rotated;

    void Establish()
    {
        _shared = new string[64];
        _rotated = new string[64];
    }

    void Because()
    {
        Parallel.For(0, _shared.Length, index => _shared[index] = Normalize(_input));
        Parallel.For(0, _rotated.Length, index => _rotated[index] = Normalize(_input with { Database = $"tenant_{index}" }));
    }

    [Fact] void should_share_the_normalization_for_the_same_inputs() => _shared.All(value => ReferenceEquals(value, _shared[0])).ShouldBeTrue();
    [Fact] void should_never_return_another_calls_placement() => _rotated.Select((value, index) => new NpgsqlConnectionStringBuilder(value).Database == $"tenant_{index}").All(matches => matches).ShouldBeTrue();
}
