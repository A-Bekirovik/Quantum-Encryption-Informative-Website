using Back_End_Quantum_Encryption.Quantum;

namespace Back_End_Quantum_Encryption.Tests;

public class Bb84SimulatorTests
{
    private readonly Bb84Simulator _simulator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void WithoutEve_KeysMatchAndAreAccepted(int seed)
    {
        var result = _simulator.Run(new Bb84Options(QubitCount: 200, Seed: seed));

        Assert.Equal(0, result.SampleErrors);
        Assert.Equal(0, result.Qber);
        Assert.False(result.EavesdropperDetected);
        Assert.True(result.KeyAccepted);
        Assert.True(result.KeysMatch);
        Assert.NotEmpty(result.AliceKey);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void WithEve_ErrorRateIsAbout25PercentAndSheIsDetected(int seed)
    {
        var result = _simulator.Run(new Bb84Options(QubitCount: 4000, EavesdropperEnabled: true, Seed: seed));

        Assert.InRange(result.Qber, 0.18, 0.32);
        Assert.True(result.EavesdropperDetected);
        Assert.False(result.KeyAccepted);
    }

    [Fact]
    public void Sifting_KeepsOnlyPositionsWithMatchingBases()
    {
        var result = _simulator.Run(new Bb84Options(QubitCount: 1000, Seed: 4));

        Assert.All(result.SiftedIndices, i => Assert.True(result.Qubits[i].BasesMatch));
        Assert.Equal(result.Qubits.Count(q => q.BasesMatch), result.SiftedIndices.Count);
        // Bases match about half the time.
        Assert.InRange(result.SiftedIndices.Count, 430, 570);
    }

    [Fact]
    public void FinalKey_ExcludesTheRevealedSampleBits()
    {
        var result = _simulator.Run(new Bb84Options(QubitCount: 400, SampleFraction: 0.25, Seed: 5));

        Assert.All(result.SampleIndices, i => Assert.False(result.Qubits[i].InFinalKey));
        Assert.Equal(result.SiftedIndices.Count - result.SampleIndices.Count, result.AliceKey.Length);
        Assert.Equal((int)Math.Round(result.SiftedIndices.Count * 0.25), result.SampleIndices.Count);

        string keyFromTrace = string.Concat(result.Qubits.Where(q => q.InFinalKey).Select(q => q.AliceBit));
        Assert.Equal(result.AliceKey, keyFromTrace);
    }

    [Fact]
    public void Trace_ContainsEveOnlyWhenEnabled()
    {
        var quiet = _simulator.Run(new Bb84Options(QubitCount: 10, Seed: 6));
        var noisy = _simulator.Run(new Bb84Options(QubitCount: 10, EavesdropperEnabled: true, Seed: 6));

        Assert.All(quiet.Qubits, q => Assert.Null(q.EveBasis));
        Assert.All(noisy.Qubits, q => Assert.NotNull(q.EveBasis));
        Assert.Contains(noisy.Steps, s => s.Title.Contains("Eve"));
        Assert.DoesNotContain(quiet.Steps, s => s.Title.Contains("Eve"));
    }

    [Fact]
    public void SameSeed_GivesSameResult()
    {
        var a = _simulator.Run(new Bb84Options(QubitCount: 100, EavesdropperEnabled: true, Seed: 42));
        var b = _simulator.Run(new Bb84Options(QubitCount: 100, EavesdropperEnabled: true, Seed: 42));

        Assert.Equal(a.AliceKey, b.AliceKey);
        Assert.Equal(a.BobKey, b.BobKey);
        Assert.Equal(a.Qubits, b.Qubits);
    }

    [Theory]
    [InlineData(0, 0.25, 0.11)]
    [InlineData(Bb84Options.MaxQubits + 1, 0.25, 0.11)]
    [InlineData(10, 1.0, 0.11)]
    [InlineData(10, -0.1, 0.11)]
    [InlineData(10, 0.25, 1.5)]
    public void InvalidOptions_Throw(int qubits, double sample, double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _simulator.Run(new Bb84Options(qubits, SampleFraction: sample, QberThreshold: threshold)));
    }
}
