// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Checks whether today's payload shapes still serve every shape in a released snapshot.
/// </summary>
public static class PayloadContractComparison
{
    /// <summary>
    /// Finds breaking changes from one released snapshot. Additions are allowed.
    /// </summary>
    /// <param name="released">The shapes a previous release could send or receive.</param>
    /// <param name="current">Today's shapes.</param>
    /// <returns>Every breaking change, in deterministic order.</returns>
    public static IReadOnlyList<ContractBreak> Compare(PayloadContractManifest released, PayloadContractManifest current)
    {
        var breaks = new List<ContractBreak>();
        foreach (var previous in released.Types.Values.OrderBy(_ => _.Identity, StringComparer.Ordinal))
        {
            if (!current.Types.TryGetValue(previous.Identity, out var today))
            {
                breaks.Add(new(ContractBreakKind.PayloadTypeRemoved, previous.Identity, $"Payload type '{previous.Identity}' was removed or moved. Keep the type at its wire identity within a major; add a new type instead."));
                continue;
            }

            if (previous.Kind != today.Kind || previous.Underlying != today.Underlying)
            {
                breaks.Add(new(ContractBreakKind.PayloadKindChanged, previous.Identity, $"Payload '{previous.Identity}' changed representation from {previous.Kind} ({previous.Underlying}) to {today.Kind} ({today.Underlying}). Keep its codec and underlying type within a major; add a new type instead."));
                continue;
            }

            CompareMembers(previous, today, breaks);
        }

        return breaks;
    }

    static void CompareMembers(PayloadTypeContract previous, PayloadTypeContract today, List<ContractBreak> breaks)
    {
        foreach (var member in previous.Members.OrderBy(_ => _.Id).ThenBy(_ => _.Name, StringComparer.Ordinal))
        {
            var current = today.Members.FirstOrDefault(_ => previous.Kind == PayloadKind.Orleans ? _.Id == member.Id : _.Name == member.Name);
            var identity = previous.Kind == PayloadKind.Orleans ? $"[Id({member.Id})]" : member.Name;
            if (current is null || (previous.Kind == PayloadKind.Orleans && current.Name != member.Name))
            {
                var kind = previous.Kind == PayloadKind.Enum ? ContractBreakKind.EnumMemberRemoved : ContractBreakKind.PayloadPropertyRemoved;
                breaks.Add(new(kind, previous.Identity, $"Payload '{previous.Identity}' no longer has member '{identity}' named '{member.Name}'. Never remove or rename a member within a major; keep it and add a new one instead."));
            }
            else if (current.Type != member.Type)
            {
                var kind = previous.Kind == PayloadKind.Enum ? ContractBreakKind.EnumValueChanged : ContractBreakKind.PayloadPropertyTypeChanged;
                breaks.Add(new(kind, previous.Identity, $"Payload '{previous.Identity}' member '{identity}' changed from '{member.Type}' to '{current.Type}'. Keep the original type or enum value within a major; add a new member instead."));
            }
        }
    }
}
