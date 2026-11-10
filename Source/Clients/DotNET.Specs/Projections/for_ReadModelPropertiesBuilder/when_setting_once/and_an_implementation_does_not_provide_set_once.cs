// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing.given;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_setting_once;

/// <summary>
/// SetOnce was added to an interface that shipped without it. The default implementation keeps that additive: an
/// implementation outside this assembly keeps compiling, and only fails if it is actually asked to set once.
/// </summary>
public class and_an_implementation_does_not_provide_set_once : Specification
{
    IStubBuilder _builder;
    Exception? _error;

    void Establish() => _builder = new BuilderWithoutClear();

    void Because() => _error = Catch.Exception(() => _builder.SetOnce(m => m.Note));

    [Fact] void should_report_that_the_operation_is_not_supported() => _error.ShouldBeOfExactType<ProjectionBuilderOperationNotSupported>();
    [Fact] void should_name_the_implementation() => _error!.Message.ShouldContain(nameof(BuilderWithoutClear));
}
