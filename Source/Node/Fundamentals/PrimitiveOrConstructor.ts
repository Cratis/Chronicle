// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PrimitiveTypeMap } from './PrimitiveTypeMap';

export type PrimitiveOrConstructor = (new (...args: any[]) => any) | keyof PrimitiveTypeMap;
