// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_using_unsupported_services : given.testing_services
{
    Exception _readModelExplorerFailure;
    Exception _materializedReadModelsFailure;
    Exception _connectionsFailure;

    void Because()
    {
        _readModelExplorerFailure = Catch.Exception(() => _ = _services.ReadModelExplorer);
        _materializedReadModelsFailure = Catch.Exception(() => _ = _services.MaterializedReadModels);
        _connectionsFailure = Catch.Exception(() => _ = _services.Connections);
    }

    [Fact] void should_not_support_read_model_explorer() => _readModelExplorerFailure.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_describe_read_model_explorer_as_unsupported() => _readModelExplorerFailure.Message.ShouldEqual("ReadModelExplorer is not supported in test scenarios.");
    [Fact] void should_not_support_materialized_read_models() => _materializedReadModelsFailure.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_describe_materialized_read_models_as_unsupported() => _materializedReadModelsFailure.Message.ShouldEqual("MaterializedReadModels is not supported in test scenarios.");
    [Fact] void should_not_support_connections() => _connectionsFailure.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_describe_connections_as_unsupported() => _connectionsFailure.Message.ShouldEqual("Connections is not supported in test scenarios.");
}
