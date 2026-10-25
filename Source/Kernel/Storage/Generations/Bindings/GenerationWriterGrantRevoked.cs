// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// A fixed grant no longer authorizes a mutation.
/// </summary>
public sealed class GenerationWriterGrantRevoked() : InvalidOperationException("The generation writer grant is no longer current.");
