// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const resolveMenuItemParams = (params: Readonly<Record<string, string | undefined>>, paramsFallback: object): Record<string, string | undefined> =>
    Object.assign({}, paramsFallback, params);
