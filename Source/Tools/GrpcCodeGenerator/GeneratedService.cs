// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents an implementation the generator produced.
/// </summary>
/// <param name="ServiceName">The name of the service.</param>
/// <param name="ContractTypeName">The fully qualified name of the contract it implements.</param>
/// <param name="TypeName">The fully qualified name of the implementation.</param>
/// <param name="Path">Where the implementation was written.</param>
public record GeneratedService(string ServiceName, string ContractTypeName, string TypeName, string Path);
