// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

public record CapturedRequest(Uri Uri, HttpMethod Method, Dictionary<string, string> Form);
