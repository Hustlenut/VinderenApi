using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VinderenApi.DbContext;
using VinderenApi.Configurations;

var builder = WebApplication.CreateBuilder(args);
//TODO: Provide secure policies
builder.Services.AddCors(
    options =>
    {
        options.AddPolicy("AllowAll",
            builder => builder
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod()

        );
    }
);

// Add services to the container.

// Adding IdentityDbContext
var dbConn = builder.Configuration["SmarterASPNET:DbConnectionString"];

builder.Services.AddDbContext<EntityContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString(dbConn));
    options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()));
});

builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(jwt =>
    {
        var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtConfig:Secret").Value);

        jwt.SaveToken = true;
        jwt.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, //remember to switch to true in production...
            ValidateAudience = false, //remember to switch to true in production...
            RequireExpirationTime = false, //remember to switch to true in production, check out refresh token.
            ValidateLifetime = true
        };
    });

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
//.AddEntityFrameworkStores<IdentityEntity>();
.AddRoles<IdentityRole>()
.AddUserStore<UserStore<IdentityUser, IdentityRole, EntityContext, string>>()
.AddRoleStore<RoleStore<IdentityRole, EntityContext, string>>()
.AddDefaultTokenProviders()
.AddEntityFrameworkStores<EntityContext>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
