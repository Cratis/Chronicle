// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_FromEveryBuilder;

public class when_setting_once : Specification
{
    record ReadModel(DateTimeOffset FirstSeen, DateTimeOffset LastSeen);

    FromEveryBuilder<ReadModel> _builder;
    Contracts.Projections.FromEveryDefinition _result;

    void Establish() => _builder = new(new DefaultNamingPolicy());

    void Because()
    {
        _builder
            .SetOnce(m => m.FirstSeen).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.LastSeen).ToEventContextProperty(c => c.Occurred);
        _result = _builder.Build();
    }

    [Fact] void should_wrap_the_first_seen_value_in_set_once() => _result.Properties["FirstSeen"].ShouldEqual("$setOnce($eventContext(Occurred))");
    [Fact] void should_keep_the_last_seen_value_as_a_plain_set() => _result.Properties["LastSeen"].ShouldEqual("$eventContext(Occurred)");
}
