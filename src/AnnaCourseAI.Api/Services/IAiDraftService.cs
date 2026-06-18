using AnnaCourseAI.Api.Models;

namespace AnnaCourseAI.Api.Services;

public interface IAiDraftService
{
    Task<string> GenerateDraftAsync(AiDraftRequest request, CancellationToken cancellationToken);
}
