// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a view over a property graph breaks a declaration rule. Every rule has its own type
/// and a stable <see cref="Reason"/> that is the same in every Chronicle client.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="reason">The stable reason code.</param>
/// <param name="message">The message that explains what is wrong.</param>
public abstract class InvalidGraphView(Type viewType, string reason, string message) : Exception(message)
{
    /// <summary>
    /// Gets the type of the view.
    /// </summary>
    public Type ViewType { get; } = viewType;

    /// <summary>
    /// Gets the stable reason code, for example <c language="text">UnknownEdge</c>.
    /// </summary>
    public string Reason { get; } = reason;
}
