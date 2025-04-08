import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/AuthService';
import { HeaderComponent } from '../components/HeaderComponent';
import {
  Button,
  makeStyles,
  tokens,
  Title2,
  Card,
  CardHeader,
} from '@fluentui/react-components';
import '../App.css';

// Define Fluent UI styles
const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'flex-start',
    width: '100vw',
    height: '100vh',
    background: tokens.colorNeutralBackground2,
    paddingTop: '65px',
    overflow: 'hidden',
    position: 'relative',
    boxSizing: 'border-box',
  },
  welcomeContainer: {
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    alignItems: 'center',
    width: '100vw',
    maxWidth: '100%',
    padding: '4rem 2rem',
    marginTop: '10vh',
    textAlign: 'center',
    boxSizing: 'border-box',
    position: 'absolute',
    left: 0,
  },
  welcomeCard: {
    maxWidth: '500px',
    width: '90%',
    padding: '2rem',
    margin: '0 auto',
    display: 'flex',
    flexDirection: 'column',
    gap: '2rem',
    alignItems: 'center',
  }
});

export const LandingPage = () => {
  const navigate = useNavigate();
  const styles = useStyles();

  useEffect(() => {
    // Check if user is already authenticated
    if (authService.isAuthenticated()) {
      // Navigate to chat if already logged in
      navigate('/chat');
    }
  }, [navigate]);

  const handleLogin = async () => {
    await authService.login();
    
    // After successful login, navigate to chat page
    if (authService.isAuthenticated()) {
      navigate('/chat');
    }
  };

  return (
    <div className={styles.root}>
      {/* Use the new HeaderComponent here */}
      <HeaderComponent />
      
      <div className={styles.welcomeContainer}>
        <Card className={styles.welcomeCard}>
          <CardHeader
            header={
              <Title2>Welcome to Knowledge Base Chat</Title2>
            }
          />
          
          <Button 
            appearance="primary" 
            size="large" 
            onClick={handleLogin}
          >
            Login
          </Button>
        </Card>
      </div>
    </div>
  );
};

export default LandingPage;