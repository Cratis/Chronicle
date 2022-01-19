// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserWindow } from 'electron';

let _mainWindow: BrowserWindow | null;

export const setMainWindow = (mainWindow: BrowserWindow) => {
    _mainWindow = mainWindow;
};

export const getMainWindow = () => {
    return _mainWindow;
};

export const clearMainWindow = () => {
    _mainWindow = null;
};
