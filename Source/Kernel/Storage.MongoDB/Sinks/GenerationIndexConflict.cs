// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// An existing generation index conflicts with the immutable execution and is never dropped or rewritten.
/// </summary>
/// <param name="name">The conflicting index.</param>
/// <param name="innerException">The server conflict, if any.</param>
public sealed class GenerationIndexConflict(string name, Exception? innerException = null) : InvalidOperationException($"Generation index '{name}' conflicts with its execution snapshot.", innerException);
