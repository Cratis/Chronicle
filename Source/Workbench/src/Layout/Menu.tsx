import { MenuProvider } from './context/MenuContext';
import { AppMenuItem } from '../refactorThis/layout';
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
            <ul>
                {model.map((item, idx) => {
                    return !item?.separator ? (
                        <Menuitem item={item} root={true} index={idx} key={idx} />
                    ) : (
                        <li></li>
                    );
                })}
            </ul>
        </MenuProvider>
    );
};
