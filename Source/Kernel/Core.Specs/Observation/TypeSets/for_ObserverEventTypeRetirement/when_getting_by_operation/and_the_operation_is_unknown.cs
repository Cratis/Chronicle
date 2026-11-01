// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_ObserverEventTypeRetirement.when_getting_by_operation;

public class and_the_operation_is_unknown : given.a_type_set_store
{
    IEnumerable<ObserverEventTypeRetirement> _result;

    void Because() => _result = ObserverEventTypeRetirement.GetByOperation("store", Guid.NewGuid(), _storage);

    [Fact] void should_have_no_result() => _result.ShouldBeEmpty();
}
