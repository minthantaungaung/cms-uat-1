using aia_core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public interface ICmsTokenGenerator
    {
        string GetAccessToken(Staff model, string uniqueDeviceId);
    }
    public class CmsTokenGenerator : ICmsTokenGenerator
    {
        private readonly string Issuer;
        private readonly string Audience;
        private readonly string AccessTokenKey;
        private readonly long accessTokenLifeSpam;
        public CmsTokenGenerator(IConfiguration config)
        {
            Issuer = config["Auth0:Domain"];
            Audience = config["Auth0:Audience"];
            AccessTokenKey = config["Auth0:AccessToken"];

            accessTokenLifeSpam = long.Parse(config["Auth0:accessTokenLifeSpam"]);
        }

        public string GetAccessToken(Staff model, string uniqueDeviceId)
        {
            var claims = new[] {
                new System.Security.Claims.Claim(CMSClaim.ID, model.Id.ToString()),
                new System.Security.Claims.Claim(CMSClaim.Email, model.Email),
                new System.Security.Claims.Claim(CMSClaim.Name, model.Name),
                new System.Security.Claims.Claim(CMSClaim.RoleID, model.RoleId.ToString()),
                new System.Security.Claims.Claim(CMSClaim.GenerateToken, uniqueDeviceId),
                new System.Security.Claims.Claim(CMSClaim.JTI, Guid.NewGuid().ToString())
            };


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AccessTokenKey));
            SigningCredentials creds = new (key, SecurityAlgorithms.HmacSha256);

            DateTime expriyDate = Utils.GetDefaultDate().AddMinutes(accessTokenLifeSpam);
            //expriyDate = DateTime.Now.AddSeconds(15);

            var token = new JwtSecurityToken(Issuer, Audience,
              claims,
              expires: expriyDate,
              signingCredentials: creds);


            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}