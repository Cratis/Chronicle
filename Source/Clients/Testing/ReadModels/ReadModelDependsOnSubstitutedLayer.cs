// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a <see cref="ReadModelScenario{TReadModel}"/> running in strict fidelity
/// mode is asked for a result for a read model whose shape depends on a layer the harness substitutes.
/// </summary>
/// <remarks>
/// By default a scenario reports its substitutions through
/// <see cref="ReadModelScenario{TReadModel}.Substitutions"/> and otherwise runs as usual. Opting in via
/// <see cref="ReadModelScenario{TReadModel}.WithStrictFidelity"/> turns that report into this error, so a suite
/// can ratchet: an existing read model keeps its in-process spec, and a newly added substituted shape has to be
/// covered by a kernel-backed spec before it can claim a green in-process one.
/// </remarks>
/// <param name="readModelType">The read model type under test.</param>
/// <param name="substitutions">The substitutions the read model depends on.</param>
public class ReadModelDependsOnSubstitutedLayer(Type readModelType, IEnumerable<ReadModelSubstitution> substitutions)
    : Exception(BuildMessage(readModelType, [.. substitutions]))
{
    static string BuildMessage(Type readModelType, IReadOnlyCollection<ReadModelSubstitution> substitutions) =>
        $"'{readModelType.Name}' depends on {substitutions.Count} layer(s) the in-process harness substitutes: " +
        $"{string.Join("; ", substitutions)}. " +
        "Strict fidelity is enabled — cover these with a kernel-backed spec, or remove the WithStrictFidelity() " +
        "call to keep running in-process and read Substitutions instead.";
}
