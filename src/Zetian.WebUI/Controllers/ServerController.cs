using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zetian.Abstractions;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Controllers
{
    /// <summary>
    /// API controller for server management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServerController : ControllerBase
    {
        private readonly ISmtpServer _smtpServer;
        private readonly IConfigurationService _configurationService;
        private readonly IDashboardService _dashboardService;

        public ServerController(
            ISmtpServer smtpServer,
            IConfigurationService configurationService,
            IDashboardService dashboardService)
        {
            _smtpServer = smtpServer;
            _configurationService = configurationService;
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Gets server status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            DashboardOverview overview = await _dashboardService.GetOverviewAsync();
            return Ok(new
            {
                IsRunning = _smtpServer.IsRunning,
                Status = overview.ServerStatus.ToString(),
                Endpoint = _smtpServer.Endpoint?.ToString(),
                Uptime = overview.Uptime,
                ActiveSessions = overview.ActiveSessions,
                TotalMessages = overview.TotalMessages,
                MessagesPerSecond = overview.MessagesPerSecond,
                CpuUsage = overview.CpuUsage,
                MemoryUsage = overview.MemoryUsage
            });
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        [HttpPost("start")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Start()
        {
            if (_smtpServer.IsRunning)
            {
                return BadRequest(new { error = "Server is already running" });
            }

            await _smtpServer.StartAsync();
            return Ok(new { message = "Server started successfully" });
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        [HttpPost("stop")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Stop()
        {
            if (!_smtpServer.IsRunning)
            {
                return BadRequest(new { error = "Server is not running" });
            }

            await _smtpServer.StopAsync();
            return Ok(new { message = "Server stopped successfully" });
        }

        /// <summary>
        /// Restarts the server
        /// </summary>
        [HttpPost("restart")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restart()
        {
            if (_smtpServer.IsRunning)
            {
                await _smtpServer.StopAsync();
            }

            await _smtpServer.StartAsync();
            return Ok(new { message = "Server restarted successfully" });
        }

        /// <summary>
        /// Gets server configuration
        /// </summary>
        [HttpGet("config")]
        public async Task<IActionResult> GetConfiguration()
        {
            ConfigurationDto config = await _configurationService.GetConfigurationAsync();
            return Ok(config);
        }

        /// <summary>
        /// Updates server configuration
        /// </summary>
        [HttpPut("config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateConfiguration([FromBody] ConfigurationUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ConfigurationUpdateResult result = await _configurationService.UpdateConfigurationAsync(request);
            if (result.Success)
            {
                return Ok(new { message = "Configuration updated successfully", requiresRestart = result.RequiresRestart });
            }

            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Gets server health
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHealth()
        {
            HealthStatus health = await _dashboardService.GetHealthStatusAsync();
            return Ok(health);
        }

        /// <summary>
        /// Gets server metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            ServerMetrics metrics = await _dashboardService.GetMetricsAsync();
            return Ok(metrics);
        }

        /// <summary>
        /// Gets server information
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                ServerName = _smtpServer.Configuration.ServerName,
                Version = GetType().Assembly.GetName().Version?.ToString(),
                DotNetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ProcessorCount = System.Environment.ProcessorCount,
                MachineName = System.Environment.MachineName
            });
        }
    }

    /// <summary>
    /// Configuration update request
    /// </summary>
    public class ConfigurationUpdateRequest
    {
        public int? Port { get; set; }
        public string? ServerName { get; set; }
        public long? MaxMessageSize { get; set; }
        public int? MaxRecipients { get; set; }
        public int? MaxConnections { get; set; }
        public int? MaxConnectionsPerIp { get; set; }
        public int? ConnectionTimeoutSeconds { get; set; }
        public int? CommandTimeoutSeconds { get; set; }
        public int? DataTimeoutSeconds { get; set; }
        public int? MaxRetryCount { get; set; }
        public bool? RequireAuthentication { get; set; }
        public bool? RequireSecureConnection { get; set; }
        public bool? EnableSmtpUtf8 { get; set; }
        public bool? EnablePipelining { get; set; }
        public bool? Enable8BitMime { get; set; }
    }
}