// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace Cratis.Chronicle;

/// <summary>
/// The single implementation of the rule for event store names and namespace names.
/// </summary>
/// <remarks>
/// The rule is applied to names supplied by any caller, or created by the kernel for a caller. Names must not
/// contain characters the storage and grain keys use as separators, and a few names are reserved.
/// </remarks>
public static class ResourceNames
{
    /// <summary>
    /// The message used when an event store name breaks the rule.
    /// </summary>
    public const string EventStoreRuleMessage = "The event store name is not valid. It must not be empty or contain '+', '#', '~', '$' or control characters, and the names 'es', 'admin', 'local' and 'config' are reserved.";

    /// <summary>
    /// The message used when a namespace name breaks the rule.
    /// </summary>
    public const string NamespaceRuleMessage = "The namespace name is not valid. It must not be empty or contain '+', '#', '~', '$' or control characters. The name 'es' is reserved, and so is 'main' in the event store 'chronicle'.";

    /// <summary>
    /// The message used when an event store name differs only in case from an existing one.
    /// </summary>
    public const string EventStoreCaseVariantMessage = "An event store whose name differs only in case already exists.";

    /// <summary>
    /// The message used when a namespace name differs only in case from an existing one in the same event store.
    /// </summary>
    public const string NamespaceCaseVariantMessage = "A namespace whose name differs only in case already exists in this event store.";

    /// <summary>
    /// The message used when the storage of a name would overlap the storage of another one.
    /// </summary>
    public const string OverlapMessage = "The name cannot be used because its storage would overlap an existing event store or namespace.";

    /// <summary>
    /// The message used when an event store or namespace is quarantined.
    /// </summary>
    public const string QuarantinedMessage = "The event store or namespace is unavailable because its storage overlaps another one. See the server log.";

    /// <summary>
    /// The message used when the names of the installation have not been checked yet.
    /// </summary>
    public const string NotReadyMessage = "The server has not finished checking existing names. Retry shortly.";

    const string NotSetName = "[NotSet]";
    const string ChronicleEventStore = "chronicle";
    const string MainNamespace = "main";

    static readonly SearchValues<char> _reservedCharacters = SearchValues.Create(CreateReservedCharacters());

    /// <summary>
    /// Check an event store name.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>The <see cref="ResourceNameViolation"/>, or <see cref="ResourceNameViolation.None"/>.</returns>
    public static ResourceNameViolation ForEventStore(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ResourceNameViolation.Empty;
        }

        if (name.Equals(NotSetName, StringComparison.OrdinalIgnoreCase))
        {
            return ResourceNameViolation.NotSet;
        }

        if (!HasNoReservedCharacters(name))
        {
            return ResourceNameViolation.ReservedCharacter;
        }

        return IsReservedEventStore(name) ? ResourceNameViolation.ReservedName : ResourceNameViolation.None;
    }

    /// <summary>
    /// Check a namespace name.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>The <see cref="ResourceNameViolation"/>, or <see cref="ResourceNameViolation.None"/>.</returns>
    public static ResourceNameViolation ForNamespace(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ResourceNameViolation.Empty;
        }

        if (!HasNoReservedCharacters(name))
        {
            return ResourceNameViolation.ReservedCharacter;
        }

        return name.Length == 2 && name.Equals("es", StringComparison.OrdinalIgnoreCase)
            ? ResourceNameViolation.ReservedName
            : ResourceNameViolation.None;
    }

    /// <summary>
    /// Check a combination of event store name and namespace name.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The <see cref="ResourceNameViolation"/>, or <see cref="ResourceNameViolation.None"/>.</returns>
    public static ResourceNameViolation ForCombination(string eventStore, string namespaceName) =>
        eventStore.Equals(ChronicleEventStore, StringComparison.OrdinalIgnoreCase) &&
        namespaceName.Equals(MainNamespace, StringComparison.OrdinalIgnoreCase)
            ? ResourceNameViolation.ReservedCombination
            : ResourceNameViolation.None;

    /// <summary>
    /// Check that a name has none of the reserved characters.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name has no reserved characters, false otherwise.</returns>
    public static bool HasNoReservedCharacters(ReadOnlySpan<char> name) => name.IndexOfAny(_reservedCharacters) < 0;

    static bool IsReservedEventStore(string name) =>
        name.Length switch
        {
            2 => name.Equals("es", StringComparison.OrdinalIgnoreCase),
            5 => name.Equals("admin", StringComparison.OrdinalIgnoreCase) || name.Equals("local", StringComparison.OrdinalIgnoreCase),
            6 => name.Equals("config", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    static char[] CreateReservedCharacters()
    {
        var characters = new List<char> { '+', '#', '~', '$' };
        for (var character = '\u0000'; character <= '\u001F'; character++)
        {
            characters.Add(character);
        }

        for (var character = '\u007F'; character <= '\u009F'; character++)
        {
            characters.Add(character);
        }

        return [.. characters];
    }
}
