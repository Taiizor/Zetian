import { NextResponse } from 'next/server';

export async function GET() {
  try {
    // You can add additional health checks here
    // For example: database connectivity, external service availability, etc.
    
    const healthData = {
      status: 'healthy',
      timestamp: new Date().toISOString(),
      uptime: process.uptime(),
      environment: process.env.NODE_ENV || 'development',
      version: process.env.npm_package_version || '1.0.0',
      memory: {
        rss: process.memoryUsage().rss / 1024 / 1024, // MB
        heapTotal: process.memoryUsage().heapTotal / 1024 / 1024, // MB
        heapUsed: process.memoryUsage().heapUsed / 1024 / 1024, // MB
      },
    };

    return NextResponse.json(healthData, { status: 200 });
  } catch (error) {
    return NextResponse.json(
      { 
        status: 'unhealthy', 
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: new Date().toISOString() 
      },
      { status: 503 }
    );
  }
}