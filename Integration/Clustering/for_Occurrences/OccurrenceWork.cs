// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[Trigger("p3c-work")]
public record OccurrenceWork(string Value);
