// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents one field of a message.
/// </summary>
/// <param name="Number">The field number - the only thing binary protobuf actually carries.</param>
/// <param name="Name">The field name, which generated code and JSON go by.</param>
/// <param name="TypeName">The proto type: a scalar proto name such as <c>int32</c>, or a fully qualified message or enum name.</param>
/// <param name="Label">Whether the field is singular or repeated.</param>
/// <param name="OneOf">The name of the oneof the field belongs to, or <see langword="null"/> when it stands alone.</param>
public record WireField(int Number, string Name, string TypeName, WireFieldLabel Label, string? OneOf);
