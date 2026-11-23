// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Signals a discarded local continuation, distinct from an operation's own failure or cancellation.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Internal continuation control flow, caught at the persistence boundary, not a public exception contract.")]
internal sealed class ObserverContinuationInvalidated() : Exception("The observer continuation is no longer current.");
