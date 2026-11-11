// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Represents an observer's reported progress and running state.
/// </summary>
/// <param name="Id">The observer identifier.</param>
/// <param name="LastHandled">The last handled event sequence number.</param>
/// <param name="Active">Whether the observer is active.</param>
internal sealed record ObserverValue(string Id, ulong LastHandled, bool Active);
