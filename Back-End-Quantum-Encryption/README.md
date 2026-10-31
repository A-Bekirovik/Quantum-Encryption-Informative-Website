# Back-end: BB84 quantum key distribution

An ASP.NET Core (.NET 8) Web API that simulates the **BB84** protocol and uses the
resulting key to encrypt and decrypt a message. Every step is returned as JSON so the
Vite front-end can visualise it.

## What is simulated

BB84 lets Alice and Bob agree on a secret key using single photons. Physics, not maths,
guarantees that an eavesdropper (Eve) is noticed.

| Step | What happens | Code |
|------|--------------|------|
| 1 | Alice picks a random bit and a random basis (`+` or `×`) per photon | `Bb84Simulator.Run` |
| 2 | She encodes each bit as a polarized photon: `+` → 0° / 90°, `×` → 45° / 135° | `Photon.Prepare` |
| 3 | *(optional)* Eve intercepts every photon, measures it in a guessed basis and sends a new photon to Bob (intercept-resend attack) | `Bb84Simulator.Run` |
| 4 | Bob measures every photon in a random basis | `Photon.Measure` |
| 5 | **Sifting**: they publicly compare *bases* (not bits) and keep only the positions where the bases match (about 50%) | `Bb84Simulator.Run` |
| 6 | **Error check**: they reveal a random sample (default 25%) of the sifted bits and compute the **QBER** (quantum bit error rate). Those bits are discarded | `Bb84Simulator.Run` |
| 7 | QBER ≤ 11%: key accepted. Otherwise they assume Eve was there and throw the key away | `Bb84Simulator.Run` |
| 8 | The key is used as a **one-time pad**: `cipher = message XOR key`. Bob decrypts with his copy of the key | `OneTimePad` |

### Why Eve gets caught

The key rule lives in `Photon.Measure`: measuring in the **same** basis returns the bit;
measuring in the **other** basis gives a 50/50 random result **and changes the photon**.
Eve guesses the wrong basis half the time, and in those cases Bob gets a wrong bit half
the time, so on the sifted bits she causes about ½ × ½ = **25%** errors. Without Eve the
simulated channel is perfect, so the QBER is 0%.

### What is *not* real

- The photons are objects in memory, not real light. The randomness comes from
  `System.Random` (with an optional seed so runs can be repeated), not from quantum hardware.
- The channel has no noise and no photon loss, so any error comes from Eve.
- Real QKD adds *error correction* and *privacy amplification* after step 6; those are left out to keep it understandable.
- If the key is shorter than the message, it is repeated and `keyReused: true` is returned,
  which is no longer a true one-time pad. The `encrypt` endpoint sends enough photons to avoid this.

## Project structure

```
Back-End-Quantum-Encryption/
├── Quantum/
│   ├── Basis.cs           Rectilinear (+) or Diagonal (×)
│   ├── Photon.cs          A qubit: Prepare and Measure (with collapse)
│   ├── Bb84Simulator.cs   Runs the protocol step by step
│   └── Bb84Models.cs      Options and result records (the JSON shapes)
├── Crypto/OneTimePad.cs   XOR encryption with the key
├── Controllers/Bb84Controller.cs   The API endpoints
├── Models/ApiModels.cs    Request and response records
└── Program.cs             Wiring: controllers, Swagger, CORS for Vite
Back-End-Quantum-Encryption.Tests/   xUnit tests (physics rules, protocol, crypto, API)
```

## Running

```bash
cd Back-End-Quantum-Encryption
dotnet run --project Back-End-Quantum-Encryption --launch-profile http
```

The API runs on `http://localhost:5080`, with Swagger UI at `http://localhost:5080/swagger`.
CORS allows the Vite dev server on `http://localhost:5173` (change it in `appsettings.json`).

```bash
dotnet test
```

## API

All endpoints are `POST` with a JSON body. Enums are sent as text (`"Rectilinear"` / `"Diagonal"`).

### `POST /api/bb84/simulate`

```json
{ "qubitCount": 32, "eavesdropper": false, "sampleFraction": 0.25, "qberThreshold": 0.11, "seed": 1 }
```

All fields are optional. Returns:

- `steps`: `[{ number, title, description }]`, text for each protocol step
- `qubits`: one entry per photon: `aliceBit`, `aliceBasis`, `sentPolarization`, `eveBasis`, `eveBit`,
  `eveResentPolarization` (Eve fields are `null` without Eve), `bobBasis`, `bobBit`, `basesMatch`,
  `usedForErrorCheck`, `isError`, `inFinalKey`
- `siftedIndices`, `sampleIndices`, `sampleErrors`, `qber`, `qberThreshold`
- `eavesdropperDetected`, `keyAccepted`, `aliceKey`, `bobKey` (strings of `0`/`1`), `keysMatch`

### `POST /api/bb84/encrypt`

```json
{ "message": "Hello Bob", "eavesdropper": false, "seed": 7 }
```

Picks enough photons for the message (max 500 bytes), runs BB84, and if the key is accepted
encrypts the message with Alice's key and decrypts it with Bob's. Returns `simulation` (as above),
`encryption` (`plaintextBits`, `keyBitsUsed`, `ciphertextBits`, `ciphertextHex`, `keyReused`),
`bobDecrypted` and a `summary`. When Eve is detected, `encryption` and `bobDecrypted` are `null`.

### `POST /api/bb84/decrypt`

```json
{ "ciphertextHex": "B1", "key": "11110000" }
```

Returns `{ "plaintext": "A" }`.

Invalid input (for example `qubitCount` outside 1–20000, or a key with other characters than 0 and 1) returns `400 Bad Request`.
