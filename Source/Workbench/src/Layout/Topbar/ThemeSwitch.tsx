import { useDarkMode } from "usehooks-ts";
import { Button } from "primereact/button";

export const ThemeSwitch = () => {
    const { isDarkMode, toggle } = useDarkMode()
    return (
        <Button onClick={toggle} className='p-button-rounded'>
            {isDarkMode ? 'Light' : 'Dark'}
        </Button>
    )
}