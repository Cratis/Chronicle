// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ProjectionNaming.when_naming;

public class a_projection_that_shares_the_read_models_name : Specification
{
    string _result = null!;

    void Because() => _result = ProjectionNaming.TypeNameFor("Samples.Backoffice.Invoice", "Invoice");

    [Fact] void should_not_collide_with_the_read_model() => _result.ShouldNotEqual("Invoice");

    [Fact] void should_suffix_it_instead() => _result.ShouldEqual("InvoiceProjection");
}
