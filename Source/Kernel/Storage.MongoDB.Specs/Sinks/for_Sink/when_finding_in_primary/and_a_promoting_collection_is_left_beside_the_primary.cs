// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

public class and_a_promoting_collection_is_left_beside_the_primary : given.a_sink_observing_a_container
{
    Exception? _error;
    ExpandoObject? _result;

    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _promotingExists = true;
    }

    async Task Because() => _error = await Catch.Exception(async () =>
        _result = await _sink.FindInPrimaryOrDefault(new Key("missing", ArrayIndexers.NoIndexers)));

    [Fact] void should_not_refuse_the_lookup() => _error.ShouldBeNull();
    [Fact] void should_return_no_instance() => _result.ShouldBeNull();
    [Fact] void should_check_collection_identity() => _listCollectionsCalls.ShouldEqual(2);
}
