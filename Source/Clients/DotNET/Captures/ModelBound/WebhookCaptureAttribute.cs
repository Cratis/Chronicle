// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to configure a capture that observes a webhook source.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="WebhookCaptureAttribute"/>.
/// </remarks>
/// <param name="path">The webhook path.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WebhookCaptureAttribute(string path) : Attribute
{
    /// <summary>
    /// Gets the webhook path.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public string? Auth { get; init; }
}
