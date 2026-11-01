// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

public class when_validating_system_store_writes : Specification
{
    static readonly string[] _reservedStoreNames = ["System", "system", "SYSTEM"];

    public static TheoryData<Type> AllCommands() => [.. given.public_store_write_commands.AllTypes];

    public static TheoryData<Type> Commands() => [.. given.public_store_write_commands.Types];

    public static TheoryData<Type, string> ReservedStores() => StoreRows(given.public_store_write_commands.Types);

    public static TheoryData<Type, string> AllowedReservedStores() => StoreRows(given.public_store_write_commands.AllowedStoreTypes);

    [Theory]
    [MemberData(nameof(AllCommands))]
    public void should_have_an_explicit_classification_and_reason(Type commandType)
    {
        var guarded = given.system_store_command_policy.Guarded.Contains(commandType.FullName!);
        var allowed = given.system_store_command_policy.Allowed.TryGetValue(commandType.FullName!, out var reason);
        ((guarded ^ allowed) && (!allowed || !string.IsNullOrWhiteSpace(reason))).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(AllowedReservedStores))]
    public async Task should_not_reserve_system_for_an_allowed_operation(Type commandType, string storeName)
    {
        var result = await given.public_store_write_commands.ValidateStore(commandType, storeName);
        var message = given.public_store_write_commands.RefusalMessage(commandType);
        result.Errors.Exists(error => error.ErrorMessage == message).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(ReservedStores))]
    public async Task should_reject_every_public_store_write(Type commandType, string storeName)
    {
        var result = await given.public_store_write_commands.ValidateStore(commandType, storeName);
        var message = given.public_store_write_commands.RefusalMessage(commandType);
        var member = given.public_store_write_commands.StoreMember(commandType);
        result.Errors.Exists(error => error.ErrorMessage == message && string.Equals(error.PropertyName, member, StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task should_not_reserve_an_application_store(Type commandType)
    {
        var result = await given.public_store_write_commands.ValidateStore(commandType, "some-event-store");
        var message = given.public_store_write_commands.RefusalMessage(commandType);
        result.Errors.Exists(error => error.ErrorMessage == message).ShouldBeFalse();
    }

    static TheoryData<Type, string> StoreRows(IEnumerable<Type> types)
    {
        var rows = new TheoryData<Type, string>();
        foreach (var type in types)
        {
            foreach (var store in _reservedStoreNames)
            {
                rows.Add(type, store);
            }
        }

        return rows;
    }
}
