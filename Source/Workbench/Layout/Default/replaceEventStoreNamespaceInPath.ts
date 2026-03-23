// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const replaceEventStoreNamespaceInPath = (
    pathname: string,
    eventStore: string | undefined,
    currentNamespace: string | undefined,
    nextNamespace: string) => {

    if (!currentNamespace || currentNamespace === nextNamespace) {
        return pathname;
    }

    const pathSegments = pathname.split('/');
    const startIndex = eventStore ? pathSegments.findIndex(segment => segment === eventStore) + 1 : 0;
    const namespaceIndex = pathSegments.findIndex((segment, index) => index >= startIndex && segment === currentNamespace);

    if (namespaceIndex < 0) {
        return pathname;
    }

    const updatedSegments = [...pathSegments];
    updatedSegments[namespaceIndex] = nextNamespace;

    return updatedSegments.join('/');
};
