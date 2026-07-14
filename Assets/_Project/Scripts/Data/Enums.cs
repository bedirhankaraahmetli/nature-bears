namespace NatureBears.Data
{
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
