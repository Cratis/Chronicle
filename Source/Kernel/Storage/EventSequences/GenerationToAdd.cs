// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents a missing generation to persist atomically with its hash and provenance.
/// </summary>
/// <param name="Generation">The target generation.</param>
/// <param name="Content">The protected content.</param>
/// <param name="Hash">The hash of the protected content.</param>
/// <param name="Provenance">The source and migration version.</param>
public record GenerationToAdd(EventTypeGeneration Generation, ExpandoObject Content, EventHash Hash, GenerationProvenance Provenance);
