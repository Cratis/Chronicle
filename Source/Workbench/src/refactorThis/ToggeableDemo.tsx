import { useRef } from 'react';
import { Panel } from 'primereact/panel';

export default function ToggleableDemo() {
    const ref = useRef<Panel>(null);
    return (
        <Panel ref={ref} header='Header' collapsed toggleable>
            <p className='m-0'>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod
                tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim
                veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea
                commodo consequat. Duis aute irure dolor in reprehenderit in voluptate
                velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat
                cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id
                est laborum.
            </p>
        </Panel>
    );
}
