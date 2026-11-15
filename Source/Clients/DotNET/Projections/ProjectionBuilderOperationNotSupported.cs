// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a projection builder implementation does not support an operation that was added
/// to its interface after the implementation was written.
/// </summary>
/// <param name="builderType">The type of builder that does not support the operation.</param>
/// <param name="operation">The name of the operation that is not supported.</param>
public class ProjectionBuilderOperationNotSupported(Type builderType, string operation)
    : Exception($"The projection builder implementation '{builderType.FullName}' does not support '{operation}'.");
