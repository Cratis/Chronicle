// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents the validator for <see cref="IgnoreRecommendation"/>.
/// </summary>
internal class IgnoreRecommendationValidator : CommandValidator<IgnoreRecommendation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreRecommendationValidator"/> class.
    /// </summary>
    public IgnoreRecommendationValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.RecommendationId).NotEmpty().WithMessage("Recommendation identifier is required.");
    }
}
