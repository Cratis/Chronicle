// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system that invokes a <see cref="IReadModelReactor"/> handler method for a read model change.
/// </summary>
public interface IReadModelReactorInvoker
{
    /// <summary>
    /// Invoke a read model reactor handler method for a change.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> used to append any side-effect events.</param>
    /// <param name="reactor">The reactor instance.</param>
    /// <param name="method">The <see cref="ReadModelReactorMethod"/> to invoke.</param>
    /// <param name="readModel">The read model instance that changed, if any.</param>
    /// <param name="changeContext">The <see cref="EventContext"/> of the event that caused the change.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve additional dependencies.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Invoke(
        IEventStore eventStore,
        object reactor,
        ReadModelReactorMethod method,
        object? readModel,
        EventContext changeContext,
        IServiceProvider serviceProvider);
}
