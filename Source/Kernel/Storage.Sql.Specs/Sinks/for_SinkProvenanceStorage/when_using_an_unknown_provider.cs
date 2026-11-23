// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_using_an_unknown_provider : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SinkProvenanceSchema.Schema((DatabaseType)int.MaxValue));

    [Fact] void should_refuse_unknown_provider_modes() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
