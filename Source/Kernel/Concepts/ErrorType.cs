// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents a oneof between <typeparamref name="TEnum"/> and <see cref="Exception"/>.
/// </summary>
/// <typeparam name="TEnum">The type of the error.</typeparam>
[GenerateOneOf]
public partial class ErrorType<TEnum> : OneOfBase<TEnum, Exception>
    where TEnum : Enum;
