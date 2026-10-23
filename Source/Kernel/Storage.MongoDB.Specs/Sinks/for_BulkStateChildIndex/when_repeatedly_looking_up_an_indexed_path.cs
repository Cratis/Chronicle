// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_repeatedly_looking_up_an_indexed_path : given.an_index
{
    readonly CountingChildren _children = new(Document(("id", 42)));
    int _initialExtractions;
    int _finalExtractions;
    bool _allLookupsFoundTheRoot = true;

    void Establish()
    {
        // Install a cache-owned instrumented collection so cloning does not replace the instrumentation.
        _states["root"] = Document(("children", _children));
        _index.Update("root", _states["root"]);
        Find("[children].id", 42);
        _initialExtractions = _children.Enumerations;
    }

    void Because()
    {
        for (var lookup = 0; lookup < 1000; lookup++)
        {
            _allLookupsFoundTheRoot &= Find("[children].id", 42).SequenceEqual(["root"]);
        }

        _finalExtractions = _children.Enumerations;
    }

    [Fact] void should_build_the_path_once() => _initialExtractions.ShouldEqual(1);
    [Fact] void should_not_reextract_on_lookup() => _finalExtractions.ShouldEqual(_initialExtractions);
    [Fact] void should_answer_every_lookup() => _allLookupsFoundTheRoot.ShouldBeTrue();

    sealed class CountingChildren(object child) : IEnumerable<object>
    {
        public int Enumerations { get; private set; }

        public IEnumerator<object> GetEnumerator()
        {
            Enumerations++;
            yield return child;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
