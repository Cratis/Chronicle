// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a read names an occurrence that is not an occurrence of the read model.
/// </summary>
/// <param name="readModel">The identifier of the read model.</param>
public class UnknownReadModelOccurrence(ReadModelIdentifier readModel)
    : Exception($"The requested occurrence is not a known occurrence of read model '{readModel}'.");
