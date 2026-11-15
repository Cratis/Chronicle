// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_a_named_sink;

public class and_the_read_model_names_no_sink : given.named_sinks
{
    ISink _result;

    async Task Because() => _result = await _sinks.GetFor(ReadModel(SinkName.Default));

    [Fact] void should_create_the_sink_on_the_default_connection() => _result.ShouldEqual(_defaultSink);
}
