import { forwardRef } from 'react';
import { AppTopbarRef } from './layout';

export const Topbar = forwardRef<AppTopbarRef>(() => {
    return <div className='layout-topbar'>Topbar</div>;
});
