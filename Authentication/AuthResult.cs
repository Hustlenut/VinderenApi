using System.IdentityModel.Tokens.Jwt;

namespace VinderenApi.Authentication
{
    public class AuthResult
    {
        public string? Token { get; set; }
        public JwtSecurityToken? Claim { get; set; }
        public bool? Result { get; set; }
        public List<string>? Errors { get; set; }
    }
}
