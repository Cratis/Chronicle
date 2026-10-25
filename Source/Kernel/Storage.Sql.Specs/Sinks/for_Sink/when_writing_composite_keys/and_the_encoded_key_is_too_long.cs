// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_composite_keys;

public class and_the_encoded_key_is_too_long : a_composite_keyed_sql_read_model
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => _sink.ApplyChanges(CompositeKey(new string('a', 500), "b"), ChangesetNamed("one"), 1UL));

    [Fact] void should_refuse_the_write() => _error.ShouldBeOfExactType<CompositeKeyTooLong>();
}
