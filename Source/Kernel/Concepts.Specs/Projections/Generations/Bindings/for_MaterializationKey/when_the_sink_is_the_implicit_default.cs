// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Concepts.Projections.Generations.Bindings.for_MaterializationKey;

public class when_the_sink_is_the_implicit_default : Specification
{
    MaterializationKey _key;
    string _value;

    void Establish() => _key = new(new(Guid.Parse("fa5dc415-e617-48a0-9975-fd1580aa321a")), "tenant one", "projection", "MongoDB", SinkConfigurationId.None);

    void Because() => _value = _key.Value;

    /// <summary>The identity every binding and gate was stored under before sinks could be named; it must never change.</summary>
    [Fact] void should_keep_the_identity_from_before_named_sinks() => _value.ShouldEqual("fa5dc415e61748a09975fd1580aa321a/tenant%20one/projection/MongoDB/00000000000000000000000000000000");
    [Fact] void should_be_the_same_when_the_default_name_is_given_explicitly() => (_key with { SinkName = SinkName.Default }).Value.ShouldEqual(_value);
    [Fact] void should_be_the_same_when_the_name_is_missing() => (_key with { SinkName = null! }).Value.ShouldEqual(_value);
}
