// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildIdentifier.when_matching;

public class and_the_key_is_scalar : Specification
{
    bool[] _eager;
    bool[] _lazy;

    void Because()
    {
        var guid = Guid.Parse("c3ffac8b-af97-4d80-ad48-561be8c60b33");
        (object? Stored, object? Lookup)[] keys = [("value", "value"), (7L, 7), ("7", 7), (guid.ToString(), guid), ("value", new for_JsonColumnDocuments.given.a_value("value")), (null, null)];
        _eager = keys.Select(pair => ChildIdentifier.For("id", pair.Lookup).Matches(pair.Stored, true)).ToArray();
        _lazy = keys.Select(pair => ChildIdentifier.For("id", pair.Lookup).Matches(pair.Stored, false)).ToArray();
    }

    [Fact] void should_keep_eager_scalar_matching() => _eager.ShouldEqual(new[] { true, true, true, true, true, true });
    [Fact] void should_keep_lazy_scalar_matching() => _lazy.ShouldEqual(new[] { true, true, true, true, true, true });
}
