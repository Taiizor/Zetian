using System.Security.Cryptography.X509Certificates;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Examples of loading different certificate formats for SMTP server
    /// </summary>
    public static class CertificateFormatsExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Certificate Format Examples ===\n");

            // Example 1: PFX/P12 Certificate with password
            await RunPfxExample();

            // Example 2: PEM Certificate
            await RunPemExample();

            // Example 3: CER/CRT Certificate
            await RunCerExample();

            // Example 4: Auto-detect format
            await RunAutoDetectExample();
        }

        private static async Task RunPfxExample()
        {
            Console.WriteLine("1. Loading PFX/P12 Certificate:");

            try
            {
                using SmtpServer server = new SmtpServerBuilder()
                    .Port(465)
                    .ServerName("PFX Certificate Server")
#if NET9_0_OR_GREATER
                    // Use the new PFX-specific method for .NET 9.0+
                    .CertificateFromPfx(
                        "path/to/certificate.pfx",
                        "password123",
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet)
#else
                    // For older versions, use the generic method
                    .Certificate("path/to/certificate.pfx", "password123")
#endif
                    .RequireSecureConnection()
                    .Build();

                Console.WriteLine("   ✓ PFX certificate loaded successfully");
                Console.WriteLine($"   Server configured on port {server.Configuration.Port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ Failed to load PFX: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task RunPemExample()
        {
            Console.WriteLine("2. Loading PEM Certificate:");

            try
            {
#if NET6_0_OR_GREATER
                // Example with separate certificate and key files
                using SmtpServer server1 = new SmtpServerBuilder()
                    .Port(587)
                    .ServerName("PEM Certificate Server")
                    .CertificateFromPem(
                        "path/to/certificate.pem",
                        "path/to/private-key.pem")
                    .Build();

                Console.WriteLine("   ✓ PEM certificate with separate key loaded");

                // Example with certificate and key in same file
                using SmtpServer server2 = new SmtpServerBuilder()
                    .Port(587)
                    .ServerName("PEM Combined Server")
                    .CertificateFromPem("path/to/combined-cert-and-key.pem")
                    .Build();

                Console.WriteLine("   ✓ Combined PEM certificate loaded");
#else
                Console.WriteLine("   ⚠ PEM certificates require .NET 6.0 or later");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ Failed to load PEM: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task RunCerExample()
        {
            Console.WriteLine("3. Loading CER/CRT Certificate:");

            try
            {
                using SmtpServer server = new SmtpServerBuilder()
                    .Port(25)
                    .ServerName("CER Certificate Server")
#if NET9_0_OR_GREATER
                    // Use the CER-specific method for .NET 9.0+
                    .CertificateFromCer("path/to/certificate.cer")
#else
                    // For older versions, use the generic method without password
                    .Certificate("path/to/certificate.cer")
#endif
                    .Build();

                Console.WriteLine("   ✓ CER certificate loaded successfully");
                Console.WriteLine("   Note: CER files typically don't contain private keys");
                Console.WriteLine("         and may not be suitable for server authentication");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ Failed to load CER: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task RunAutoDetectExample()
        {
            Console.WriteLine("4. Auto-Detect Certificate Format:");

            // The generic Certificate method attempts to auto-detect format
            string[] testFiles =
            {
                "certificate.pfx",
                "certificate.p12",
                "certificate.pem",
                "certificate.cer",
                "certificate.crt"
            };

            foreach (string file in testFiles)
            {
                try
                {
                    using SmtpServer server = new SmtpServerBuilder()
                        .Port(587)
                        .ServerName($"Auto-detect: {file}")
                        .Certificate($"path/to/{file}", GetPasswordForFile(file))
                        .Build();

                    Console.WriteLine($"   ✓ {file} loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ✗ {file}: {ex.Message}");
                }
            }

            Console.WriteLine();
        }

        private static string? GetPasswordForFile(string filename)
        {
            // PFX and P12 files usually require passwords
            if (filename.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
            {
                return "password123";
            }

            // PEM, CER, CRT usually don't require passwords
            return null;
        }

        /// <summary>
        /// Demonstrates best practices for certificate loading
        /// </summary>
        public static void ShowBestPractices()
        {
            Console.WriteLine("=== Certificate Loading Best Practices ===\n");

            Console.WriteLine("1. PFX/P12 Files:");
            Console.WriteLine("   - Contains both certificate and private key");
            Console.WriteLine("   - Usually password-protected");
            Console.WriteLine("   - Best for production use");
            Console.WriteLine("   - Use CertificateFromPfx() for explicit loading");
            Console.WriteLine();

            Console.WriteLine("2. PEM Files:");
            Console.WriteLine("   - Text-based format (Base64)");
            Console.WriteLine("   - Can contain certificate, private key, or both");
            Console.WriteLine("   - Common in Linux/Unix environments");
            Console.WriteLine("   - Use CertificateFromPem() with separate or combined files");
            Console.WriteLine();

            Console.WriteLine("3. CER/CRT Files:");
            Console.WriteLine("   - Usually contains only the certificate (no private key)");
            Console.WriteLine("   - May not be suitable for server authentication");
            Console.WriteLine("   - Use CertificateFromCer() for explicit loading");
            Console.WriteLine();

            Console.WriteLine("4. Security Recommendations:");
            Console.WriteLine("   - Store certificates securely");
            Console.WriteLine("   - Use strong passwords for PFX files");
            Console.WriteLine("   - Consider using X509KeyStorageFlags.EphemeralKeySet");
            Console.WriteLine("   - Rotate certificates regularly");
            Console.WriteLine("   - Use proper file permissions");
            Console.WriteLine();

            Console.WriteLine("5. Platform Considerations:");
            Console.WriteLine("   - PEM support requires .NET 6.0+");
            Console.WriteLine("   - X509CertificateLoader requires .NET 9.0+");
            Console.WriteLine("   - Older versions fall back to X509Certificate2 constructors");
        }
    }
}