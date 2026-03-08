// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import 'primeicons/primeicons.css';
// Tailwind must come BEFORE PrimeReact so that @layer primereact is declared
// after @layer base/utilities and therefore has higher cascade priority.
// This mirrors the production app's index.css: @import "tailwindcss" → @import primereact theme.
import '@cratis/components/styles';
import './preview.css';
import 'primereact/resources/themes/lara-dark-blue/theme.css';

import type { Preview, Decorator } from '@storybook/react';
import React from 'react';
import { PrimeReactProvider } from 'primereact/api';
import { Arc } from '@cratis/arc.react';
import { MVVM } from '@cratis/arc.react.mvvm';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';

const withProviders: Decorator = (Story) => (
    <PrimeReactProvider value={{ ripple: true }}>
        <Arc development={true}>
            <MVVM>
                <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                    <Story />
                </DialogComponents>
            </MVVM>
        </Arc>
    </PrimeReactProvider>
);

const preview: Preview = {
    decorators: [withProviders],
    parameters: {
        actions: { argTypesRegex: '^on[A-Z].*' },
        controls: { expanded: true },
        backgrounds: {
            default: 'dark',
            values: [
                { name: 'dark', value: '#1e293b' },
                { name: 'light', value: '#ffffff' },
            ],
        },
    },
};

export default preview;
