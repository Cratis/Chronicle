// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the outcome of a manual stream scope repair.
/// </summary>
/// <param name="IsSuccess">Whether the exact manual closure was removed.</param>
/// <param name="Error">The reason a repair was rejected, or None on success.</param>
public record ReopenStreamScopeOutcome(bool IsSuccess, ReopenStreamScopeError Error);
