// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child entry for a logged shift, keyed by <see cref="Worker"/>.
/// </summary>
/// <param name="Worker">The worker, used as the child key.</param>
/// <param name="Hours">The hours logged for the worker.</param>
public record ShiftEntry(
    string Worker,
    decimal Hours);
