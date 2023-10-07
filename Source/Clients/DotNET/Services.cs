// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis;

/// <summary>
/// Represents services available for the client.
/// </summary>
public class Services
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Services"/> class.
    /// </summary>
    /// <param name="artifactsProvider"><see cref="IClientArtifactsProvider"/> for all artifacts.</param>
    public Services(IClientArtifactsProvider artifactsProvider)
    {
    }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/> available.
    /// </summary>
    public IEventTypes EventTypes { get; }
}
