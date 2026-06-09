// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_ReadModels.when_projecting_self_referential_children_with_keyed_from_event.and_keyed_from_event_is_appended_with_sub_features.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_projecting_self_referential_children_with_keyed_from_event;

[Collection(ChronicleCollection.Name)]
public class and_keyed_from_event_is_appended_with_sub_features(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture)
        : given.a_self_ref_keyed_module_with_features(chronicleFixture)
    {
        public KeyedSelfRefFeature Result;

        async Task Because()
        {
            await AppendFeature();
            await AppendSubFeature();
            await AppendTemplateUpdate();
            Result = await ChronicleFixture.EventStore.ReadModels.GetInstanceByKey<KeyedSelfRefFeature>(Feature1Id);
        }
    }

    [Fact]
    void should_have_feature_name() => Context.Result.Name.ShouldEqual("Feature 1");

    [Fact]
    void should_have_ui_template_set() => Context.Result.UITemplateId.ShouldEqual("template-1");

    [Fact]
    void should_have_one_sub_feature() => Context.Result.SubFeatures.ShouldNotBeNull().Count().ShouldEqual(1);

    [Fact]
    void should_have_sub_feature_name() => Context.Result.SubFeatures!.First().Name.ShouldEqual("Sub Feature 1");

    [Fact]
    void should_not_have_self_as_sub_feature() =>
        Context.Result.SubFeatures!.Any(sf => sf.Name == Context.Result.Name).ShouldBeFalse();
}
