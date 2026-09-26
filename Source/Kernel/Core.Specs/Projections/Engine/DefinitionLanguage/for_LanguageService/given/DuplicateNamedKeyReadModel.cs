// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

public record DuplicateNamedKeyReadModel(Other.OrderKey OrderKey, OrderKey Id);
