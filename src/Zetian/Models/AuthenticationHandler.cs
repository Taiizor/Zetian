using System.Threading.Tasks;

namespace Zetian.Models
{
    /// <summary>
    /// Authentication handler delegate
    /// </summary>
    public delegate Task<AuthenticationResult> AuthenticationHandler(string? username, string? password);
}