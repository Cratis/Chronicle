// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.ModelBound;
using Cratis.Chronicle.Integration.GraphProjections.Organization;

namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// Represents the base of the specifications that tell a story to the organization declared with the model-bound attributes.
/// </summary>
/// <remarks>
/// A specification tells its story in <c language="text">Establish</c>, with <see cref="Tell(Guid, object, bool)"/>; the story is appended in order, and the graph is read afterwards.
/// </remarks>
/// <param name="fixture">The <see cref="GraphChronicleFixture"/>.</param>
public abstract class a_model_bound_organization(GraphChronicleFixture fixture) : GraphSpecification(fixture)
{
    readonly List<StoryStep> _story = [];

    /// <summary>
    /// Gets the sequence numbers the events of the story were appended at, in the order they were told.
    /// </summary>
    public List<ulong> Sequences { get; } = [];

    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => OrganizationEventTypes.All;

    /// <inheritdoc/>
    public override IEnumerable<Type> GraphElements => Elements;

    /// <summary>
    /// Gets the nodes and edges the graph is declared with. Override to declare fewer.
    /// </summary>
    protected virtual IEnumerable<Type> Elements => ModelBoundOrganizationTypes.All;

    /// <summary>
    /// Tell the next event of the story. Events occur a day after the one before.
    /// </summary>
    /// <param name="eventSourceId">The event source it is appended to.</param>
    /// <param name="event">The event.</param>
    /// <param name="fails">Whether an observer is expected to fail on it.</param>
    protected void Tell(Guid eventSourceId, object @event, bool fails = false) =>
        _story.Add(new(eventSourceId.ToString(), @event, OrganizationStory.Start.AddDays(_story.Count), fails));

    /// <summary>
    /// Append the story told so far and wait for it, which is what <c language="text">Because</c> does once; call it again for a second part of the story.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task AppendTheStory()
    {
        foreach (var step in _story)
        {
            Sequences.Add(await Append(step.EventSourceId, step.Event, step.Occurred, step.Fails));
        }

        _story.Clear();
        await ReadGraph();
    }

    async Task Because() => await AppendTheStory();
}
