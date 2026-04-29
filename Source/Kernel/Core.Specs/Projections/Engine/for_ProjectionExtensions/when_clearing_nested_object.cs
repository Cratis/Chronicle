// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionExtensions;

public class when_clearing_nested_object : given.an_observable_and_event_setup
{
    PropertyPath _nestedProperty;

    void Establish()
    {
        _nestedProperty = new PropertyPath("command");
        _observable.ClearNested(_nestedProperty);
    }

    void Because() => _observable.OnNext(_eventContext);

    [Fact] void should_call_clear_nested_on_changeset() => _changeset.Received(1).ClearNested(_nestedProperty);
}
