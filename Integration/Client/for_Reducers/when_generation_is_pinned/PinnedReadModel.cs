// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Integration.for_Reducers.when_generation_is_pinned;

public record PinnedReadModel([property: PII] string FullName);
