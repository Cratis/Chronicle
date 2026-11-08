// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents the sink a read model resolves to.
/// </summary>
/// <param name="Type">The <see cref="SinkTypeId"/> of the sink.</param>
/// <param name="Name">The <see cref="SinkName"/> of the configuration, or <see cref="SinkName.Default"/> for the implicit default sink.</param>
public record ResolvedSink(SinkTypeId Type, SinkName Name)
{
    /// <summary>
    /// Gets a value indicating whether the sink is the implicit default, which uses the kernel's own connection.
    /// </summary>
    public bool IsImplicitDefault => Name.IsDefault;
}
