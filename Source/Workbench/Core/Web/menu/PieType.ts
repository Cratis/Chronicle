// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type Position = {
    x: number;
    y: number;
};

export type PieType = {
    startAngle: number;
    endAngle: number;
    getPos: (width: number) => Position;
    getWidth: (width: number) => number;
    getHeight: (width: number) => number;
    textAnchor: string;
    textY: number;
    getTextCenteredX: (width: number) => number;
    getTextCenteredY: (width: number) => number;
    getTextCenteredAnchor: string;
};
