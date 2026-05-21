using System.Collections.Generic;

public static class TutorialSessionFactory
{
    public static SessionConfig EnsureTutorialBlock(SessionConfig source)
    {
        var config = source != null
            ? source.DeepClone()
            : new SessionConfig
            {
                blocks = new List<BlockConfig>()
            };

        if (config.blocks == null)
            config.blocks = new List<BlockConfig>();

        return config;
    }

    public static BlockConfig CreateTutorialBlock()
    {
        return new BlockConfig
        {
            block_template_id = "tutorial-block",
            block_order = 0,
            trial_count = 3,
            is_tutorial = true,
            distal_scene = CreateTutorialDistalScene(),
            valley_a = CreateTutorialMap(seed: 1001),
            valley_b = CreateTutorialMap(seed: 1002, minGreenRatio: 0.65f, maxGreenRatio: 0.8f),
            distal_advice_visible_probability = 1f,
            distal_advice_reliable_probability = 1f
        };
    }

    private static DistalSceneConfig CreateTutorialDistalScene()
    {
        return new DistalSceneConfig
        {
            min_total_bugs = 18,
            max_total_bugs = 26,
            min_green_ratio = 0.45f,
            max_green_ratio = 0.75f,
            gap_min = 0.2f,
            gap_max = 0.3f
        };
    }

    private static MapGenConfig CreateTutorialMap(
        long seed,
        float minGreenRatio = 0.45f,
        float maxGreenRatio = 0.75f)
    {
        return new MapGenConfig
        {
            trap_count = 2,
            min_distance = 3,
            max_distance = 4,
            min_total_bugs = 18,
            max_total_bugs = 26,
            min_green_ratio = minGreenRatio,
            max_green_ratio = maxGreenRatio,
            gap_min = 0.2f,
            gap_max = 0.3f,
            path_visible = 1f,
            suboptimal_path_probability = 0f,
            detour_probability = 0f,
            motor_advice_visible_probability = 1f,
            motor_advice_reliable_probability = 1f,
            suboptimal_trap_probability = 0f,
            min_suboptimal_traps = 0,
            max_suboptimal_traps = 0,
            fog_probability = 0f,
            seed = seed
        };
    }
}
