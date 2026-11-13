// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Represents the accumulated model state for one source.
/// </summary>
/// <param name="Source">The event source.</param>
/// <param name="Count">The contribution count.</param>
/// <param name="Sum">The contribution sum.</param>
internal sealed record ModelValue(string Source, int Count, long Sum);
