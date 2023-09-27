using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VinderenApi.DbContext;
using VinderenApi.Configurations;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using Microsoft.AspNetCore;


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

//Razor pages
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AddPageRoute("/", "/Pages");
    });

// Adding IdentityDbContext
// Get the secret connection string before declaring the var.
builder.Configuration.AddUserSecrets<Program>();
// For development:
var dbConn = builder.Configuration["Secret:SmarterASPNET"];

builder.Services.AddDbContext<EntityContext>(options =>
{
    options.UseSqlServer(dbConn); //builder.Configuration.GetConnectionString() gets the string from appsettings.
    options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug())); //To log DB interactions in EF Core, like "SaveChanges()"
});


// Create a singletons from secret values
var jwtConfigSecret = builder.Configuration["Secret2:JwtConfigSecret"];
var jwtConfigIssuer = builder.Configuration["Secret3:JwtConfigIssuer"];
var jwtConfigAudience = builder.Configuration["Secret4:JwtConfigAudience"];
var loginAttemptsCacheKeyPrefix = builder.Configuration["Secret5:LoginAttemptsCacheKeyPrefix"];


var jwtConfig = new JwtConfig
{
    Secret = jwtConfigSecret,
    Issuer = jwtConfigIssuer,
    Audience = jwtConfigAudience
};

var cacheKeyConfig = new CacheKeyConfig
{
    LoginAttemptsCacheKeyPrefix = loginAttemptsCacheKeyPrefix
};

builder.Services.AddSingleton(jwtConfig);
builder.Services.AddSingleton(cacheKeyConfig);

//Configures the usage of Identity.
//Essensially connects and configures the EntityContext to the tables like AspNetUser and AspNetRoles in the database.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddUserStore<UserStore<IdentityUser, IdentityRole, EntityContext, string>>()
.AddRoleStore<RoleStore<IdentityRole, EntityContext, string>>()
.AddDefaultTokenProviders()
.AddEntityFrameworkStores<EntityContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(jwt =>
    {
        var key = Encoding.ASCII.GetBytes(jwtConfig.Secret); // Upon receiving the token back from the client, this specifies where the "authorizer" should be comparing.
        
        jwt.RequireHttpsMetadata = false;
        jwt.SaveToken = true;

		// These parameters ensures that the token is validated correctly by intercepting the http requests
		jwt.TokenValidationParameters = new TokenValidationParameters()
        {
            //RoleClaimType = "role", why is this one inhibiting the authorization???
			ValidAlgorithms = new List<string> { SecurityAlgorithms.HmacSha256 },
			IssuerSigningKey = new SymmetricSecurityKey(key),
			ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateIssuer = true, //if true, an issuer is generally the host server for (this) API
            ValidAudience = jwtConfig.Audience,
            ValidateAudience = true, // if true, validates the expected receiver 
            RequireExpirationTime = false, 
            ValidateLifetime = false
		};
    });

builder.Services.AddAuthorization();

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
} else if(app.Environment.IsProduction())
{
    app.Urls.Add("http://0.0.0.0:80"); // Listen on 0.0.0.0:80 for production in Render
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

app.Run();
