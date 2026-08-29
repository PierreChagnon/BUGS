using System;
using UnityEngine;

// -----------------------------
// Seeds et dérivations RNG partagées par tout le projet.
//
// Une seule implémentation FNV-1a 64-bit, toujours masquée sur 31 bits
// positifs (System.Random(int) peut lever sur int.MinValue sous Mono) et
// jamais 0 (0 = "pas de seed" dans les configs).
// -----------------------------

public static class SeedUtility
{
    const ulong FnvOffset = 1469598103934665603UL;
    const ulong FnvPrime = 1099511628211UL;

    // Seed 31 bits positive : exacte en JSON/JS (pas de perte de précision).
    public static long GenerateSeed()
    {
        unchecked
        {
            int ticksHash = DateTime.UtcNow.Ticks.GetHashCode();
            int guidHash = Guid.NewGuid().GetHashCode();
            int seed = (ticksHash ^ guidHash) & 0x7FFFFFFF;
            return seed == 0 ? 1 : seed;
        }
    }

    // Dérive une seed d'un couple (seed, scope texte) : stable, rapide, cross-platform.
    public static int DeriveScopedSeed(long seed, string scope)
    {
        unchecked
        {
            ulong h = FnvOffset;
            MixLong(ref h, seed);

            if (!string.IsNullOrEmpty(scope))
            {
                for (int i = 0; i < scope.Length; i++)
                {
                    h ^= (byte)scope[i];
                    h *= FnvPrime;
                }
            }

            return ToPositiveNonZero(h);
        }
    }

    // Dérive la seed d'un trial (ou d'un salt de bloc) depuis la seed de bloc.
    public static long DeriveTrialSeed(long blockSeed, int trialIndex)
    {
        unchecked
        {
            ulong h = FnvOffset;
            MixLong(ref h, blockSeed);

            uint index = (uint)Mathf.Max(0, trialIndex);
            for (int i = 0; i < 4; i++)
            {
                h ^= (byte)(index & 0xFF);
                h *= FnvPrime;
                index >>= 8;
            }

            return ToPositiveNonZero(h);
        }
    }

    static void MixLong(ref ulong hash, long value)
    {
        unchecked
        {
            ulong raw = (ulong)value;
            for (int i = 0; i < 8; i++)
            {
                hash ^= (byte)(raw & 0xFF);
                hash *= FnvPrime;
                raw >>= 8;
            }
        }
    }

    static int ToPositiveNonZero(ulong hash)
    {
        int seed = (int)(hash & 0x7FFFFFFF);
        return seed == 0 ? 1 : seed;
    }
}
