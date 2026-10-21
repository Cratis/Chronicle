// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when the kernel reports an unrecognized scoped stream completion error.
/// </summary>
/// <param name="error">The error value reported by the kernel.</param>
public class UnknownCompleteStreamError(int error) : Exception($"The kernel reported an unknown stream completion error '{error}'.");
