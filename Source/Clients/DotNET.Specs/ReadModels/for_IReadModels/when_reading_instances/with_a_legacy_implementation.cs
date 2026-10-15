// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_IReadModels.when_reading_instances;

public class with_a_legacy_implementation : Specification
{
    IReadModels _readModels;
    IEnumerable<object> _instances;
    IEnumerable<object> _result;

    void Establish()
    {
        var inner = Substitute.For<IReadModels>();
        _instances = [new object()];
        inner.GetInstances<object>().Returns(_instances);
        _readModels = new given.LegacyReadModels(inner);
    }

    async Task Because() => _result = await _readModels.GetInstances<object>();

    [Fact] void should_preserve_existing_reads() => _result.ShouldEqual(_instances);
}
