using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using ODataProductService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var modelBuilder = new ODataConventionModelBuilder();
modelBuilder.EntitySet<Product>("Products");


// OData API
builder.Services.AddControllers().AddOData(opt =>
    opt.Select().Filter().OrderBy().Expand().Count().SetMaxTop(null)
        .AddRouteComponents("odata", modelBuilder.GetEdmModel()));

var app = builder.Build();

var logger = app.Logger;

// Fehlerbehandlung & HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HTTP Strict Transport Security (erhöht Sicherheit)
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Razor Pages bleibt
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

