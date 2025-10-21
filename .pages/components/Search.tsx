'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Fuse from 'fuse.js';
import { Search as SearchIcon, X, FileText, Code2, Hash } from 'lucide-react';

interface SearchItem {
  title: string;
  description: string;
  path: string;
  category: string;
  content?: string;
}

const searchData: SearchItem[] = [
  // Docs
  { title: 'Getting Started', description: 'Quick setup and first steps', path: '/docs/getting-started', category: 'Documentation' },
  { title: 'Configuration', description: 'Server settings and configuration', path: '/docs/configuration', category: 'Documentation' },
  { title: 'Authentication', description: 'Security and authentication', path: '/docs/authentication', category: 'Documentation' },
  { title: 'Message Processing', description: 'Receiving and processing messages', path: '/docs/message-processing', category: 'Documentation' },
  { title: 'Extensions', description: 'Plugin and extension development', path: '/docs/extensions', category: 'Documentation' },
  
  // API
  { title: 'SmtpServer', description: 'Main SMTP server class', path: '/api#core-classes', category: 'API' },
  { title: 'SmtpServerBuilder', description: 'Server configuration with fluent builder pattern', path: '/api#core-classes', category: 'API' },
  { title: 'ISmtpMessage', description: 'SMTP message interface', path: '/api#interfaces', category: 'API' },
  { title: 'ISmtpSession', description: 'SMTP session interface', path: '/api#interfaces', category: 'API' },
  { title: 'IMessageStore', description: 'Message storage interface', path: '/api#interfaces', category: 'API' },
  { title: 'IMailboxFilter', description: 'Mailbox filtering interface', path: '/api#interfaces', category: 'API' },
  
  // Examples
  { title: 'Basic SMTP Server', description: 'A simple SMTP server in its most basic form', path: '/examples#basic', category: 'Examples' },
  { title: 'Authenticated Server', description: 'Secure server with username and password', path: '/examples#authenticated', category: 'Examples' },
  { title: 'TLS/SSL Secured Server', description: 'Encrypted connections with STARTTLS', path: '/examples#secure', category: 'Examples' },
  { title: 'Rate Limiting', description: 'Speed limiting for spam protection', path: '/examples#rate-limited', category: 'Examples' },
  { title: 'Message Filtering', description: 'Domain and content-based filtering', path: '/examples#filtered', category: 'Examples' },
  { title: 'Message Storage', description: 'Saving messages to file system or database', path: '/examples#storage', category: 'Examples' },
];

// Fuse instance'ını component dışında oluştur
const fuse = new Fuse(searchData, {
  keys: ['title', 'description', 'content'],
  threshold: 0.3,
  includeScore: true,
});

export function Search() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const modalRef = useRef<HTMLDivElement>(null);

  const handleSearch = useCallback((searchQuery: string) => {
    if (searchQuery.trim() === '') {
      setResults([]);
      setSelectedIndex(0);
      return;
    }
    
    const searchResults = fuse.search(searchQuery);
    const newResults = searchResults.map(r => r.item).slice(0, 8);
    setResults(newResults);
    setSelectedIndex(0);
  }, []);

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
      default:
        return <FileText className="h-4 w-4" />;
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
        <div className="fixed inset-0 z-50 flex items-start justify-center pt-20 bg-black/50 backdrop-blur-sm">
          <div
            ref={modalRef}
            className="w-full max-w-2xl bg-white dark:bg-gray-900 rounded-xl shadow-2xl overflow-hidden animate-slide-up"
          >
            {/* Search Input */}
            <div className="flex items-center gap-3 p-4 border-b border-gray-200 dark:border-gray-800">
              <SearchIcon className="h-5 w-5 text-gray-400" />
              <input
                ref={inputRef}
                type="text"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search in documentation..."
                className="flex-1 bg-transparent text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none"
              />
              <button
                onClick={() => {
                  setIsOpen(false);
                  setQuery('');
                }}
                className="p-1 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors"
              >
                <X className="h-4 w-4 text-gray-400" />
              </button>
            </div>

            {/* Search Results */}
            {results.length > 0 ? (
              <div className="max-h-96 overflow-y-auto p-2">
                {results.map((result, index) => (
                  <button
                    key={`${result.path}-${index}`}
                    onClick={() => {
                      router.push(result.path);
                      setIsOpen(false);
                    }}
                    onMouseEnter={() => setSelectedIndex(index)}
                    className={`w-full flex items-start gap-3 p-3 rounded-lg text-left transition-colors ${
                      index === selectedIndex
                        ? 'bg-blue-100 dark:bg-blue-900/30'
                        : 'hover:bg-gray-100 dark:hover:bg-gray-800'
                    }`}
                  >
                    <div className="mt-0.5 text-gray-400">
                      {getCategoryIcon(result.category)}
                    </div>
                    <div className="flex-1">
                      <div className="font-medium text-gray-900 dark:text-white">
                        {result.title}
                      </div>
                      <div className="text-sm text-gray-600 dark:text-gray-400 line-clamp-1">
                        {result.description}
                      </div>
                      <div className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                        {result.category}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            ) : query.trim() !== '' ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                <SearchIcon className="h-8 w-8 mx-auto mb-3 opacity-50" />
                <p>No results found for "{query}"</p>
              </div>
            ) : (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                <p className="text-sm">Start typing to search</p>
                <div className="flex items-center justify-center gap-4 mt-4 text-xs">
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
            )}
          </div>
        </div>
      )}
    </>
  );
}