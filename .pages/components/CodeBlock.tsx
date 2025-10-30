'use client';

import { useEffect, useRef, useState } from 'react';
import { Copy, Check } from 'lucide-react';
import Prism from 'prismjs';

// Import Prism plugins
import 'prismjs/plugins/line-numbers/prism-line-numbers';
import 'prismjs/plugins/line-highlight/prism-line-highlight';

// Import Prism languages
import 'prismjs/components/prism-sql';
import 'prismjs/components/prism-bash';
import 'prismjs/components/prism-json';
import 'prismjs/components/prism-yaml';
import 'prismjs/components/prism-csharp';
import 'prismjs/components/prism-javascript';
import 'prismjs/components/prism-powershell';

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
      {/* macOS Window Header */}
      <div className="relative flex items-center justify-between bg-gray-200 dark:bg-gray-800 px-4 py-3 rounded-t-lg border-b border-gray-300 dark:border-gray-700">
        {/* Left side: macOS Traffic Lights */}
        <div className="flex items-center gap-2 z-10">
          <div className="w-3 h-3 rounded-full bg-red-500" />
          <div className="w-3 h-3 rounded-full bg-yellow-500" />
          <div className="w-3 h-3 rounded-full bg-green-500" />
        </div>

        {/* Center: Filename with background */}
        {filename && (
          <div className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2">
            <div className="px-3 py-1 rounded-md bg-gray-300 dark:bg-gray-700 border border-gray-400 dark:border-gray-600">
              <span className="text-sm text-gray-800 dark:text-gray-200 font-mono font-medium whitespace-nowrap">
                {filename}
              </span>
            </div>
          </div>
        )}

        {/* Right side: Copy button */}
        <button
          onClick={handleCopy}
          className="p-2 bg-gray-300 hover:bg-gray-400 dark:bg-gray-700 dark:hover:bg-gray-600 rounded-lg transition-all z-10"
          aria-label={copied ? 'Copied!' : 'Copy code'}
        >
          {copied ? (
            <Check className="h-4 w-4 text-green-600 dark:text-green-400" />
          ) : (
            <Copy className="h-4 w-4 text-gray-700 dark:text-gray-300" />
          )}
        </button>
      </div>

      {/* Code Block */}
      <div className="relative bg-white dark:bg-[#1e1e1e] rounded-b-lg overflow-hidden border border-gray-300 dark:border-gray-700 border-t-0">

        {/* Code Content */}
        <div className="overflow-x-auto">
          <pre 
            className={[
              showLineNumbers ? 'py-4 pr-4' : 'p-4',
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