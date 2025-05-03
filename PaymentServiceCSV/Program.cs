using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using CsvHelper;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); // sorgt dafür, dass XML korrekt erkannt/verarbeitet wird

// synchrone Operationen zulassen (vorallem für XML und CSV benötigt!)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});


var app = builder.Build();

var logger = app.Logger;

var payments = new List<PaymentServiceCSV.Payment>();

// Fehlerbehandlung & HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HTTP Strict Transport Security (erhöht Sicherheit)
}

app.UseHttpsRedirection();
app.UseRouting();         
app.UseAuthorization();    

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();


/*
 * Minimal API: Payment-Service
*/

// GET-Endpunkt
app.MapGet("/api/payment", (HttpRequest request) =>
{
    if(!payments.Any())
    {
        logger.LogInformation("Keine Einträge vorhanden.");
        return Results.StatusCode(204); //No Content
    }

    // „Accept“-Header auslesen, um gewünschtes Format zu erkennen
    var accept = request.Headers["Accept"].ToString().ToLower();
    logger.LogInformation("GET /api/payment aufgerufen mit Accept: {AcceptHeader}", accept);

    if (accept.Contains("application/xml"))
    {
        return Results.Content(SerializeXml(payments), "application/xml");
    }
    else if (accept.Contains("text/csv"))
    {
        return Results.File(Encoding.UTF8.GetBytes(GenerateCsv(payments)), "text/csv", "payment.csv");
    }
    else if (accept.Contains("application/json"))
    {
        return Results.Json(payments);
    }

    logger.LogWarning("Nicht unterstütztes Accept-Format: {AcceptHeader}", accept);
    return Results.StatusCode(406); // Not Acceptable: Nicht zulässiges Format
});

// POST-Endpunkt
app.MapPost("/api/payment", async (HttpRequest request) =>
{
    var contentType = request.ContentType?.ToLower();
    logger.LogInformation("POST /api/payment aufgerufen mit Content-Type: {ContentType}", contentType);


    PaymentServiceCSV.Payment payment;

    // Request-Body lesen
    using var reader = new StreamReader(request.Body);

    var format ="";

    // Je nach Content-Type unterschiedlich deserialisieren
    if (contentType?.Contains("application/xml") == true)
    {
        var serializer = new XmlSerializer(typeof(PaymentServiceCSV.Payment));
        payment = (PaymentServiceCSV.Payment)serializer.Deserialize(reader)!;
        payment.Id = payments.Count + 1;
        format = "XML";
        logger.LogInformation("Payment-Objekt aus {Format} empfangen: {@Payment}", "XML", payment.Id);
    }
    else if (contentType?.Contains("text/csv") == true)
    {
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        payment = csv.GetRecords<PaymentServiceCSV.Payment>().First();
        payment.Id = payments.Count + 1;
        format = "CSV";
        logger.LogInformation("Payment-Objekt aus {Format} empfangen: {@Payment}", "CSV", payment.Id);
    }
    else if(contentType?.Contains("application/json") == true)
    {
        var body = await reader.ReadToEndAsync();
        payment = System.Text.Json.JsonSerializer.Deserialize<PaymentServiceCSV.Payment>(body)!;
        payment.Id = payments.Count + 1;
        format = "JSON";
        logger.LogInformation("Payment-Objekt aus {Format} empfangen: {@Payment}", "JSON", payment.Id);
    }
    else
    {
        logger.LogWarning("Nicht unterstützter Content-Type: {ContentType}", contentType);
        return Results.StatusCode(406); // Not Acceptable: Nicht zulässiges Format
    }

    logger.LogInformation("Payment-Objekt aus {@Format} empfangen: {@Payment}", format, payment.Id);

    payments.Add(payment);
    // Erfolgreiche Rückgabe (zurück als JSON)
    return Results.Json(new { message = "Received", data = payment });
});

app.Run();


/*
 * Helper-Methoden
 */

// CSV erzeugen
string GenerateCsv(List<PaymentServiceCSV.Payment> list)
{
    using var sw = new StringWriter();
    using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
    csv.WriteField("Id");
    csv.WriteHeader<PaymentServiceCSV.Payment>();
    csv.NextRecord();
    foreach (var payment in list)
    {
        csv.WriteField(payment.Id);  
        csv.WriteRecord(payment);    
        csv.NextRecord();
    }
    return sw.ToString();
}

// XML erzeugen
string SerializeXml(List<PaymentServiceCSV.Payment> list)
{
    var serializer = new XmlSerializer(typeof(List<PaymentServiceCSV.Payment>));
    using var stringWriter = new StringWriter();
    serializer.Serialize(stringWriter, list);
    return stringWriter.ToString();
}
