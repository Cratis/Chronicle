// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_instances_with_paging;

public class with_a_zero_take : given.instances_in_the_sink
{
    IEnumerable<PagedReadModel> _result = [];
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () => _result = await _readModels.Materialized.GetInstances<PagedReadModel>(5, 0));

    [Fact] void should_not_throw() => _error.ShouldBeNull();

    [Fact] void should_return_no_instances() => _result.ShouldBeEmpty();
}
