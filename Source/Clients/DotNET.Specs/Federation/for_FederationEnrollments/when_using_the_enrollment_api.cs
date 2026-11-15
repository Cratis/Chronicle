// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Federation.for_FederationEnrollments;

public class when_using_the_enrollment_api : Specification
{
    IFederation _wire;
    FederationEnrollments _enrollments;
    CreateFederationEnrollmentRequest _createRequest;
    RedeemFederationEnrollmentRequest _redeemRequest;
    RevokeFederationEnrollmentRequest _revokeRequest;
    FederationEnrollmentCode _created;
    FederationRedemption _redeemed;
    FederationEnrollmentStatus _notEnrolled;
    Exception _failure;

    void Establish()
    {
        _wire = Substitute.For<IFederation>();
        _wire.CreateFederationEnrollment(Arg.Any<CreateFederationEnrollmentRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _createRequest = call.Arg<CreateFederationEnrollmentRequest>();
                return new CommandResult<CreateFederationEnrollmentResponse> { Response = new() { Id = "enrollment-1", Code = "cfe1.code", ExpiresAt = DateTimeOffset.UnixEpoch.AddDays(7) } };
            });
        _wire.RedeemFederationEnrollment(Arg.Any<RedeemFederationEnrollmentRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _redeemRequest = call.Arg<RedeemFederationEnrollmentRequest>();
                return new CommandResult<RedeemFederationEnrollmentResponse>
                {
                    Response = new()
                    {
                        Identity = "spiffe://example/customer/acme/kernel/prod",
                        Serial = "ABC",
                        NotAfter = DateTimeOffset.UnixEpoch.AddDays(14),
                        Endpoint = "https://federation.example.com/",
                        AlreadyEnrolled = false
                    }
                };
            });
        _wire.RevokeFederationEnrollment(Arg.Any<RevokeFederationEnrollmentRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _revokeRequest = call.Arg<RevokeFederationEnrollmentRequest>();
                return new CommandResult();
            });
        _wire.CurrentEnrollmentStatus(Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new QueryResult<FederationEnrollmentStatusResponse> { Data = new() });

        var services = Substitute.For<IServices>();
        services.Federation.Returns(_wire);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _enrollments = new(connection);
    }

    async Task Because()
    {
        _created = await _enrollments.Create("acme", "prod");
        _redeemed = await _enrollments.Redeem("cfe1.code");
        await _enrollments.Revoke("enrollment-1");
        _notEnrolled = await _enrollments.GetStatus();

        _wire.RevokeFederationEnrollment(Arg.Any<RevokeFederationEnrollmentRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new CommandResult { ExceptionMessages = ["the enrollment is not known"] });
        _failure = await Catch.Exception(() => _enrollments.Revoke("nothing"));
    }

    [Fact] void should_send_the_subject_and_instance_when_creating() => (_createRequest.Subject, _createRequest.Instance).ShouldEqual(("acme", "prod"));
    [Fact] void should_return_the_code_and_expiry() => (_created.Id, _created.Code, _created.ExpiresAt).ShouldEqual(("enrollment-1", "cfe1.code", DateTimeOffset.UnixEpoch.AddDays(7)));
    [Fact] void should_not_show_the_code_when_printed() => _created.ToString().ShouldNotContain("cfe1.code");
    [Fact] void should_send_the_code_when_redeeming() => _redeemRequest.Code.ShouldEqual("cfe1.code");
    [Fact] void should_return_the_identity_when_redeeming() => _redeemed.Identity.ShouldEqual("spiffe://example/customer/acme/kernel/prod");
    [Fact] void should_send_the_enrollment_id_when_revoking() => _revokeRequest.Id.ShouldEqual("enrollment-1");
    [Fact] void should_report_not_enrolled_without_an_expiry() => _notEnrolled.NotAfter.ShouldBeNull();
    [Fact] void should_throw_when_the_kernel_refuses() => _failure.ShouldNotBeNull();
}
