namespace CleanArchitecture.Application.Export.Interfaces;

public interface ITemplateService
{
    Task<Stream> GetTemplateStreamAsync(string type, string name, CancellationToken ct = default);
    void InvalidateCache(string type, string name);
}