// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

#pragma warning disable SA1402, SA1649, CA1819 // The declarations of one graph live together, and an array of primitives is what a graph property is.
namespace Cratis.Chronicle.Integration.GraphProjections.Organization;

/// <summary>
/// One step of a story: an event appended to an event source.
/// </summary>
/// <param name="EventSourceId">The event source.</param>
/// <param name="Event">The event.</param>
/// <param name="Occurred">When it occurred.</param>
/// <param name="Fails">Whether an observer is expected to fail on the event.</param>
public record StoryStep(EventSourceId EventSourceId, object Event, DateTimeOffset Occurred, bool Fails = false);

/// <summary>
/// The story every declaration of the organization is told, so that what they build can be compared.
/// </summary>
public static class OrganizationStory
{
    /// <summary>The person Ada.</summary>
    public static readonly Guid Ada = Guid.Parse("aa000000-0000-4000-8000-000000000001");

    /// <summary>The person Bob.</summary>
    public static readonly Guid Bob = Guid.Parse("bb000000-0000-4000-8000-000000000002");

    /// <summary>The person Cy, who is removed.</summary>
    public static readonly Guid Cy = Guid.Parse("cc000000-0000-4000-8000-000000000003");

    /// <summary>The group Engineering.</summary>
    public static readonly Guid Engineering = Guid.Parse("e0000000-0000-4000-8000-000000000011");

    /// <summary>The group Operations.</summary>
    public static readonly Guid Operations = Guid.Parse("e0000000-0000-4000-8000-000000000012");

    /// <summary>The place Lobby.</summary>
    public static readonly Guid Lobby = Guid.Parse("f0000000-0000-4000-8000-000000000021");

    /// <summary>The place Cafe.</summary>
    public static readonly Guid Cafe = Guid.Parse("f0000000-0000-4000-8000-000000000022");

    /// <summary>The first visit.</summary>
    public static readonly Guid FirstVisit = Guid.Parse("d0000000-0000-4000-8000-000000000031");

    /// <summary>The second visit.</summary>
    public static readonly Guid SecondVisit = Guid.Parse("d0000000-0000-4000-8000-000000000032");

    /// <summary>The third visit.</summary>
    public static readonly Guid ThirdVisit = Guid.Parse("d0000000-0000-4000-8000-000000000033");

    /// <summary>The time the story starts.</summary>
    public static readonly DateTimeOffset Start = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Gets the steps of the story. Events occur one day apart.
    /// </summary>
    public static IReadOnlyList<StoryStep> Steps { get; } = Build();

    static List<StoryStep> Build()
    {
        var steps = new List<StoryStep>();
        void Tell(Guid source, object @event) => steps.Add(new(source.ToString(), @event, Start.AddDays(steps.Count)));

        Tell(Ada, new OrgPersonRegistered(Ada, "Ada", ["admin", "dev"]));
        Tell(Bob, new OrgPersonRegistered(Bob, "Bob", ["dev"]));
        Tell(Cy, new OrgPersonRegistered(Cy, "Cy", []));
        Tell(Engineering, new OrgGroupCreated(Engineering, "Engineering"));
        Tell(Operations, new OrgGroupCreated(Operations, "Operations"));
        Tell(Ada, new OrgMemberAdded(Ada, Engineering));
        Tell(Bob, new OrgMemberAdded(Bob, Engineering));
        Tell(Bob, new OrgMemberAdded(Bob, Operations));
        Tell(Cy, new OrgMemberAdded(Cy, Engineering));
        Tell(Bob, new OrgMemberRemoved(Bob, Engineering));
        Tell(Ada, new OrgPersonRenamed(Ada, "Ada L."));
        Tell(Ada, new OrgPointsAwarded(Ada, 10));
        Tell(Ada, new OrgPointsAwarded(Ada, 5));
        Tell(Ada, new OrgPointsRevoked(Ada, 3));
        Tell(Ada, new OrgPlaceVisited(Ada, Lobby, FirstVisit));
        Tell(Ada, new OrgPlaceVisited(Ada, Lobby, SecondVisit));
        Tell(Bob, new OrgPlaceVisited(Bob, Cafe, ThirdVisit));
        Tell(Ada, new OrgPersonGreeted(Ada, Bob, 3));
        Tell(Ada, new OrgPersonGreeted(Ada, Bob, 4));
        Tell(Ada, new OrgPersonGreeted(Ada, Cy, 1));
        Tell(Ada, new OrgGreetingRetracted(Ada, Bob, 3));
        Tell(Cy, new OrgPersonRemoved(Cy));
        return steps;
    }
}
