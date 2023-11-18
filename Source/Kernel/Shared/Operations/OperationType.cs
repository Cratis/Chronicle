// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Operations;

/// <summary>
/// Represents type of a job.
/// </summary>
/// <param name="Value">String representation of the job type.</param>
/// <remarks>
/// The expected format is <c>Namespace.Type</c>.
/// </remarks>
public record OperationType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The <see cref="OperationType"/> for when it is not set.
    /// </summary>
    public static readonly OperationType NotSet = new("Undefined");

    /// <summary>
    /// Implicitly convert from <see cref="Type"/> to <see cref="OperationType"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to convert from.</param>
    public static implicit operator OperationType(Type type) => new(type.AssemblyQualifiedName ?? type.Name);

    /// <summary>
    /// Implicitly convert from <see cref="OperationType"/> to <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="OperationType"/> to convert from.</param>
    public static implicit operator Type(OperationType type) => Type.GetType(type.Value) ?? throw new UnknownClrTypeForOperationType(type);
}
