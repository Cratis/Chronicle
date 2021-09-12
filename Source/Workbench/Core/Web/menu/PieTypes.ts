// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PieType } from './PieType';

const pi = Math.PI;

export const HALF: PieType = {
    startAngle: -90 * (pi / 180),
    endAngle: 90 * (pi / 180),
    getPos: function pos(width) {
        return { x: width / 2, y: width / 2 };
    },
    getWidth: function w(width) {
        return width / 2;
    },
    getHeight: function height(width) {
        return width / 2;
    },
    textAnchor: 'middle',
    textY: 15,
    getTextCenteredX: function getTextCenteredX(width) {
        return width / 2;
    },
    getTextCenteredY: function getTextCenteredY(width) {
        return width / 2 - 15;
    },
    getTextCenteredAnchor: 'middle'
};

export const CIRCLE: PieType = {
    startAngle: 0,
    endAngle: pi * 2,
    getPos: function pos(width) {
        return { x: width / 2, y: width / 2 };
    },
    getWidth: function w(width) {
        return width / 2;
    },
    getHeight: function height(width) {
        return width;
    },
    textAnchor: 'middle',
    textY: 10,
    getTextCenteredX: function getTextCenteredX(width) {
        return width / 2;
    },
    getTextCenteredY: function getTextCenteredY(width) {
        return width / 2;
    },
    getTextCenteredAnchor: 'middle'
};

export const ARC: PieType = {
    startAngle: 0,
    endAngle: pi / 2,
    getPos: function pos(width) {
        return { x: 0 / 2, y: width };
    },
    getWidth: function w(width) {
        return width;
    },
    getHeight: function height(width) {
        return width;
    },
    textAnchor: 'middle',
    textY: 5,
    getTextCenteredX: function getTextCenteredX() {
        return 10;
    },
    getTextCenteredY: function getTextCenteredY(width) {
        return width - 15;
    },
    getTextCenteredAnchor: 'start'
};

export const enterAntiClockwise = {
    startAngle: Math.PI * 2,
    endAngle: Math.PI * 2
};
