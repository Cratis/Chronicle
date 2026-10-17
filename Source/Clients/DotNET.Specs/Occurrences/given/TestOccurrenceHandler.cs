// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.given;

public sealed class TestOccurrenceHandler : IOccurrenceHandler<TestTrigger>, IAsyncDisposable
{
    public Func<TestTrigger, OccurrenceContext, CancellationToken, Task> OnHandle { get; set; } = (_, _, _) => Task.CompletedTask;

    public bool Disposed { get; private set; }

    Task IOccurrenceHandler<TestTrigger>.Handle(TestTrigger trigger, OccurrenceContext context, CancellationToken cancellationToken) => OnHandle(trigger, context, cancellationToken);

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
