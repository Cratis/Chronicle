// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when an event store name or namespace name breaks the resource name rule.
/// </summary>
/// <remarks>
/// The message never contains the supplied value.
/// </remarks>
/// <param name="message">The message.</param>
/// <param name="violation">The <see cref="ResourceNameViolation"/>.</param>
public class InvalidResourceName(string message, ResourceNameViolation violation) : Exception(message)
{
    /// <summary>
    /// Gets the <see cref="ResourceNameViolation"/>.
    /// </summary>
    public ResourceNameViolation Violation { get; } = violation;

    /// <summary>
    /// Create the exception for an event store name.
    /// </summary>
    /// <param name="violation">The <see cref="ResourceNameViolation"/>.</param>
    /// <returns>A new <see cref="InvalidResourceName"/>.</returns>
    public static InvalidResourceName ForEventStore(ResourceNameViolation violation) => new(ResourceNames.EventStoreRuleMessage, violation);

    /// <summary>
    /// Create the exception for a namespace name.
    /// </summary>
    /// <param name="violation">The <see cref="ResourceNameViolation"/>.</param>
    /// <returns>A new <see cref="InvalidResourceName"/>.</returns>
    public static InvalidResourceName ForNamespace(ResourceNameViolation violation) => new(ResourceNames.NamespaceRuleMessage, violation);
}
