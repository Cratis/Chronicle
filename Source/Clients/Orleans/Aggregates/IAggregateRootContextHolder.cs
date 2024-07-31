// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;

#pragma warning disable SA1402

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Internal interface for accessing the <see cref="IAggregateRootContext"/>.
/// </summary>
internal interface IAggregateRootContextHolder
{
    /// <summary>
    /// Gets the context of the aggregate root.
    /// </summary>
    IAggregateRootContext? Context { get; set; }
}
