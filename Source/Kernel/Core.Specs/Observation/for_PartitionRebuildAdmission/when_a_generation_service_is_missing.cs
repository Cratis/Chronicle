// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_a_generation_service_is_missing : given.an_admission_check
{
    void Establish()
    {
        _store.GenerationFloor.Get().Returns(_floor);
        _services.GetService(typeof(IMaterializationBindingStorage)).Returns((object?)null);
    }

    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_fail_closed() => _result.ShouldBeFalse();
}
