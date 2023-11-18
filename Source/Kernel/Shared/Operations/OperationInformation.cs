// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Operations;

namespace Aksio.Cratis.Kernel.Operations;

/// <summary>
/// Represents information about an operation.
/// </summary>
/// <param name="Id">The unique identifier of the operation.</param>
/// <param name="Name">The name of the operation.</param>
/// <param name="Details">The details of the operation.</param>
/// <param name="Type">The type of the operation.</param>
/// <param name="Occurred">When the operation occurred.</param>
public record OperationInformation(
    OperationId Id,
    OperationName Name,
    OperationDetails Details,
    OperationType Type,
    DateTimeOffset Occurred);
