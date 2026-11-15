// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.given;

/// <summary>
/// Distinctive synthetic content for diagnostic boundary specifications.
/// </summary>
public static class SensitiveTarget
{
    /// <summary>
    /// The synthetic user marker.
    /// </summary>
    public const string User = "hostileUser7d1c";

    /// <summary>
    /// The synthetic password marker.
    /// </summary>
    public const string Password = "4242424242424242";

    /// <summary>
    /// The path marker.
    /// </summary>
    public const string PathMarker = "hostilePath9a3e";

    /// <summary>
    /// The query marker.
    /// </summary>
    public const string QueryMarker = "hostileQuery5b2f";

    /// <summary>
    /// The fragment marker.
    /// </summary>
    public const string FragmentMarker = "hostileFrag3e8a";

    /// <summary>
    /// The receiver reason marker.
    /// </summary>
    public const string Reason = "hostileReason6c4d";

    /// <summary>
    /// Gets all sensitive markers.
    /// </summary>
    public static IEnumerable<string> Markers => [User, Password, PathMarker, QueryMarker, FragmentMarker, Reason, "hostile"];

    /// <summary>
    /// Creates an isolated metrics host.
    /// </summary>
    /// <returns>A unique reserved host.</returns>
    public static string UniqueHost() => $"r{Guid.NewGuid():N}.invalid";

    /// <summary>
    /// Creates a target with synthetic credentials.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>The raw target URL.</returns>
    public static string WithCredentials(string host) => $"https://{User}:{Password}@{host}:8443/{PathMarker}?token={QueryMarker}#{FragmentMarker}";

    /// <summary>
    /// Creates a target without credentials.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>The raw target URL.</returns>
    public static string WithPathAndQuery(string host) => $"https://{host}:8443/{PathMarker}?token={QueryMarker}#{FragmentMarker}";

    /// <summary>
    /// Detects synthetic sensitive text.
    /// </summary>
    /// <param name="text">The diagnostic text.</param>
    /// <returns>Whether a marker is present.</returns>
    public static bool Leaks(string? text) => text is not null && Markers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
