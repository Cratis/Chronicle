// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Badge } from 'primereact/badge';

import { tw } from 'typewind';

export const Notifications = () => {
    return (<div className={tw.flex_1}>
        <div className={tw.flex.justify_end.gap_3}>
            <i className="pi pi-bell p-overlay-badge" style={{ fontSize: '25px' }}>
                <Badge value="2"></Badge>
            </i>
        </div>
    </div>)
};
