// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_an_unexpressible_all_count : CodeGeneration.given.generated_order_code
{
    const string Declaration = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          all
            count ItemCount
        """;

    void Because() => _result = GenerateModelBound(Declaration);

    [Fact] void should_explain_the_unexpressible_count() => _result.ShouldContain("// Not expressible with model-bound attributes: every-mapping 'ItemCount' in projection 'Order'. Use the declarative projection.");
    [Fact] void should_not_misrepresent_count_as_a_property_name() => _result.ShouldNotContain("FromAll(property: \"count\")");
    [Fact] void should_keep_the_event_subscription() => _result.ShouldContain("[FromEvent<OrderPlaced>]");
    [Fact] void should_still_compile() => ShouldCompile(GenerateDeclarative(Declaration), _result);
}
