// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ComponentDefaults } from '@primereact/types/core';
import { styles as accordionStyles } from '@primereact/styles/accordion';
import { styles as autocompleteStyles } from '@primereact/styles/autocomplete';
import { styles as avatarStyles } from '@primereact/styles/avatar';
import { styles as avatargroupStyles } from '@primereact/styles/avatargroup';
import { styles as badgeStyles } from '@primereact/styles/badge';
import { styles as breadcrumbStyles } from '@primereact/styles/breadcrumb';
import { styles as buttonStyles } from '@primereact/styles/button';
import { styles as buttongroupStyles } from '@primereact/styles/buttongroup';
import { styles as cardStyles } from '@primereact/styles/card';
import { styles as carouselStyles } from '@primereact/styles/carousel';
import { styles as checkboxStyles } from '@primereact/styles/checkbox';
import { styles as checkboxgroupStyles } from '@primereact/styles/checkboxgroup';
import { styles as chipStyles } from '@primereact/styles/chip';
import { styles as compareStyles } from '@primereact/styles/compare';
import { styles as contextmenuStyles } from '@primereact/styles/contextmenu';
import { styles as datatableStyles } from '@primereact/styles/datatable';
import { styles as dataviewStyles } from '@primereact/styles/dataview';
import { styles as datepickerStyles } from '@primereact/styles/datepicker';
import { styles as dialogStyles } from '@primereact/styles/dialog';
import { styles as dividerStyles } from '@primereact/styles/divider';
import { styles as drawerStyles } from '@primereact/styles/drawer';
import { styles as fieldsetStyles } from '@primereact/styles/fieldset';
import { styles as fileuploadStyles } from '@primereact/styles/fileupload';
import { styles as floatlabelStyles } from '@primereact/styles/floatlabel';
import { styles as fluidStyles } from '@primereact/styles/fluid';
import { styles as galleryStyles } from '@primereact/styles/gallery';
import { styles as iconfieldStyles } from '@primereact/styles/iconfield';
import { styles as iftalabelStyles } from '@primereact/styles/iftalabel';
import { styles as inplaceStyles } from '@primereact/styles/inplace';
import { styles as inputcolorStyles } from '@primereact/styles/inputcolor';
import { styles as inputgroupStyles } from '@primereact/styles/inputgroup';
import { styles as inputnumberStyles } from '@primereact/styles/inputnumber';
import { styles as inputotpStyles } from '@primereact/styles/inputotp';
import { styles as inputtagsStyles } from '@primereact/styles/inputtags';
import { styles as inputtextStyles } from '@primereact/styles/inputtext';
import { styles as knobStyles } from '@primereact/styles/knob';
import { styles as labelStyles } from '@primereact/styles/label';
import { styles as listboxStyles } from '@primereact/styles/listbox';
import { styles as menuStyles } from '@primereact/styles/menu';
import { styles as messageStyles } from '@primereact/styles/message';
import { styles as metergroupStyles } from '@primereact/styles/metergroup';
import { styles as navigationmenuStyles } from '@primereact/styles/navigationmenu';
import { styles as organizationchartStyles } from '@primereact/styles/organizationchart';
import { styles as overlaybadgeStyles } from '@primereact/styles/overlaybadge';
import { styles as paginatorStyles } from '@primereact/styles/paginator';
import { styles as panelStyles } from '@primereact/styles/panel';
import { styles as passwordStyles } from '@primereact/styles/password';
import { styles as popoverStyles } from '@primereact/styles/popover';
import { styles as progressbarStyles } from '@primereact/styles/progressbar';
import { styles as progressspinnerStyles } from '@primereact/styles/progressspinner';
import { styles as radiobuttonStyles } from '@primereact/styles/radiobutton';
import { styles as radiobuttongroupStyles } from '@primereact/styles/radiobuttongroup';
import { styles as ratingStyles } from '@primereact/styles/rating';
import { styles as scrollareaStyles } from '@primereact/styles/scrollarea';
import { styles as selectStyles } from '@primereact/styles/select';
import { layoutStyles, styles as sidebarStyles } from '@primereact/styles/sidebar';
import { styles as skeletonStyles } from '@primereact/styles/skeleton';
import { styles as sliderStyles } from '@primereact/styles/slider';
import { styles as speeddialStyles } from '@primereact/styles/speeddial';
import { styles as splitterStyles } from '@primereact/styles/splitter';
import { styles as stepperStyles } from '@primereact/styles/stepper';
import { styles as tabsStyles } from '@primereact/styles/tabs';
import { styles as tagStyles } from '@primereact/styles/tag';
import { styles as terminalStyles } from '@primereact/styles/terminal';
import { styles as textareaStyles } from '@primereact/styles/textarea';
import { styles as timelineStyles } from '@primereact/styles/timeline';
import { styles as toastStyles } from '@primereact/styles/toast';
import { styles as toasterStyles } from '@primereact/styles/toaster';
import { styles as togglebuttonStyles } from '@primereact/styles/togglebutton';
import { styles as togglebuttongroupStyles } from '@primereact/styles/togglebuttongroup';
import { styles as toggleswitchStyles } from '@primereact/styles/toggleswitch';
import { styles as toolbarStyles } from '@primereact/styles/toolbar';
import { styles as treeStyles } from '@primereact/styles/tree';
import { styles as tooltipStyles } from '@primereact/styles/tooltip';

const anchoredOverlay = 'p-anchored-overlay';
const collapsible = 'p-collapsible';
const overlayMask = 'p-overlay-mask';

/** A root that carries a component's `styles`, optionally with the motion its parts animate through. */
const styled = (styles: object, motion?: string) =>
    ({ props: motion ? { styles, motionProps: { name: motion } } : { styles } });

/** A part that only needs its motion name — its classes come from the root's `styles`. */
const motion = (name: string) => ({ props: { motionProps: { name } } });

/**
 * PrimeReact 11's own component styles, keyed by primitive component name, ready to be
 * handed to `PrimeReactProvider` as `defaults`.
 *
 * Vendored from `@cratis/components@3`'s `styled` subpath. Components 4 dropped PrimeReact
 * entirely, but the Workbench still renders a number of PrimeReact primitives directly
 * (tabs, popover, toggle buttons, knob, input tags, icon field, toolbar, and the grouping /
 * server-sorted tables Components' `DataTableCore` does not cover). Those are Prime islands
 * and keep their own provider, theme and license.
 *
 * PrimeReact 11 splits every component in two: the `primereact/*` primitives, which own
 * behavior and render structural markup with `data-scope` / `data-part` attributes and no
 * class names, and `@primereact/styles/*`, which holds the `p-*` class names and the CSS a
 * `@primeuix/themes` preset drives. `@primereact/ui/*` is nothing more than the two glued
 * together. PrimeReact's provider accepts default props per component name (`defaults`), and
 * `styles` is a public prop on every primitive, so this map performs that same gluing for
 * every primitive rendered under the provider.
 *
 * Every entry keys the primitive's *root* name (`Dialog.Root`, `Button`), because the parts
 * read their class names from the root's styles. The overlay/collapsible parts that animate
 * are also listed, with the motion name the theme's transitions expect.
 */
export const primeReactStyles: ComponentDefaults = {
    'Accordion.Root': styled(accordionStyles, collapsible),
    'AutoComplete.Root': styled(autocompleteStyles),
    'AutoComplete.Popup': motion(anchoredOverlay),
    'Avatar.Root': styled(avatarStyles),
    'AvatarGroup': styled(avatargroupStyles),
    'Badge': styled(badgeStyles),
    'Breadcrumb.Root': styled(breadcrumbStyles),
    'Button': styled(buttonStyles),
    'ButtonGroup': styled(buttongroupStyles),
    'Card.Root': styled(cardStyles),
    'Carousel.Root': styled(carouselStyles),
    'Checkbox.Root': styled(checkboxStyles),
    'CheckboxGroup': styled(checkboxgroupStyles),
    'Chip.Root': styled(chipStyles),
    'Compare.Root': styled(compareStyles),
    'ContextMenu.Root': styled(contextmenuStyles),
    'ContextMenu.Popup': motion(anchoredOverlay),
    'DataTable.Root': styled(datatableStyles),
    'DataView.Root': styled(dataviewStyles),
    'DatePicker.Root': styled(datepickerStyles),
    'DatePicker.Popup': motion(anchoredOverlay),
    'Dialog.Root': styled(dialogStyles),
    'Dialog.Backdrop': motion(overlayMask),
    'Dialog.Popup': motion('p-dialog'),
    'Divider': styled(dividerStyles),
    'Drawer.Root': styled(drawerStyles),
    'Drawer.Backdrop': motion(overlayMask),
    'Drawer.Popup': motion('p-drawer'),
    'Fieldset.Root': styled(fieldsetStyles, collapsible),
    'FileUpload.Root': styled(fileuploadStyles),
    'FloatLabel': styled(floatlabelStyles),
    'Fluid': styled(fluidStyles),
    'Gallery.Root': styled(galleryStyles),
    'IconField.Root': styled(iconfieldStyles),
    'IftaLabel': styled(iftalabelStyles),
    'Inplace.Root': styled(inplaceStyles),
    'InputColor.Root': styled(inputcolorStyles),
    'InputGroup.Root': styled(inputgroupStyles),
    'InputNumber.Root': styled(inputnumberStyles),
    'InputOtp.Root': styled(inputotpStyles),
    'InputPassword': styled(passwordStyles),
    'InputTags.Root': styled(inputtagsStyles),
    'InputText': styled(inputtextStyles),
    'Knob.Root': styled(knobStyles),
    'Label': styled(labelStyles),
    'Listbox.Root': styled(listboxStyles),
    'Menu.Root': styled(menuStyles),
    'Menu.Popup': motion(anchoredOverlay),
    'Message.Root': styled(messageStyles),
    'MeterGroup.Root': styled(metergroupStyles),
    'NavigationMenu': styled(navigationmenuStyles),
    'OrganizationChart.Root': styled(organizationchartStyles),
    'OverlayBadge': styled(overlaybadgeStyles),
    'Paginator.Root': styled(paginatorStyles),
    'Panel.Root': styled(panelStyles, collapsible),
    'Popover.Root': styled(popoverStyles),
    'Popover.Popup': motion(anchoredOverlay),
    'ProgressBar.Root': styled(progressbarStyles),
    'ProgressSpinner.Root': styled(progressspinnerStyles),
    'RadioButton.Root': styled(radiobuttonStyles),
    'RadioButtonGroup': styled(radiobuttongroupStyles),
    'Rating.Root': styled(ratingStyles),
    'ScrollArea.Root': styled(scrollareaStyles),
    'Select.Root': styled(selectStyles),
    'Select.Popup': motion(anchoredOverlay),
    'Sidebar.Root': styled(sidebarStyles),
    'Sidebar.Layout': styled(layoutStyles),
    'Sidebar.Backdrop': motion(overlayMask),
    'Skeleton': styled(skeletonStyles),
    'Slider.Root': styled(sliderStyles),
    'SpeedDial.Root': styled(speeddialStyles),
    'Splitter.Root': styled(splitterStyles),
    'Stepper.Root': styled(stepperStyles, collapsible),
    'Tabs.Root': styled(tabsStyles),
    'Tag': styled(tagStyles),
    'Terminal.Root': styled(terminalStyles),
    'Textarea': styled(textareaStyles),
    'Timeline.Root': styled(timelineStyles),
    'Toast.Root': styled(toastStyles),
    'Toaster.Root': styled(toasterStyles),
    'ToggleButton.Root': styled(togglebuttonStyles),
    'ToggleButtonGroup': styled(togglebuttongroupStyles),
    'ToggleSwitch.Root': styled(toggleswitchStyles),
    'Toolbar.Root': styled(toolbarStyles),
    'Tooltip.Root': styled(tooltipStyles),
    'Tooltip.Popup': motion('p-tooltip'),
    'Tree.Root': styled(treeStyles),
};