// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when synthetic read-model input cannot be stored.
/// </summary>
/// <param name="reason">Why the append failed.</param>
public sealed class SyntheticEventAppendFailed(string reason) : Exception($"Synthetic event append failed: {reason}");
