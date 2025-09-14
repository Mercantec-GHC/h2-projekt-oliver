using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace API.Services
{
    public class LdapOptions
    {
        public string Server { get; set; } = "WIN-N4I62GSQN78.johotel.local";
        public int Port { get; set; } = 636;                 
        public bool UseSsl { get; set; } = true;             
        public bool StartTls { get; set; } = false;          
        public bool AllowSelfSigned { get; set; } = true;    
        public string SearchBase { get; set; } = "DC=johotel,DC=local";
        public string? BindDn { get; set; }                  
        public string? BindPassword { get; set; }
        public string UserAttribute { get; set; } = "userPrincipalName";
        public Dictionary<string, string> GroupMappings { get; set; } = new();
    }

    public record LdapUser(string Login, string? Email, string? DisplayName, List<string> Groups);

    public interface ILdapAuthService
    {
        Task<(bool ok, LdapUser? user, string? error)> ValidateAsync(string login, string password, CancellationToken ct = default);
    }

    public class LdapAuthService : ILdapAuthService
    {
        private readonly LdapOptions _opt;
        public LdapAuthService(IOptions<LdapOptions> opt) => _opt = opt.Value;

        public async Task<(bool ok, LdapUser? user, string? error)> ValidateAsync(string login, string password, CancellationToken ct = default)
        {
            try
            {
                // LDAP forbindelse
                var id = new LdapDirectoryIdentifier(_opt.Server, _opt.Port, false, false);

                using var conn = new LdapConnection(id);
                if (_opt.UseSsl) conn.SessionOptions.SecureSocketLayer = true;
                if (_opt.StartTls) conn.SessionOptions.StartTransportLayerSecurity(null);
                if (_opt.AllowSelfSigned)
                {
                    
                    conn.SessionOptions.VerifyServerCertificate = (c, cert) => true;
                }

                if (!string.IsNullOrWhiteSpace(_opt.BindDn) && !string.IsNullOrWhiteSpace(_opt.BindPassword))
                {
                    // Bind som servicekonto 
                    conn.AuthType = AuthType.Basic;
                    conn.Credential = new NetworkCredential(_opt.BindDn, _opt.BindPassword);
                    await Task.Run(() => conn.Bind(), ct);

                    // Søg brugeren
                    string filter = $"(&(|(objectClass=user)(objectClass=person))({_opt.UserAttribute}={EscapeFilter(login)}))";
                    var attrs = new[] { "distinguishedName", "cn", "displayName", "mail", "memberOf", "userPrincipalName", "sAMAccountName" };
                    var req = new SearchRequest(_opt.SearchBase, filter, SearchScope.Subtree, attrs);
                    var resp = (SearchResponse)await Task.Run(() => conn.SendRequest(req), ct);
                    if (resp.Entries.Count == 0) return (false, null, "User not found");

                    var entry = resp.Entries[0];
                    var userDn = entry.DistinguishedName;

                    // Rebind som brugeren for at validere password
                    conn.AuthType = AuthType.Basic; 
                    conn.Credential = new NetworkCredential(userDn, password);
                    await Task.Run(() => conn.Bind(), ct);

                    var (email, display, groups) = Extract(entry);
                    return (true, new LdapUser(login, email, display, groups), null);
                }
                else
                {
                    // Direkte bind som brugeren 
                    conn.AuthType = AuthType.Negotiate; // tillader UPN
                    conn.Credential = new NetworkCredential(login, password);
                    await Task.Run(() => conn.Bind(), ct);

                    
                    string idAttr = _opt.UserAttribute;
                    string filter = $"(&(|(objectClass=user)(objectClass=person))({idAttr}={EscapeFilter(login)}))";
                    var attrs = new[] { "distinguishedName", "cn", "displayName", "mail", "memberOf", "userPrincipalName", "sAMAccountName" };
                    var req = new SearchRequest(_opt.SearchBase, filter, SearchScope.Subtree, attrs);
                    var resp = (SearchResponse)await Task.Run(() => conn.SendRequest(req), ct);

                    if (resp.Entries.Count == 0)
                        return (true, new LdapUser(login, null, null, new List<string>()), null);

                    var entry = resp.Entries[0];
                    var (email, display, groups) = Extract(entry);
                    return (true, new LdapUser(login, email, display, groups), null);
                }
            }
            catch (LdapException ex)
            {
                return (false, null, $"LDAP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private static string EscapeFilter(string value)
        {
            // RFC4515 escaping
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

        private static (string? email, string? displayName, List<string> groups) Extract(SearchResultEntry entry)
        {
            string? Get(string attr)
            {
                if (!entry.Attributes.Contains(attr)) return null;
                var vals = entry.Attributes[attr]?.GetValues(typeof(string));
                if (vals is { Length: > 0 }) return vals[0]?.ToString();
                return null;
            }

            var email = Get("mail");
            var display = Get("displayName") ?? Get("cn");

            var groups = new List<string>();
            if (entry.Attributes.Contains("memberOf"))
            {
                var vals = entry.Attributes["memberOf"].GetValues(typeof(string));
                foreach (var v in vals)
                {
                    var dn = v?.ToString() ?? string.Empty;
                    if (dn.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                    {
                        var cn = dn.Split(',', 2)[0].Substring(3);
                        groups.Add(cn);
                    }
                }
            }
            return (email, display, groups);
        }
    }
}
