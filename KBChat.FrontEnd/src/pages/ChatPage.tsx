import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/AuthService';
import { HeaderComponent } from '../components/HeaderComponent';
import { useTheme } from '../contexts/ThemeContext';
import ReactMarkdown from 'react-markdown';
import {
  Button,
  Input,
  Text,
  makeStyles,
  tokens,
  Card,
  Avatar,
  Spinner,
  useId,
  Title1,
  Body1,
  Badge,
} from '@fluentui/react-components';
import '../App.css';

// Define Fluent UI styles
const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    width: '100vw',
    background: tokens.colorNeutralBackground2,
  },
  chatContainer: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    padding: '1rem 1rem',
    marginTop: '50px',
    overflow: 'hidden',
    boxSizing: 'border-box',
    height: 'calc(100vh - 50px)',
    width: '100%',
  },
  messagesContainer: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    overflowY: 'auto',
    padding: '1rem 0',
    gap: '16px',
    marginBottom: '10px',
  },
  message: {
    marginBottom: '10px',
    maxWidth: '85%',
  },
  userMessage: {
    alignSelf: 'flex-end',
  },
  assistantMessage: {
    alignSelf: 'flex-start',
  },
  roleLabel: {
    fontSize: '0.9rem',
    fontWeight: 'bold',
    padding: '2px 8px',
    borderRadius: '4px',
    marginBottom: '8px',
    display: 'block',
  },
  userRole: {
    backgroundColor: tokens.colorNeutralBackground4,
    color: tokens.colorNeutralForeground1,
  },
  assistantRole: {
    backgroundColor: tokens.colorNeutralBackground4,
    color: tokens.colorNeutralForeground1,
  },
  messageHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    marginBottom: '8px',
    padding: '0 16px',
  },
  messageContent: {
    padding: '0 16px 16px',
  },
  messageForm: {
    display: 'flex',
    marginTop: '1rem',
    gap: '10px',
    width: '100%',
  },
  inputField: {
    flex: 1,
  },
  documentReferences: {
    marginTop: '12px',
    paddingTop: '10px',
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  samplePromptsContainer: {
    marginTop: '40px',
    width: '90%',
    marginLeft: 'auto',
    marginRight: 'auto',
    display: 'flex',
    flexDirection: 'column',
  },
  promptsHeader: {
    textAlign: 'center',
    borderBottom: `2px solid ${tokens.colorNeutralStroke2}`,
    paddingBottom: '15px',
    marginBottom: '30px',
  },
  promptsContent: {
    paddingTop: '10px',
  },
  samplePromptsRow: {
    display: 'flex',
    justifyContent: 'flex-start',
    gap: '15px',
    marginBottom: '15px',
    width: '100%',
  },
  samplePromptButton: {
    flex: 1,
    minHeight: '70px',
    height: '70px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    textAlign: 'center',
    whiteSpace: 'normal',
    wordBreak: 'break-word',
    overflow: 'hidden',
    width: '33%', // Set equal width
  },
  emptyChat: {
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100%',
    textAlign: 'center',
    padding: '20px',
    color: tokens.colorNeutralForeground3,
  },
  typingIndicator: {
    padding: '10px',
    display: 'inline-flex',
    alignItems: 'center',
    gap: '5px',
  },
});

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  documentReferences?: string[];
}

interface ChatRequestBody {
  message: string;
  threadId: string;
}

interface ChatResponseBody {
  response: string;
  documentReferences: string[];
}

export const ChatPage = () => {
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [message, setMessage] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);
  const [threadId, setThreadId] = useState('');
  const navigate = useNavigate();
  const [samplePrompts, setSamplePrompts] = useState<string[]>([]);
  const { theme } = useTheme();
  const styles = useStyles();
  const inputId = useId('message-input');
  
  const account = authService.getAccount();
  const username = account ? (account.name || account.username || 'You') : 'You';

  // Generate a new ThreadId when the component mounts
  useEffect(() => {
    generateNewThreadId();
    
    // Get sample prompts from environment variable
    const samplesEnv = import.meta.env.VITE_SAMPLEPROMPTS as string;
    if (samplesEnv) {
      // Using semicolon as delimiter
      const prompts = samplesEnv.split(';').filter(prompt => prompt.trim() !== '');
      setSamplePrompts(prompts);
    }
  }, []);

  useEffect(() => {
    // Check authentication on page load
    const account = authService.getAccount();
    if (!account) {
      navigate('/');
    }
  }, [navigate]);

  // Generate a new random ThreadId
  const generateNewThreadId = () => {
    const newThreadId = `thread-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
    setThreadId(newThreadId);
  };

  const handleClearChat = () => {
    setMessages([]);
    generateNewThreadId();
  };
  
  const handleSamplePromptClick = (prompt: string) => {
    setMessage(prompt);
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim()) return;

    // Add user message to chat
    const userMessage: ChatMessage = {
      role: 'user',
      content: message,
    };
    
    setMessages([...messages, userMessage]);
    setMessage('');
    setLoading(true);

    try {
      // Get fresh token for API call
      const token = await authService.getToken();
      
      if (!token) {
        // If token acquisition failed, redirect to login
        navigate('/');
        return;
      }

      // Prepare the request body according to the API's expected format
      const requestBody: ChatRequestBody = {
        message: userMessage.content,
        threadId: threadId
      };

      // Call API with bearer token
      const apiPath = import.meta.env.VITE_API_PATH;
      console.log('API Path:', apiPath);
      const response = await fetch(apiPath, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(requestBody),
      });

      if (response.ok) {
        const data: ChatResponseBody = await response.json();
        const botMessage: ChatMessage = {
          role: 'assistant',
          content: data.response || 'No response from API',
          documentReferences: data.documentReferences
        };
        setMessages(prev => [...prev, botMessage]);
      } else {
        // Handle API error
        const botMessage: ChatMessage = {
          role: 'assistant',
          content: 'Sorry, there was an error processing your request.',
        };
        setMessages(prev => [...prev, botMessage]);
        console.error('API Error:', response.status);
      }
    } catch (error) {
      console.error('Failed to send message:', error);
      const botMessage: ChatMessage = {
        role: 'assistant',
        content: 'Sorry, there was an error processing your request.',
      };
      setMessages(prev => [...prev, botMessage]);
    } finally {
      setLoading(false);
    }
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  // Add effect to scroll to bottom when messages change
  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // Function to render sample prompts in rows of 3
  const renderSamplePrompts = () => {
    if (!samplePrompts.length) return null;
    
    const rows = [];
    for (let i = 0; i < samplePrompts.length; i += 3) {
      const rowPrompts = samplePrompts.slice(i, i + 3);
      rows.push(
        <div key={i} className={styles.samplePromptsRow}>
          {rowPrompts.map((prompt, index) => (
            <Button 
              key={`${i}-${index}`}
              appearance="outline"
              className={styles.samplePromptButton}
              onClick={() => handleSamplePromptClick(prompt)}
            >
              {prompt}
            </Button>
          ))}
        </div>
      );
    }
    
    return (
      <div className={styles.samplePromptsContainer}>
        <Title1 align="center" className={styles.promptsHeader}>Try these prompts:</Title1>
        <div className={styles.promptsContent}>
          {rows}
        </div>
      </div>
    );
  };

  return (
    <div className={styles.root}>
      {/* Replace the header with our new HeaderComponent */}
      <HeaderComponent onClearChat={handleClearChat} />
      
      <div className={styles.chatContainer}>
        <div className={styles.messagesContainer}>
          {messages.length === 0 ? (
            <div className={styles.emptyChat}>
              <Body1>Start a new conversation by typing a message below.</Body1>
              {renderSamplePrompts()}
            </div>
          ) : (
            messages.map((msg, index) => (
              <div 
                key={index} 
                style={{ 
                  display: 'flex', 
                  justifyContent: msg.role === 'user' ? 'flex-end' : 'flex-start',
                  width: '100%'
                }}
              >
                <Card 
                  className={`${styles.message} ${msg.role === 'user' ? styles.userMessage : styles.assistantMessage}`}
                  style={{
                    backgroundColor: msg.role === 'user' 
                      ? theme === 'light' 
                        ? tokens.colorNeutralBackground4
                        : tokens.colorNeutralBackground4
                      : theme === 'light' 
                        ? tokens.colorNeutralBackground1
                        : tokens.colorNeutralBackground1,
                    color: msg.role === 'user'
                      ? theme === 'light'
                        ? tokens.colorNeutralForeground1
                        : tokens.colorNeutralForeground1
                      : theme === 'light'
                        ? tokens.colorNeutralForeground1
                        : tokens.colorNeutralForeground2
                  }}
                >
                  {/* Replace the CardHeader with a clearer role indicator */}
                  <div className={styles.messageHeader}>
                    <Avatar
                      aria-label={msg.role === 'user' ? 'User avatar' : 'Assistant avatar'} 
                      name={msg.role === 'user' ? username : 'Assistant'} 
                      color={msg.role === 'user' ? 'neutral' : 'colorful'} 
                    />
                    <Badge 
                      appearance="filled"
                      color={msg.role === 'user' ? 'important' : 'informative'}
                      style={{
                        fontSize: '0.9rem',
                        padding: '2px 8px',
                      }}
                    >
                      {msg.role === 'user' ? 'You' : 'Assistant'}
                    </Badge>
                  </div>
                  
                  {msg.role === 'user' ? (
                    <div className={styles.messageContent}>
                      <Body1>{msg.content}</Body1>
                    </div>
                  ) : (
                    <div className={styles.messageContent}>
                      <div className="markdown-content">
                        <ReactMarkdown>{msg.content}</ReactMarkdown>
                        
                        {msg.documentReferences && msg.documentReferences.length > 0 && (
                          <div className={styles.documentReferences}>
                            <Text weight="semibold" style={{ fontSize: tokens.fontSizeBase200 }}>References:</Text>
                            <ul>
                              {msg.documentReferences.map((ref, i) => (
                                <li key={i}>
                                  <a href={ref} target="_blank" rel="noopener noreferrer">
                                    {ref}
                                  </a>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    </div>
                  )}
                </Card>
              </div>
            ))
          )}
          {loading && (
            <div style={{ 
              display: 'flex', 
              justifyContent: 'flex-start',
              width: '100%'
            }}>
              <Card 
                className={`${styles.message} ${styles.assistantMessage}`}
                style={{
                  backgroundColor: theme === 'light' 
                    ? tokens.colorNeutralBackground1
                    : tokens.colorNeutralBackground1,
                  color: theme === 'light'
                    ? tokens.colorNeutralForeground1
                    : tokens.colorNeutralForeground2
                }}
              >
                {/* Use the same enhanced role indicator for the loading state */}
                <div className={styles.messageHeader}>
                  <Avatar aria-label="Assistant avatar" name="Assistant" color="colorful" />
                  <Badge 
                    appearance="filled"
                    color="informative"
                    style={{
                      fontSize: '0.9rem',
                      padding: '2px 8px',
                    }}
                  >
                    Assistant
                  </Badge>
                </div>
                <div className={styles.messageContent}>
                  <div className={styles.typingIndicator}>
                    <Spinner size="tiny" />
                    <Text>Thinking...</Text>
                  </div>
                </div>
              </Card>
            </div>
          )}
          <div ref={messagesEndRef} /> {/* Add this div as the last element */}
        </div>
        
        <form onSubmit={handleSendMessage} className={styles.messageForm}>
          <Input
            id={inputId}
            className={styles.inputField}
            value={message}
            onChange={(_, data) => setMessage(data.value)}
            placeholder="Type your message here..."
            disabled={loading}
            appearance="outline"
            size="large"
          />
          <Button 
            type="submit" 
            disabled={loading || !message.trim()}
            appearance="outline"
            style={{
              backgroundColor: theme === 'light' 
                ? tokens.colorNeutralBackground4 
                : tokens.colorNeutralBackground3,
              color: tokens.colorNeutralForeground1,
            }}
            size="large"
          >
            Send
          </Button>
        </form>
      </div>
    </div>
  );
};

export default ChatPage;