using System;
using System.Collections.Generic;

// -----------------------------------------------------------------------------
// Logique de construction de l'ordre joue
//
// 1. Les blocs sont toujours tries par block_order afin de construire l'ordre
//    canonique defini dans le dashboard.
// 2. Si randomize_blocks est desactive, cet ordre canonique est retourne tel quel.
// 3. Si la randomisation est active, chaque bloc is_order_locked est d'abord place
//    exactement a la position indiquee par son block_order. Un verrou est toujours
//    prioritaire, y compris lorsqu'il concerne un bloc de tutoriel.
// 4. Les blocs de tutoriel non verrouilles sont melanges entre eux puis places dans
//    les premieres positions encore libres. Les blocs experimentaux non verrouilles
//    sont melanges et occupent toutes les positions libres restantes.
// 5. L'ordre complet est controle, y compris autour des blocs verrouilles, pour
//    eviter deux config_fingerprint identiques a la suite. Une nouvelle permutation
//    est tentee jusqu'a MAX_RANDOMIZATION_ATTEMPTS fois.
// 6. Si la contrainte des fingerprints est impossible a satisfaire, une permutation
//    aleatoire normale est conservee comme fallback, sans jamais deplacer les blocs
//    verrouilles. Si les positions verrouillees du payload sont invalides, l'ordre
//    canonique est retourne.
//
// La methode est appelee une seule fois au chargement de la session. La liste
// retournee devient ensuite la source de verite de l'ordre joue.
// -----------------------------------------------------------------------------

public static class BlockOrderRandomizer
{
    public const int MAX_RANDOMIZATION_ATTEMPTS = 200;

    public static List<BlockConfig> BuildPlayedOrder(
        IReadOnlyList<BlockConfig> blocks,
        bool randomize,
        Random random)
    {
        var canonical = new List<BlockConfig>();
        if (blocks != null)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                    canonical.Add(blocks[i]);
            }
        }

        canonical.Sort((left, right) => left.block_order.CompareTo(right.block_order));
        if (!randomize || canonical.Count <= 1)
            return canonical;

        random ??= new Random();

        int blockCount = canonical.Count;
        var lockedSlots = new BlockConfig[blockCount];
        var unlockedTutorials = new List<BlockConfig>();
        var unlockedExperimentBlocks = new List<BlockConfig>();

        for (int i = 0; i < blockCount; i++)
        {
            BlockConfig block = canonical[i];
            if (!block.is_order_locked)
            {
                if (block.is_tutorial)
                    unlockedTutorials.Add(block);
                else
                    unlockedExperimentBlocks.Add(block);

                continue;
            }

            int lockedIndex = block.block_order - 1;
            if (lockedIndex < 0 ||
                lockedIndex >= blockCount ||
                lockedSlots[lockedIndex] != null)
            {
                // Le payload API garantit normalement des block_order uniques de 1 a N.
                // En cas de payload invalide, l'ordre canonique est le seul fallback sur.
                return canonical;
            }

            lockedSlots[lockedIndex] = block;
        }

        var availableSlots = new List<int>();
        for (int i = 0; i < blockCount; i++)
        {
            if (lockedSlots[i] == null)
                availableSlots.Add(i);
        }

        var tutorialSlots = new List<int>();
        var experimentSlots = new List<int>();
        for (int i = 0; i < availableSlots.Count; i++)
        {
            if (i < unlockedTutorials.Count)
                tutorialSlots.Add(availableSlots[i]);
            else
                experimentSlots.Add(availableSlots[i]);
        }

        List<BlockConfig> fallback = null;
        for (int attempt = 0; attempt < MAX_RANDOMIZATION_ATTEMPTS; attempt++)
        {
            var tutorials = new List<BlockConfig>(unlockedTutorials);
            var experimentBlocks = new List<BlockConfig>(unlockedExperimentBlocks);
            Shuffle(tutorials, random);
            Shuffle(experimentBlocks, random);

            var candidate = new BlockConfig[blockCount];
            Array.Copy(lockedSlots, candidate, blockCount);

            for (int i = 0; i < tutorialSlots.Count; i++)
                candidate[tutorialSlots[i]] = tutorials[i];

            for (int i = 0; i < experimentSlots.Count; i++)
                candidate[experimentSlots[i]] = experimentBlocks[i];

            var playedOrder = new List<BlockConfig>(candidate);
            fallback ??= playedOrder;

            if (!HasIdenticalConsecutiveBlocks(playedOrder))
                return playedOrder;
        }

        return fallback ?? canonical;
    }

    public static bool HasIdenticalConsecutiveBlocks(IReadOnlyList<BlockConfig> blocks)
    {
        if (blocks == null)
            return false;

        for (int i = 1; i < blocks.Count; i++)
        {
            if (blocks[i - 1]?.config_fingerprint == blocks[i]?.config_fingerprint)
                return true;
        }

        return false;
    }

    static void Shuffle<T>(IList<T> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int otherIndex = random.Next(i + 1);
            (values[i], values[otherIndex]) = (values[otherIndex], values[i]);
        }
    }
}
