// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_a_filter : given.a_collection_stream
{
    async Task Because()
    {
        _observation = _readModels.Observe<Row>(row => row.Name == "Keep");
        Subscribe();
        await Emit(new Row("one", "Keep"), new Row("two", "Discard"));
    }

    [Fact] void should_emit_only_matching_instances() => _emissions.Single().ShouldContainOnly(new Row("one", "Keep"));
    [Fact] void should_filter_the_complete_set() => _request.PageSize.ShouldEqual(int.MaxValue);
}
