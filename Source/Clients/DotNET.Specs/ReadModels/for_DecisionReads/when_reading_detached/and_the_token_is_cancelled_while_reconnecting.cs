// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_token_is_cancelled_while_reconnecting : given.a_decision_reader
{
    bool _completedBeforeCancellation;
    Exception _error;

    async Task Establish()
    {
        await _reader.GetDetached<Model>("source");
        NeverConnect();
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var read = _reader.GetDetached<Model>("source", cancellation.Token);
        _completedBeforeCancellation = read.IsCompleted;
        await cancellation.CancelAsync();
        _error = await Record.ExceptionAsync(() => read);
    }

    [Fact] void should_return_control_while_the_connection_is_pending() => _completedBeforeCancellation.ShouldBeFalse();
    [Fact] void should_observe_the_token() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_connect_synchronously() => _ = _accessor.DidNotReceive().Services;
    [Fact] void should_only_fold_the_first_read() => _folds.ShouldEqual(1);
}
