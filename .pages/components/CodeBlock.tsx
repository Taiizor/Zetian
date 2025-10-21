'use client';

import { useEffect, useRef, useState } from 'react';
import { Copy, Check } from 'lucide-react';
import Prism from 'prismjs';

// Import Prism plugins
import 'prismjs/plugins/line-numbers/prism-line-numbers';
import 'prismjs/plugins/line-highlight/prism-line-highlight';

// Import Prism languages
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-bash';
import 'prismjs/components/prism-powershell';
import 'prismjs/components/prism-json';
import 'prismjs/components/prism-markup';

interface CodeBlockProps {
  code: string;
  language?: string;
  filename?: string;
  showLineNumbers?: boolean;
  highlightLines?: number[];
  className?: string;
}

export default function CodeBlock({
  code,
  language = 'csharp',
  filename,
  showLineNumbers = true,
  highlightLines = [],
  className = '',
}: CodeBlockProps) {
  const [copied, setCopied] = useState(false);
  const codeRef = useRef<HTMLElement>(null);

  useEffect(() => {
    if (codeRef.current) {
      Prism.highlightElement(codeRef.current);
    }
  }, [code, language]);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className={`relative group ${className}`}>
      {/* Header */}
      {filename && (
        <div className="flex items-center justify-between bg-gray-100 dark:bg-gray-800 px-4 py-2 rounded-t-lg border-b border-gray-200 dark:border-gray-700">
          <span className="text-sm text-gray-700 dark:text-gray-300 font-mono">{filename}</span>
          <span className="text-xs text-gray-500 dark:text-gray-500 uppercase">{language}</span>
        </div>
      )}

      {/* Code Block */}
      <div className={`relative bg-gray-50 dark:bg-gray-900 ${filename ? '' : 'rounded-t-lg'} rounded-b-lg overflow-hidden border border-gray-200 dark:border-gray-800`}>
        {/* Copy Button */}
        <button
          onClick={handleCopy}
          className="absolute top-3 right-3 p-2 bg-gray-200 hover:bg-gray-300 dark:bg-gray-800 dark:hover:bg-gray-700 rounded-lg transition-all opacity-0 group-hover:opacity-100 z-10"
          aria-label={copied ? 'Copied!' : 'Copy code'}
        >
          {copied ? (
            <Check className="h-4 w-4 text-green-600 dark:text-green-400" />
          ) : (
            <Copy className="h-4 w-4 text-gray-600 dark:text-gray-400" />
          )}
        </button>

        {/* Code Content */}
        <div className="overflow-x-auto">
          <pre 
            className={[
              'p-4',
              'text-sm',
              showLineNumbers && 'line-numbers',
              `language-${language}`
            ].filter(Boolean).join(' ')}
            tabIndex={0}
            {...(highlightLines.length > 0 && { 'data-line': highlightLines.join(',') })}
          >
            <code ref={codeRef} className={`language-${language}`}>
              {code}
            </code>
          </pre>
        </div>
      </div>
    </div>
  );
}