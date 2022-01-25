// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type Constructor<T extends {} = {}> = new (...args: any[]) => T;
