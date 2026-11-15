// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;

namespace Cratis.Chronicle.Integration.GraphProjections.when_projecting_the_organization;

/// <summary>
/// Represents the base of the specifications that tell the whole organization story to a declaration.
/// </summary>
/// <param name="fixture">The <see cref="GraphChronicleFixture"/>.</param>
public abstract class a_told_organization_story(GraphChronicleFixture fixture) : GraphSpecification(fixture)
{
    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => OrganizationEventTypes.All;

    /// <summary>
    /// Register the declaration when it is not discovered from the client's artifacts.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task Register() => Task.CompletedTask;

    async Task Because()
    {
        await Register();
        foreach (var step in OrganizationStory.Steps)
        {
            await Append(step.EventSourceId, step.Event, step.Occurred);
        }

        await ReadGraph();
    }
}
