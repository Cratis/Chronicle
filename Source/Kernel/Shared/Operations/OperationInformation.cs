// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Operations;

namespace Aksio.Cratis.Kernel.Operations;

/// <summary>
/// Represents information about an operation.
/// </summary>
/// <param name="OperationId">The unique identifier of the operation.</param>
/// <param name="Name">The name of the operation.</param>
/// <param name="Details">The details of the operation.</param>
/// <param name="Type">The type of the operation.</param>
public record OperationInformation(
    OperationId OperationId,
    OperationName Name,
    OperationDetails Details,
    OperationType Type);
