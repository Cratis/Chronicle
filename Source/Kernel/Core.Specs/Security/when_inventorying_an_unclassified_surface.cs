// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

public class when_inventorying_an_unclassified_surface : Specification
{
    string[] _unlisted;

    void Because() => _unlisted = given.store_scoped_grpc_inventory.Unlisted(
        given.store_scoped_grpc_inventory.For(typeof(given.IUnclassifiedStoreService)),
        given.store_scoped_grpc_operations.All).ToArray();

    [Fact] void should_report_every_unclassified_store_operation() => _unlisted.ShouldContainOnly([
        "Cratis.Chronicle.Security.given.IUnclassifiedStoreService.Execute",
        "Cratis.Chronicle.Security.given.IUnclassifiedStoreService.ExecuteNested",
        "Cratis.Chronicle.Security.given.IUnclassifiedStoreService.ExecuteConcept"]);
    [Fact] void should_not_report_a_global_operation() => _unlisted.ShouldNotContain("Cratis.Chronicle.Security.given.IUnclassifiedStoreService.ExecuteGlobal");
}
