import React from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/AuthService';
import { useTheme } from '../contexts/ThemeContext';
import copilotIcon from '../assets/copilot-icon.svg';
import {
  makeStyles,
  tokens,
  Menu,
  MenuButton,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuTrigger,
  MenuDivider,
  shorthands,
  mergeClasses
} from '@fluentui/react-components';
import {
  WeatherMoon24Regular,
  WeatherSunny24Regular,
  Person24Regular,
  SignOut24Regular,
  DeleteRegular
} from '@fluentui/react-icons';

// Define base styles
const useBaseStyles = makeStyles({
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '0.6rem 1rem',
    height: '50px',
    boxShadow: tokens.shadow4,
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 1000,
  },
  headerContent: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    width: '100%',
    padding: '0 1rem',
    maxWidth: '100%',
  },
  appTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    fontSize: '1.2rem',
  },
  appIcon: {
    height: '24px',
    width: '24px',
  },
  userMenuWrapper: {
    display: 'flex',
    alignItems: 'center',
    gap: '10px',
  },
  menuButton: {
    // Base styles for menu button
  },
});

// Define light theme styles
const useLightThemeStyles = makeStyles({
  header: {
    background: '#f5f5f5', // Light gray background similar to dark theme's style
    color: '#333333', // Dark text for good contrast on light background
    ...shorthands.borderBottom('1px', 'solid', tokens.colorNeutralStroke1),
  },
  appTitle: {
    color: '#333333', // Dark text for good contrast
  },
  menuButton: {
    color: '#333333', // Dark text for menu button in light theme
  },
});

// Define dark theme styles
const useDarkThemeStyles = makeStyles({
  header: {
    background: '#2b2b2b', // Darker background for dark theme
    color: 'white', // White text on dark background for good contrast
    ...shorthands.borderBottom('1px', 'solid', tokens.colorNeutralStroke1),
  },
  appTitle: {
    color: 'white', // White text in dark theme for good contrast
  },
  menuButton: {
    color: 'white', // White text for menu button in dark theme
  },
});

interface HeaderComponentProps {
  onClearChat?: () => void; // Optional prop for clearing chat (only needed in ChatPage)
}

export const HeaderComponent: React.FC<HeaderComponentProps> = ({ onClearChat }) => {
  const { theme, toggleTheme } = useTheme();
  const baseStyles = useBaseStyles();
  const lightStyles = useLightThemeStyles();
  const darkStyles = useDarkThemeStyles();
  
  // Merge the base styles with theme-specific styles
  const styles = {
    header: mergeClasses(
      baseStyles.header,
      theme === 'light' ? lightStyles.header : darkStyles.header
    ),
    headerContent: baseStyles.headerContent,
    appTitle: mergeClasses(
      baseStyles.appTitle,
      theme === 'light' ? lightStyles.appTitle : darkStyles.appTitle
    ),
    appIcon: baseStyles.appIcon,
    userMenuWrapper: baseStyles.userMenuWrapper,
    menuButton: mergeClasses(
      baseStyles.menuButton,
      theme === 'light' ? lightStyles.menuButton : darkStyles.menuButton
    ),
  };
  
  const navigate = useNavigate();
  const isAuthenticated = authService.isAuthenticated();
  const account = authService.getAccount();
  const username = account ? (account.name || account.username) : '';

  const handleLogout = async () => {
    await authService.logout();
    navigate('/');
  };
  
  return (
    <div className={styles.header}>
      <div className={styles.headerContent}>
        <div className={styles.appTitle}>
          <img src={copilotIcon} alt="Copilot Icon" className={styles.appIcon} />
          <span>Knowledge Base Chat</span>
        </div>
        
        {isAuthenticated ? (
          // Show user menu with options when authenticated
          <div className={styles.userMenuWrapper}>
            <Menu>
              <MenuTrigger disableButtonEnhancement>
                <MenuButton 
                  appearance="subtle" 
                  icon={<Person24Regular />}
                  className={styles.menuButton}
                >
                  {username}
                </MenuButton>
              </MenuTrigger>

              <MenuPopover>
                <MenuList>
                  {/* Only show Clear Chat option if provided */}
                  {onClearChat && (
                    <MenuItem
                      icon={<DeleteRegular />}
                      onClick={onClearChat}
                    >
                      Clear Chat
                    </MenuItem>
                  )}
                  <MenuItem
                    icon={theme === 'light' ? <WeatherSunny24Regular /> : <WeatherMoon24Regular />}
                    onClick={toggleTheme}
                  >
                    {theme === 'light' ? 'Switch to Dark Theme' : 'Switch to Light Theme'}
                  </MenuItem>
                  <MenuDivider />
                  <MenuItem
                    icon={<SignOut24Regular />}
                    onClick={handleLogout}
                  >
                    Logout
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>
          </div>
        ) : (
          // Show just theme toggle when not authenticated
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <MenuButton 
                appearance="subtle" 
                icon={theme === 'light' ? <WeatherSunny24Regular /> : <WeatherMoon24Regular />}
                className={styles.menuButton}
              >
                {theme === 'light' ? 'Light Theme' : 'Dark Theme'}
              </MenuButton>
            </MenuTrigger>

            <MenuPopover>
              <MenuList>
                <MenuItem
                  icon={theme === 'light' ? <WeatherMoon24Regular /> : <WeatherSunny24Regular />}
                  onClick={toggleTheme}
                >
                  {theme === 'light' ? 'Switch to Dark Theme' : 'Switch to Light Theme'}
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        )}
      </div>
    </div>
  );
};

export default HeaderComponent;
