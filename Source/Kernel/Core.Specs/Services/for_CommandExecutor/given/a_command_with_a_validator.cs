// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.given;

public class a_command_with_a_validator : Specification
{
    protected internal record TheCommand(string Name);

    protected internal class TheCommandValidator : CommandValidator<TheCommand>
    {
        public TheCommandValidator()
        {
            RuleFor(_ => _.Name).NotEmpty().WithMessage("Name is required.");
        }
    }
}
