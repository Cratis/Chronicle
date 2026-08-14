// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useState } from 'react';

const storageKey = 'cratis.workbench.sequences.hierarchy.width';

/** The narrowest the hierarchy can be dragged, and the width it starts at. */
export const minimumHierarchyWidth = 200;

/**
 * Remember how wide the user left the hierarchy.
 *
 * It starts at its narrowest so the results get the room by default; widening it is a deliberate
 * act, and one worth remembering across visits.
 * @returns The width to render at, and what to call when the user drags the split.
 */
export const useHierarchyWidth = () => {
    const [width] = useState(() => {
        const stored = Number(localStorage.getItem(storageKey));
        return Number.isFinite(stored) && stored >= minimumHierarchyWidth ? stored : minimumHierarchyWidth;
    });

    // Allotment reports every pane's size on any layout change, including the ones that come from
    // the window resizing, so the width is only written when it is genuinely usable.
    const onChange = useCallback((sizes: number[]) => {
        const [hierarchy] = sizes;
        if (hierarchy >= minimumHierarchyWidth) localStorage.setItem(storageKey, String(Math.round(hierarchy)));
    }, []);

    return { width, onChange };
};
