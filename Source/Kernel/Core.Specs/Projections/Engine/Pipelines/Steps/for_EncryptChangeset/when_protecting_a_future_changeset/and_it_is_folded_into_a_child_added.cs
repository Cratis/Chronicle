// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_protecting_a_future_changeset;

public class and_it_is_folded_into_a_child_added : given.a_future_changeset
{
    Changeset<AppendedEvent, ExpandoObject> _main;

    async Task Establish()
    {
        _main = _changeset;
        AddChild();
        await _step.Perform(_projection, _context);
        _changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _context.Event, _main.CurrentState);
        _context = _context with { Changeset = _changeset };
        UpdateChild();
    }

    async Task Because()
    {
        await _step.ProtectFutureChangeset(_projection, _context);
        foreach (var change in _changeset.Changes)
        {
            _main.Add(change);
        }
    }

    [Fact] void should_hold_ciphertext_in_the_child_added() => ((IDictionary<string, object?>)_main.Changes.OfType<ChildAdded>().Single().Child)["email"].ShouldNotEqual(Email);
    [Fact] void should_not_hold_the_new_plaintext_anywhere_in_the_main_changes() => _main.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(_ => _.Differences).Any(_ => Equals(_.Changed, Email)).ShouldBeFalse();
}
