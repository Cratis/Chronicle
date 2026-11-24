// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// A call to a grain that has not been answered yet.
/// </summary>
/// <param name="Id">The identity of the call, unique within the silo.</param>
public readonly record struct PendingCall(long Id);
