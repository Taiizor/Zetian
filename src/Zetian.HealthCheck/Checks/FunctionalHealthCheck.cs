using System;
using System.Threading;
using System.Threading.Tasks;
using Zetian.HealthCheck.Abstractions;
using Zetian.HealthCheck.Models;

namespace Zetian.HealthCheck.Checks
{
    /// <summary>
    /// Functional health check implementation
    /// </summary>
    internal class FunctionalHealthCheck(Func<CancellationToken, Task<HealthCheckResult>> checkFunc) : IHealthCheck
    {
        private readonly Func<CancellationToken, Task<HealthCheckResult>> _checkFunc = checkFunc ?? throw new ArgumentNullException(nameof(checkFunc));

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return _checkFunc(cancellationToken);
        }
    }
}