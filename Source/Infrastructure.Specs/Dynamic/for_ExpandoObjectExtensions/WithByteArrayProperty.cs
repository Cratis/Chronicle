// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

#pragma warning disable CA1819 // A byte[] property is the exact shape under test - AsExpandoObject must not shred it
public record WithByteArrayProperty(byte[] PhotoData);
#pragma warning restore CA1819
