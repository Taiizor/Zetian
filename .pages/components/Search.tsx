'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Fuse from 'fuse.js';
import { 
  Search as SearchIcon, 
  X, 
  FileText, 
  Code2, 
  Hash, 
  Shield, 
  Database, 
  Gauge, 
  Clock, 
  Star,
  Zap,
  Settings,
  Package,
  ChevronRight
} from 'lucide-react';

interface SearchItem {
  title: string;
  description: string;
  path: string;
  category: string;
  tags?: string[];
  content?: string;
  code?: string;
  popular?: boolean;
}

const searchData: SearchItem[] = [
  // Main Pages
  { title: 'Home', description: 'Modern and scalable SMTP server library for .NET', path: '/', category: 'Main', tags: ['home', 'start', 'overview'] },
  { title: 'Documentation', description: 'Complete documentation and guides', path: '/docs', category: 'Main', tags: ['docs', 'guides', 'manual'] },
  { title: 'API Reference', description: 'Complete API documentation', path: '/api', category: 'Main', tags: ['api', 'reference', 'classes'] },
  { title: 'Examples', description: 'Code examples and use cases', path: '/examples', category: 'Main', tags: ['examples', 'code', 'samples'] },
  { title: 'Changelog', description: 'Version history and release notes', path: '/changelog', category: 'Main', tags: ['changelog', 'versions', 'releases'] },
  
  // Documentation
  { 
    title: 'Getting Started', 
    description: 'Quick setup guide - install NuGet package, create basic server, start receiving emails', 
    path: '/docs/getting-started', 
    category: 'Documentation',
    tags: ['install', 'setup', 'quickstart', 'nuget'],
    popular: true,
    code: 'dotnet add package Zetian'
  },
  { 
    title: 'Configuration', 
    description: 'Server settings - ports, certificates, timeouts, buffer sizes, connection limits', 
    path: '/docs/configuration', 
    category: 'Documentation',
    tags: ['config', 'settings', 'ports', 'ssl', 'tls', 'certificate']
  },
  { 
    title: 'Authentication', 
    description: 'PLAIN, LOGIN, custom authentication handlers, RequireAuthentication, AllowPlainTextAuthentication', 
    path: '/docs/authentication', 
    category: 'Documentation',
    tags: ['auth', 'security', 'login', 'plain', 'password'],
    popular: true
  },
  { 
    title: 'Message Processing', 
    description: 'MessageReceived event, filtering, spam detection, forwarding, storage', 
    path: '/docs/message-processing', 
    category: 'Documentation',
    tags: ['message', 'email', 'processing', 'filter', 'spam']
  },
  { 
    title: 'Extensions', 
    description: 'Rate limiting, spam filters, custom storage, statistics, domain validation', 
    path: '/docs/extensions', 
    category: 'Documentation',
    tags: ['extensions', 'plugins', 'rate-limit', 'spam-filter']
  },
  
  // Core Classes
  { 
    title: 'SmtpServer', 
    description: 'Main server class - StartAsync(), StopAsync(), MessageReceived, SessionCreated, SessionCompleted, ErrorOccurred events', 
    path: '/api#core-classes', 
    category: 'API',
    tags: ['server', 'main', 'start', 'stop', 'events'],
    popular: true,
    code: 'var server = new SmtpServerBuilder().Port(25).Build();'
  },
  { 
    title: 'SmtpServerBuilder', 
    description: 'Fluent builder - Port(), ServerName(), Certificate(), RequireAuthentication(), WithFileMessageStore(), Build()', 
    path: '/api#core-classes', 
    category: 'API',
    tags: ['builder', 'fluent', 'configuration'],
    code: '.Port(587).RequireAuthentication().Certificate(cert)'
  },
  { 
    title: 'SmtpServerConfiguration', 
    description: 'All server configuration properties - Port, MaxMessageSize, MaxConnections, Timeouts, Buffers', 
    path: '/api#core-classes', 
    category: 'API',
    tags: ['configuration', 'settings']
  },
  
  // Interfaces
  { 
    title: 'ISmtpMessage', 
    description: 'Message interface - Id, From, Recipients, Subject, TextBody, HtmlBody, GetRawData(), SaveToFileAsync()', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['message', 'interface', 'email'],
    code: 'e.Message.From?.Address; e.Message.SaveToFileAsync(path);'
  },
  { 
    title: 'ISmtpSession', 
    description: 'Session interface - Id, RemoteEndPoint, IsAuthenticated, AuthenticatedIdentity, MessageCount', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['session', 'interface', 'connection'],
    code: 'if (e.Session.IsAuthenticated) { ... }'
  },
  { 
    title: 'IMessageStore', 
    description: 'Storage interface - SaveAsync(session, message, ct) for custom message storage implementations', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['storage', 'interface', 'save']
  },
  { 
    title: 'IMailboxFilter', 
    description: 'Filtering interface - CanAcceptFromAsync(), CanDeliverToAsync() for domain/recipient validation', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['filter', 'interface', 'validation']
  },
  { 
    title: 'IAuthenticator', 
    description: 'Custom authentication mechanism interface - AuthenticateAsync() method', 
    path: '/api#authentication', 
    category: 'API',
    tags: ['authenticator', 'interface', 'custom']
  },
  { 
    title: 'IRateLimiter', 
    description: 'Rate limiting interface - IsAllowedAsync(), RecordRequestAsync(), GetRemainingAsync()', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['ratelimit', 'interface', 'throttle']
  },
  
  // Storage Classes
  { 
    title: 'FileMessageStore', 
    description: 'Built-in file storage - saves messages to disk with optional date folders', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'file', 'disk'],
    code: '.WithFileMessageStore(@"C:\\smtp_messages", createDateFolders: true)'
  },
  { 
    title: 'DomainMailboxFilter', 
    description: 'Domain-based filtering - whitelist/blacklist for sender and recipient domains', 
    path: '/api#storage', 
    category: 'API',
    tags: ['filter', 'domain', 'whitelist', 'blacklist'],
    code: '.WithSenderDomainWhitelist("trusted.com")'
  },
  
  // Extension Methods
  { 
    title: 'AddRateLimiting', 
    description: 'Extension method - adds rate limiting per IP address with configurable limits', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'ratelimit', 'throttle'],
    code: 'server.AddRateLimiting(RateLimitConfiguration.PerHour(100))'
  },
  { 
    title: 'AddMessageFilter', 
    description: 'Extension method - adds custom message filtering logic', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'filter', 'custom'],
    code: 'server.AddMessageFilter(msg => msg.Size < 10_000_000)'
  },
  { 
    title: 'SaveMessagesToDirectory', 
    description: 'Extension method - automatically saves all received messages to a directory', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'save', 'directory'],
    code: 'server.SaveMessagesToDirectory(@"C:\\emails")'
  },
  
  // Examples
  { 
    title: 'Basic Example', 
    description: 'Simple server on port 25 - no auth, accepts all messages, logs to console', 
    path: '/examples#basic', 
    category: 'Examples',
    tags: ['example', 'basic', 'simple'],
    popular: true,
    code: 'var server = new SmtpServerBuilder().Port(25).Build();'
  },
  { 
    title: 'Authenticated Example', 
    description: 'Port 587 with PLAIN/LOGIN auth - AuthenticationHandler, RequireAuthentication', 
    path: '/examples#authenticated', 
    category: 'Examples',
    tags: ['example', 'auth', 'secure', 'password'],
    code: '.RequireAuthentication().AuthenticationHandler(handler)'
  },
  { 
    title: 'Secure Example', 
    description: 'TLS/SSL with STARTTLS - Certificate(), RequireSecureConnection(), port 587', 
    path: '/examples#secure', 
    category: 'Examples',
    tags: ['example', 'secure', 'tls', 'ssl', 'certificate'],
    code: '.Certificate("cert.pfx", "password").RequireSecureConnection()'
  },
  { 
    title: 'Rate Limited Example', 
    description: 'Spam protection - PerMinute/PerHour limits, connection limits per IP', 
    path: '/examples#rate-limited', 
    category: 'Examples',
    tags: ['example', 'ratelimit', 'spam', 'protection'],
    code: '.AddRateLimiting(RateLimitConfiguration.PerMinute(10))'
  },
  { 
    title: 'Custom Processing', 
    description: 'Domain filtering, content validation, spam word detection, size limits', 
    path: '/examples#filtered', 
    category: 'Examples',
    tags: ['example', 'filter', 'custom', 'validation'],
    code: 'if (spamWords.Any(w => message.Subject?.Contains(w)))'
  },
  { 
    title: 'Message Storage', 
    description: 'FileMessageStore, custom IMessageStore implementations, JSON metadata', 
    path: '/examples#storage', 
    category: 'Examples',
    tags: ['example', 'storage', 'save', 'database'],
    code: '.WithFileMessageStore(directory, createDateFolders: true)'
  },
  
  // Common Issues & Solutions
  { 
    title: 'Port 25 Access Denied', 
    description: 'Solution: Run as administrator or use port > 1024 for testing', 
    path: '/docs/getting-started#troubleshooting', 
    category: 'Troubleshooting',
    tags: ['error', 'port', 'permission', 'admin']
  },
  { 
    title: 'Authentication Error 538', 
    description: 'Encryption required - use AllowPlainTextAuthentication() for testing without TLS', 
    path: '/docs/authentication#common-errors', 
    category: 'Troubleshooting',
    tags: ['error', 'auth', '538', 'encryption'],
    code: '.AllowPlainTextAuthentication()'
  },
  { 
    title: 'Connection Limit Exceeded', 
    description: 'MaxConnectionsPerIP default is 10 - increase with .MaxConnectionsPerIP(100)', 
    path: '/docs/configuration#connection-limits', 
    category: 'Troubleshooting',
    tags: ['error', 'connection', 'limit'],
    code: '.MaxConnectionsPerIP(100)'
  },
];

// Enhanced Fuse configuration for better search
const fuse = new Fuse(searchData, {
  keys: [
    { name: 'title', weight: 0.4 },
    { name: 'description', weight: 0.3 },
    { name: 'tags', weight: 0.2 },
    { name: 'content', weight: 0.05 },
    { name: 'code', weight: 0.05 }
  ],
  threshold: 0.4,
  includeScore: true,
  includeMatches: true,
  minMatchCharLength: 1,
  shouldSort: true,
  location: 0,
  distance: 100,
});

export function Search() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [recentSearches, setRecentSearches] = useState<string[]>([]);
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const modalRef = useRef<HTMLDivElement>(null);
  
  // Load recent searches from localStorage
  useEffect(() => {
    const saved = localStorage.getItem('recentSearches');
    if (saved) {
      setRecentSearches(JSON.parse(saved).slice(0, 5));
    }
  }, []);
  
  // Save search to recent
  const saveToRecent = useCallback((searchTerm: string) => {
    if (!searchTerm.trim()) return;
    const updated = [searchTerm, ...recentSearches.filter(s => s !== searchTerm)].slice(0, 5);
    setRecentSearches(updated);
    localStorage.setItem('recentSearches', JSON.stringify(updated));
  }, [recentSearches]);
  
  // Get popular searches
  const popularSearches = searchData.filter(item => item.popular).slice(0, 6);
  
  // Get categories for filtering
  const categories = ['All', ...Array.from(new Set(searchData.map(item => item.category)))];

  const handleSearch = useCallback((searchQuery: string) => {
    if (searchQuery.trim() === '') {
      setResults([]);
      setSelectedIndex(0);
      return;
    }
    
    let searchResults = fuse.search(searchQuery);
    
    // Filter by category if selected
    if (selectedCategory !== 'All') {
      searchResults = searchResults.filter(r => r.item.category === selectedCategory);
    }
    
    // Sort by score and popularity
    searchResults.sort((a, b) => {
      const scoreA = a.score || 0;
      const scoreB = b.score || 0;
      const popularA = a.item.popular ? -0.1 : 0;
      const popularB = b.item.popular ? -0.1 : 0;
      return (scoreA + popularA) - (scoreB + popularB);
    });
    
    const newResults = searchResults.map(r => r.item).slice(0, 12);
    setResults(newResults);
    setSelectedIndex(0);
  }, [selectedCategory]);

  useEffect(() => {
    handleSearch(query);
  }, [query, handleSearch]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Cmd/Ctrl + K to open search
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setIsOpen(true);
      }
      
      // Escape to close
      if (e.key === 'Escape' && isOpen) {
        setIsOpen(false);
      }
      
      // Arrow navigation
      if (isOpen && results.length > 0) {
        if (e.key === 'ArrowDown') {
          e.preventDefault();
          setSelectedIndex((prev) => (prev + 1) % results.length);
        } else if (e.key === 'ArrowUp') {
          e.preventDefault();
          setSelectedIndex((prev) => (prev - 1 + results.length) % results.length);
        } else if (e.key === 'Enter') {
          e.preventDefault();
          const selected = results[selectedIndex];
          if (selected) {
            router.push(selected.path);
            setIsOpen(false);
            setQuery('');
          }
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, results, selectedIndex, router]);

  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    } else {
      // Dialog kapandığında state'leri temizle
      setQuery('');
      setResults([]);
      setSelectedIndex(0);
    }
  }, [isOpen]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (modalRef.current && !modalRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'Documentation':
        return <FileText className="h-4 w-4" />;
      case 'API':
        return <Hash className="h-4 w-4" />;
      case 'Examples':
        return <Code2 className="h-4 w-4" />;
      case 'Troubleshooting':
        return <Settings className="h-4 w-4" />;
      case 'Main':
        return <Package className="h-4 w-4" />;
      default:
        return <FileText className="h-4 w-4" />;
    }
  };
  
  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Documentation':
        return 'text-blue-600 dark:text-blue-400';
      case 'API':
        return 'text-purple-600 dark:text-purple-400';
      case 'Examples':
        return 'text-green-600 dark:text-green-400';
      case 'Troubleshooting':
        return 'text-red-600 dark:text-red-400';
      case 'Main':
        return 'text-gray-600 dark:text-gray-400';
      default:
        return 'text-gray-600 dark:text-gray-400';
    }
  };

  return (
    <>
      {/* Search Button */}
      <button
        onClick={() => setIsOpen(true)}
        className="flex items-center gap-2 px-3 py-1.5 text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors"
        aria-label="Search (Cmd+K)"
      >
        <SearchIcon className="h-4 w-4" />
        <span className="hidden sm:inline">Search...</span>
        <kbd className="hidden sm:inline-flex items-center gap-1 px-1.5 py-0.5 text-xs text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 rounded">
          <span className="text-xs">⌘</span>K
        </kbd>
      </button>

      {/* Search Modal */}
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center pt-16 bg-black/50 backdrop-blur-sm">
          <div
            ref={modalRef}
            className="w-full max-w-3xl bg-white dark:bg-gray-900 rounded-xl shadow-2xl overflow-hidden animate-slide-up"
          >
            {/* Search Input */}
            <div className="flex flex-col gap-3 p-4 border-b border-gray-200 dark:border-gray-800">
              <div className="flex items-center gap-3">
                <SearchIcon className="h-5 w-5 text-gray-400" />
                <input
                  ref={inputRef}
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search documentation, API, examples..."
                  className="flex-1 bg-transparent text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none"
                />
                <button
                  onClick={() => {
                    setIsOpen(false);
                    setQuery('');
                    setSelectedCategory('All');
                  }}
                  className="p-1 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors"
                >
                  <X className="h-4 w-4 text-gray-400" />
                </button>
              </div>
              
              {/* Category Filter */}
              <div className="flex items-center gap-2 overflow-x-auto pb-1">
                <span className="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">Filter:</span>
                {categories.map((cat) => (
                  <button
                    key={cat}
                    onClick={() => {
                      setSelectedCategory(cat);
                      handleSearch(query);
                    }}
                    className={`px-2 py-1 text-xs rounded-md transition-colors whitespace-nowrap ${
                      selectedCategory === cat
                        ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                        : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700'
                    }`}
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* Search Results */}
            {results.length > 0 ? (
              <div className="max-h-[60vh] overflow-y-auto p-2">
                {results.map((result, index) => (
                  <button
                    key={`${result.path}-${index}`}
                    onClick={() => {
                      saveToRecent(result.title);
                      router.push(result.path);
                      setIsOpen(false);
                    }}
                    onMouseEnter={() => setSelectedIndex(index)}
                    className={`w-full flex items-start gap-3 p-3 rounded-lg text-left transition-colors ${
                      index === selectedIndex
                        ? 'bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800'
                        : 'hover:bg-gray-50 dark:hover:bg-gray-800/50'
                    }`}
                  >
                    <div className={`mt-0.5 ${getCategoryColor(result.category)}`}>
                      {getCategoryIcon(result.category)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <div className="font-medium text-gray-900 dark:text-white">
                          {result.title}
                        </div>
                        {result.popular && (
                          <Star className="h-3 w-3 text-yellow-500 fill-yellow-500" />
                        )}
                      </div>
                      <div className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                        {result.description}
                      </div>
                      {result.code && (
                        <div className="mt-1.5 p-1.5 bg-gray-100 dark:bg-gray-800 rounded text-xs font-mono text-gray-700 dark:text-gray-300 truncate">
                          {result.code}
                        </div>
                      )}
                      <div className="flex items-center gap-2 mt-1.5">
                        <div className={`text-xs ${getCategoryColor(result.category)}`}>
                          {result.category}
                        </div>
                        {result.tags && result.tags.length > 0 && (
                          <>
                            <span className="text-gray-300 dark:text-gray-700">•</span>
                            <div className="flex gap-1">
                              {result.tags.slice(0, 3).map((tag) => (
                                <span
                                  key={tag}
                                  className="px-1.5 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-xs rounded"
                                >
                                  {tag}
                                </span>
                              ))}
                            </div>
                          </>
                        )}
                      </div>
                    </div>
                    <ChevronRight className="h-4 w-4 text-gray-400 mt-3 flex-shrink-0" />
                  </button>
                ))}
              </div>
            ) : query.trim() !== '' ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                <SearchIcon className="h-8 w-8 mx-auto mb-3 opacity-50" />
                <p>No results found for "{query}"</p>
                {selectedCategory !== 'All' && (
                  <p className="text-sm mt-2">
                    in {selectedCategory} category.
                    <button
                      onClick={() => setSelectedCategory('All')}
                      className="text-blue-600 dark:text-blue-400 hover:underline ml-1"
                    >
                      Search all categories
                    </button>
                  </p>
                )}
              </div>
            ) : (
              <div className="p-6">
                {/* Popular Searches */}
                {popularSearches.length > 0 && (
                  <div className="mb-6">
                    <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 mb-2">
                      <Star className="h-3 w-3" />
                      <span>Popular</span>
                    </div>
                    <div className="grid grid-cols-2 gap-2">
                      {popularSearches.map((item) => (
                        <button
                          key={item.path}
                          onClick={() => {
                            router.push(item.path);
                            setIsOpen(false);
                          }}
                          className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-left"
                        >
                          <div className={getCategoryColor(item.category)}>
                            {getCategoryIcon(item.category)}
                          </div>
                          <div className="min-w-0">
                            <div className="text-sm font-medium text-gray-900 dark:text-white truncate">
                              {item.title}
                            </div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">
                              {item.category}
                            </div>
                          </div>
                        </button>
                      ))}
                    </div>
                  </div>
                )}
                
                {/* Recent Searches */}
                {recentSearches.length > 0 && (
                  <div className="mb-6">
                    <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 mb-2">
                      <Clock className="h-3 w-3" />
                      <span>Recent</span>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {recentSearches.map((term) => (
                        <button
                          key={term}
                          onClick={() => setQuery(term)}
                          className="px-3 py-1 bg-gray-100 dark:bg-gray-800 text-sm text-gray-700 dark:text-gray-300 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
                        >
                          {term}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
                
                {/* Keyboard Shortcuts */}
                <div className="text-center pt-4 border-t border-gray-200 dark:border-gray-800">
                  <p className="text-sm text-gray-500 dark:text-gray-400 mb-3">Keyboard shortcuts</p>
                  <div className="flex items-center justify-center gap-6 text-xs">
                    <span className="flex items-center gap-1">
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">↑</kbd>
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">↓</kbd>
                      Navigate
                    </span>
                    <span className="flex items-center gap-1">
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">Enter</kbd>
                      Select
                    </span>
                    <span className="flex items-center gap-1">
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">Esc</kbd>
                      Close
                    </span>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  );
}