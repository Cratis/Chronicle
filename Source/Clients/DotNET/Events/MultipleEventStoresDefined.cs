// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// The exception that is thrown when an observer defines event types originating from multiple event stores.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MultipleEventStoresDefined"/> class.
/// </remarks>
/// <param name="observerType">The type of the observer.</param>
/// <param name="eventStores">The collection of event store names found.</param>
public class MultipleEventStoresDefined(Type observerType, IEnumerable<string> eventStores)
    : Exception($"Observer '{observerType.FullName}' handles event types from multiple event stores: {string.Join(", ", eventStores.Select(s => $"'{s}'"))}. An observer can only observe events from a single event store.")
{
}
