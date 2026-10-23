// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

record MatrixRow(
    string Id,
    string Proves,
    int[] Owners,
    string Status,
    string[] Jobs,
    SpecSelector[] Specs,
    PlantedDefect[] PlantedDefects,
    string[] RequiredProofs);
