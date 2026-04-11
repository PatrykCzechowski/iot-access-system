# Testy Jednostkowe — Przewodnik

## Spis treści

1. [Struktura projektów testowych](#1-struktura-projektów-testowych)
2. [Tworzenie projektów testowych](#2-tworzenie-projektów-testowych)
3. [Konwencje nazewnicze](#3-konwencje-nazewnicze)
4. [Testy encji domenowych (bez mockowania)](#4-testy-encji-domenowych-bez-mockowania)
5. [Testy walidatorów FluentValidation](#5-testy-walidatorów-fluentvalidation)
6. [Testy handlerów CQRS (z NSubstitute)](#6-testy-handlerów-cqrs-z-nsubstitute)
7. [Testy query handlerów](#7-testy-query-handlerów)
8. [Testy metod pomocniczych (MqttTopics)](#8-testy-metod-pomocniczych-mqtttopics)
9. [Uruchamianie testów](#9-uruchamianie-testów)
10. [Wskazówki i dobre praktyki](#10-wskazówki-i-dobre-praktyki)

---

## 1. Struktura projektów testowych

```
tests/
  AccessControl.Domain.Tests/          ← testy encji i value objects (ZERO zależności)
    AccessControl.Domain.Tests.csproj
    Entities/
      AccessCardTests.cs
      AccessZoneTests.cs
      DeviceTests.cs
      ...
    ValueObjects/
      DiscoveredDeviceInfoTests.cs

  AccessControl.Application.Tests/     ← testy handlerów, walidatorów, utilsów
    AccessControl.Application.Tests.csproj
    Cards/
      CreateAccessCardCommandValidatorTests.cs
      CreateAccessCardCommandHandlerTests.cs
      GetAccessCardsQueryHandlerTests.cs
    Common/
      MqttTopicsTests.cs
    ...
```

**Zasada**: struktura folderów w testach odzwierciedla strukturę kodu źródłowego.

---

## 2. Tworzenie projektów testowych

### Krok 1 — Domain Tests

```bash
cd c:\Users\patcz\Desktop\iot

# Utwórz projekt testowy
dotnet new xunit -o tests/AccessControl.Domain.Tests -f net10.0

# Dodaj referencję do testowanego projektu
dotnet add tests/AccessControl.Domain.Tests reference src/AccessControl.Domain

# Dodaj FluentAssertions
dotnet add tests/AccessControl.Domain.Tests package FluentAssertions

# Dodaj do solution
dotnet sln add tests/AccessControl.Domain.Tests --solution-folder tests
```

### Krok 2 — Application Tests

```bash
dotnet new xunit -o tests/AccessControl.Application.Tests -f net10.0

dotnet add tests/AccessControl.Application.Tests reference src/AccessControl.Application
dotnet add tests/AccessControl.Application.Tests reference src/AccessControl.Domain

# Pakiety
dotnet add tests/AccessControl.Application.Tests package FluentAssertions
dotnet add tests/AccessControl.Application.Tests package NSubstitute
dotnet add tests/AccessControl.Application.Tests package FluentValidation.TestHelper

dotnet sln add tests/AccessControl.Application.Tests --solution-folder tests
```

### Krok 3 — Weryfikacja

```bash
dotnet build tests/
```

---

## 3. Konwencje nazewnicze

### Format nazwy testu

```
NazwaMetody_Scenariusz_OczekiwanyRezultat
```

Przykłady:
- `Create_WithValidUid_ReturnsCardWithNormalizedUid`
- `Create_WithEmptyUid_ThrowsArgumentException`
- `UpdateConfiguration_WithInvalidKey_ThrowsDomainValidationException`

### Wzorzec AAA (Arrange → Act → Assert)

Każdy test dzielimy na 3 sekcje:

```csharp
[Fact]
public void Create_WithValidUid_ReturnsCardWithNormalizedUid()
{
    // Arrange — przygotuj dane wejściowe
    var uid = "ab:cd:ef:12";

    // Act — wywołaj testowaną metodę
    var card = AccessCard.Create(uid);

    // Assert — sprawdź wynik
    card.CardUid.Should().Be("AB:CD:EF:12");
}
```

---

## 4. Testy encji domenowych (bez mockowania)

Testy domenowe to najlepszy punkt startowy — nie wymagają żadnego mockowania, testują czystą logikę biznesową.

### Pełny przykład: `AccessCardTests.cs`

```csharp
using AccessControl.Domain.Entities;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class AccessCardTests
{
    // ── Create ──────────────────────────────────────────────

    [Fact]
    public void Create_WithValidUid_ReturnsCardWithCorrectProperties()
    {
        // Arrange
        var uid = "ab:cd:ef:12";
        var label = "Karta wejściowa";

        // Act
        var card = AccessCard.Create(uid, label);

        // Assert
        card.Id.Should().NotBeEmpty();
        card.CardUid.Should().Be("AB:CD:EF:12");   // znormalizowany do uppercase
        card.Label.Should().Be("Karta wejściowa");
        card.IsActive.Should().BeTrue();            // domyślnie aktywna
        card.CardholderId.Should().BeNull();        // brak przypisanego właściciela
        card.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUid_ThrowsArgumentException(string? uid)
    {
        // Act
        var act = () => AccessCard.Create(uid!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithLabel_TrimsWhitespace()
    {
        // Act
        var card = AccessCard.Create("AABB", "  Main door  ");

        // Assert
        card.Label.Should().Be("Main door");
    }

    [Fact]
    public void Create_WithNullLabel_SetsLabelToNull()
    {
        // Act
        var card = AccessCard.Create("AABB");

        // Assert
        card.Label.Should().BeNull();
    }

    // ── NormalizeUid ────────────────────────────────────────

    [Theory]
    [InlineData("ab:cd:ef", "AB:CD:EF")]
    [InlineData("  AB:CD  ", "AB:CD")]
    [InlineData("aAbBcC", "AABBCC")]
    public void NormalizeUid_VariousInputs_ReturnsUppercaseTrimmed(string input, string expected)
    {
        // Act
        var result = AccessCard.NormalizeUid(input);

        // Assert
        result.Should().Be(expected);
    }

    // ── AssignCardholder ────────────────────────────────────

    [Fact]
    public void AssignCardholder_WithValidId_SetsCardholderId()
    {
        // Arrange
        var card = AccessCard.Create("AABB");
        var cardholderId = Guid.NewGuid();

        // Act
        card.AssignCardholder(cardholderId);

        // Assert
        card.CardholderId.Should().Be(cardholderId);
    }

    [Fact]
    public void AssignCardholder_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var card = AccessCard.Create("AABB");

        // Act
        var act = () => card.AssignCardholder(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    // ── UnassignCardholder ──────────────────────────────────

    [Fact]
    public void UnassignCardholder_WhenAssigned_ClearsCardholderId()
    {
        // Arrange
        var card = AccessCard.Create("AABB");
        card.AssignCardholder(Guid.NewGuid());

        // Act
        card.UnassignCardholder();

        // Assert
        card.CardholderId.Should().BeNull();
    }

    // ── Update ──────────────────────────────────────────────

    [Fact]
    public void Update_WithNewValues_UpdatesLabelAndIsActive()
    {
        // Arrange
        var card = AccessCard.Create("AABB", "Old label");

        // Act
        card.Update("New label", false);

        // Assert
        card.Label.Should().Be("New label");
        card.IsActive.Should().BeFalse();
        card.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    // ── Deactivate ──────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveToFalse()
    {
        // Arrange
        var card = AccessCard.Create("AABB");
        card.IsActive.Should().BeTrue(); // warunek wstępny

        // Act
        card.Deactivate();

        // Assert
        card.IsActive.Should().BeFalse();
    }
}
```

### Pełny przykład: `AccessZoneTests.cs`

```csharp
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class AccessZoneTests
{
    [Fact]
    public void Create_WithValidName_ReturnsZone()
    {
        // Act
        var zone = AccessZone.Create("Strefa A", "Główne wejście");

        // Assert
        zone.Name.Should().Be("Strefa A");
        zone.Description.Should().Be("Główne wejście");
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ThrowsDomainValidationException()
    {
        // Arrange
        var longName = new string('x', 101);

        // Act
        var act = () => AccessZone.Create(longName);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*cannot exceed 100*");
    }

    [Fact]
    public void Create_WithDescriptionExceeding500Chars_ThrowsDomainValidationException()
    {
        // Arrange
        var longDesc = new string('x', 501);

        // Act
        var act = () => AccessZone.Create("OK", longDesc);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*cannot exceed 500*");
    }

    [Fact]
    public void UpdateName_WithValidName_UpdatesNameAndTimestamp()
    {
        // Arrange
        var zone = AccessZone.Create("Old");
        var beforeUpdate = zone.UpdatedAt;

        // Act
        zone.UpdateName("New");

        // Assert
        zone.Name.Should().Be("New");
        zone.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void UpdateDescription_WithNull_ClearsDescription()
    {
        // Arrange
        var zone = AccessZone.Create("Zone", "Some description");

        // Act
        zone.UpdateDescription(null);

        // Assert
        zone.Description.Should().BeNull();
    }
}
```

### Pełny przykład: `DeviceTests.cs` (bardziej złożona encja)

```csharp
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using AccessControl.Domain.Exceptions;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class DeviceTests
{
    // Helper — tworzy prawidłowe urządzenie do użycia w testach
    private static Device CreateValidDevice() => Device.Create(
        name: "Card Reader 1",
        zoneId: Guid.NewGuid(),
        hardwareId: Guid.NewGuid(),
        adapterType: DeviceAdapterType.CardReader,
        features: DeviceFeatures.CardReader);

    // ── Create ──────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInput_ReturnsDevice()
    {
        // Act
        var device = CreateValidDevice();

        // Assert
        device.Id.Should().NotBeEmpty();
        device.Name.Should().Be("Card Reader 1");
        device.Status.Should().Be(DeviceStatus.Offline); // domyślnie offline
        device.LastHeartbeat.Should().BeNull();
        device.Configuration.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyZoneId_ThrowsDomainValidationException()
    {
        // Act
        var act = () => Device.Create("Reader", Guid.Empty, Guid.NewGuid(),
            DeviceAdapterType.CardReader, DeviceFeatures.CardReader);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*ZoneId*");
    }

    // ── RecordHeartbeat ─────────────────────────────────────

    [Fact]
    public void RecordHeartbeat_WhenOffline_SetsStatusToOnline()
    {
        // Arrange
        var device = CreateValidDevice();
        device.Status.Should().Be(DeviceStatus.Offline);

        // Act
        device.RecordHeartbeat();

        // Assert
        device.Status.Should().Be(DeviceStatus.Online);
        device.LastHeartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    // ── UpdateConfiguration ─────────────────────────────────

    [Fact]
    public void UpdateConfiguration_WithValidSettings_UpdatesValues()
    {
        // Arrange
        var device = CreateValidDevice();

        // Act
        device.UpdateConfiguration(new Dictionary<string, string>
        {
            ["buzzerEnabled"] = "true",
            ["ledBrightness"] = "128"
        });

        // Assert
        device.Configuration.Should().ContainKey("buzzerEnabled").WhoseValue.Should().Be("true");
        device.Configuration.Should().ContainKey("ledBrightness").WhoseValue.Should().Be("128");
    }

    [Fact]
    public void UpdateConfiguration_WithInvalidKey_ThrowsDomainValidationException()
    {
        // Arrange
        var device = CreateValidDevice();

        // Act
        var act = () => device.UpdateConfiguration(new Dictionary<string, string>
        {
            ["nonExistentKey"] = "value"
        });

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Invalid configuration keys*nonExistentKey*");
    }

    [Fact]
    public void UpdateConfiguration_WithInvalidValue_ThrowsDomainValidationException()
    {
        // Arrange
        var device = CreateValidDevice();

        // Act — ledBrightness musi być 0-255, "999" jest poza zakresem
        var act = () => device.UpdateConfiguration(new Dictionary<string, string>
        {
            ["ledBrightness"] = "999"
        });

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Invalid configuration values*");
    }
}
```

---

## 5. Testy walidatorów FluentValidation

FluentValidation dostarcza specjalny pakiet `FluentValidation.TestHelper` z metodami `.TestValidate()` i `.ShouldHaveValidationErrorFor()`.

### Przykład: `CreateAccessCardCommandValidatorTests.cs`

```csharp
using AccessControl.Application.Cards.Commands;
using FluentValidation.TestHelper;

namespace AccessControl.Application.Tests.Cards;

public class CreateAccessCardCommandValidatorTests
{
    // Tworzymy JEDNĄ instancję walidatora — jest bezstanowy
    private readonly CreateAccessCardCommandValidator _validator = new();

    // ── CardUid ─────────────────────────────────────────────

    [Fact]
    public void Validate_WithEmptyCardUid_HasError()
    {
        // Arrange
        var command = new CreateAccessCardCommand("", null, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardUid);
    }

    [Fact]
    public void Validate_WithCardUidExceeding20Chars_HasError()
    {
        // Arrange
        var command = new CreateAccessCardCommand(new string('A', 21), null, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardUid);
    }

    [Fact]
    public void Validate_WithValidCardUid_HasNoError()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AABBCCDD", null, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CardUid);
    }

    // ── CardholderId ────────────────────────────────────────

    [Fact]
    public void Validate_WithEmptyCardholderId_HasError()
    {
        // Arrange — Guid.Empty jest niedozwolony jeśli jest podany
        var command = new CreateAccessCardCommand("AABB", Guid.Empty, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CardholderId);
    }

    [Fact]
    public void Validate_WithNullCardholderId_HasNoError()
    {
        // Arrange — null jest OK (karta bez właściciela)
        var command = new CreateAccessCardCommand("AABB", null, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CardholderId);
    }

    // ── Label ───────────────────────────────────────────────

    [Fact]
    public void Validate_WithLabelExceeding200Chars_HasError()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AABB", null, new string('x', 201));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Label);
    }

    // ── Pełna walidacja ─────────────────────────────────────

    [Fact]
    public void Validate_WithAllValidFields_HasNoErrors()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AABBCCDD", Guid.NewGuid(), "Main entrance");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

---

## 6. Testy handlerów CQRS (z NSubstitute)

Handlery mają zależności (repozytoria, serwisy) — mockujemy je za pomocą **NSubstitute**.

### Kluczowe koncepcje NSubstitute

```csharp
// Tworzenie mocka
var repo = Substitute.For<IAccessCardRepository>();

// Konfiguracja zwracanej wartości
repo.ExistsByCardUidAsync("AABB", Arg.Any<CancellationToken>())
    .Returns(false);

// Weryfikacja, że metoda została wywołana
await repo.Received(1).AddAsync(Arg.Any<AccessCard>(), Arg.Any<CancellationToken>());

// Weryfikacja, że metoda NIE została wywołana
await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
```

### Pełny przykład: `CreateAccessCardCommandHandlerTests.cs`

```csharp
using AccessControl.Application.Cards.Commands;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace AccessControl.Application.Tests.Cards;

public class CreateAccessCardCommandHandlerTests
{
    // Zależności (mocki)
    private readonly IAccessCardRepository _cardRepository = Substitute.For<IAccessCardRepository>();
    private readonly ICardholderRepository _cardholderRepository = Substitute.For<ICardholderRepository>();

    // System under test (SUT) — testowany handler
    private readonly CreateAccessCardCommandHandler _sut;

    public CreateAccessCardCommandHandlerTests()
    {
        // Wstrzykujemy mocki do handlera
        _sut = new CreateAccessCardCommandHandler(_cardRepository, _cardholderRepository);
    }

    // ── Happy path ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WithNewCardUid_CreatesCardAndReturnsId()
    {
        // Arrange
        var command = new CreateAccessCardCommand("aabb", null, "Test card");

        _cardRepository.ExistsByCardUidAsync("AABB", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty(); // zwrócony Guid

        // Sprawdź, że karta została dodana do repozytorium
        await _cardRepository.Received(1)
            .AddAsync(Arg.Is<AccessCard>(c =>
                c.CardUid == "AABB" &&          // UID znormalizowany
                c.Label == "Test card" &&
                c.IsActive == true),
                Arg.Any<CancellationToken>());

        // Sprawdź, że zmiany zostały zapisane
        await _cardRepository.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Duplikat UID ────────────────────────────────────────

    [Fact]
    public async Task Handle_WithDuplicateUid_ThrowsBusinessRuleException()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AABB", null, null);

        _cardRepository.ExistsByCardUidAsync("AABB", Arg.Any<CancellationToken>())
            .Returns(true);  // ← karta już istnieje!

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already registered*");

        // Upewnij się, że NIE próbowano zapisać
        await _cardRepository.DidNotReceive()
            .AddAsync(Arg.Any<AccessCard>(), Arg.Any<CancellationToken>());
    }

    // ── Z istniejącym cardholderem ──────────────────────────

    [Fact]
    public async Task Handle_WithValidCardholderId_AssignsCardholder()
    {
        // Arrange
        var cardholderId = Guid.NewGuid();
        var command = new CreateAccessCardCommand("AABB", cardholderId, null);

        _cardRepository.ExistsByCardUidAsync("AABB", Arg.Any<CancellationToken>())
            .Returns(false);

        // Symuluj istniejącego cardholdera
        var cardholder = Cardholder.Create("Jan", "Kowalski");
        _cardholderRepository.GetByIdAsync(cardholderId, Arg.Any<CancellationToken>())
            .Returns(cardholder);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        await _cardRepository.Received(1)
            .AddAsync(Arg.Is<AccessCard>(c => c.CardholderId == cardholder.Id),
                Arg.Any<CancellationToken>());
    }

    // ── Z nieistniejącym cardholderem ───────────────────────

    [Fact]
    public async Task Handle_WithNonExistentCardholderId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var cardholderId = Guid.NewGuid();
        var command = new CreateAccessCardCommand("AABB", cardholderId, null);

        _cardRepository.ExistsByCardUidAsync("AABB", Arg.Any<CancellationToken>())
            .Returns(false);

        _cardholderRepository.GetByIdAsync(cardholderId, Arg.Any<CancellationToken>())
            .Returns((Cardholder?)null);  // ← nie znaleziono!

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{cardholderId}*");
    }
}
```

---

## 7. Testy query handlerów

Query handlery są prostsze — sprawdzamy mapowanie encji na DTO.

### Przykład: `GetAccessCardsQueryHandlerTests.cs`

```csharp
using AccessControl.Application.Cards.DTOs;
using AccessControl.Application.Cards.Queries;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AccessControl.Application.Tests.Cards;

public class GetAccessCardsQueryHandlerTests
{
    private readonly IAccessCardRepository _repository = Substitute.For<IAccessCardRepository>();
    private readonly GetAccessCardsQueryHandler _sut;

    public GetAccessCardsQueryHandlerTests()
    {
        _sut = new GetAccessCardsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithCards_ReturnsMappedDtos()
    {
        // Arrange
        var card = AccessCard.Create("AABB", "Test");

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { card });

        // Act
        var result = await _sut.Handle(new GetAccessCardsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);

        var dto = result.First();
        dto.CardUid.Should().Be("AABB");
        dto.Label.Should().Be("Test");
        dto.IsActive.Should().BeTrue();
        dto.CardholderId.Should().BeNull();
        dto.CardholderName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoCards_ReturnsEmptyCollection()
    {
        // Arrange
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AccessCard>());

        // Act
        var result = await _sut.Handle(new GetAccessCardsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
```

---

## 8. Testy metod pomocniczych (MqttTopics)

Statyczne utility classes testuje się bez mockowania — czyste wejście → czyste wyjście.

### Przykład: `MqttTopicsTests.cs`

```csharp
using AccessControl.Application.Common;
using FluentAssertions;

namespace AccessControl.Application.Tests.Common;

public class MqttTopicsTests
{
    // ── TryExtractHardwareId ────────────────────────────────

    [Fact]
    public void TryExtractHardwareId_WithValidTopic_ReturnsTrueAndExtractsGuid()
    {
        // Arrange
        var hwid = Guid.NewGuid();
        var topic = $"accesscontrol/{hwid}/card/scanned";

        // Act
        var result = MqttTopics.TryExtractHardwareId(topic, out var extracted);

        // Assert
        result.Should().BeTrue();
        extracted.Should().Be(hwid);
    }

    [Theory]
    [InlineData("invalid/topic")]
    [InlineData("accesscontrol/not-a-guid/card")]
    [InlineData("")]
    public void TryExtractHardwareId_WithInvalidTopic_ReturnsFalse(string topic)
    {
        // Act
        var result = MqttTopics.TryExtractHardwareId(topic, out var extracted);

        // Assert
        result.Should().BeFalse();
        extracted.Should().Be(Guid.Empty);
    }

    // ── Topic builders ──────────────────────────────────────

    [Fact]
    public void CardEnroll_ReturnsCorrectTopic()
    {
        // Arrange
        var hwid = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // Act
        var topic = MqttTopics.CardEnroll(hwid);

        // Assert
        topic.Should().Be("accesscontrol/11111111-2222-3333-4444-555555555555/card/enroll");
    }

    [Fact]
    public void LockCommand_ReturnsCorrectTopic()
    {
        var hwid = Guid.NewGuid();

        var topic = MqttTopics.LockCommand(hwid);

        topic.Should().StartWith("accesscontrol/")
             .And.EndWith("/lock/command");
    }

    // ── Subscribe patterns ──────────────────────────────────

    [Fact]
    public void SubscribePatterns_ContainsExpectedTopics()
    {
        MqttTopics.SubscribePatterns.Should().Contain(p => p.Contains("heartbeat"));
        MqttTopics.SubscribePatterns.Should().Contain(p => p.Contains("card/scanned"));
        MqttTopics.SubscribePatterns.Should().Contain(p => p.Contains("announce"));
    }
}
```

---

## 9. Uruchamianie testów

### Z terminala

```bash
# Wszystkie testy
dotnet test tests/

# Tylko testy domenowe
dotnet test tests/AccessControl.Domain.Tests/

# Tylko testy aplikacji
dotnet test tests/AccessControl.Application.Tests/

# Z większą ilością szczegółów
dotnet test tests/ --verbosity normal

# Filtrowanie po nazwie testu
dotnet test tests/ --filter "AccessCardTests"

# Filtrowanie po pełnej nazwie
dotnet test tests/ --filter "FullyQualifiedName~Create_WithValidUid"
```

### Z VS Code

Jeśli masz zainstalowane **C# Dev Kit**, testy pojawią się w panelu **Test Explorer** (ikona kolby po lewej stronie). Możesz tam:
- Uruchamiać pojedyncze testy klikając ▶
- Uruchamiać wszystkie testy w klasie
- Debugować testy z breakpointami

---

## 10. Wskazówki i dobre praktyki

### Co testować, a czego nie

| ✅ Testuj | ❌ Nie testuj |
|-----------|---------------|
| Logika biznesowa w encjach | Gettery/settery bez logiki |
| Factory methods (`Create()`) | Frameworki (EF Core, MediatR) |
| Walidatory FluentValidation | Prywatne pola/metody |
| Handlery CQRS (logika w `Handle()`) | Konfigurację DI |
| Metody statyczne z logiką | Proste mapowania bez warunków |
| Rzucanie wyjątków przy błędnych danych | Kod infrastrukturalny (baza danych) |

### Atrybuty xUnit

```csharp
[Fact]           // Test bez parametrów — jeden scenariusz
[Theory]         // Test z parametrami — wiele scenariuszy
[InlineData()]   // Dane inline dla [Theory]
```

### Przydatne asercje FluentAssertions

```csharp
// Wartości
result.Should().Be(42);
result.Should().NotBeNull();
result.Should().BeGreaterThan(0);

// Stringi
name.Should().Be("Jan");
name.Should().Contain("an");
name.Should().StartWith("J");
name.Should().BeNullOrEmpty();

// Kolekcje
list.Should().HaveCount(3);
list.Should().BeEmpty();
list.Should().Contain(item);
list.Should().AllSatisfy(x => x.IsActive.Should().BeTrue());

// Daty
date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
date.Should().BeAfter(startTime);

// Wyjątki (synchroniczne)
var act = () => SomeMethod();
act.Should().Throw<BusinessRuleException>()
   .WithMessage("*already*");

// Wyjątki (asynchroniczne)
var act = () => SomeAsyncMethod();
await act.Should().ThrowAsync<KeyNotFoundException>();

// Guid
id.Should().NotBeEmpty();
id.Should().Be(expectedId);
```

### Kolejność pisania testów

Zacznij od **najłatwiejszych** i buduj pewność siebie:

1. **Encje domenowe** — zero zależności, czysta logika → `AccessCardTests`
2. **Walidatory** — też proste, `TestValidate()` jest intuicyjny
3. **Statyczne utility** — `MqttTopicsTests`
4. **Query handlery** — jeden mock (repozytorium), proste mapowanie
5. **Command handlery** — kilka mocków, bardziej złożone scenariusze

### Szablon nowego testu

Gdy tworzysz nowy plik testowy, użyj tego szablonu:

```csharp
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities; // ← dopasuj namespace

public class NazwaKlasyTests
{
    [Fact]
    public void MetodaTestowana_Scenariusz_OczekiwanyWynik()
    {
        // Arrange

        // Act

        // Assert
    }
}
```

Dla handlerów z zależnościami:

```csharp
using FluentAssertions;
using NSubstitute;

namespace AccessControl.Application.Tests.NazwaFeaturu;

public class NazwaHandleraTests
{
    private readonly IRepozytorium _repo = Substitute.For<IRepozytorium>();
    private readonly NazwaHandlera _sut;

    public NazwaHandleraTests()
    {
        _sut = new NazwaHandlera(_repo);
    }

    [Fact]
    public async Task Handle_Scenariusz_OczekiwanyWynik()
    {
        // Arrange
        _repo.MetodaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(wartość);

        // Act
        var result = await _sut.Handle(new Komenda(...), CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }
}
```
