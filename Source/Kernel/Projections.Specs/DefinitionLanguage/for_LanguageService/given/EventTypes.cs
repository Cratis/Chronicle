// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public class DefaultEvent
{
    public string Name { get; set; } = string.Empty;
}

public record SimulationAdded(string Name, string? Source, string? Target);

public record UserCreated(string UserId, string Name, int Age, ContactInfo ContactInfo, Address Address, string? Email, string? MiddleName, bool IsActive, double Score, string? ContactInfoEmail, string? AddressCity);

public record ContactInfo(string? Email);

public record Address(string? City);

public record UserAdded(string UserId, string Name, string? Role, string? GroupId);

public record UserRoleChanged(string UserId, string Role, string? GroupId);

public record ActivityLogged(string Id, DateTimeOffset Occurred);

public record TestEvent(int Value, string? Name);

public record MoneyDeposited(decimal Amount);

public record MoneyWithdrawn(decimal Amount);

public record HubRouteAdded(string Id, string SimulationConfigurationId, string TransportTypeId, string SourceHubId, string DestinationHubId);

public record WarehouseRouteAdded(string Id, string SimulationConfigurationId, string TransportTypeId, string? SourceWarehouseId, string DestinationHubId);

public record HubRouteAddedToSimulationConfiguration(string Id, string SimulationConfigurationId, string TransportTypeId, string SourceHubId, string DestinationHubId);

public record WarehouseRouteAddedToSimulationConfiguration(string Id, string SimulationConfigurationId, string TransportTypeId, string? SourceWarehouseId, string DestinationHubId);

public record UserRemoved(string UserId, string? GroupId);

public record UserAssignedToGroup(string UserId, string GroupId);

public record GenericEvent(string UserId, string? FullName, string? EmailAddress);

public record OrderCreated(string OrderId, decimal Total);

public record UserLoggedIn(string UserId, DateTimeOffset Timestamp);

public record EventA(string Value, string SharedProperty, string? SharedValue);

public record EventB(string Value, string SharedProperty, string? SharedValue);

public record EventC(string Value, string SharedProperty, string? SharedValue);

public record GroupCreated(string GroupId, string Name, string? Description);

public record GroupRenamed(string GroupId, string NewName);

public record GroupDeleted(string GroupId);

public record MemberJoined(string GroupId, string UserId);

public record MemberLeft(string GroupId, string UserId);

public record UserAddedToGroup(string UserId, string GroupId, string Role, string UserName);

public record UserUpdated(string UserId, string Name, string? Email);

public record UserActivated(string UserId);

public record UserRegistered(string UserId, string Name);

public record UserRemovedFromGroup(string UserId, string GroupId);

public record UserDeleted(string UserId);

public record ProfileCreated(string UserId, string Name);

public record SettingsCreated(string SettingsId);

public record SettingsUpdated(string SettingsId);

public record DepartmentCreated(string DepartmentId, string Name);

public record EmployeeHired(string EmployeeId, string DepartmentId, string Name);
