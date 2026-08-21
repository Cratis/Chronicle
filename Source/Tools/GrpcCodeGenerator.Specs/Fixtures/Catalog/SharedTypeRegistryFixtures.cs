// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649 // Multiple test fixture types intentionally grouped in one file

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.SharedTypeCatalog;

/// <summary>
/// Stands in for a <c>[ReadModel]</c> declared under a Chronicle area namespace, so it is Chronicle-owned by
/// namespace and would otherwise pass every other check - the read-model exclusion is the only thing that has to
/// reject it, since it already becomes its own <c>&lt;Name&gt;Response</c> message through the per-service DTO
/// path.
/// </summary>
/// <param name="Id">The identifier.</param>
[ReadModel]
public record CoreOwnedReadModel(string Id)
{
    /// <summary>Returns all instances.</summary>
    /// <returns>An empty enumerable.</returns>
    public static IEnumerable<CoreOwnedReadModel> GetAll() => [];
}

/// <summary>
/// Stands in for a Core-owned enum declared directly under a Chronicle area namespace -
/// <c>Cratis.Chronicle.Jobs.JobStatus</c> is the real shape this mirrors.
/// </summary>
public enum CoreOwnedStatus
{
    /// <summary>Represents the first value.</summary>
    First = 0,

    /// <summary>Represents the second value.</summary>
    Second = 1,
}

/// <summary>
/// Stands in for a plain Core-owned record referenced by more than one artifact - <c>Identity</c> and
/// <c>Causation</c> are the real shapes this mirrors.
/// </summary>
public class CoreOwnedValue
{
    /// <summary>Gets or sets a value.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// A test-only marker for a class that should never be treated as a shared-type candidate.
/// </summary>
public interface INotAConcreteType;

/// <summary>
/// Stands in for a type declared abstract - never instantiable, so never something to mirror.
/// </summary>
public abstract class AbstractCoreType;

/// <summary>
/// Stands in for a <c>ConceptAs&lt;T&gt;</c> declared under a Chronicle area namespace - Chronicle-owned by
/// namespace, so the concept exclusion is what has to reject it. A concept already travels as its unwrapped
/// primitive (<see cref="Tools.GrpcCodeGenerator.TypeHelper.UnwrapConceptType"/>), so it must never reach the
/// registry as a type to mirror in its own right.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record CoreOwnedConcept(string Value) : ConceptAs<string>(Value);
