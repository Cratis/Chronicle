// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a request to generate declarative C# projection code from DSL.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Dsl">The DSL representation of the projection.</param>
[Command]
public record GenerateDeclarativeCode(string EventStore, string Namespace, string Dsl)
{
    /// <summary>
    /// Handles the generate declarative code request.
    /// </summary>
    /// <param name="projections">The <see cref="IProjections"/> service.</param>
    /// <returns>The generated C# code or errors.</returns>
    internal async Task<GeneratedCodeResult> Handle(IProjections projections)
    {
        var request = new GenerateDeclarativeCodeRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            Dsl = Dsl
        };

        var result = await projections.GenerateDeclarativeCodeFromDsl(request);

        return result.Value switch
        {
            GeneratedCode code => new GeneratedCodeResult(code.Code, []),
            ProjectionDefinitionParsingErrors errors => new GeneratedCodeResult(
                string.Empty,
                errors.Errors.Select(e => new ProjectionDefinitionSyntaxError(e.Message, e.Line, e.Column))),
            _ => throw new InvalidOperationException("Unexpected result type from GenerateDeclarativeCodeFromDsl")
        };
    }
}
