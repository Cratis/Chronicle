// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import Button from 'primevue/button';
import Chip from 'primevue/chip';
import Panel from 'primevue/panel';
import { App } from 'vue';

export const GlobalComponents = (app: App<Element>) => {
    app.component('Button', Button)
        .component('Chip', Chip)
        .component('Panel', Panel);
}
