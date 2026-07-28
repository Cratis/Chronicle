// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepCheckpointDebounce;

public class when_reporting_as_many_batches_as_the_interval : Specification
{
    JobStepCheckpointDebounce _debounce;
    bool _shouldWrite;

    void Establish()
    {
        _debounce = new JobStepCheckpointDebounce(3);
        _debounce.Report(afterEveryBatch: false);
        _debounce.Report(afterEveryBatch: false);
    }

    void Because() => _shouldWrite = _debounce.Report(afterEveryBatch: false);

    [Fact] void should_ask_for_a_write() => _shouldWrite.ShouldBeTrue();
}
