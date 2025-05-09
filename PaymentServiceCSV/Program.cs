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

// GET-by-ID-Endpunkt
app.MapGet("/api/payment/{id:int}", (HttpRequest request, int id) =>
{
    var payment = payments.FirstOrDefault(p => p.Id == id);
    if (payment == null)
    {
        logger.LogWarning("Payment mit ID {Id} nicht gefunden.", id);
        return Results.NotFound(new { message = $"Payment mit ID {id} nicht gefunden." });
    }

    var accept = request.Headers["Accept"].ToString().ToLower();
    logger.LogInformation("GET /api/payment/{id} aufgerufen mit Accept: {AcceptHeader}",id, accept);

    if (accept.Contains("application/xml"))
    {
        var serializer = new XmlSerializer(typeof(PaymentServiceCSV.Payment));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, payment);
        return Results.Content(stringWriter.ToString(), "application/xml");
    }
    else if (accept.Contains("text/csv"))
    {
        using var sw = new StringWriter();
        using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
        csv.WriteHeader<PaymentServiceCSV.Payment>();
        csv.NextRecord();
        csv.WriteRecord(payment);
        return Results.File(Encoding.UTF8.GetBytes(sw.ToString()), "text/csv", $"payment_{id}.csv");
    }
    else if (accept.Contains("application/json"))
    {
        return Results.Json(payment);
    }

    logger.LogWarning("Nicht unterstütztes Accept-Format: {AcceptHeader}", accept);
    return Results.StatusCode(406); // Not Acceptable
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


// PUT-Endpunkt
app.MapPut("/api/payment/{id:int}", async (HttpRequest request, int id) =>
{
    var existing = payments.FirstOrDefault(p => p.Id == id);
    if (existing == null)
    {
        logger.LogWarning("PUT /api/payment/{id} – Payment mit ID {id} nicht gefunden.", id, id);
        return Results.NotFound(new { message = $"Payment mit ID {id} nicht gefunden." });
    }

    var contentType = request.ContentType?.ToLower();
    logger.LogInformation("PUT /api/payment/{id} aufgerufen mit Content-Type: {ContentType}", id, contentType);

    using var reader = new StreamReader(request.Body);
    PaymentServiceCSV.Payment updatedPayment;

    if (contentType?.Contains("application/xml") == true)
    {
        var serializer = new XmlSerializer(typeof(PaymentServiceCSV.Payment));
        updatedPayment = (PaymentServiceCSV.Payment)serializer.Deserialize(reader)!;
    }
    else if (contentType?.Contains("text/csv") == true)
    {
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        updatedPayment = csv.GetRecords<PaymentServiceCSV.Payment>().First();
    }
    else if (contentType?.Contains("application/json") == true)
    {
        var body = await reader.ReadToEndAsync();
        updatedPayment = System.Text.Json.JsonSerializer.Deserialize<PaymentServiceCSV.Payment>(body)!;
    }
    else
    {
        logger.LogWarning("PUT /api/payment/{id} – Nicht unterstützter Content-Type: {ContentType}", contentType);
        return Results.StatusCode(406); // Not Acceptable
    }

    // Bestehendes Objekt aktualisieren
    updatedPayment.Id = id; // ID beibehalten
    var index = payments.FindIndex(p => p.Id == id);
    payments[index] = updatedPayment;

    logger.LogInformation("Payment mit ID {Id} erfolgreich aktualisiert.", id);
    return Results.Json(new { message = "Updated", data = updatedPayment });
});


// DELETE-Endpunkt
app.MapDelete("/api/payment/{id:int}", (int id) =>
{
    var payment = payments.FirstOrDefault(p => p.Id == id);
    if (payment == null)
    {
        logger.LogWarning("DELETE /api/payment/{id} – Payment mit ID {d} nicht gefunden.", id, id);
        return Results.NotFound(new { message = $"Payment mit ID {id} nicht gefunden." });
    }

    payments.Remove(payment);
    logger.LogInformation("Payment mit ID {Id} erfolgreich gelöscht.", id);
    return Results.Json(new { message = $"Payment mit ID {id} gelöscht." });
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
