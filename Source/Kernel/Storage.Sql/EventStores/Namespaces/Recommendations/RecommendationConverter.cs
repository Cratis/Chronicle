// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations;

/// <summary>
/// Converts between <see cref="Recommendation"/> and <see cref="RecommendationState"/>.
/// </summary>
public class RecommendationConverter
{
    /// <summary>
    /// Converts a <see cref="RecommendationState"/> to a <see cref="Recommendation"/>.
    /// </summary>
    /// <param name="recommendationId">The recommendation identifier.</param>
    /// <param name="recommendationState">The recommendation state.</param>
    /// <returns>The recommendation entity.</returns>
    public Recommendation ToEntity(RecommendationId recommendationId, RecommendationState recommendationState)
    {
        return new Recommendation
        {
            Id = recommendationId.Value,
            Name = recommendationState.Name.Value,
            Description = recommendationState.Description.Value,
            Type = recommendationState.Type.Value,
            Occurred = recommendationState.Occurred,
            RequestJson = JsonSerializer.Serialize((object?)recommendationState.Request)
        };
    }

    /// <summary>
    /// Converts a <see cref="Recommendation"/> to a <see cref="RecommendationState"/>.
    /// </summary>
    /// <param name="entity">The recommendation entity.</param>
    /// <returns>The recommendation state.</returns>
    public RecommendationState ToRecommendationState(Recommendation entity)
    {
        var recommendationType = new RecommendationType(entity.Type);
        return new RecommendationState
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Type = recommendationType,
            Occurred = entity.Occurred,
            Request = DeserializeRequest(recommendationType, entity.RequestJson)
        };
    }

    /// <summary>
    /// Deserialize a recommendation request from JSON to its concrete CLR type.
    /// </summary>
    /// <param name="recommendationType">The <see cref="RecommendationType"/> identifying the implementing recommendation.</param>
    /// <param name="requestJson">The JSON payload to deserialize.</param>
    /// <returns>The deserialized <see cref="IRecommendationRequest"/> instance, or <see langword="null"/> when the payload is empty or the request CLR type cannot be resolved.</returns>
    /// <remarks>
    /// System.Text.Json cannot deserialize directly to the <see cref="IRecommendationRequest"/>
    /// interface — it has no polymorphism configured. Resolve the concrete CLR type through the
    /// <see cref="RecommendationType"/> (which carries the assembly-qualified name of the
    /// <see cref="IRecommendation{TRequest}"/> implementation) and deserialize to <c>TRequest</c>.
    /// Mirrors MongoDB's <c>RecommendationStateSerializer.GetRequestClrType</c>.
    /// </remarks>
    static IRecommendationRequest DeserializeRequest(RecommendationType recommendationType, string requestJson)
    {
        if (string.IsNullOrEmpty(requestJson) || requestJson == "null")
        {
            return null!;
        }

        var requestClrType = GetRequestClrType(recommendationType);
        if (requestClrType is null)
        {
            return null!;
        }

        return (IRecommendationRequest)JsonSerializer.Deserialize(requestJson, requestClrType)!;
    }

    static Type? GetRequestClrType(RecommendationType recommendationType)
    {
        try
        {
            Type recommendationInterfaceType = recommendationType;
            var genericRecommendationInterface = recommendationInterfaceType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRecommendation<>));

            return genericRecommendationInterface?.GetGenericArguments()[0];
        }
        catch
        {
            return null;
        }
    }
}
