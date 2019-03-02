using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using WebAPITemplate.Database;
using WebAPITemplate.Helpers;
using WebAPITemplate.Helpers.Messages;

namespace WebAPITemplate
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DataConnection")));
            services.AddLocalization(o => o.ResourcesPath = "Resources");

            // Dependecy Injection definitions
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddMvc()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Globals.TokenAuthenticateScheme;
                options.DefaultChallengeScheme = Globals.TokenChallengeScheme;
            })
            .AddJwtBearer(Globals.TokenAuthenticateScheme, jwtBearerOptions =>
            {
                jwtBearerOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
                        var userId = context.Principal.Identity.Name;
                        var user = unitOfWork.UsersRepository.GetByID(userId);
                        if (user == null)
                        {
                            context.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    }
                };
                jwtBearerOptions.RequireHttpsMetadata = false;
                jwtBearerOptions.SaveToken = true;
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.TokenSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = Globals.TokenIssuer,
                    ValidateAudience = true,
                    ValidAudience = Globals.TokenAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // LOCALIZATION CONFIGURATION
            var supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("es")
            };
            var options = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("es"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
            };
            app.UseRequestLocalization(options);

            app.UseCors("MyPolicy");

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
