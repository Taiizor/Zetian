using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Secure SMTP server with TLS/SSL support
    /// </summary>
    public static class SecureExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Secure SMTP Server with TLS on port 587...");
            Console.WriteLine();

            // Create a self-signed certificate for demo purposes
            X509Certificate2 certificate = CreateSelfSignedCertificate();

            // Create a secure SMTP server
            using SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .ServerName("Secure SMTP Server")
                .Certificate(certificate)
                .RequireSecureConnection(false) // STARTTLS support
                .MaxMessageSizeMB(50)
                .Build();

            // Subscribe to events
            server.SessionCreated += (sender, e) =>
            {
                string secure = e.Session.IsSecure ? "SECURE" : "PLAIN";
                Console.WriteLine($"[SESSION] New {secure} connection from {e.Session.RemoteEndPoint}");
            };

            server.MessageReceived += (sender, e) =>
            {
                string secure = e.Session.IsSecure ? "SECURE" : "PLAIN";
                Console.WriteLine($"[MESSAGE] Received {secure} message:");
                Console.WriteLine($"  From: {e.Message.From?.Address}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine($"  Connection Type: {secure}");
                Console.WriteLine();
            };

            // Start the server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine("The server supports STARTTLS for secure connections.");
            Console.WriteLine();
            Console.WriteLine("Test with:");
            Console.WriteLine("  telnet localhost 587");
            Console.WriteLine("  EHLO client");
            Console.WriteLine("  STARTTLS");
            Console.WriteLine();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }

        private static X509Certificate2 CreateSelfSignedCertificate()
        {
            X500DistinguishedName distinguishedName = new("CN=localhost");
            using RSA rsa = RSA.Create(2048);

            CertificateRequest request = new(
                distinguishedName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            X509Certificate2 certificate = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddYears(1));

#if NET9_0_OR_GREATER
            byte[] pfxData = certificate.Export(X509ContentType.Pfx, "");
            return X509CertificateLoader.LoadPkcs12(
                pfxData,
                "",
                X509KeyStorageFlags.MachineKeySet);
#else
            return new X509Certificate2(
                certificate.Export(X509ContentType.Pfx, ""),
                "",
                X509KeyStorageFlags.MachineKeySet);
#endif
        }
    }
}