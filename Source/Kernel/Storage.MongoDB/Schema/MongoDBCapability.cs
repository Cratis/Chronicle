// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Describes one probed capability without retaining server error text or connection details.
/// </summary>
/// <param name="State">The probe outcome.</param>
/// <param name="Reason">The explanation safe to include in diagnostics.</param>
public record MongoDBCapability(MongoDBCapabilityState State, string Reason)
{
    /// <summary>
    /// Gets a successful probe outcome.
    /// </summary>
    public static MongoDBCapability Supported { get; } = new(MongoDBCapabilityState.Supported, string.Empty);

    /// <summary>
    /// Describes an explicitly unsupported capability.
    /// </summary>
    /// <param name="reason">The explanation.</param>
    /// <returns>The capability outcome.</returns>
    public static MongoDBCapability Unsupported(string reason) => new(MongoDBCapabilityState.Unsupported, reason);

    /// <summary>
    /// Describes a probe that could not establish feature support.
    /// </summary>
    /// <param name="reason">The explanation.</param>
    /// <returns>The capability outcome.</returns>
    public static MongoDBCapability Inconclusive(string reason) => new(MongoDBCapabilityState.Inconclusive, reason);
}
