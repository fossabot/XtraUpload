﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using XtraUpload.Authentication.Service.Common;
using XtraUpload.Domain;

namespace XtraUpload.Authentication.Service
{
    /// <summary>
    /// Factory to generate jwt tokens
    /// </summary>
    public class JwtFactory : IJwtFactory
    {
        private readonly IOptionsMonitor<JwtIssuerOptions> _jwtOptions;

        public JwtFactory(IOptionsMonitor<JwtIssuerOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions;
            ThrowIfInvalidOptions(_jwtOptions.CurrentValue);
        }

        public async Task<string> GenerateEncodedToken(string userName, ClaimsIdentity identity)
        {
            var claims = new List<Claim>
            {
                 new Claim(JwtRegisteredClaimNames.Sub, userName),
                 new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.CurrentValue.JtiGenerator()),
                 new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.CurrentValue.IssuedAt).ToString(), ClaimValueTypes.Integer64)
            };
            claims.AddRange(identity.Claims);

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.CurrentValue.Issuer,
                audience: _jwtOptions.CurrentValue.Audience,
                claims: claims,
                notBefore: _jwtOptions.CurrentValue.NotBefore,
                expires: _jwtOptions.CurrentValue.Expiration,
                signingCredentials: _jwtOptions.CurrentValue.SigningCredentials);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        public ClaimsIdentity GenerateClaimsIdentity(User user, IEnumerable<RoleClaim> claims)
        {
            List<Claim> jwtClaims = new List<Claim>()
            {
                new Claim("id", user.Id)
            };
            foreach (var claim in claims)
            {
                jwtClaims.Add(new Claim(claim.ClaimType, claim.ClaimValue) );
            }
            return new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), jwtClaims);
        }

        /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() -
                               new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                              .TotalSeconds);

        private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.ValidFor <= 0)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
            }
        }
    }
}