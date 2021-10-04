// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';

import { app, BrowserWindow } from 'electron';
import isDev from 'electron-is-dev';
import { setMainWindow } from './globals';
import path from 'path';

import shellPath from 'shell-path';

let mainWindow: BrowserWindow | null;

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require('electron-squirrel-startup')) { // eslint-disable-line global-require
    app.quit();
}

function fixPath() {
    if (process.platform !== 'darwin') {
        return;
    }

    process.env.PATH = shellPath.sync() || [
        './node_modules/.bin',
        '/.nodebrew/current/bin',
        '/usr/local/bin',
        process.env.PATH
    ].join(':');
}

function createWindow() {
    fixPath();

    const windowConfig: any = {
        width: 1000,
        height: 900,
        webPreferences: {
            nodeIntegration: true,
            enableRemoteModule: false,
            contextIsolation: false,
            sandbox: false,
            webSecurity: false
        },
        titleBarStyle: 'hidden',
        darkTheme: true
    };

    if (process.platform === 'darwin') {
        windowConfig.vibrancy = 'dark';
        windowConfig.frame = false;
    } else {
        windowConfig.backgroundColor = '#333';
    }

    // Create the browser window.
    mainWindow = new BrowserWindow(windowConfig);
    mainWindow.setMenuBarVisibility(false);
    setMainWindow(mainWindow);

    if (isDev) {
        mainWindow.loadURL('http://localhost:9100');
        mainWindow.webContents.openDevTools();
    } else {
        mainWindow.loadFile('./build/index.html');
    }

    mainWindow.once('ready-to-show', () => mainWindow?.show());

    mainWindow.on('closed', () => {
        mainWindow = null;
    });
}

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
});
