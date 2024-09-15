using API.Extensions;
using API.Middleware;
using Infrastructue.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Infrastructure.Data;
using Infrastructure.Data.Identity;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities.Identity;
using System;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddIdentityServices(builder.Configuration);
        builder.Services.AddSwaggerDocumentation();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseStatusCodePagesWithReExecute("/errors/{0}");
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "Content")),
            RequestPath = "/Content"
        });

     


        app.UseHttpsRedirection();


        app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapFallbackToController("Index", "Fallback");

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<StoreContext>();
        var identityContext = services.GetRequiredService<AppIdentityDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            await context.Database.MigrateAsync();
            await identityContext.Database.MigrateAsync();
            await StoreContextSeed.SeedAsync(context);
            await AppIdentityDbContextSeed.SeedUsersAsync(userManager);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured during migration");
        }

        app.Run();
    }
}