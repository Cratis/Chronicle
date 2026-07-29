// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when a type adorned with <see cref="JsonSchemaTypeAttribute"/> points at itself.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SelfReferencingJsonSchemaType"/> class.
/// Generating the schema for such a type would recurse forever, so it is rejected up front with a message
/// naming the offending type rather than being left to overflow the stack.
/// </remarks>
/// <param name="type"><see cref="Type"/> that represents itself.</param>
public class SelfReferencingJsonSchemaType(Type type) : Exception($"Type '{type.FullName}' is adorned with a {nameof(JsonSchemaTypeAttribute)} that points at itself. Point it at the type its converter actually produces.");
