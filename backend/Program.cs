using AccountingERP.Data;
using Microsoft.EntityFrameworkCore;

SanitizeUrlsEnvironment("ASPNETCORE_URLS");
SanitizeUrlsEnvironment("DOTNET_URLS");

var builder = WebApplication.CreateBuilder(SanitizeUrlArgs(args));

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite("Data Source=accounting.db");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ 允許跨域（給前端用）
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

static string[] SanitizeUrlArgs(string[] args)
{
    if (args is null || args.Length == 0)
    {
        return args;
    }

    var normalized = (string[])args.Clone();
    for (var i = 0; i < normalized.Length; i++)
    {
        var current = normalized[i];
        if (string.Equals(current, "--urls", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(current, "-u", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 < normalized.Length)
            {
                normalized[i + 1] = NormalizeUrls(normalized[i + 1]);
            }
        }
        else if (current.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase))
        {
            var urlValue = current.Substring("--urls=".Length);
            normalized[i] = "--urls=" + NormalizeUrls(urlValue);
        }
    }

    return normalized;
}

static void SanitizeUrlsEnvironment(string variableName)
{
    var value = Environment.GetEnvironmentVariable(variableName);
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    Environment.SetEnvironmentVariable(variableName, NormalizeUrls(value));
}

static string NormalizeUrls(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return raw;
    }

    var segments = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (var i = 0; i < segments.Length; i++)
    {
        var segment = segments[i];
        if (segment.Contains("://", StringComparison.Ordinal))
        {
            continue;
        }

        var colonIndex = segment.IndexOf(':');
        if (colonIndex <= 0)
        {
            continue;
        }

        segments[i] = segment.Insert(colonIndex + 1, "//");
    }

    return string.Join(';', segments);
}
