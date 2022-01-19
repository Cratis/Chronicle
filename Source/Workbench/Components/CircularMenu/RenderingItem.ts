// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CircularMenuItem } from './CircularMenuItem';
import { ICircularMenuProps } from './ICircularMenuProps';
import { TinyColor } from '@ctrl/tinycolor';
import { default as IconNames } from '@fluentui/font-icons-mdl2/lib/data/AllIconNames.json';

export interface RenderingItem extends CircularMenuItem {
    index: number;
    identifier: string;
    isPreviousItem: boolean;
    dashSize: number;
    iconX: number;
    iconY: number;
    iconString?: string;
    iconWidth: number;
    iconHeight: number;
}


export const getRenderingItemsFor = (props: ICircularMenuProps, exit: boolean) => {
    const circleSize = ((Math.PI * 2) / props.items.length);
    const mid = circleSize / 2;

    return props.items.map((item, index) => {
        const iconCurvePosition = (circleSize * (index + 1)) + (Math.PI / 2) - mid;
        const iconX = Math.sin(iconCurvePosition) * 16;
        const iconY = Math.cos(iconCurvePosition) * 16;
        const icon = IconNames.find(_ => _.name === item.icon);

        let iconString = '';
        if (icon?.unicode) {
            iconString = JSON.parse(`["\\u${icon?.unicode}"]`)[0];
        }

        return {
            ...item,
            index,
            identifier: `${props.name}-item-${index}`,
            color: item.color || new TinyColor(props.color || '#333').brighten(index * (100 / props.items.length)).toHexString(true),
            isPreviousItem: exit,
            dashSize: 100 / props.items.length,
            iconX,
            iconY,
            iconString

        } as RenderingItem;
    });
};
