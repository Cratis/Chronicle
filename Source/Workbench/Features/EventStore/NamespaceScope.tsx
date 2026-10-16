// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Outlet, useParams } from 'react-router-dom';

/**
 * Renders the pages of one namespace, and starts them afresh when the namespace changes.
 *
 * Switching namespace only changes a route parameter, so React would otherwise keep the same page
 * mounted: an open query in the sequence workspace keeps running against the namespace it was
 * opened in, and a selected read model keeps showing the previous namespace's instances. Keying
 * the outlet on the namespace gives the new namespace a page that has never seen the old one.
 * @returns The rendered pages for the current namespace.
 */
export const NamespaceScope = () => {
    const { namespace } = useParams<{ namespace: string }>();
    return <Outlet key={namespace} />;
};
