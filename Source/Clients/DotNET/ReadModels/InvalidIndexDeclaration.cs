// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception thrown when a read model declares an invalid index.
/// </summary>
/// <param name="readModelType">The type declaring the index.</param>
/// <param name="reason">Why the declaration is invalid.</param>
public sealed class InvalidIndexDeclaration(Type readModelType, string reason)
    : Exception($"Read model '{readModelType.FullName}' has an invalid index declaration: {reason}");
