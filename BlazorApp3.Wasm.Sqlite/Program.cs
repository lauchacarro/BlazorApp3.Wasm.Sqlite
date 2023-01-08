using BlazorApp3.Wasm.Sqlite;
using BlazorApp3.Wasm.Sqlite.Data;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddDbContextFactory<ClientSideDbContext>(
           options => options.UseSqlite($"Filename={DataSynchronizer.SqliteDbFilename}"));

builder.Services.AddScoped<DataSynchronizer>();

var app = builder.Build();

await app.RunAsync();

