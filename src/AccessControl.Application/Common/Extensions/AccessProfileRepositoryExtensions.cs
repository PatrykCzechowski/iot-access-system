using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;

namespace AccessControl.Application.Common.Extensions;

public static class AccessProfileRepositoryExtensions
{
    public static async Task<List<AccessProfile>> GetByIdsOrThrowAsync(
        this IAccessProfileRepository repository,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var profiles = await repository.GetByIdsTrackedAsync(ids, cancellationToken);
        if (profiles.Count != ids.Count)
        {
            var missingIds = ids.Except(profiles.Select(p => p.Id));
            throw new DomainValidationException($"Access profiles not found: {string.Join(", ", missingIds)}");
        }

        return profiles;
    }
}
