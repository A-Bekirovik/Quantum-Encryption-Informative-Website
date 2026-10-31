using Back_End_Quantum_Encryption.Quantum;

namespace Back_End_Quantum_Encryption.Tests;

public class PhotonTests
{
    [Theory]
    [InlineData(0, Basis.Rectilinear, 0)]
    [InlineData(1, Basis.Rectilinear, 90)]
    [InlineData(0, Basis.Diagonal, 45)]
    [InlineData(1, Basis.Diagonal, 135)]
    public void Prepare_EncodesBitAsPolarization(int bit, Basis basis, int degrees)
    {
        Assert.Equal(degrees, Photon.Prepare(bit, basis).PolarizationDegrees);
    }

    [Fact]
    public void Measure_InSameBasis_AlwaysReturnsEncodedBit()
    {
        var rng = new Random(1);
        for (int i = 0; i < 100; i++)
        {
            int bit = i % 2;
            Assert.Equal(bit, Photon.Prepare(bit, Basis.Diagonal).Measure(Basis.Diagonal, rng));
        }
    }

    [Fact]
    public void Measure_InOtherBasis_IsRandomAboutHalfTheTime()
    {
        var rng = new Random(2);
        int ones = Enumerable.Range(0, 2000)
            .Count(_ => Photon.Prepare(0, Basis.Rectilinear).Measure(Basis.Diagonal, rng) == 1);
        Assert.InRange(ones, 900, 1100);
    }

    [Fact]
    public void Measure_InOtherBasis_CollapsesThePhoton()
    {
        var rng = new Random(3);
        var photon = Photon.Prepare(1, Basis.Rectilinear);
        int result = photon.Measure(Basis.Diagonal, rng);

        // After the measurement the photon is in the diagonal basis, so a second
        // diagonal measurement must give the same answer.
        Assert.Equal(result, photon.Measure(Basis.Diagonal, rng));
        Assert.Contains(photon.PolarizationDegrees, new[] { 45, 135 });
    }

    [Fact]
    public void Prepare_RejectsInvalidBit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Photon.Prepare(2, Basis.Rectilinear));
    }
}
