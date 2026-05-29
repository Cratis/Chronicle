// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to configure a capture that observes an API source.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ApiCaptureAttribute"/>.
/// </remarks>
/// <param name="api">The API name to observe.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ApiCaptureAttribute(string api) : Attribute
{
    /// <summary>
    /// Gets the API name to observe.
    /// </summary>
    public string Api { get; } = api;

    /// <summary>
    /// Gets or sets the poll interval.
    /// </summary>
    public string? Poll { get; init; }

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public string? Auth { get; init; }

    /// <summary>
    /// Gets or sets the route for the configured API.
    /// </summary>
    public string? Route { get; init; }
}
