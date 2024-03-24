// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB;

/// <summary>
/// Exception that gets thrown when a concept is missing a value key.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingValueKeyInConcept"/> class.
/// </remarks>
public class MissingValueKeyInConcept() : Exception("Expected a concept object, but no key named 'Value' or 'value' was found on the object");
