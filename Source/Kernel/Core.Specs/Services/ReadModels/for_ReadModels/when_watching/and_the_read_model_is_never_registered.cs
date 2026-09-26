// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_read_model_is_never_registered : given.all_dependencies
{
    readonly TaskCompletionSource<Exception> _faulted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _result;

    void Establish()
    {
        var unpopulated = (Concepts.ReadModels.ReadModelDefinition)RuntimeHelpers.GetUninitializedObject(typeof(Concepts.ReadModels.ReadModelDefinition));
        _readModel.GetDefinition().Returns(unpopulated);
        ((ReadModels)_service).DelayBetweenReadModelChecks = (_, _) => Task.CompletedTask;
    }

    async Task Because()
    {
        _service.Watch(new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" })
            .Subscribe(_ => { }, error => _faulted.TrySetResult(error));

        _result = await _faulted.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact] void should_report_the_missing_read_model() => _result.ShouldBeOfExactType<ReadModelNotFound>();
    [Fact] void should_exhaust_the_registration_checks() => _readModel.Received(50).GetDefinition();
}
