import { MenuProvider } from './context/MenuContext';
import { AppMenuItem } from './layout';
import { Menuitem } from './MenuItem';

export const Menu = () => {
    const model: AppMenuItem[] = [
        {
            label: 'Home',
            items: [{ label: 'sa', icon: '', to: '/' }],
        },
        {
            label: 'UI Components',
            items: [
                {
                    label: 'dss',
                    icon: '',
                    to: '',
                },
                { label: 'sds', icon: '', to: '' },
                { label: 'ds', icon: '', to: '' },
            ],
        },
    ];

    return (
        <MenuProvider>
            <ul className='layout-menu'>
                {model.map((item, idx) => {
                    return !item?.separator ? (
                        <Menuitem item={item} root={true} index={idx} key={idx} />
                    ) : (
                        <li className='menu-separator'></li>
                    );
                })}
            </ul>
        </MenuProvider>
    );
};
