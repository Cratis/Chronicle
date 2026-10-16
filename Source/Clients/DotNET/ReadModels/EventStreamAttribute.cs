// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Declares a source-keyed read model whose state is folded from one event stream.
/// </summary>
/// <typeparam name="TSource">The event source definition declaring the stream.</typeparam>
/// <param name="stream">The declared stream name.</param>
/// <remarks>
/// The model is implicitly passive. Reads must supply a stream scope. The definition's event source type
/// filters both the fold and its guard; events written without that source type are not included.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class EventStreamAttribute<TSource>(string stream) : Attribute, IEventStreamReadModelDeclaration
    where TSource : IEventSource
{
    /// <summary>
    /// Gets the declared stream name.
    /// </summary>
    public string Stream { get; } = stream;

    /// <summary>
    /// Gets the event source definition type.
    /// </summary>
    public Type EventSource => typeof(TSource);
}
