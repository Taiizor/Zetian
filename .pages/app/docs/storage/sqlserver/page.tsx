'use client';

import Link from 'next/link';
import { Database, Shield, CheckCircle, Zap, Server, FileCode2, HardDrive } from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = `dotnet add package Zetian
dotnet add package Zetian.Storage.SqlServer`;

const quickStartExample = `using Zetian.Server;
using Zetian.Storage.SqlServer.Extensions;

// Basic setup - table created automatically
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpDb;Trusted_Connection=true;")
    .Build();

await server.StartAsync();`;

const advancedExample = `var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpStorage;Integrated Security=true;",
        config =>
        {
            config.TableName = "SmtpMessages";
            config.AttachmentsTableName = "SmtpAttachments";
            config.SchemaName = "mail";
            config.AutoCreateTable = true;
            config.StoreAttachmentsSeparately = true;
            config.CompressMessageBody = true;
            config.MaxMessageSizeMB = 50;
            config.EnableRetry = true;
            config.MaxRetryAttempts = 3;
        })
    .Build();`;

const connectionStrings = `// Windows Authentication
"Server=localhost;Database=SmtpDb;Trusted_Connection=true;"

// SQL Server Authentication
"Server=localhost;Database=SmtpDb;User Id=sa;Password=YourPassword;"

// Azure SQL Database
"Server=tcp:yourserver.database.windows.net,1433;
 Initial Catalog=SmtpDb;User ID=yourusername;Password=yourpassword;
 Encrypt=True;TrustServerCertificate=False;"

// Named Instance
"Server=localhost\\SQLEXPRESS;Database=SmtpDb;Trusted_Connection=true;"`;

const tableSchema = `-- Messages table (auto-created)
CREATE TABLE [SmtpMessages] (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    MessageId NVARCHAR(255) NOT NULL UNIQUE,
    SessionId NVARCHAR(255) NOT NULL,
    FromAddress NVARCHAR(500),
    ToAddresses NVARCHAR(MAX) NOT NULL,
    Subject NVARCHAR(1000),
    ReceivedDate DATETIME2 NOT NULL,
    MessageSize BIGINT NOT NULL,
    MessageBody VARBINARY(MAX) NOT NULL,
    IsCompressed BIT DEFAULT 0,
    Headers NVARCHAR(MAX),
    HasAttachments BIT DEFAULT 0,
    AttachmentCount INT DEFAULT 0,
    RemoteIP NVARCHAR(45),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);`;

export default function SqlServerStoragePage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-blue-600 dark:hover:text-blue-400">Documentation</Link>
            <span>/</span>
            <Link href="/docs/storage" className="hover:text-blue-600 dark:hover:text-blue-400">Storage</Link>
            <span>/</span>
            <span>SQL Server</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">SQL Server Storage Provider</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Enterprise-grade message storage with Microsoft SQL Server and Azure SQL Database.
          </p>
        </div>

        {/* Features */}
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Database className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Enterprise Features</h3>
              <div className="flex flex-wrap gap-2">
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  ACID Compliant
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Auto Schema Creation
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Message Compression
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Full-Text Search
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Azure SQL Support
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Installation */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Installation</h2>
          <CodeBlock code={installCommand} language="bash" showLineNumbers={false} />
        </section>

        {/* Quick Start */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Zap className="h-6 w-6" />
            Quick Start
          </h2>
          <CodeBlock code={quickStartExample} language="csharp" filename="QuickStart.cs" />
        </section>

        {/* Advanced Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Advanced Configuration
          </h2>
          <CodeBlock code={advancedExample} language="csharp" filename="AdvancedConfig.cs" />
        </section>

        {/* Configuration Table */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Configuration Options</h2>
          <div className="overflow-x-auto">
            <table className="min-w-full bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                    Option
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                    Default
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                    Description
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">TableName</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"SmtpMessages"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Main messages table</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">SchemaName</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"dbo"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Database schema</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">AutoCreateTable</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Auto-create tables</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">StoreAttachmentsSeparately</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Separate attachments table</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">CompressMessageBody</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">GZIP compression</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">MaxMessageSizeMB</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">100</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Max message size</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Connection Strings */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <FileCode2 className="h-6 w-6" />
            Connection Strings
          </h2>
          <CodeBlock code={connectionStrings} language="csharp" filename="ConnectionStrings.cs" />
        </section>

        {/* Schema */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <HardDrive className="h-6 w-6" />
            Table Schema
          </h2>
          <CodeBlock code={tableSchema} language="sql" filename="Schema.sql" />
        </section>

        {/* Performance Tips */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Shield className="h-6 w-6" />
            Performance & Security
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable Compression</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Reduces storage by 60-80%</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Connection Pooling</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Adjust pool size for load</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Use Least Privilege</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Grant minimal permissions</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable TDE</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Encryption at rest</p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/docs/storage/postgresql" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  PostgreSQL Storage →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">JSONB and partitioning</p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          <Link href="/docs/storage/mongodb" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  MongoDB Storage →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">NoSQL with GridFS</p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}