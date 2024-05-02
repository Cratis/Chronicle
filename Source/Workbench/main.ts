//  Copyright (c) Cratis. All rights reserved.
//  Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { createApp } from 'vue';
import './Styles/style.css';
import App from './App.vue';
import PrimeVueUnstyled from "primevue/config";
import Lara from './Styles/presets/lara';
import { GlobalComponents } from './GlobalComponents';
import Ripple from 'primevue/ripple';
import { container } from 'tsyringe';
import Plugin from './VueDI';

const app = createApp(App)
    .use(Plugin, {
        container
    })
    .use(PrimeVueUnstyled, {
        pt: Lara,
        ripple: true
    })
    .directive('ripple', Ripple);
GlobalComponents(app);
app.mount('#app');
