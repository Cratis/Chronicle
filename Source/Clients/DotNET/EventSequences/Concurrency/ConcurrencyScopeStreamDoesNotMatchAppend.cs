// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// The exception that is thrown when a definition-linked scope guards a different stream than a single-route append.
/// </summary>
/// <param name="definition">The definition shared by the scope and append.</param>
/// <param name="scopeStream">The stream guarded by the scope.</param>
/// <param name="appendStream">The stream receiving the append.</param>
public class ConcurrencyScopeStreamDoesNotMatchAppend(Type definition, EventStreamType scopeStream, EventStreamType appendStream)
    : Exception($"Concurrency scope for '{definition.FullName}.{scopeStream}' does not match append to '{definition.FullName}.{appendStream}'. Use a labelled independent condition to intentionally guard another stream, or enable ConcurrencyOptions.AllowRouteScopeStreamMismatch for legacy behavior.");
