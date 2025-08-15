
using System.Text;
using JWTAuthTemplate.Context;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Extensions;

namespace JWTAuthTemplate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            builder.Services.Configure<MinioSettingsDTO>(builder.Configuration.GetSection("MinioSettings"));
            builder.Services.AddSingleton<MinioService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Configuration["JWT:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["JWT:Secret"];
            builder.Configuration["JWT:ValidAudience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["JWT:ValidAudience"];
            builder.Configuration["JWT:ValidIssuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["JWT:ValidIssuer"];
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("DefaultConnection");

            //Add Postgres database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
            using (var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                       .UseNpgsql(connectionString).Options))
            {
                context.Database.Migrate();
            }

            //Add identity
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            //Set up JWT
            builder.Services.AddAuthentication(opts =>
                {
                    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                //TODO: CHANGE THESE VALUES!!!
                //These settings are super insecure. DO NOT USE THESE IN PRODUCTION.
                .AddJwtBearer(opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!))
                    };
                })
                .AddCookie(opts =>
                {
                    opts.Cookie.HttpOnly = true;
                    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    opts.LoginPath = "/account/login"; // Define your login path
                });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<TestMatrixService>();

            if (builder.Environment.IsDevelopment())
            {
                //Comment this out to avoid seed data
                //SampleSeedData.SeedData(builder.Services.BuildServiceProvider().GetRequiredService<ApplicationDbContext>());

                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new() { Title = "JWTAuthTemplate", Version = "v1"});
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter a valid token",
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                        Scheme = "Bearer"

                    });
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type=ReferenceType.SecurityScheme,
                                    Id="Bearer"
                                }
                            },
                            new string[]{}
                        }
                    });
                });
            }

            var app = builder.Build();

            app.UseCors("AllowAll");
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}