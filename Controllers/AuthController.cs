using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using VinderenApi.DbContext;
using VinderenApi.Models.DTO;

namespace VinderenApi.Controllers
{
    [Route("api/auth/[controller]")]
    public class AuthController
    {
        private readonly UserManager<IdentityUser> _userManager;
        //private readonly JwtConfig _jwtConfig;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<EntityContext> _identityDbContextOptions;

        public AuthController(UserManager<IdentityUser> userManager,
            /*JwtConfig jwtConfig*/
            IConfiguration configuration,
            DbContextOptions<EntityContext> identityDbContextOptions)
        {
            _userManager = userManager;
            //_jwtConfig = jwtConfig;
            _configuration = configuration;
            _identityDbContextOptions = identityDbContextOptions;

        }



    }
}
