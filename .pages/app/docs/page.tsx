import { Metadata } from 'next';
import Link from 'next/link';
import { 
  BookOpen, 
  Rocket, 
  Settings, 
  Shield, 
  Code2, 
  Package,
  FileText,
  Github,
  Zap,
  ChevronRight,
  Heart,
  Database,
  Send,
  Filter,
  Activity
} from 'lucide-react';

export const metadata: Metadata = {
  title: 'Documentation',
  description: 'Zetian SMTP Server documentation - Installation, configuration and usage guide',
};

const docCategories = [
  {
    title: 'Getting Started',
    description: 'Quick setup and first steps',
    icon: Rocket,
    href: '/docs/getting-started',
    color: 'from-blue-500 to-indigo-600',
    items: [
      'Installation',
      'First Server',
      'Basic Concepts',
      'Quick Start'
    ]
  },
  {
    title: 'Configuration',
    description: 'Server settings and configuration',
    icon: Settings,
    href: '/docs/configuration',
    color: 'from-green-500 to-emerald-600',
    items: [
      'Port Settings',
      'TLS/SSL',
      'Rate Limiting',
      'Message Filters'
    ]
  },
  {
    title: 'Authentication',
    description: 'Security and authentication',
    icon: Shield,
    href: '/docs/authentication',
    color: 'from-purple-500 to-pink-600',
    items: [
      'PLAIN Auth',
      'LOGIN Auth',
      'Custom Auth',
      'TLS/STARTTLS'
    ]
  },
  {
    title: 'Message Processing',
    description: 'Receiving and processing messages',
    icon: FileText,
    href: '/docs/message-processing',
    color: 'from-yellow-500 to-orange-600',
    items: [
      'Event Handlers',
      'Message Storage',
      'Filtering',
      'Forwarding'
    ]
  },
  {
    title: 'Storage Providers',
    description: 'Message storage backends',
    icon: Database,
    href: '/docs/storage',
    color: 'from-indigo-500 to-blue-600',
    items: [
      'SQL Server / Azure SQL',
      'PostgreSQL / MongoDB',
      'Redis Cache',
      'S3 / Azure Blob'
    ]
  },
  {
    title: 'Relay',
    description: 'SMTP relay and proxy features',
    icon: Send,
    href: '/docs/relay',
    color: 'from-blue-500 to-purple-600',
    items: [
      'Smart Host Support',
      'Queue Management',
      'Load Balancing',
      'MX Routing'
    ]
  },
  {
    title: 'Anti Spam',
    description: 'Advanced spam protection',
    icon: Filter,
    href: '/docs/anti-spam',
    color: 'from-red-500 to-orange-600',
    items: [
      'Bayesian Filtering',
      'SPF/DKIM/DMARC',
      'RBL/DNSBL Checking',
      'Greylisting'
    ]
  },
  {
    title: 'Extensions',
    description: 'Plugin and extension development',
    icon: Package,
    href: '/docs/extensions',
    color: 'from-purple-500 to-indigo-600',
    items: [
      'Custom Filters',
      'Storage Providers',
      'Event Extensions',
      'Middleware'
    ]
  },
  {
    title: 'Monitoring',
    description: 'Real-time metrics and observability',
    icon: Activity,
    href: '/docs/monitoring',
    color: 'from-purple-500 to-pink-600',
    items: [
      'Prometheus Exporter',
      'OpenTelemetry',
      'Server Statistics',
      'Grafana Dashboard'
    ]
  },
  {
    title: 'Health Check',
    description: 'Health monitoring and checks',
    icon: Heart,
    href: '/docs/health-check',
    color: 'from-pink-500 to-rose-600',
    items: [
      'Basic Setup',
      'Custom Checks',
      'Kubernetes Integration',
      'HTTP Endpoints'
    ]
  },
  {
    title: 'API Reference',
    description: 'Detailed API documentation',
    icon: Code2,
    href: '/api',
    color: 'from-teal-500 to-cyan-600',
    items: [
      'SmtpServer',
      'SmtpServerBuilder',
      'ISmtpMessage',
      'Events'
    ]
  }
];

export default function DocsPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Documentation
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 max-w-3xl mx-auto">
            Access all information about Zetian SMTP Server here.
            Everything from quick start guide to detailed API reference.
          </p>
        </div>

        {/* Quick Links */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-12 max-w-4xl mx-auto">
          <Link 
            href="/docs/getting-started"
            className="flex items-center gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg shadow-sm hover:shadow-md transition-all border border-gray-200 dark:border-gray-800 group"
          >
            <div className="p-2 bg-primary-100 dark:bg-primary-900/30 rounded-lg group-hover:scale-110 transition-transform">
              <Zap className="h-5 w-5 text-primary-600 dark:text-primary-400" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-gray-900 dark:text-white">Quick Start</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">Start in 5 minutes</p>
            </div>
            <ChevronRight className="h-4 w-4 text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400" />
          </Link>

          <Link 
            href="/examples"
            className="flex items-center gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg shadow-sm hover:shadow-md transition-all border border-gray-200 dark:border-gray-800 group"
          >
            <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg group-hover:scale-110 transition-transform">
              <Code2 className="h-5 w-5 text-green-600 dark:text-green-400" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-gray-900 dark:text-white">Examples</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">Ready-to-use code samples</p>
            </div>
            <ChevronRight className="h-4 w-4 text-gray-400 group-hover:text-green-600 dark:group-hover:text-green-400" />
          </Link>

          <a 
            href="https://github.com/Taiizor/Zetian"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg shadow-sm hover:shadow-md transition-all border border-gray-200 dark:border-gray-800 group"
          >
            <div className="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg group-hover:scale-110 transition-transform">
              <Github className="h-5 w-5 text-gray-600 dark:text-gray-400" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-gray-900 dark:text-white">GitHub</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">Go to source code</p>
            </div>
            <ChevronRight className="h-4 w-4 text-gray-400 group-hover:text-gray-600 dark:group-hover:text-gray-400" />
          </a>
        </div>

        {/* Documentation Categories */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {docCategories.map((category) => {
            const Icon = category.icon;
            return (
              <Link
                key={category.href}
                href={category.href}
                className="group bg-white dark:bg-gray-900 rounded-xl p-6 shadow-sm hover:shadow-xl transition-all duration-300 border border-gray-200 dark:border-gray-800 relative overflow-hidden"
              >
                {/* Background Gradient */}
                <div className={`absolute inset-0 bg-gradient-to-br ${category.color} opacity-0 group-hover:opacity-5 transition-opacity`} />
                
                {/* Icon */}
                <div className={`inline-flex p-3 rounded-lg bg-gradient-to-br ${category.color} mb-4`}>
                  <Icon className="h-6 w-6 text-white" />
                </div>
                
                {/* Content */}
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
                  {category.title}
                </h3>
                
                <p className="text-gray-600 dark:text-gray-400 mb-4">
                  {category.description}
                </p>
                
                {/* Items */}
                <ul className="space-y-1">
                  {category.items.map((item) => (
                    <li key={item} className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-500">
                      <ChevronRight className="h-3 w-3" />
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
                
                {/* Arrow */}
                <div className="absolute bottom-6 right-6 opacity-0 group-hover:opacity-100 transition-opacity">
                  <ChevronRight className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                </div>
              </Link>
            );
          })}
        </div>

        {/* Help Section */}
        <div className="mt-16 text-center">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-gray-100 dark:bg-gray-800 rounded-full text-sm">
            <BookOpen className="h-4 w-4 text-gray-600 dark:text-gray-400" />
            <span className="text-gray-600 dark:text-gray-400">
              Need help?
            </span>
            <a 
              href="https://github.com/Taiizor/Zetian/issues"
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              GitHub Issues
            </a>
            <span className="text-gray-400 dark:text-gray-600">or</span>
            <a 
              href="https://github.com/Taiizor/Zetian/discussions"
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              Discussions
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}