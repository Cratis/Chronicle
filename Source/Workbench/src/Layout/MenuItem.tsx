import { CSSTransition } from 'react-transition-group';
import { MenuContext } from './context/MenuContext';
import { classNames } from 'primereact/utils';
import { AppMenuItemProps } from './layout';
import { Ripple } from 'primereact/ripple';
import React, { useContext } from 'react';

export const Menuitem = (props: AppMenuItemProps) => {
    const { activeMenu, setActiveMenu } = useContext(MenuContext);
    const item = props.item;
    const key = props.parentKey
        ? props.parentKey + '-' + props.index
        : String(props.index);
    const active = activeMenu === key || activeMenu?.startsWith(key + '-');

    const itemClick = (event: React.MouseEvent<HTMLAnchorElement, MouseEvent>) => {
        //avoid processing disabled items
        if (item!.disabled) {
            event.preventDefault();
            return;
        }

        //execute command
        if (item!.command) {
            item!.command({ originalEvent: event, item: item! });
        }

        // toggle active state
        if (item!.items) setActiveMenu(active ? (props.parentKey as string) : key);
        else setActiveMenu(key);
    };

    const subMenu = item!.items && item!.visible !== false && (
        <CSSTransition
            timeout={{ enter: 1000, exit: 450 }}
            classNames='layout-submenu'
            in={props.root ? true : active}
            key={item!.label}
        >
            <ul>
                {item!.items.map((child, idx) => {
                    return (
                        <Menuitem
                            item={child}
                            index={idx}
                            className={child.badgeClass}
                            parentKey={key}
                            key={idx}
                        />
                    );
                })}
            </ul>
        </CSSTransition>
    );

    return (
        <li
            className={classNames({
                'layout-root-menuitem': props.root,
                'active-menuitem': active,
            })}
        >
            {(!item!.to || item!.items) && item!.visible !== false ? (
                <a
                    href={item!.url}
                    onClick={(e) => itemClick(e)}
                    className={classNames(item!.class, 'p-ripple')}
                    target={item!.target}
                    tabIndex={0}
                >
                    <i className={classNames('layout-menuitem-icon', item!.icon)}></i>
                    <span className='layout-menuitem-text'>{item!.label}</span>
                    {item!.items && (
                        <i className='pi pi-fw pi-angle-down layout-submenu-toggler'></i>

                    )}
                    <Ripple />
                </a>
            ) : null}

            {item!.to && !item!.items && item!.visible !== false ? (
                <>Links goes here</>
            ) : null}

            {subMenu}
        </li>
    );
};
