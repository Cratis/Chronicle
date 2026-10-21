// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_child_every_and_join_only_events : CodeGeneration.given.generated_order_code
{
    const string Declaration = NestedAndChildEvery + "\n    join Product on ProductId\n      with ProductRenamed\n        ProductName = Name";

    void Because() => _result = GenerateModelBound(Declaration);

    [Fact] void should_explain_the_unexpressible_join_mapping() => _result.ShouldContain("// Not expressible with model-bound attributes: every-mapping 'LastUpdated' for join event 'ProductRenamed' in children 'Lines'. Use the declarative projection.");
    [Fact] void should_keep_the_from_event_context_mapping() => _result.ShouldContain("SetFromContext<ItemAddedToOrder>(\"Occurred\")");
    [Fact] void should_keep_the_join() => _result.ShouldContain("Join<ProductRenamed>(eventPropertyName: nameof(ProductRenamed.Name))");
    [Fact] void should_not_emit_a_root_every_attribute_for_the_child() => _result.ShouldNotContain("[FromEvery");
    [Fact] void should_still_compile() => ShouldCompile(GenerateDeclarative(Declaration), _result);
}
