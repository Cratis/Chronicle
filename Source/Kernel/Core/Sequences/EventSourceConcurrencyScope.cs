// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the <see cref="ConcurrencyScope"/> to validate the events for one event source against, as part of
/// a batch spanning several event sources.
/// </summary>
/// <param name="EventSourceId">The event source the scope applies to.</param>
/// <param name="Scope">The scope to validate against.</param>
/// <remarks>
/// A list of these rather than an <c>IDictionary&lt;string, ConcurrencyScope&gt;</c> - the TypeScript proxy
/// generator does not emit the import for a type reached only as a dictionary value, so a map here would compile
/// on the kernel side and fail on the Workbench side. A sequence of a shared type already generates correctly.
/// </remarks>
public record EventSourceConcurrencyScope(string EventSourceId, ConcurrencyScope Scope);
