namespace NatureBears.Data
{
    /// <summary>
    /// Stable gameplay ids for every resource. Enum NAMES are used as save-file
    /// keys and values are serialized in the inspector — never rename or reorder;
    /// only append with the next explicit value. Values must stay contiguous
    /// from 0 (code indexes cached arrays by (int)type).
    /// </summary>
    public enum ResourceType
    {
        Timberwood      = 0,
        FreshSalmon     = 1,
        RareWildflowers = 2,
        ForestMushrooms = 3,
        Embers          = 4,
        Meals           = 5,
        GoldenHoney     = 6,
        SlumberPoints   = 7
    }

    public enum ResourceCategory
    {
        RawMaterial, // Timberwood, Fresh Salmon, Rare Wildflowers, Forest Mushrooms
        Refined,     // Embers
        Meal,        // Chef Panda's Meals / Pastries
        Currency,    // Golden Honey (soft), Slumber Points (prestige)
        Special
    }

    public enum SkillBranch
    {
        Offline,
        Active,
        Production
    }

    public enum SkillEffectType
    {
        OfflineEarningsMultiplier,
        ActiveTapMultiplier,
        ProductionRateMultiplier,
        OfflineDurationCap,
        CostReduction
    }
}
