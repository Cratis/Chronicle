// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the options for connecting a Chronicle client to an event store.
/// </summary>
/// <remarks>
/// Extends <see cref="ChronicleOptions"/> with settings that are required for connecting to a specific
/// event store and resolving the event store namespace. These settings apply to all .NET hosts
/// (ASP.NET Core, worker services, etc.). For ASP.NET Core-specific options such as HTTP header-based
/// namespace resolution, see <c>ChronicleAspNetCoreOptions</c>.
/// </remarks>
public class ChronicleClientOptions : ChronicleOptions
{
    /// <summary>
    /// Gets or sets the name of the event store to use.
    /// </summary>
    [Required]
    public EventStoreName EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the <see cref="IEventStoreNamespaceResolver"/> to use.
    /// Defaults to <see cref="DefaultEventStoreNamespaceResolver"/>, which always returns
    /// the default namespace. Override this to use a different resolution strategy.
    /// </summary>
    public Type EventStoreNamespaceResolverType { get; set; } = typeof(DefaultEventStoreNamespaceResolver);
}
