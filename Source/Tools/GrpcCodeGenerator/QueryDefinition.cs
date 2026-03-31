// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents a read model with its associated query methods.
/// </summary>
/// <param name="readModelType">The read model type.</param>
/// <param name="methods">The query methods discovered on the read model.</param>
public class QueryDefinition(Type readModelType, IReadOnlyList<QueryMethodDefinition> methods)
{
    /// <summary>Gets the read model type.</summary>
    public Type ReadModelType { get; } = readModelType;

    /// <summary>Gets the query methods.</summary>
    public IReadOnlyList<QueryMethodDefinition> Methods { get; } = methods;
}

