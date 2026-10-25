// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using RegisterManyRequest = Cratis.Chronicle.Contracts.ReadModels.RegisterManyRequest;

namespace Cratis.Chronicle.Projections.for_Projections;

public class when_handlers_use_registered_container_names : given.projections_for_explicit_registration
{
    RegisterManyRequest _registration;

    void Establish()
    {
        _namingPolicy = Substitute.For<INamingPolicy>();
        _namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(call => $"custom-{call.Arg<Type>().Name}");
        _clientArtifacts.Projections.Returns([typeof(CustomerProjection)]);
        _clientArtifacts.ModelBoundProjections.Returns([typeof(RegisteredCustomer)]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<Customer>>(typeof(CustomerProjection)).Returns(new CustomerProjection());
        _projections = new Projections(_eventStore, _eventTypes, _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance);
    }

    async Task Because()
    {
        await _projections.Discover();
        await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());
        await _projections.Register<Overridden>(projection => projection.ContainerName("stock").From<ItemAdded>());
        var schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        schemaGenerator.GenerateForReadModel(Arg.Any<Type>()).Returns(new JsonSchema());
        var readModels = new ReadModels.ReadModels(
            _eventStore,
            _namingPolicy,
            _projections,
            Substitute.For<IReducers>(),
            _eventTypes,
            schemaGenerator,
            Options.Create(new ChronicleOptions()),
            _jsonSerializerOptions,
            Substitute.For<IReadModelWatcherManager>(),
            Substitute.For<IReducerObservers>(),
            Substitute.For<IMaterializedReadModels>(),
            NullLogger<ReadModels.ReadModels>.Instance);
        _registration = readModels.PrepareRegistration();
    }

    [Fact] void should_use_the_registered_fluent_container() => HandlerFor<Customer>().ContainerName.Value.ShouldEqual(ContainerFor<Customer>());
    [Fact] void should_use_the_registered_model_bound_container() => HandlerFor<RegisteredCustomer>().ContainerName.Value.ShouldEqual(ContainerFor<RegisteredCustomer>());
    [Fact] void should_use_the_registered_explicit_container() => HandlerFor<Inventory>().ContainerName.Value.ShouldEqual(ContainerFor<Inventory>());
    [Fact] void should_use_the_registered_override() => HandlerFor<Overridden>().ContainerName.Value.ShouldEqual(ContainerFor<Overridden>());
    [Fact] void should_apply_the_custom_naming_policy() => HandlerFor<Customer>().ContainerName.Value.ShouldEqual("custom-Customer");
    [Fact] void should_preserve_the_override() => HandlerFor<Overridden>().ContainerName.Value.ShouldEqual("stock");
    [Fact] void should_preserve_the_read_model_identifiers() => _projections.Definitions.Select(_ => _.ReadModel).ShouldContainOnly(
        typeof(Customer).FullName, typeof(RegisteredCustomer).FullName, typeof(Inventory).FullName, typeof(Overridden).FullName);

    IProjectionHandler HandlerFor<T>() => _projections.GetAllHandlers().Single(_ => _.ReadModelType == typeof(T));
    string ContainerFor<T>() => _registration.ReadModels.Single(_ => _.Type.Identifier == typeof(T).FullName).ContainerName;

    public record Overridden(string Name);

    public class CustomerProjection : IProjectionFor<Customer>
    {
        public void Define(IProjectionBuilderFor<Customer> builder) => builder.From<CustomerRegistered>();
    }
}
