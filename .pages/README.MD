# Zetian SMTP Server Documentation Site

This folder contains the modern, SEO-optimized documentation site for Zetian SMTP Server.

## 🚀 Features

- ✅ **Next.js 15** - Modern React framework
- ✅ **Server-Side Rendering (SSR)** - Excellent SEO performance
- ✅ **Responsive Design** - Great appearance on all devices
- ✅ **Dark Mode** - Eye-friendly dark theme support
- ✅ **Search Feature** - Quick search with Cmd/Ctrl+K
- ✅ **Code Examples** - Real-world scenarios
- ✅ **API Reference** - Detailed API documentation
- ✅ **SEO Optimizations** - Sitemap, robots.txt, meta tags
- ✅ **Modern UI** - Tailwind CSS and Framer Motion animations

## 📦 Installation

1. **Install dependencies:**

```bash
cd pages
npm install
```

2. **Start development server:**

```bash
npm run dev
```

The site will open at http://localhost:3000.

## 🏗️ Build and Deploy

### Production Build

```bash
npm run build
```

### Production Server

```bash
npm run start
```

## 📁 Project Structure

```
pages/
├── app/                     # Next.js App Router
│   ├── page.tsx            # Homepage
│   ├── layout.tsx          # Main layout and SEO metadata
│   ├── globals.css         # Global styles
│   ├── docs/               # Documentation pages
│   │   ├── page.tsx        # Documentation homepage
│   │   └── getting-started/
│   │       └── page.tsx    # Getting started guide
│   ├── examples/           # Code examples
│   │   └── page.tsx        # Examples page
│   └── api/                # API reference
│       └── page.tsx        # API documentation
├── components/             # React components
│   ├── Navigation.tsx      # Main navigation
│   ├── Footer.tsx          # Footer component
│   ├── Search.tsx          # Search component
│   └── Providers.tsx       # Theme provider
├── lib/                    # Helper functions
│   └── utils.ts            # Utility functions
├── public/                 # Static files
│   ├── robots.txt          # SEO robots file
│   ├── sitemap.xml         # XML sitemap
│   └── manifest.json       # PWA manifest
└── package.json            # Project dependencies
```

## 🎨 Customization

### Color Theme

Edit `tailwind.config.ts` file to change colors.

### Content Update

- **Homepage:** `app/page.tsx`
- **Documentation:** `app/docs/` folder
- **Examples:** `app/examples/page.tsx`
- **API Reference:** `app/api/page.tsx`

### Adding New Pages

1. Create a new folder in `app/`
2. Add `page.tsx` file inside
3. Add link to Navigation: `components/Navigation.tsx`
4. Add to Search data: `components/Search.tsx`

## 🔍 SEO

- **Meta Tags:** Customized metadata on each page
- **Sitemap:** `public/sitemap.xml`
- **Robots:** `public/robots.txt`
- **Open Graph:** For social media shares
- **Structured Data:** For search engines

## 🌙 Dark Mode

Automatic system theme detection and manual theme switching support.

## ⌨️ Keyboard Shortcuts

- `Cmd/Ctrl + K` - Open search
- `Esc` - Close search
- `↑/↓` - Navigate results
- `Enter` - Select result

## 📝 License

MIT

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Commit your changes
4. Push your branch
5. Open Pull Request

## 💬 Support

- GitHub Issues: https://github.com/Taiizor/Zetian/issues
- GitHub Discussions: https://github.com/Taiizor/Zetian/discussions

---

**Zetian SMTP Server** - Built for the .NET community with ❤️