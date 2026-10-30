// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class and_a_composite_passes_through_two_collections : given.an_invalid_index_registration
{
    [CompositeIndex("First.Name", "Second.Name")]
    record InvalidModel(List<Entry> First, Entry[] Second);
    record Entry(string Name);

    protected override Type ReadModelType => typeof(InvalidModel);

    async Task Because() => _exception = await Catch.Exception(_readModels.Register);

    [Fact] void should_reject_the_declaration() => _exception.ShouldBeOfExactType<InvalidIndexDeclaration>();
    [Fact] void should_explain_the_invalid_declaration() => _exception.Message.ShouldContain("more than one collection-element path");
    [Fact] void should_identify_the_read_model() => _exception.Message.ShouldContain(typeof(InvalidModel).FullName);
    [Fact] void should_not_send_a_registration() => _service.DidNotReceive().RegisterMany(Arg.Any<RegisterManyRequest>());
}
