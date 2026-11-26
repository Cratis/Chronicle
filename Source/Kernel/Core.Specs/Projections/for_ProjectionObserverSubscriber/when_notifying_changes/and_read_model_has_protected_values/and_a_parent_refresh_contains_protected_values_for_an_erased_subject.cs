// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_read_model_has_protected_values;

public class and_a_parent_refresh_contains_protected_values_for_an_erased_subject : given.a_change_for_a_model_with_protected_values
{
    const string ParentOwner = "erased-parent-owner";
    readonly Key _parent = new("parent", ArrayIndexers.NoIndexers);
    string _protectedName;
    JsonObject _released;

    async Task Establish()
    {
        var schema = ReadModelSchema();
        var plaintext = _converter.ToExpandoObject(new JsonObject { ["id"] = "parent", ["name"] = "Ada Lovelace", ["secret"] = "display-only" }, schema);
        ((IDictionary<string, object?>)plaintext)[WellKnownProperties.ReadModelInstanceInitialized] = true;
        var parent = await _compliance.Apply(_key.EventStore, _key.Namespace, schema, ParentOwner, plaintext);
        _protectedName = _converter.ToJsonObject(parent, schema)["name"]!.GetValue<string>();
        await _keys.RecordErasureFor(_key.EventStore, _key.Namespace, ParentOwner);
        await _keys.DeleteFor(_key.EventStore, _key.Namespace, ParentOwner);

        var removal = new ChildRemovedFromAll("groups", "groupId", "removed-group", ArrayIndexers.NoIndexers);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.HasChanges.Returns(true);
        changeset.CurrentState.Returns(plaintext);
        changeset.Changes.Returns([removal]);
        _cachedPipeline.ReadCurrent(Arg.Any<IReadOnlyCollection<Key>>())
            .Returns(Task.FromResult<IReadOnlyList<(Key Key, ExpandoObject State)>>([(_parent, parent)]));
        var context = new ProjectionEventContext("removed-group", _event, changeset, ProjectionOperationType.Join | ProjectionOperationType.Remove, false);
        context.AddAffectedParentKeys([_parent]);
        _cachedPipeline.Handle(_event).Returns(context);
    }

    async Task Because()
    {
        _notifier.When(_ => _.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()))
            .Do(call =>
            {
                var notification = call.Arg<IReadOnlyList<ReadModelChangeNotification>>().Single();
                _document = notification.ReadModel;
                _change = notification.Change;
            });
        await _subscriber.OnNext("removed-group", [_event], new(null));
        await _silo.TimerRegistry.FireAllAsync();
        _released = await ReleaseAsWatchDoes();
    }

    [Fact] void should_route_the_parent_notification_to_the_stored_owner() => _change.Subject.ShouldEqual(ParentOwner);
    [Fact] void should_keep_the_parent_value_protected_until_watch_releases_it() => _document["name"]!.GetValue<string>().ShouldEqual(_protectedName);
    [Fact] void should_not_publish_erased_personal_data_to_watch() => _released["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_namespace_confidential_values() => _released["secret"]!.GetValue<string>().ShouldEqual("display-only");
}
