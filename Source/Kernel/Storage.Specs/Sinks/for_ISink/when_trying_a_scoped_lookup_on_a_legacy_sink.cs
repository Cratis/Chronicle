// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink;

public class when_trying_a_scoped_lookup_on_a_legacy_sink : Specification
{
    bool _found;

    async Task Because()
    {
        ISink sink = new given.a_legacy_sink();
        var result = await sink.TryFindRootKeyByChildValueInRoot("children.id", "child", new Key("root", ArrayIndexers.NoIndexers));
        _found = result.HasValue;
    }

    [Fact] void should_allow_the_resolver_to_fall_back_to_the_legacy_lookup() => _found.ShouldBeFalse();
}
