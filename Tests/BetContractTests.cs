using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Mundialito.Models;
using static Tests.BetsTestHarness;

namespace Tests;

/// <summary>
/// The client must be able to send back what the server sent it. It could not: BetViewModel
/// carried the game only as a nested object while UpdateBetModel required a flat GameId, so
/// echoing a create response bound GameId to 0, GetGame(0) returned null and the validator
/// dereferenced it - reported to the player as a 400 "Object reference not set to an
/// instance of an object" (#170). SaveBetModel carries no ids at all, which is what makes
/// that unrepresentable; these tests keep it that way.
/// </summary>
[TestFixture]
public class BetContractTests
{
    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Test]
    public void ABetResponseRoundTripsThroughTheWriteModel()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: true);
        var bet = MakeBet(7, alice, game);

        var json = JsonSerializer.Serialize(new BetViewModel(bet, Now));
        var save = JsonSerializer.Deserialize<SaveBetModel>(json)!;

        Assert.Multiple(() =>
        {
            Assert.That(Validate(save), Is.Empty,
                "the server's own response does not satisfy the model it must be sent back as");
            Assert.That(save.HomeScore, Is.EqualTo(bet.HomeScore));
            Assert.That(save.AwayScore, Is.EqualTo(bet.AwayScore));
            Assert.That(save.CardsMark, Is.EqualTo(bet.CardsMark));
            Assert.That(save.CornersMark, Is.EqualTo(bet.CornersMark));
        });
    }

    [Test]
    public void AnOmittedScoreIsRejectedRatherThanRecordedAsZero()
    {
        // [Required] on a non-nullable int is a no-op: this body used to bind HomeScore to
        // 0, pass [Range(0,10)] and silently record a 0-0 bet.
        var save = JsonSerializer.Deserialize<SaveBetModel>(
            "{\"AwayScore\":1,\"CardsMark\":\"1\",\"CornersMark\":\"2\"}")!;

        Assert.That(Validate(save).SelectMany(r => r.MemberNames), Does.Contain(nameof(SaveBetModel.HomeScore)));
    }

    [Test]
    public void TheWriteModelCarriesNoIdentifiers()
    {
        // A bet's game is fixed at creation and its owner is the caller. Any id reappearing
        // here is a field the client would get to choose - see #171 for what that allowed.
        var identifiers = typeof(SaveBetModel).GetProperties()
            .Select(p => p.Name)
            .Where(name => name.EndsWith("Id", StringComparison.Ordinal))
            .ToList();

        Assert.That(identifiers, Is.Empty);
    }
}
