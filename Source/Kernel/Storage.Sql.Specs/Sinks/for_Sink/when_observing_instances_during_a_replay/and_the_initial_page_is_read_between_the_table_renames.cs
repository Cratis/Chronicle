// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances_during_a_replay;

public class and_the_initial_page_is_read_between_the_table_renames : when_ending_a_replay.given.a_replay_with_an_interleaved_reader
{
    readonly ReplaySubject<IEnumerable<ExpandoObject>> _pages = new();
    IDisposable _subscription;
    IEnumerable<ExpandoObject> _firstPage;
    IEnumerable<ExpandoObject> _rebuiltPage;
    IEnumerable<ExpandoObject> _pageAfterTheReplay;

    void Establish() => _afterPrimaryRename = async () =>
    {
        _subscription = _reader.ObserveInstances().Subscribe(_pages);
        _firstPage = await _pages.FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(10));
    };

    async Task Because()
    {
        await _sink.EndReplay(ReplayContext());
        _rebuiltPage = await _pages.FirstAsync(page => CountOf(page.Single()) == 2)
            .ToTask().WaitAsync(TimeSpan.FromSeconds(10));
        await _reader.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
        _pageAfterTheReplay = await _pages.FirstAsync(page => CountOf(page.Single()) == 3)
            .ToTask().WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _pages.Dispose();
    }

    static int CountOf(ExpandoObject instance) => (int)((IDictionary<string, object?>)instance)["count"]!;

    [Fact] void should_stream_the_previously_committed_state_during_promotion() => CountOf(_firstPage.Single()).ShouldEqual(1);
    [Fact] void should_stream_the_rebuilt_state_after_promotion() => CountOf(_rebuiltPage.Single()).ShouldEqual(2);
    [Fact] void should_stream_a_write_to_the_promoted_read_model() => CountOf(_pageAfterTheReplay.Single()).ShouldEqual(3);
}
