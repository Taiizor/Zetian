'use client';

import { useEffect, useRef, useState } from 'react';
import { Copy, Check } from 'lucide-react';
import Prism from 'prismjs';

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

  const lines = code.split('\n');

  return (
    <div className={`relative group ${className}`}>
      {/* Header */}
      {filename && (
        <div className="flex items-center justify-between bg-gray-800 dark:bg-gray-900 px-4 py-2 rounded-t-lg border-b border-gray-700">
          <span className="text-sm text-gray-300 font-mono">{filename}</span>
          <span className="text-xs text-gray-500 uppercase">{language}</span>
        </div>
      )}

      {/* Code Block */}
      <div className={`relative bg-gray-900 dark:bg-gray-950 ${filename ? '' : 'rounded-t-lg'} rounded-b-lg overflow-hidden`}>
        {/* Copy Button */}
        <button
          onClick={handleCopy}
          className="absolute top-3 right-3 p-2 bg-gray-800 hover:bg-gray-700 rounded-lg transition-all opacity-0 group-hover:opacity-100 z-10"
          aria-label={copied ? 'Copied!' : 'Copy code'}
        >
          {copied ? (
            <Check className="h-4 w-4 text-green-400" />
          ) : (
            <Copy className="h-4 w-4 text-gray-400" />
          )}
        </button>

        {/* Code Content */}
        <div className="overflow-x-auto">
          <pre className={`p-4 text-sm ${showLineNumbers ? 'pl-12' : ''}`}>
            {showLineNumbers ? (
              <code className={`language-${language}`}>
                {lines.map((line, index) => {
                  const lineNumber = index + 1;
                  const isHighlighted = highlightLines.includes(lineNumber);
                  return (
                    <div
                      key={index}
                      className={`table-row ${isHighlighted ? 'bg-blue-500/10' : ''}`}
                    >
                      <span className="table-cell text-right pr-4 select-none text-gray-600 dark:text-gray-500 w-8">
                        {lineNumber}
                      </span>
                      <span className="table-cell">
                        {line}
                        {'\n'}
                      </span>
                    </div>
                  );
                })}
              </code>
            ) : (
              <code ref={codeRef} className={`language-${language}`}>
                {code}
              </code>
            )}
          </pre>
        </div>
      </div>
    </div>
  );
}