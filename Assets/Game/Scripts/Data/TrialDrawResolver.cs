using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Tirages probabilistes de niveau trial et de scène (seed du trial, via le
// RNG scopé de FlowController ou de LevelRegistry). Les spawners/controllers
// créent leur RNG et consomment ici la résolution des variables
// expérimentales (règle projet : résolution dans Data/).
// -----------------------------

public static class TrialDrawResolver
{
    public struct TrialForcedDraw
    {
        public float proximal_probability;
        public bool proximal_is_forced;
        public float proximal_optimal_probability;
        public bool proximal_was_optimal;
        public float motor_probability;
        public bool motor_is_forced;
    }

    public struct ProximalAdviceDraw
    {
        public bool reliable;
        public bool route_suboptimal;
        public bool with_detour;
        public bool visible;
    }

    public struct MotorAdviceDraw
    {
        public MotorKeySet active_set;
        public MotorKeySet displayed_set;
        public bool visible;
        public bool reliable;
    }

    // Forçages proximal et moteur du trial (scope RollTrialForcedChoicesForCurrentTrial).
    public static TrialForcedDraw DrawTrialForcedChoices(BlockConfig block, System.Random rng)
    {
        var draw = new TrialForcedDraw
        {
            proximal_probability = Mathf.Clamp01(block.proximal_forced_probability),
            motor_probability = Mathf.Clamp01(block.motor_forced_probability)
        };

        draw.proximal_is_forced = rng.NextDouble() < draw.proximal_probability;
        if (draw.proximal_is_forced)
        {
            draw.proximal_optimal_probability = Mathf.Clamp01(block.proximal_forced_optimal_probability);
            draw.proximal_was_optimal = rng.NextDouble() < draw.proximal_optimal_probability;
        }

        draw.motor_is_forced = rng.NextDouble() < draw.motor_probability;
        return draw;
    }

    // Les quatre variables du conseil proximal, sur un flux RNG dédié :
    // la résolution ne dépend plus du nombre d'appels RNG consommés par la
    // construction des chemins. Toujours quatre tirages, dans cet ordre.
    public static ProximalAdviceDraw DrawProximalAdvice(MapGenConfig map, System.Random rng)
    {
        return new ProximalAdviceDraw
        {
            reliable = rng.NextDouble() < Mathf.Clamp01(map.proximal_advice_reliable_probability),
            route_suboptimal = rng.NextDouble() < Mathf.Clamp01(map.suboptimal_path_probability),
            with_detour = rng.NextDouble() < Mathf.Clamp01(map.detour_probability),
            visible = rng.NextDouble() < Mathf.Clamp01(map.path_visible_probability)
        };
    }

    // Set actif, visibilité et fiabilité du conseil moteur (scope MotorAdviceController).
    // Sans advisor, le conseil n'est jamais visible mais le tirage est consommé
    // (flux stable quel que soit l'advisor).
    public static MotorAdviceDraw DrawMotorAdvice(
        bool hasAdvisor,
        bool choiceIsForced,
        MotorKeySet forcedSet,
        float visibleProbability,
        float reliableProbability,
        System.Random rng)
    {
        var draw = new MotorAdviceDraw
        {
            active_set = choiceIsForced ? forcedSet : (MotorKeySet)rng.Next(0, 3),
            displayed_set = MotorKeySet.None
        };

        draw.visible = rng.NextDouble() < (hasAdvisor ? visibleProbability : 0f);
        if (!draw.visible)
            return draw;

        if (choiceIsForced)
        {
            draw.reliable = true;
            draw.displayed_set = draw.active_set;
            return draw;
        }

        draw.reliable = rng.NextDouble() < reliableProbability;
        draw.displayed_set = draw.reliable ? draw.active_set : DrawOtherSet(rng, draw.active_set);
        return draw;
    }

    // Brouillard actif ce trial (scope FogSpawner). En proximal forced, le
    // brouillard est toujours actif et aucun tirage n'est consommé.
    public static bool DrawFogActive(float fogProbability, bool proximalChoiceIsForced, System.Random rng)
    {
        if (proximalChoiceIsForced)
            return true;

        return rng.NextDouble() < fogProbability;
    }

    // Nombre de pièges à poser sur le chemin suboptimal (scope TrapSpawner) :
    // 0 si la probabilité ne se réalise pas. Probabilité puis nombre, dans cet ordre.
    public static int DrawSuboptimalTrapCount(float probability, int minTraps, int maxTraps, System.Random rng)
    {
        if (probability <= 0f)
            return 0;

        if (rng.NextDouble() >= probability)
            return 0;

        int min = Mathf.Max(0, minTraps);
        int max = Mathf.Max(min, maxTraps);
        return rng.Next(min, max + 1);
    }

    static MotorKeySet DrawOtherSet(System.Random rng, MotorKeySet current)
    {
        var options = new List<MotorKeySet> { MotorKeySet.ZQSD, MotorKeySet.TFGH, MotorKeySet.IJKL };
        options.Remove(current);
        return options[rng.Next(0, options.Count)];
    }
}
