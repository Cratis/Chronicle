// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.GraphProjections.Organization;

/// <summary>
/// The same graph as the model-bound and the fluent one, declared in the projection declaration language.
/// </summary>
public static class OrganizationDeclaration
{
    /// <summary>
    /// The name of the graph, which is also the identifier of the projection.
    /// </summary>
    public const string Name = "OrgPdl";

    /// <summary>
    /// Gets the declaration.
    /// </summary>
    public static readonly string Text = string.Join(
        '\n',
        "graph OrgPdl",
        "  node Person",
        "    personId Uuid key",
        "    name String",
        "    tags String[]",
        "    score Int",
        "    visits Int",
        "    registered DateTime",
        "    kind String",
        "  node Group",
        "    groupId Uuid key",
        "    name String",
        "    memberCount Int",
        "  node Place",
        "    placeId Uuid key",
        "  edge MEMBER_OF source Person target Group",
        "    since DateTime",
        "  edge VISITED source Person target Place identified by visitId",
        "    visitId Uuid",
        "  edge GREETED source Person target Person",
        "    times Int",
        "    warmth Int",
        "",
        "projection OrgPdl => graph OrgPdl",
        "  node Person",
        "    from OrgPersonRegistered key personId",
        "      registered = $eventContext.occurred",
        "      kind = \"person\"",
        "    from OrgPersonRenamed key personId",
        "    from OrgPointsAwarded key personId",
        "      add score by points",
        "    from OrgPointsRevoked key personId",
        "      subtract score by points",
        "    from OrgPlaceVisited key personId",
        "      count visits",
        "    remove with OrgPersonRemoved key personId detach",
        "  node Group",
        "    from OrgGroupCreated key groupId",
        "    from OrgMemberAdded key groupId",
        "      increment memberCount",
        "    from OrgMemberRemoved key groupId",
        "      decrement memberCount",
        "    remove with OrgGroupDissolved key groupId",
        "  node Place",
        "    from OrgPlaceVisited key placeId",
        "  edge MEMBER_OF",
        "    from OrgMemberAdded source personId target groupId",
        "      since = $eventContext.occurred",
        "    remove with OrgMemberRemoved source personId target groupId",
        "  edge VISITED",
        "    from OrgPlaceVisited source personId target placeId identified by visitId",
        "  edge GREETED",
        "    from OrgPersonGreeted source personId target otherId",
        "      increment times",
        "      add warmth by warmth",
        "    from OrgGreetingRetracted source personId target otherId",
        "      decrement times",
        "      subtract warmth by warmth");
}
