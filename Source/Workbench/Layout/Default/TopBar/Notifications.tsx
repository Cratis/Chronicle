// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Badge } from 'primereact/badge';

export const Notifications = () => {
    return (<div className='flex-1'>
        <div className={'flex justify-end gap-3 '}>
            <i className="pi pi-bell p-overlay-badge" style={{ fontSize: '25px' }}>
                <Badge value="2"></Badge>
            </i>
        </div>
    </div>);
};
