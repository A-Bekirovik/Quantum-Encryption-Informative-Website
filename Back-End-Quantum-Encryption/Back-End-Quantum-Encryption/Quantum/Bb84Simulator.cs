using System.Globalization;
using System.Text;

namespace Back_End_Quantum_Encryption.Quantum;

/// <summary>
/// Simulates the BB84 quantum key distribution protocol (Bennett &amp; Brassard, 1984).
/// The photons are simulated in software, but the rules they follow (measurement in
/// the wrong basis gives a random result and destroys the state) are the real ones.
/// </summary>
public sealed class Bb84Simulator
{
    public Bb84Result Run(Bb84Options options)
    {
        Validate(options);
        var rng = options.Seed is int seed ? new Random(seed) : new Random();
        int n = options.QubitCount;
        var steps = new List<SimulationStep>();

        // Step 1: Alice picks a random bit and a random basis for every photon.
        int[] aliceBits = RandomBits(n, rng);
        Basis[] aliceBases = RandomBases(n, rng);
        steps.Add(new(1, "Alice generates random bits and bases",
            $"Alice flips a coin {n} times for the bits and {n} times for the bases (+ or ×). " +
            "Both lists stay secret for now."));

        // Step 2: Alice encodes each bit as a polarized photon and sends it.
        var photons = new Photon[n];
        int[] sentPolarization = new int[n];
        for (int i = 0; i < n; i++)
        {
            photons[i] = Photon.Prepare(aliceBits[i], aliceBases[i]);
            sentPolarization[i] = photons[i].PolarizationDegrees;
        }
        steps.Add(new(2, "Alice sends polarized photons",
            "Each bit becomes a photon: in + it is 0° (0) or 90° (1), in × it is 45° (0) or 135° (1). " +
            "The photons travel over the quantum channel to Bob."));

        // Step 3 (optional): Eve intercepts, measures in a random basis and sends a new photon on.
        Basis[]? eveBases = null;
        int[]? eveBits = null;
        int[]? eveResent = null;
        if (options.EavesdropperEnabled)
        {
            eveBases = RandomBases(n, rng);
            eveBits = new int[n];
            eveResent = new int[n];
            for (int i = 0; i < n; i++)
            {
                eveBits[i] = photons[i].Measure(eveBases[i], rng);
                // Eve cannot copy a photon (no-cloning theorem), so she sends Bob a fresh
                // one prepared in her own basis with the result she measured.
                photons[i] = Photon.Prepare(eveBits[i], eveBases[i]);
                eveResent[i] = photons[i].PolarizationDegrees;
            }
            steps.Add(new(3, "Eve intercepts the photons",
                "Eve does not know Alice's bases, so she guesses one for every photon. " +
                "About half the time she guesses wrong, gets a random result and sends Bob a photon " +
                "in the wrong state. That leaves traces Alice and Bob can detect."));
        }

        // Step 4: Bob measures every photon in a basis of his own choosing.
        Basis[] bobBases = RandomBases(n, rng);
        int[] bobBits = new int[n];
        for (int i = 0; i < n; i++)
            bobBits[i] = photons[i].Measure(bobBases[i], rng);
        steps.Add(new(steps.Count + 1, "Bob measures with random bases",
            "Bob does not know Alice's bases either, so he picks + or × at random for every photon. " +
            "If he picks the same basis as Alice he reads her bit; otherwise his result is a coin flip."));

        // Step 5: Sifting. They compare bases (not bits!) over a public channel and keep the matches.
        var sifted = Enumerable.Range(0, n).Where(i => aliceBases[i] == bobBases[i]).ToList();
        steps.Add(new(steps.Count + 1, "Sifting: compare bases publicly",
            $"Alice and Bob announce which basis they used, never the bits. In {sifted.Count} of {n} cases " +
            "the bases match; only those positions are kept, the rest is thrown away."));

        // Step 6: Error check. They reveal a random sample of the sifted bits to estimate the error rate.
        int sampleSize = Math.Min(sifted.Count, (int)Math.Round(sifted.Count * options.SampleFraction));
        var sample = Shuffle(sifted, rng).Take(sampleSize).Order().ToList();
        int sampleErrors = sample.Count(i => aliceBits[i] != bobBits[i]);
        double qber = sampleSize == 0 ? 0 : (double)sampleErrors / sampleSize;
        bool detected = qber > options.QberThreshold;
        steps.Add(new(steps.Count + 1, "Error check (QBER)",
            $"They publicly compare {sampleSize} of the sifted bits and find {sampleErrors} differences, " +
            $"a quantum bit error rate of {Percent(qber)}. These revealed bits are no longer secret and are discarded. " +
            $"The threshold is {Percent(options.QberThreshold)}: an intercept-resend eavesdropper causes about 25%."));

        // Step 7: Decide. The remaining sifted bits form the shared secret key.
        var sampleSet = sample.ToHashSet();
        var keyIndices = sifted.Where(i => !sampleSet.Contains(i)).ToList();
        string aliceKey = BitString(keyIndices.Select(i => aliceBits[i]));
        string bobKey = BitString(keyIndices.Select(i => bobBits[i]));
        bool accepted = !detected && keyIndices.Count > 0;
        steps.Add(new(steps.Count + 1, accepted ? "Key accepted" : "Key rejected",
            accepted
                ? $"The error rate is low enough. The remaining {keyIndices.Count} bits are the shared secret key."
                : detected
                    ? "The error rate is too high, so Alice and Bob assume someone was listening. They throw the key away and start over."
                    : "No bits are left for a key. Send more photons."));

        var keySet = keyIndices.ToHashSet();
        var qubits = new QubitTrace[n];
        for (int i = 0; i < n; i++)
        {
            bool match = aliceBases[i] == bobBases[i];
            qubits[i] = new QubitTrace(
                Index: i,
                AliceBit: aliceBits[i],
                AliceBasis: aliceBases[i],
                SentPolarization: sentPolarization[i],
                EveBasis: eveBases?[i],
                EveBit: eveBits?[i],
                EveResentPolarization: eveResent?[i],
                BobBasis: bobBases[i],
                BobBit: bobBits[i],
                BasesMatch: match,
                UsedForErrorCheck: sampleSet.Contains(i),
                IsError: match && aliceBits[i] != bobBits[i],
                InFinalKey: keySet.Contains(i));
        }

        return new Bb84Result(
            QubitCount: n,
            EavesdropperEnabled: options.EavesdropperEnabled,
            Steps: steps,
            Qubits: qubits,
            SiftedIndices: sifted,
            SampleIndices: sample,
            SampleErrors: sampleErrors,
            Qber: qber,
            QberThreshold: options.QberThreshold,
            EavesdropperDetected: detected,
            KeyAccepted: accepted,
            AliceKey: aliceKey,
            BobKey: bobKey,
            KeysMatch: aliceKey == bobKey);
    }

    private static void Validate(Bb84Options o)
    {
        if (o.QubitCount < 1 || o.QubitCount > Bb84Options.MaxQubits)
            throw new ArgumentOutOfRangeException(nameof(o.QubitCount), $"QubitCount must be between 1 and {Bb84Options.MaxQubits}.");
        if (o.SampleFraction is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(o.SampleFraction), "SampleFraction must be at least 0 and less than 1.");
        if (o.QberThreshold is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(o.QberThreshold), "QberThreshold must be between 0 and 1.");
    }

    private static int[] RandomBits(int n, Random rng) =>
        Enumerable.Range(0, n).Select(_ => rng.Next(2)).ToArray();

    private static Basis[] RandomBases(int n, Random rng) =>
        Enumerable.Range(0, n).Select(_ => rng.Next(2) == 0 ? Basis.Rectilinear : Basis.Diagonal).ToArray();

    // Fisher-Yates shuffle on a copy, used to pick a random sample.
    private static List<int> Shuffle(IEnumerable<int> source, Random rng)
    {
        var list = source.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static string BitString(IEnumerable<int> bits)
    {
        var sb = new StringBuilder();
        foreach (int b in bits) sb.Append(b == 1 ? '1' : '0');
        return sb.ToString();
    }

    private static string Percent(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
}
