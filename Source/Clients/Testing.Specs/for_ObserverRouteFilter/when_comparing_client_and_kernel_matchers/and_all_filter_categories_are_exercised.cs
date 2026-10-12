// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Testing.for_ObserverRouteFilter.when_comparing_client_and_kernel_matchers;

public class and_all_filter_categories_are_exercised : Specification
{
    readonly List<string> _differences = [];
    int _checked;

    void Because()
    {
        foreach (var type in new[] { typeof(Combined), typeof(Unfiltered), typeof(DefaultOnly), typeof(TagsOnly) })
        {
            var filter = new ObserverRouteFilter(type, Substitute.For<IEventSources>(), includeTagFilters: true);
            foreach (var source in new[] { "Orders", "Default", "Other" })
            {
                foreach (var stream in new[] { "Payments", "All", "Other" })
                {
                    foreach (var tags in new string[][] { [], ["blue"], ["green"], ["red"], ["blue", "green"] })
                    {
                        var context = EventContext.Empty with { EventSourceType = source, EventStreamType = stream, Tags = tags.Select(tag => new Tag(tag)).ToArray() };
                        _checked++;
                        if (filter.MatchesContext(context) != filter.MatchesClient(context))
                        {
                            _differences.Add($"{type.Name}/{source}/{stream}/{string.Join(',', tags)}");
                        }
                    }
                }
            }
        }
    }

    [Fact] void should_match_the_kernel_in_every_case() => _differences.ShouldBeEmpty();
    [Fact] void should_compare_a_nonempty_complete_matrix() => _checked.ShouldEqual(180);

    [FilterEventsByTag("blue"), FilterEventsByTag("green"), EventSourceType("Orders"), EventStreamType("Payments")]
    public class Combined;

    [Tag("blue")]
    public class Unfiltered;

    [EventSourceType("Default")]
    public class DefaultOnly;

    [FilterEventsByTag("blue"), FilterEventsByTag("green")]
    public class TagsOnly;
}
