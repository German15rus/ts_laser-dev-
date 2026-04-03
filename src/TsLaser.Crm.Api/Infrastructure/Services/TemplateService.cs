namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class TemplateService(IWebHostEnvironment environment)
{
    public async Task<string> LoadAsync(string templateName, IDictionary<string, string>? replacements = null, CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(environment.ContentRootPath, "Templates", templateName);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templateName}", templatePath);
        }

        var content = await File.ReadAllTextAsync(templatePath, cancellationToken);

        if (replacements is null || replacements.Count == 0)
        {
            return content;
        }

        foreach (var replacement in replacements)
        {
            content = content.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
        }

        return content;
    }
}
