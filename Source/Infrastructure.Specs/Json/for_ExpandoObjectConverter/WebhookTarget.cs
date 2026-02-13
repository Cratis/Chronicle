// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public record WebhookTarget(
    string Url,
    OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization> Authorization);
