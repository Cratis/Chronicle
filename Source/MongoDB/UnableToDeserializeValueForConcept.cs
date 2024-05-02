// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB;

/// <summary>
/// Exception that gets thrown when a value cannot be deserialized for a concept.
/// </summary>
/// <param name="type">The type of the value that could not be deserialized.</param>
public class UnableToDeserializeValueForConcept(Type type) : Exception($"Unable to deserialize value for concept from value of type {type.FullName}");
