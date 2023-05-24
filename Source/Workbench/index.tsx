// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { createRoot } from 'react-dom/client';


import { App } from './App';
import { BrowserRouter } from 'react-router-dom';
import { CssBaseline, Paper, ThemeProvider } from '@mui/material';
import { ModalProvider } from '@aksio/applications-mui';

import { theme } from './theme';

const root = createRoot(document.getElementById('root')!);
root.render(
    <BrowserRouter>
        <ThemeProvider theme={theme}>
            <CssBaseline />
            <ModalProvider>
                <Paper elevation={0} sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                    <App />
                </Paper>
            </ModalProvider>
        </ThemeProvider>
    </BrowserRouter>
);
