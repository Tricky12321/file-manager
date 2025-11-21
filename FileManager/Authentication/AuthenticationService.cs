using System;
using System.Text;

namespace FileManager.Authentication;

public static class AuthenticationService
{
    public static bool ValidateJwtToken(string token, string secret, out System.Security.Claims.ClaimsPrincipal principal)
    {
        principal = null;
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false, // Set to true if you want to validate the issuer
                ValidateAudience = false, // Set to true if you want to validate the audience
                ValidateLifetime = true, // Ensure the token is not expired
                ClockSkew = TimeSpan.Zero
            };

            principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JWT Validation Failed: {ex.Message}");
            return false;
        }
    }
}