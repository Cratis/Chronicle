// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const resolveSidebarNamespace = (routeNamespace: string | undefined, previousNamespace: string) =>
    routeNamespace ?? previousNamespace;
