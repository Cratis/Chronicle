// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_first_reader_cancels_during_agreement : given.a_decision_reader
{
    readonly TaskCompletionSource<GetDefinitionsResponse> _listing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _firstError;
    DecisionRead<Model> _secondRead;
    int _listingCalls;

    void Establish() => _readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ =>
        {
            ++_listingCalls;
            return _listing.Task.WaitAsync(_.Arg<CallContext>().CancellationToken);
        });

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var first = _reader.GetDetached<Model>("source", cancellation.Token);
        var second = _reader.GetDetached<Model>("source");
        await cancellation.CancelAsync();
        _firstError = await Record.ExceptionAsync(() => first.WaitAsync(TimeSpan.FromSeconds(3)));
        _listing.SetResult(new GetDefinitionsResponse
        {
            ReadModels = [new ReadModelDefinition
            {
                Type = new() { Identifier = typeof(Model).GetReadModelIdentifier() },
                ObserverIdentifier = "projection", ObserverType = ReadModelObserverType.Projection
            }]
        });
        _secondRead = await second;
    }

    [Fact] void should_cancel_only_the_first_waiter() => _firstError.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_share_the_agreement() => _listingCalls.ShouldEqual(1);
    [Fact] void should_return_the_other_read() => _secondRead.Exists.ShouldBeTrue();
}
