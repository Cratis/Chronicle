// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RenderingItem } from './RenderingItem';
import { default as styles } from './CircularMenuItemCircle.module.scss';

export const CircularMenuItemCircle = (item: RenderingItem) => {
    const color = item.color;
    const elementSize = item.dashSize * item.index;
    const strokeDashArray = `${100 - (elementSize)} ${elementSize}`;

    const className = `${styles.menuItem} ${item.isPreviousItem ? styles.exit : styles.enter}`;

    return (
        <circle style={{}}
            id={item.identifier}
            ref={(element) => {
                element?.style.setProperty('--array', strokeDashArray);
                element?.style.setProperty('--offset', `0`);
            }}
            cx="20"
            cy="20"
            r="16"
            className={className}
            onClick={() => item.onClick?.(item)}
            stroke={color}
        />
    );
};
