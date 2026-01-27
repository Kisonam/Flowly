

namespace Flowly.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportAsMarkdownZipAsync(string userId);
    Task<byte[]> ExportAsJsonAsync(string userId);
    Task<byte[]> ExportAsCsvAsync(string userId);
    Task<byte[]> ExportAsPdfAsync(string userId);
}
