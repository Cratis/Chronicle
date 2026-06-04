// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for observing instances of a read model.
/// </summary>
[Message]
public class ObserveInstancesRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the optional occurrence to get instances from.
    /// </summary>
    public string? Occurrence { get; set; }
}
