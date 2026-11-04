// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_preserving_uncertain_input_acceptance : Specification
{
    List<Exception> _connectionFailures;
    List<Exception> _builderFailures;

    void Because()
    {
        _connectionFailures = [];
        _builderFailures = [];

        foreach (var input in a_connection_string_corpus.UncertainInputs)
        {
            _connectionFailures.Add(Catch.Exception(() => _ = new ChronicleConnectionString(input)));
            _builderFailures.Add(Catch.Exception(() => _ = new ChronicleConnectionStringBuilder(input)));
        }
    }

    [Fact] void should_keep_accepting_the_corpus_as_connections() => _connectionFailures.TrueForAll(value => value is null).ShouldBeTrue();
    [Fact] void should_keep_accepting_the_corpus_as_builders() => _builderFailures.TrueForAll(value => value is null).ShouldBeTrue();
}
