using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using API.Services;

namespace API.Controllers
{
    [ApiController]
    [Route("api/diag")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly LdapOptions _opt;
        private readonly ILdapAuthService _ldap;

        public DiagnosticsController(IOptions<LdapOptions> opt, ILdapAuthService ldap)
        {
            _opt = opt.Value;
            _ldap = ldap;
        }

        // OMDØBT så vi ikke kolliderer med System.Net.Dns
        [HttpGet("dns")]
        [AllowAnonymous]
        public IActionResult DnsLookup([FromQuery] string? host = null)
        {
            var h = host ?? _opt.Server;
            try
            {
                var addrs = System.Net.Dns.GetHostAddresses(h)
                    .Select(a => new { a.AddressFamily, Address = a.ToString() });
                return Ok(new { host = h, addresses = addrs });
            }
            catch (Exception ex)
            {
                return BadRequest(new { host = h, error = ex.Message });
            }
        }

        [HttpGet("ldap/port")]
        [AllowAnonymous]
        public async Task<IActionResult> Port([FromQuery] string? host = null, [FromQuery] int? port = null, [FromQuery] int timeoutMs = 3000)
        {
            var h = host ?? _opt.Server;
            var p = port ?? _opt.Port;
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(h, p);
                var done = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));
                if (done != connectTask)
                    return StatusCode(504, new { host = h, port = p, ok = false, error = "Timeout" });

                return Ok(new { host = h, port = p, ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { host = h, port = p, ok = false, error = ex.Message });
            }
        }

        public record BindDto(string Login, string Password);

        [HttpPost("ldap/bind")]
        [AllowAnonymous] // fjern/beskytt i prod
        public async Task<IActionResult> Bind([FromBody] BindDto dto)
        {
            var (ok, user, err) = await _ldap.ValidateAsync(dto.Login, dto.Password);
            if (!ok) return Unauthorized(new { ok, error = err });
            return Ok(new { ok, user });
        }
    }
}
