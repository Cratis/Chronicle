// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Operations;

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Holds the state of a <see cref="IOperation{TRequest}"/>.
/// </summary>
public class OperationState
{
    /// <summary>
    /// Gets or sets the <see cref="OperationId"/>.
    /// </summary>
    public OperationId Id { get; set; } = OperationId.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="OperationName"/>.
    /// </summary>
    public OperationName Name { get; set; } = OperationName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="OperationDetails"/>.
    /// </summary>
    public OperationDetails Details { get; set; } = OperationDetails.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="OperationType"/>.
    /// </summary>
    public OperationType Type { get; set; } = OperationType.NotSet;

    /// <summary>
    /// Gets or sets when the operation occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the request associated with the operation.
    /// </summary>
    public object Request { get; set; } = default!;
}
