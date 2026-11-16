// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

public class an_ignored_legacy_definition_change : a_definition_change
{
    protected ReplayCandidateRequest _legacy;

    void Establish()
    {
        var json = JsonSerializer.SerializeToNode(Request(string.Empty))!.AsObject();
        json.Remove(nameof(ReplayCandidateRequest.ChangeId));
        _legacy = json.Deserialize<ReplayCandidateRequest>()!;
        Store(_legacy);
    }
}
