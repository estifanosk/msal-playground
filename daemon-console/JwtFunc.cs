
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace daemon_console
{
    public delegate Task<AuthenticationResult> JwtFunc(string audience);
}