using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

public class DevDataSeeder(
    UserManager<ApplicationUser> userManager,
    AccessControlDbContext db)
{
    // Deterministic IDs for dev seed data
    private static readonly Guid Zone1Id         = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Zone2Id         = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid ReaderZone1Hwid = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid LockZone1Hwid   = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid ReaderZone2Hwid = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var (userIds, seededUsers) = await SeedUsersAsync();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var seededDevices = await SeedDevicesAsync(cancellationToken);
        var seededCards = await SeedCardsAsync(userIds, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SeedResult(seededUsers, seededDevices, seededCards);
    }

    private async Task<(Dictionary<string, string> Ids, int NewCount)> SeedUsersAsync()
    {
        var usersToSeed = new[]
        {
            new { Email = "jan.kowalski@dev.local",      FullName = "Jan Kowalski",     Password = "Password1!" },
            new { Email = "anna.nowak@dev.local",        FullName = "Anna Nowak",        Password = "Password1!" },
            new { Email = "piotr.wisniewski@dev.local",  FullName = "Piotr Wiśniewski", Password = "Password1!" },
        };

        var ids = new Dictionary<string, string>();
        int newCount = 0;

        foreach (var u in usersToSeed)
        {
            var existing = await userManager.FindByEmailAsync(u.Email);
            if (existing is not null)
            {
                ids[u.Email] = existing.Id;
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = u.Email,
                Email = u.Email,
                FullName = u.FullName,
                Role = UserRole.Operator,
                MustChangePassword = false,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(user, u.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create dev user '{u.Email}': {errors}");
            }

            ids[u.Email] = user.Id;
            newCount++;
        }

        return (ids, newCount);
    }

    private async Task<int> SeedDevicesAsync(CancellationToken cancellationToken)
    {
        var devicesToSeed = new[]
        {
            new
            {
                HardwareId  = ReaderZone1Hwid,
                Name        = "Czytnik kart — Strefa 1",
                ZoneId      = Zone1Id,
                AdapterType = DeviceAdapterType.CardReader,
                Features    = DeviceFeatures.CardReader,
            },
            new
            {
                HardwareId  = LockZone1Hwid,
                Name        = "Zamek elektryczny — Strefa 1",
                ZoneId      = Zone1Id,
                AdapterType = DeviceAdapterType.LockPinExecutor,
                Features    = DeviceFeatures.LockControl,
            },
            new
            {
                HardwareId  = ReaderZone2Hwid,
                Name        = "Czytnik kart — Strefa 2",
                ZoneId      = Zone2Id,
                AdapterType = DeviceAdapterType.CardReader,
                Features    = DeviceFeatures.CardReader,
            },
        };

        var hwids = devicesToSeed.Select(d => d.HardwareId).ToList();
        var existingHwids = await db.Devices
            .Where(x => hwids.Contains(x.HardwareId))
            .Select(x => x.HardwareId)
            .ToHashSetAsync(cancellationToken);

        int added = 0;

        foreach (var d in devicesToSeed)
        {
            if (existingHwids.Contains(d.HardwareId))
            {
                continue;
            }

            var device = Device.Create(d.Name, d.ZoneId, d.HardwareId, d.AdapterType, d.Features);
            db.Devices.Add(device);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    private async Task<int> SeedCardsAsync(
        Dictionary<string, string> userIds,
        CancellationToken cancellationToken)
    {
        userIds.TryGetValue("jan.kowalski@dev.local", out var janId);
        userIds.TryGetValue("anna.nowak@dev.local", out var annaId);

        var cardsToSeed = new[]
        {
            new { CardUid = "AABBCCDD", ZoneId = Zone1Id, UserId = janId,  Label = (string?)"Karta Jana Kowalskiego",   IsActive = true  },
            new { CardUid = "EEFF0011", ZoneId = Zone1Id, UserId = (string?)null, Label = (string?)"Karta niezarejestrowana", IsActive = true  },
            new { CardUid = "22334455", ZoneId = Zone2Id, UserId = annaId, Label = (string?)"Karta Anny Nowak",          IsActive = true  },
            new { CardUid = "66778899", ZoneId = Zone2Id, UserId = (string?)null, Label = (string?)"Karta nieaktywna (demo)",  IsActive = false },
        };

        var uids = cardsToSeed.Select(c => c.CardUid).ToList();
        var existingUids = await db.AccessCards
            .Where(x => uids.Contains(x.CardUid))
            .Select(x => x.CardUid)
            .ToHashSetAsync(cancellationToken);

        int added = 0;

        foreach (var c in cardsToSeed)
        {
            if (existingUids.Contains(c.CardUid))
            {
                continue;
            }

            var card = AccessCard.Create(c.CardUid, c.ZoneId, c.Label);
            if (c.UserId is not null)
            {
                card.AssignUser(c.UserId);
            }

            if (!c.IsActive)
            {
                card.Deactivate();
            }

            db.AccessCards.Add(card);
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    public record SeedResult(int SeededUsers, int SeededDevices, int SeededCards);
}
