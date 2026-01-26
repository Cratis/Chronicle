// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore.Namespaces;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents the settings for connecting to Chronicle.
/// </summary>
public class ChronicleAspNetCoreOptions : ChronicleOptions
{
    /// <summary>
    /// Gets or sets the name of the event store to use.
    /// </summary>
    [Required]
    public EventStoreName EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the HTTP header to use for resolving the namespace.
    /// </summary>
    [Required]
    public string NamespaceHttpHeader { get; set; } = "x-cratis-tenant-id";

    /// <summary>
    /// Gets or sets the type of the <see cref="IEventStoreNamespaceResolver"/> to use.
    /// If not set, defaults to <see cref="HttpHeaderEventStoreNamespaceResolver"/>.
    /// If an <see cref="IEventStoreNamespaceResolver"/> instance is set (via the base class property) and is not
    /// a <see cref="DefaultEventStoreNamespaceResolver"/>, it will be used instead of this type.
    /// </summary>
    public Type EventStoreNamespaceResolverType { get; set; } = typeof(HttpHeaderEventStoreNamespaceResolver);
}
