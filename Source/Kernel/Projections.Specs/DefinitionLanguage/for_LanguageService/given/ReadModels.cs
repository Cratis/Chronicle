// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable CA1819 // Properties should not return arrays

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public class DefaultReadModel
{
    public string Name { get; set; } = string.Empty;
}

public record Simulation(string Name);

public record UserReadModel(
    string Name,
    int Age,
    string? Email,
    string? MiddleName,
    bool IsActive,
    double Score,
    string Status,
    int Version,
    double Rating,
    string? Metadata,
    string? City,
    string? FullName,
    string? GroupId,
    string? UserId,
    string? SharedProperty,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt,
    Group[]? Groups,
    string? ContactInfoEmail,
    string? AddressCity);

public record GroupReadModel(string Name, string? Description, List<Member> Members);

public record Member(string UserId, string? Role, DateTimeOffset? JoinedAt);

public record ActivityReadModel(string Id, string EventSourceId, long SequenceNumber, DateTimeOffset Occurred, string? CorrelationId);

public record TestModel(string Name);

public record AccountReadModel(decimal Balance);

public record Model(string Id, string? Name, string? Email, DateTimeOffset LastUpdated, int? LoginCount, int? EventCount, int? RetryCount, decimal? Total);

public record UserGroupReadModel(string Name, List<UserMember> Members, List<Group>? Groups, string? SettingsId, string? Role, DateTimeOffset? CreatedAt, string? Description, int? MemberCount, DateTimeOffset? LastUpdated);

public record Group(string Id, string Name, string? Role, DateTimeOffset? JoinedAt);

public record UserMember(string UserId, string? Role, DateTimeOffset? JoinedAt);

public record TransportRouteReadModel(string Id, string SimulationConfigurationId, string TransportTypeId, string SourceHubId, string DestinationHubId, string? SourceWarehouseId);

public record CompanyReadModel(string Id, Department[]? Departments);

public record Department(string Id, string Name, List<Employee>? Employees);

public record Employee(string Id, string Name);

public record OrderReadModel(string Id, decimal Total);

public record Users(string Name);
