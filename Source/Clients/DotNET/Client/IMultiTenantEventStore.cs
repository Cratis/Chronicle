// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Client;

/// <summary>
/// Defines a multi tenant event store.
/// </summary>
public interface IMultiTenantEventStore
{
    /// <summary>
    /// Gets the <see cref="IMultiTenantEventSequences"/>.
    /// </summary>
    IMultiTenantEventSequences Sequences { get; }
}
