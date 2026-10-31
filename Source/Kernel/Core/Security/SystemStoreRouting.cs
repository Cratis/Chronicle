// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The routing values the Arc filters inspect on commands and queries.
/// </summary>
internal static class SystemStoreRouting
{
    /// <summary>
    /// The names, in pascal case, of the properties and parameters that route a call, and so end up in grain keys.
    /// </summary>
    internal static readonly string[] Names =
    [
        "EventStore",
        "Namespace",
        "EventSequenceId",
        "ObserverId",
        "ProjectionId",
        "ReactorId",
        "ReducerId",
        "WebhookId",
        "ExternalServiceId",
        "SubscriptionId",
        "TargetEventStore",
        "ReadModel"
    ];

    /// <summary>
    /// Checks whether a routing name names an event store.
    /// </summary>
    /// <param name="name">The routing name.</param>
    /// <returns>True if it names an event store; false otherwise.</returns>
    internal static bool IsStoreName(string name) =>
        string.Equals(name, "EventStore", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "TargetEventStore", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether a type carries a string value, either directly or as a concept.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is a string or a string concept; false otherwise.</returns>
    internal static bool IsStringLike(Type type) => type == typeof(string) || typeof(ConceptAs<string>).IsAssignableFrom(type);

    /// <summary>
    /// Gets the string of a routing value.
    /// </summary>
    /// <param name="value">The value, a string or a string concept.</param>
    /// <returns>The string, or null if it is neither.</returns>
    internal static string? AsString(object? value) => value switch
    {
        string text => text,
        ConceptAs<string> concept => concept.Value,
        _ => null
    };

    /// <summary>
    /// Gets the caller of an operation.
    /// </summary>
    /// <param name="serviceProvider">The services of the operation.</param>
    /// <param name="authorized">The principal Arc authorized the operation for, if any.</param>
    /// <returns>The principal, or null if there is none.</returns>
    internal static ClaimsPrincipal? CallerFor(IServiceProvider serviceProvider, ClaimsPrincipal? authorized) =>
        authorized ?? (serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor)?.HttpContext?.User;
}
