// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorMethods;

public class when_discovering : Specification
{
    IReadOnlyList<ReadModelReactorMethod> _methods;

    void Because() => _methods = ReadModelReactorMethods.GetFor(typeof(ReactorWithAllMethods));

    [Fact] void should_discover_three_handler_methods() => _methods.Count.ShouldEqual(3);
    [Fact] void should_discover_the_added_method_as_a_single_read_model() => _methods.Single(_ => _.ChangeType == ReadModelChangeType.Added).IsCollection.ShouldBeFalse();
    [Fact] void should_discover_the_added_read_model_type() => _methods.Single(_ => _.ChangeType == ReadModelChangeType.Added).ReadModelType.ShouldEqual(typeof(WatchedReadModel));
    [Fact] void should_discover_the_modified_method_as_a_collection() => _methods.Single(_ => _.ChangeType == ReadModelChangeType.Modified).IsCollection.ShouldBeTrue();
    [Fact] void should_discover_the_modified_read_model_type() => _methods.Single(_ => _.ChangeType == ReadModelChangeType.Modified).ReadModelType.ShouldEqual(typeof(WatchedReadModel));
    [Fact] void should_discover_the_removed_method_as_a_single_read_model() => _methods.Single(_ => _.ChangeType == ReadModelChangeType.Removed).IsCollection.ShouldBeFalse();
    [Fact] void should_discover_only_the_watched_read_model_type() => ReadModelReactorMethods.GetReadModelTypesFor(typeof(ReactorWithAllMethods)).ShouldContainOnly(typeof(WatchedReadModel));
}
