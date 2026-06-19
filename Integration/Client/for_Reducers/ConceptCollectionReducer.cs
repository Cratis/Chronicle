// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers;

/// <summary>
/// Reducer that folds <see cref="ConceptCollectionItemAdded"/> events into a growing
/// <see cref="ConceptCollectionReadModel.Items"/> concept collection.
/// </summary>
[DependencyInjection.IgnoreConvention]
public class ConceptCollectionReducer : IReducerFor<ConceptCollectionReadModel>
{
    /// <summary>
    /// Gets the number of handled events.
    /// </summary>
    public int HandledEvents;

    /// <summary>
    /// Handles a <see cref="ConceptCollectionItemAdded"/> by appending the item to the collection.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <param name="input">The current read model.</param>
    /// <param name="ctx">The event context.</param>
    /// <returns>The updated read model.</returns>
    public Task<ConceptCollectionReadModel?> OnConceptCollectionItemAdded(ConceptCollectionItemAdded evt, ConceptCollectionReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        var items = input?.Items?.ToList() ?? [];
        items.Add(evt.Item);
        return Task.FromResult<ConceptCollectionReadModel?>(new ConceptCollectionReadModel(evt.Item, items));
    }

    /// <summary>
    /// Waits until the handled event count reaches the specified value.
    /// </summary>
    /// <param name="count">Target event count.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>A <see cref="Task"/> that completes when the count is reached.</returns>
    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
