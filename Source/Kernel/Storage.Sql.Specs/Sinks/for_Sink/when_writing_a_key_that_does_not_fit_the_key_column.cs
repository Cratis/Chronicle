// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_a_key_that_does_not_fit_the_key_column : given.a_sink_with_composite_keys
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () => await Write(KeyOf(new string('a', 150), new string('b', 150)), "too long"));

    [Fact] void should_refuse_the_write() => _error.ShouldBeOfExactType<CompositeKeyTooLong>();
    [Fact] void should_name_the_read_model() => _error!.Message.Contains("composite").ShouldBeTrue();
    [Fact] void should_state_the_length() => _error!.Message.Contains("312").ShouldBeTrue();
}
