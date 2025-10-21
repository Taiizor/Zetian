import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "@/components/Providers";
import { Navigation } from "@/components/Navigation";
import { Footer } from "@/components/Footer";
import { Analytics } from "@/components/Analytics";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: {
    default: "Zetian - Professional SMTP Server for .NET",
    template: "%s | Zetian SMTP Server",
  },
  description: "High-performance, extensible SMTP server library for .NET with TLS support, authentication, rate limiting, and modern async patterns.",
  keywords: [
    "SMTP",
    "email server",
    ".NET",
    "C#",
    "mail server",
    "TLS",
    "authentication",
    "rate limiting",
    "async",
    "dotnet",
  ],
  authors: [{ name: "Taiizor", url: "https://github.com/Taiizor" }],
  creator: "Taiizor",
  publisher: "Vegalya",
  metadataBase: new URL("https://zetian.soferity.com"),
  openGraph: {
    title: "Zetian - Professional SMTP Server for .NET",
    description: "High-performance SMTP server library with minimal dependencies",
    url: "https://zetian.soferity.com",
    siteName: "Zetian Documentation",
    type: "website",
    locale: "en_GB",
    images: [
      {
        url: "/Logo.png",
        width: 1200,
        height: 630,
        alt: "Zetian SMTP Server",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "Zetian - Professional SMTP Server for .NET",
    description: "High-performance SMTP server library with minimal dependencies",
    images: ["/Logo.png"],
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <link rel="icon" href="/favicon.ico" sizes="any" />
        <link rel="manifest" href="/manifest.json" />
      </head>
      <body className={`${inter.className} antialiased`}>
        <Providers>
          <div className="flex min-h-screen flex-col">
            <Navigation />
            <main className="flex-1">{children}</main>
            <Footer />
          </div>
        </Providers>
        <Analytics />
      </body>
    </html>
  );
}