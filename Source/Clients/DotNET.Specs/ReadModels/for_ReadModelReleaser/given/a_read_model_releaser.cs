// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.given;

public class a_read_model_releaser : Specification
{
    public record Order(Guid Id, string Customer);

    protected IEventStore _eventStore;
    protected IJsonSchemaGenerator _schemaGenerator;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;

    /// <summary>
    /// The releaser under specification. It is internal to the client, so the field holding it can be no more
    /// accessible than internal; <c language="csharp">private protected</c> keeps the specification class itself
    /// public, because a non-public specification is silently never discovered by the runner (CRSPEC0006) and every
    /// assertion in it would pass without ever running.
    /// </summary>
    private protected ReadModelReleaser _releaser;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());

        _services = Substitute.For<IServices>();
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(_services);

        _releaser = new ReadModelReleaser(
            _eventStore,
            _schemaGenerator,
            _servicesAccessor,
            new JsonSerializerOptions(),
            Substitute.For<ILogger>());
    }
}
