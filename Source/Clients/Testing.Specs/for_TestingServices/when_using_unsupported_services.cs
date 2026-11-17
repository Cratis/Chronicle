// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_using_unsupported_services : given.testing_services
{
    [Fact] void should_not_support_read_model_explorer() => Catch.Exception(() => _ = _services.ReadModelExplorer).ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_describe_read_model_explorer_as_unsupported() => Catch.Exception(() => _ = _services.ReadModelExplorer).Message.ShouldEqual("ReadModelExplorer is not supported in test scenarios.");
    [Fact] void should_describe_materialized_read_models_as_unsupported() => Catch.Exception(() => _ = _services.MaterializedReadModels).Message.ShouldEqual("MaterializedReadModels is not supported in test scenarios.");
    [Fact] void should_not_support_connections() => Catch.Exception(() => _ = _services.Connections).ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_describe_connections_as_unsupported() => Catch.Exception(() => _ = _services.Connections).Message.ShouldEqual("Connections is not supported in test scenarios.");
}
