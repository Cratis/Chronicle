// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_mode_is_per_value_and_remove_event_carries_a_case_insensitive_composite_value : given.a_per_value_constraint
{
    void Establish()
    {
        _definition = _definition with
        {
            EventDefinitions = [new("added", ["Tenant", "Id"])],
            IgnoreCasing = true,
            RemovalEventDefinitions = [new("removed", ["Organization", "RemovedId"])]
        };
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["Organization"] = "ACME";
        ((IDictionary<string, object?>)content)["RemovedId"] = "SomeValue";
        var context = new ConstraintValidationContext([], _owner, "removed", content);
        _updater = new(_definition, context, _storage);
    }

    async Task Because() => await _updater.Update(42L);

    [Fact] async Task should_release_the_normalized_composite_in_declared_order() => await _storage.Received(1).RemoveValue(_owner, _definition, "f5e87e56c3eea097f6976d9781a152a63ad150bcca22dc88f537c1875d92d3d4", string.Empty);
}
