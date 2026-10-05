// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Filters a reactor or reducer to events from a declared event source and stream.
/// </summary>
/// <typeparam name="TSource">The event source definition that owns the stream.</typeparam>
/// <param name="stream">The declared stream to observe.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class FromEventSourceAttribute<TSource>(string stream) : Attribute
    where TSource : IEventSource
{
    /// <summary>
    /// Gets the declared event stream to observe.
    /// </summary>
    public string Stream { get; } = stream;
}
