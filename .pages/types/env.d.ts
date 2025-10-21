declare namespace NodeJS {
  interface ProcessEnv {
    // Next.js defaults
    NODE_ENV: 'development' | 'production' | 'test';
  }
}