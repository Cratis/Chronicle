// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PieType } from './PieType';

export type CircularMenuConfig = {
    colors: any[];
    width: number;
    height: number;
    type: PieType;
    showIcon: boolean;
    showCenteredLabel: boolean;
    centeredLabelFontSize: string;
    sizeIcon: string;
    colorIcon: any;
    pieSize: number;
    backgroundColor: string;
};
