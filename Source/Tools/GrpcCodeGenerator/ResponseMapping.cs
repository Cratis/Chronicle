// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents a mapping from a domain type onto the contract message that carries it.
/// </summary>
/// <param name="DomainType">The type the artifact produces.</param>
/// <param name="ContractTypeName">The fully qualified name of the generated message.</param>
/// <param name="MethodName">The name of the generated mapping method.</param>
/// <param name="Members">The members to copy, as name and declared type.</param>
public record ResponseMapping(
    Type DomainType,
    string ContractTypeName,
    string MethodName,
    IReadOnlyList<(string Name, Type Type)> Members);
