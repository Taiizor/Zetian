using System.Threading.Tasks;
using Zetian.Models;

namespace Zetian.Delegates
{
    /// <summary>
    /// Authentication handler delegate
    /// </summary>
    public delegate Task<AuthenticationResult> AuthenticationHandler(string? username, string? password);
}