// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration of an outbox that will be observed for an inbox.
/// </summary>
public class Outbox
{
    /// <summary>
    /// Gets the microservice identifier that holds the outbox.
    /// </summary>
    public string Microservice { get; init; } = Guid.Empty.ToString();
}
