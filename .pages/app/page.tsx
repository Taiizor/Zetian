'use client';

import Link from 'next/link';
import { motion } from 'framer-motion';
import { 
  Zap, 
  Shield, 
  Code2, 
  Github, 
  Globe, 
  ArrowRight,
  CheckCircle,
  Package,
  Gauge,
  Layers,
  Calendar
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const features = [
  {
    icon: Shield,
    title: 'Security Focused',
    description: 'Complete security with TLS/SSL support, STARTTLS and multiple authentication mechanisms.',
    color: 'from-green-500 to-emerald-600'
  },
  {
    icon: Zap,
    title: 'High Performance',
    description: 'Efficient async/await patterns, optimized network I/O operations and minimal memory allocations.',
    color: 'from-yellow-500 to-orange-600'
  },
  {
    icon: Code2,
    title: 'Modern API',
    description: 'Easy to use with async/await patterns, event-driven architecture and fluent builder pattern.',
    color: 'from-blue-500 to-indigo-600'
  },
  {
    icon: Layers,
    title: 'Extensible',
    description: 'Full flexibility with plugin architecture, custom filters and message store implementations.',
    color: 'from-purple-500 to-pink-600'
  },
  {
    icon: Gauge,
    title: 'Rate Limiting',
    description: 'Spam protection and resource management with built-in rate limiting.',
    color: 'from-red-500 to-rose-600'
  },
  {
    icon: Globe,
    title: 'SMTPUTF8 Support',
    description: 'Full UTF-8 support for international email addresses.',
    color: 'from-teal-500 to-cyan-600'
  }
];

const codeExample = `using Zetian.Server;

// Create SMTP server
using var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .Certificate("cert.pfx", "password")
    .MaxMessageSizeMB(25)
    .Build();

// When message received
server.MessageReceived += (sender, e) => {
    Console.WriteLine($"From: {e.Message.From}");
    Console.WriteLine($"Subject: {e.Message.Subject}");
};

await server.StartAsync();`;

export default function Home() {
  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-b from-gray-50 via-white to-gray-50 dark:from-gray-950 dark:via-gray-900 dark:to-gray-950">
        <div className="absolute inset-0 bg-grid-gray-100/50 dark:bg-grid-gray-800/50" />
        <div className="container mx-auto px-4 py-24 relative">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
            className="text-center max-w-4xl mx-auto"
          >
            <div className="inline-flex items-center gap-2 bg-primary-100 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300 px-4 py-2 rounded-full text-sm font-medium mb-6">
              <Package className="h-4 w-4" />
              <span>Available on NuGet</span>
            </div>
            
            <h1 className="text-5xl md:text-6xl font-bold text-gray-900 dark:text-white mb-6">
              <span
                className="font-bold"
                style={{
                  background: 'linear-gradient(to right, #0ea5e9, #0369a1)',
                  WebkitBackgroundClip: 'text',
                  WebkitTextFillColor: 'transparent',
                }}
              >
                Professional
              </span>
              {" "}SMTP Server for .NET
            </h1>
            
            <p className="text-xl text-gray-600 dark:text-gray-400 mb-8 leading-relaxed">
              High-performance, extensible, and secure SMTP server library.
              With modern async/await patterns and minimal dependencies.
            </p>
            
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Link
                href="/docs/getting-started"
                className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-primary-600 hover:bg-primary-700 text-black dark:text-white rounded-lg font-medium transition-all transform hover:scale-105 shadow-lg hover:shadow-xl"
              >
                <Zap className="h-5 w-5" />
                Get Started
                <ArrowRight className="h-4 w-4" />
              </Link>
              
              <Link
                href="/docs"
                className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-gray-100 hover:bg-gray-200 dark:bg-gray-800 dark:hover:bg-gray-700 text-gray-900 dark:text-white rounded-lg font-medium transition-all"
              >
                <Code2 className="h-5 w-5" />
                Documentation
              </Link>
              
              <Link
                href="/changelog"
                className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-gray-100 hover:bg-gray-200 dark:bg-gray-800 dark:hover:bg-gray-700 text-gray-900 dark:text-white rounded-lg font-medium transition-all"
              >
                <Calendar className="h-5 w-5" />
                Changelog
              </Link>
              
              <a
                href="https://github.com/Taiizor/Zetian"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-gray-900 hover:bg-gray-800 dark:bg-gray-700 dark:hover:bg-gray-600 text-white rounded-lg font-medium transition-all"
              >
                <Github className="h-5 w-5" />
                GitHub
              </a>
            </div>
            
            {/* Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mt-16">
              <div>
                <div className="text-3xl font-bold text-gray-900 dark:text-white">10+</div>
                <div className="text-sm text-gray-600 dark:text-gray-400">.NET Versions</div>
              </div>
              <div>
                <div className="text-3xl font-bold text-gray-900 dark:text-white">500k+</div>
                <div className="text-sm text-gray-600 dark:text-gray-400">Downloads</div>
              </div>
              <div>
                <div className="text-3xl font-bold text-gray-900 dark:text-white">100%</div>
                <div className="text-sm text-gray-600 dark:text-gray-400">Open Source</div>
              </div>
              <div>
                <div className="text-3xl font-bold text-gray-900 dark:text-white">MIT</div>
                <div className="text-sm text-gray-600 dark:text-gray-400">License</div>
              </div>
            </div>
          </motion.div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-24 bg-white dark:bg-gray-900">
        <div className="container mx-auto px-4">
          <motion.div
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            className="text-center mb-16"
          >
            <h2 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
              Powerful Features
            </h2>
            <p className="text-xl text-gray-600 dark:text-gray-400">
              Enterprise-level SMTP server features
            </p>
          </motion.div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {features.map((feature, index) => {
              const Icon = feature.icon;
              return (
                <motion.div
                  key={index}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: index * 0.1 }}
                  className="group relative bg-white dark:bg-gray-800 rounded-2xl p-8 shadow-sm hover:shadow-xl transition-all duration-300 border border-gray-200 dark:border-gray-700"
                >
                  <div className={`absolute inset-0 bg-gradient-to-br ${feature.color} opacity-0 group-hover:opacity-5 rounded-2xl transition-opacity`} />
                  
                  <div className={`inline-flex p-3 rounded-lg bg-gradient-to-br ${feature.color} mb-4`}>
                    <Icon className="h-6 w-6 text-white" />
                  </div>
                  
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                    {feature.title}
                  </h3>
                  
                  <p className="text-gray-600 dark:text-gray-400">
                    {feature.description}
                  </p>
                </motion.div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Code Example Section */}
      <section className="py-24 bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto px-4">
          <div className="max-w-6xl mx-auto">
            <motion.div
              initial={{ opacity: 0 }}
              whileInView={{ opacity: 1 }}
              viewport={{ once: true }}
              className="text-center mb-16"
            >
              <h2 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
                Simple and Powerful API
              </h2>
              <p className="text-xl text-gray-600 dark:text-gray-400">
                Set up and run your SMTP server in minutes
              </p>
            </motion.div>
            
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true }}
            >
              <CodeBlock 
                code={codeExample}
                language="csharp"
                filename="Program.cs"
                showLineNumbers={false}
              />
            </motion.div>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-12">
              <div className="flex items-start gap-3">
                <CheckCircle className="h-5 w-5 text-green-500 mt-1 flex-shrink-0" />
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Easy Setup</h4>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    One-command installation with NuGet
                  </p>
                </div>
              </div>
              
              <div className="flex items-start gap-3">
                <CheckCircle className="h-5 w-5 text-green-500 mt-1 flex-shrink-0" />
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Fluent Builder</h4>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Readable and understandable configuration
                  </p>
                </div>
              </div>
              
              <div className="flex items-start gap-3">
                <CheckCircle className="h-5 w-5 text-green-500 mt-1 flex-shrink-0" />
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Event-Driven</h4>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Full control with rich event system
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-24 bg-gradient-to-r from-primary-600 to-primary-800">
        <div className="container mx-auto px-4 text-center">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            className="max-w-3xl mx-auto"
          >
            <h2 className="text-4xl font-bold text-black dark:text-white mb-6">
              Ready to Get Started?
            </h2>
            <p className="text-xl text-primary-100 mb-8">
              Set up your powerful and reliable SMTP server with Zetian in minutes.
            </p>
            
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Link
                href="/docs/getting-started"
                className="inline-flex items-center justify-center gap-2 px-8 py-4 bg-white text-primary-700 rounded-lg font-medium transition-all transform hover:scale-105 shadow-lg hover:shadow-xl"
              >
                <Zap className="h-5 w-5" />
                Go to Documentation
                <ArrowRight className="h-4 w-4" />
              </Link>
              
              <a
                href="https://www.nuget.org/packages/Zetian"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center gap-2 px-8 py-4 bg-primary-700 hover:bg-primary-900 text-black dark:text-white rounded-lg font-medium transition-all border border-primary-500"
              >
                <Package className="h-5 w-5" />
                Download from NuGet
              </a>
            </div>
          </motion.div>
        </div>
      </section>
    </div>
  );
}