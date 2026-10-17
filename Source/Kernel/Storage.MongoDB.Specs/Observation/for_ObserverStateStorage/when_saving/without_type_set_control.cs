// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class without_type_set_control : given.an_observer_state_storage
{
    async Task Because() => await _storage.Save(_state);

    [Fact] void should_not_write_the_new_field_before_initialization() => _set.Contains("typeSetControl").ShouldBeFalse();
}
