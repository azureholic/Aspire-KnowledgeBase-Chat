import "./App.css";
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { MsalProvider } from "@azure/msal-react";
import { authService } from "./services/AuthService";
import LandingPage from "./pages/LandingPage";
import ChatPage from "./pages/ChatPage";
import { FluentProvider, teamsDarkTheme, teamsLightTheme } from '@fluentui/react-components';
import { ThemeProvider, useTheme } from "./contexts/ThemeContext";

// Auth guard component to protect routes
const PrivateRoute = ({ children }: { children: React.ReactNode }) => {
  const isAuthenticated = authService.isAuthenticated();
  return isAuthenticated ? <>{children}</> : <Navigate to="/" />;
};

// Theme-aware app content
const ThemeAwareContent = () => {
  const { theme } = useTheme();
  
  return (
    <FluentProvider theme={theme === 'dark' ? teamsDarkTheme : teamsLightTheme}>
      <Router>
        <div className="App">
          <Routes>
            <Route path="/" element={<LandingPage />} />
            <Route path="/chat" element={
              <PrivateRoute>
                <ChatPage />
              </PrivateRoute>
            } />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </div>
      </Router>
    </FluentProvider>
  );
};

function App() {
  return (
    <ThemeProvider>
      <MsalProvider instance={authService.getMsalInstance()}>
        <ThemeAwareContent />
      </MsalProvider>
    </ThemeProvider>
  );
}

export default App;
