// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalState;

public class when_encoding_namespaces : Specification
{
    static readonly string[] _namespaces = ["Default", "a.b", "a%2Eb", "a$b", "100%", "%252E", "%", ".", "$", "a.b$c%d"];

    string[] _encoded = [];
    string[] _decoded = [];

    void Because()
    {
        _encoded = [.. _namespaces.Select(Sink.FencedGlobalState.EncodeNamespace)];
        _decoded = [.. _encoded.Select(Sink.FencedGlobalState.DecodeNamespace)];
    }

    [Fact] void should_round_trip_every_namespace() => _decoded.ShouldEqual(_namespaces);
    [Fact] void should_not_contain_dots_or_dollars() => _encoded.ShouldEachConformTo(_ => !_.Contains('.') && !_.Contains('$'));
    [Fact] void should_give_distinct_encodings_for_distinct_namespaces() => _encoded.Distinct().Count().ShouldEqual(_namespaces.Length);
}
