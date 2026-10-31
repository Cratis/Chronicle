// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class and_the_sink_changes : given.a_collection_stream
{
    async Task Because()
    {
        _observation = _readModels.Observe<Row>();
        Subscribe();
        await Emit(new Row("one", "Before"));
        await Emit(new Row("one", "After"), new Row("two", "Added"));
    }

    [Fact] void should_emit_again() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_emit_the_full_new_set() => _emissions[^1].ShouldContainOnly(new Row("one", "After"), new Row("two", "Added"));
}
