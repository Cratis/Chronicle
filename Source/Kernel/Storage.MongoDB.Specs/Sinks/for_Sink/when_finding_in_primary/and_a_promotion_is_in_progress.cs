// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

public class and_a_promotion_is_in_progress : given.a_sink_observing_a_container
{
    Exception? _error;
    ExpandoObject? _result;

    void Establish()
    {
        _promotingExists = true;
    }

    async Task Because() => _error = await Catch.Exception(async () =>
        _result = await _sink.FindInPrimaryOrDefault(new Key("missing", ArrayIndexers.NoIndexers)));

    [Fact] void should_refuse_an_ambiguous_lookup() => _error.ShouldBeOfExactType<ReadModelPrimaryIsChanging>();
    [Fact] void should_not_return_an_instance() => _result.ShouldBeNull();
    [Fact] void should_log_the_ordinary_promotion_window_at_debug() => _logger.Entries.Count(entry => entry.Level == LogLevel.Debug).ShouldEqual(3);
    [Fact] void should_warn_only_when_giving_up() => _logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_name_the_promoting_collection() => _logger.Entries.All(entry => entry.Message.Contains(PromotingName, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_check_collection_identity() => _listCollectionsCalls.ShouldEqual(3);
}
