// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.when_dispatching_commands_with_body_properties;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_values_are_supplied : given.a_service_with_body_properties
{
    readonly Guid _id = Guid.NewGuid();
    RegisterProductWithOptions _command = null!;

    async Task Because() => _command = await GenerateAndDispatch<RegisterProductWithOptions>(request =>
    {
        Set(request, "Id", _id);
        Set(request, "IncludeReceipt", true);
        Set(request, "Notify", true);
        Set(request, "Name", "Product");
        Set(request, "Enabled", true);
        Set(request, "Status", Enum.ToObject(request.GetType().GetProperty("Status")!.PropertyType, 1));
        var details = Activator.CreateInstance(request.GetType().GetProperty("Details")!.PropertyType)!;
        Set(details, "Value", "Details");
        Set(request, "Details", details);
    });

    [Fact] void should_dispatch_once() => _dispatchCount.ShouldEqual(1);
    [Fact] void should_preserve_the_constructor_argument() => _command.Id.ShouldEqual(new ProductId(_id));
    [Fact] void should_assign_the_init_only_property() => _command.IncludeReceipt.ShouldBeTrue();
    [Fact] void should_assign_the_settable_property() => _command.Notify.ShouldBeTrue();
    [Fact] void should_convert_the_optional_concept() => _command.Name.ShouldEqual(new ProductName("Product"));
    [Fact] void should_assign_the_nullable_value() => _command.Enabled.ShouldEqual(true);
    [Fact] void should_convert_the_shared_enum() => _command.Status.ShouldEqual(CoreOwnedStatus.Second);
    [Fact] void should_convert_the_optional_shared_type() => _command.Details!.Value.ShouldEqual("Details");
    [Fact] void should_not_duplicate_constructor_properties_with_different_casing() => _requestType.GetProperties().Count(property => string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)).ShouldEqual(1);
    [Fact] void should_preserve_the_existing_constructor_field_number() => _indexes["Id"].ShouldEqual(7);
    [Fact] void should_append_all_six_body_fields() => _indexes.Count.ShouldEqual(7);
    [Fact] void should_number_new_fields_after_existing_fields() => _indexes.Where(pair => pair.Key != "Id").Select(pair => pair.Value).ShouldContainOnly([8, 9, 10, 11, 12, 13]);
}
