// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when BSON serialization was frozen with an incompatible serializer for mutation state.
/// </summary>
/// <param name="valueType">The mutation value type.</param>
/// <param name="expectedSerializer">The required serializer type.</param>
/// <param name="actualSerializer">The serializer type already registered.</param>
public class IncompatibleEventSequenceMutationBsonSerializer(
    Type valueType,
    Type expectedSerializer,
    Type actualSerializer) :
    Exception($"The BSON serializer for '{valueType.FullName}' is '{actualSerializer.FullName}', but mutation persistence requires '{expectedSerializer.FullName}'.");
