// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// The exception that is thrown when an artifact declares a type protobuf cannot put on the wire.
/// </summary>
/// <param name="type">The type that has no wire representation.</param>
public class UnrepresentableTransportType(Type type)
    : Exception(
        $"'{type.FullName}' has no protobuf representation, and no contract primitive stands in for it. Generating it " +
        "anyway would emit an empty message - a schema that parses and transmits nothing. Add a [ProtoContract] " +
        "primitive under Source/Kernel/Contracts/Primitives with implicit conversions both ways, then map it in " +
        nameof(TransportTypes) + ".");
