// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.given;

public class a_read_model_releaser : Specification
{
    public record Order(Guid Id, string Customer);

    /// <summary>
    /// Carries only a namespace-scoped <see cref="EncryptedAttribute"/> value and deliberately declares no
    /// <c language="csharp">Id</c> or <see cref="SubjectAttribute"/> - the ordinary shape for a secret shared
    /// across every document in a namespace, such as a webhook signing secret.
    /// </summary>
    /// <param name="Secret">The namespace-scoped encrypted value.</param>
    public record WebhookConfiguration([property: Encrypted(EncryptionScope.Namespace)] string Secret);

    /// <summary>
    /// Carries only a global-scoped <see cref="EncryptedAttribute"/> value and deliberately declares no
    /// <c language="csharp">Id</c> or <see cref="SubjectAttribute"/> - the ordinary shape for an
    /// installation-wide secret, such as a license token.
    /// </summary>
    /// <param name="Token">The global-scoped encrypted value.</param>
    public record LicenseInformation([property: Encrypted(EncryptionScope.Global)] string Token);

    /// <summary>
    /// Carries a subject-scoped <see cref="PIIAttribute"/> value and declares an <c language="csharp">Id</c>,
    /// so a subject resolves normally - the regression case release must keep working exactly as before.
    /// </summary>
    /// <param name="Id">The read model's key, which doubles as its resolvable subject.</param>
    /// <param name="Email">The subject-scoped PII value.</param>
    public record Customer(Guid Id, [property: PII] string Email);

    /// <summary>
    /// Carries both a subject-scoped <see cref="PIIAttribute"/> value and a namespace-scoped
    /// <see cref="EncryptedAttribute"/> value, with no <c language="csharp">Id</c> or <see cref="SubjectAttribute"/> -
    /// the boundary case between the two: release must still run for the namespace-scoped value even though the
    /// PII value has no subject to be released against.
    /// </summary>
    /// <param name="Notes">The subject-scoped PII value, which has no subject to release against.</param>
    /// <param name="Secret">The namespace-scoped encrypted value, which needs no subject at all.</param>
    public record MixedProtection([property: PII] string Notes, [property: Encrypted(EncryptionScope.Namespace)] string Secret);

    protected IEventStore _eventStore;
    protected IJsonSchemaGenerator _schemaGenerator;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected ICompliance _compliance;

    readonly JsonSchemaGenerator _realGenerator = new(
        new ComplianceMetadataResolver(
            new KnownInstancesOf<ICanProvideComplianceMetadataForType>(new PIIMetadataProvider()),
            new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(new PIIMetadataProvider())),
        new SecurityMetadataResolver(
            new KnownInstancesOf<ICanProvideSecurityMetadataForType>(new EncryptedMetadataProvider()),
            new KnownInstancesOf<ICanProvideSecurityMetadataForProperty>(new EncryptedMetadataProvider())),
        new DefaultNamingPolicy());

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

        _compliance = Substitute.For<ICompliance>();
        _services = Substitute.For<IServices>();
        _services.Compliance.Returns(_compliance);
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

    /// <summary>
    /// Wires the mocked <see cref="IJsonSchemaGenerator"/> to hand back a real, fully-resolved schema for
    /// <typeparamref name="TReadModel"/>, generated by a real <see cref="JsonSchemaGenerator"/> wired with real
    /// <see cref="PIIMetadataProvider"/>/<see cref="EncryptedMetadataProvider"/> - the same resolution the runtime
    /// generator performs, so the schema actually carries the compliance/security metadata the fixture's attributes
    /// declare.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to generate the schema for.</typeparam>
    protected void GivenSchemaFor<TReadModel>() =>
        _schemaGenerator.Generate(typeof(TReadModel)).Returns(_realGenerator.Generate(typeof(TReadModel)));

    /// <summary>
    /// Stubs the kernel's release RPC to echo the payload back unchanged, capturing the request it was called with.
    /// </summary>
    protected void GivenReleaseEchoesPayload() =>
        _compliance.Release(Arg.Any<ReleaseRequest>()).Returns(callInfo =>
        {
            var request = callInfo.Arg<ReleaseRequest>();
            return Task.FromResult(new ReleaseResponse { Payload = request.Payload });
        });

    protected ReleaseRequest? LastReleaseRequest() =>
        _compliance.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ICompliance.Release))
            .Select(call => (ReleaseRequest)call.GetArguments()[0]!)
            .LastOrDefault();
}
