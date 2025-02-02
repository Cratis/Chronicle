// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEngine.for_ProjectionExtensions.when_applying_from_filter;

public class when_handling_remove : given.an_observable_and_event_setup
{
    void Establish() => _observable.Remove();

    void Because() => _observable.OnNext(_eventContext);

    [Fact] void should_remove_on_changeset() => _changeset.Received(1).Remove();
}
