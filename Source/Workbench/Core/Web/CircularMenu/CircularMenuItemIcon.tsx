// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useRef } from 'react';
import { RenderingItem } from './RenderingItem';
import { default as styles } from './CircularMenuItemIcon.module.scss';

let first = true;

export const CircularMenuItemIcon = (item: RenderingItem) => {
    const [XOffset, setXOffset] = useState(20);
    const [YOffset, setYOffset] = useState(20);
    const element = useRef<SVGTextElement>(null);

    useEffect(() => {
        if (element?.current) {
            const dim = element.current.getBBox();
            const width = dim?.width || 0;
            const height = dim?.height || 0;
            const x = 20 - (width / 2);
            const y = 20 + (height / 2);
            setXOffset(x);
            setYOffset(y);
        }
    }, []);

    return (
        <text
            id={item.identifier}
            ref={element}
            x={item.iconX + XOffset}
            y={item.iconY + YOffset}
            fill={item.iconColor || 'white'}
            fontSize={3}
            onClick={() => item.onClick?.(item)}
            className={`${styles.menuItem} ${item.isPreviousItem ? styles.exit : styles.enter}`}>{item.iconString}</text>
    );
};
