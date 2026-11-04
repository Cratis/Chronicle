// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseIdentity;

/// <summary>
/// The names that collide in the logical naming, which is why the rule refuses them.
/// </summary>
public class when_naming_logical_databases_for_reserved_names : Specification
{
    string _storeA;
    string _readModelsOfStoreEs;
    string _storeWithSeparators;
    string _namespaceOfSeparatedName;
    string _readModelsOfSeparatedStore;

    void Because()
    {
        _storeA = DatabaseIdentity.Store("a").LogicalName("c");
        _readModelsOfStoreEs = new DatabaseIdentity(DatabaseKind.ReadModels, "es", "a").LogicalName("c");
        _storeWithSeparators = DatabaseIdentity.Store("a+es+b").LogicalName("c");
        _namespaceOfSeparatedName = new DatabaseIdentity(DatabaseKind.Namespace, "a", "es+b").LogicalName("c");
        _readModelsOfSeparatedStore = new DatabaseIdentity(DatabaseKind.ReadModels, "es+a", EventStoreNamespaceName.Default.Value).LogicalName("c");
    }

    [Fact] void should_have_the_es_store_overlap_another_store() => _readModelsOfStoreEs.ShouldEqual(_storeA);
    [Fact] void should_have_a_separated_store_overlap_a_namespace() => _storeWithSeparators.ShouldEqual(_namespaceOfSeparatedName);
    [Fact] void should_have_separated_read_models_overlap_a_store() => _readModelsOfSeparatedStore.ShouldEqual(_storeA);
    [Fact] void should_refuse_these_names() => new[] { "es", "es+a", "a+es+b" }.Select(ResourceNames.ForEventStore).ShouldEachConformTo(_ => _ != ResourceNameViolation.None);
}
