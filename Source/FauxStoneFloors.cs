using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenerateFauxStoneFloors;

[StaticConstructorOnStartup]
public static class FauxStoneFloors
{
    private const int BuildCost = 6;
    private const string TerrainBlueprintGraphicPath = "Things/Special/TerrainBlueprint";

    [NotNull] [ItemNotNull] private static readonly IReadOnlyList<string> Prefixes = new List<string>
    {
        #region Alpha Biomes Prefixes

        "AB_",
        "GU_"

        #endregion
    };

    [NotNull] [ItemNotNull] private static readonly IReadOnlyList<string> IgnoredDefNames = new List<string>
    {
        #region Alpha Biomes DefNames

        "GU_AncientMetals"

        #endregion
    };

    [NotNull] private static readonly Dictionary<Type, HashSet<ushort>> GeneratedHashes = new();

    [UsedImplicitly]
    static FauxStoneFloors()
    {
        AddImpliedFauxFloors();
    }

    private static void AddImpliedFauxFloors()
    {
        foreach (TerrainDef terrainDef in GenerateFauxStoneFloors())
        {
            DefGenerator.AddImpliedDef(terrainDef);
            if (terrainDef.BuildableByPlayer)
            {
                DefGenerator.AddImpliedDef(terrainDef.blueprintDef);
                DefGenerator.AddImpliedDef(terrainDef.frameDef);
            }
        }

        DesignationCategoryDefOf.Floors!.ResolveReferences();
        WealthWatcher.ResetStaticData();
    }

    private static void GenerateNewHash<T>([NotNull] T def) where T : Def
    {
#if DEBUG
        Log.Message($"Generating hash for {def.defName}");
#endif
        var alreadyGeneratedHashes = GeneratedHashes.TryGetValue(typeof(T));
        if (alreadyGeneratedHashes == null)
        {
            alreadyGeneratedHashes = new HashSet<ushort>();
            GeneratedHashes.SetOrAdd(typeof(T), alreadyGeneratedHashes);
        }

        var existingHashes = DefDatabase<T>.AllDefs!.Select(d =>
                (d ?? throw new NullReferenceException("found null def while generating hash")).shortHash)
            .ToList();
        var generatedHash = (ushort)(GenText.StableStringHash(def.defName) % ushort.MaxValue);
        var iterations = 0;

        while (generatedHash == 0 || existingHashes.Contains(generatedHash) ||
               alreadyGeneratedHashes.Contains(generatedHash))
        {
            generatedHash++;
            iterations++;
            if (iterations > 5000)
                Log.Warning(
                    "[FauxRockFloors] Short hashes are saturated. There are probably too many Defs, or the author of this mod screwed something up. Either way, go complain somewhere.");
        }
#if DEBUG
        if (DefDatabase<T>.GetByShortHash(generatedHash) is { } existingDef)
            Log.Error(
                $"Hash {generatedHash} already exists on {existingDef.defName} but was also generated for {def.defName}");
#endif
        alreadyGeneratedHashes.Add(generatedHash);
        def.shortHash = generatedHash;
    }

    // Thanks Alpha Biomes, very cool
    [CanBeNull]
    private static ThingDef GetBlocksForRock([NotNull] ThingDef rockDef)
    {
        if (rockDef.defName == null)
            throw new ArgumentNullException(nameof(rockDef), "Found rock def with null defName");
#if DEBUG
        Log.Message($"Trying to find blocks for {rockDef.ToStringSafe()}");
#endif
        if (rockDef.butcherProducts?.Select(thingCount => thingCount!.thingDef)
                .FirstOrDefault(butcherProduct =>
                    butcherProduct?.thingCategories?.Contains(ThingCategoryDefOf.StoneBlocks) ?? false) is { } block)
            return block;

        switch (rockDef.defName)
        {
            case "AB_Obsidianstone":
                return DefDatabase<ThingDef>.GetNamed("AB_BlocksObsidian");
            case "AB_SlimeStone":
                return DefDatabase<ThingDef>.GetNamed("AB_SlimeMeal");
            case "GU_AncientMetals":
                return null;
            case "BiomesIslands_CoralRock":
                return DefDatabase<ThingDef>.GetNamed("BiomesIslands_BlocksCoral");
        }

        if (Prefixes.FirstOrDefault(prefix => rockDef.defName!.StartsWith(prefix)) is { } rockDefNamePrefix)
        {
            var rockDefNameUnprefixed = rockDef.defName!.Substring(rockDefNamePrefix.Length);
            return DefDatabase<ThingDef>.GetNamed(rockDefNamePrefix + "Blocks" + rockDefNameUnprefixed, false);
        }

        return DefDatabase<ThingDef>.GetNamed("Blocks" + rockDef.defName, false);
    }

    [NotNull]
    [ItemNotNull]
    private static IEnumerable<TerrainDef> GenerateFauxStoneFloors(ModContentPack pack = null)
    {
        var rocks = DefDatabase<ThingDef>.AllDefs!.Where(def =>
            def?.building is not null && def.building.isNaturalRock && !def.building.isResourceRock);

        DesignatorDropdownGroupDef roughDesignationDropdown = new()
        {
            defName = "FloorRoughStoneFaux",
            label = "faux rough floor",
            generated = true
        };
        DesignatorDropdownGroupDef roughHewnDesignatorDropdown = new()
        {
            defName = "FloorRoughHewnStoneFaux",
            label = "faux rough-hewn floor",
            generated = true
        };

        List<TerrainDef> result = new();
        foreach (ThingDef rock in rocks)
        {
#if DEBUG
            Log.Message($"Generating floors for {rock.ToStringSafe()}");
#endif
            if (IgnoredDefNames.Contains(rock.defName))
            {
#if DEBUG
                Log.Message("def lacks a good material to make a floor of and is explicitly ignored.");
#endif
                continue;
            }

            try
            {
                FauxRoughStone fauxRoughDef = new(rock, pack);
                FauxRoughHewnStone fauxRoughHewnDef = new(rock, pack);
                FauxSmoothStone fauxSmoothDef = new(rock, pack);

                fauxRoughDef.designatorDropdown = roughDesignationDropdown;
                fauxRoughHewnDef.designatorDropdown = roughHewnDesignatorDropdown;

                fauxRoughDef.smoothedTerrain = fauxSmoothDef;
                fauxRoughHewnDef.smoothedTerrain = fauxSmoothDef;

                result.Add(fauxRoughDef);
                result.Add(fauxRoughHewnDef);
                result.Add(fauxSmoothDef);
            }
            catch (ArgumentNullException nullException)
            {
                Log.Error(nullException.Message);
            }
            catch (ArgumentOutOfRangeException argumentException)
            {
                Log.Error(argumentException.Message);
            }
        }

        return result;
    }

    private static ThingDef GenerateBlueprint([NotNull] TerrainDef terrainDef)
    {
        if (terrainDef == null) throw new ArgumentNullException(nameof(terrainDef));

        ThingDef blueprintDef = new()
        {
            category = ThingCategory.Ethereal,
            altitudeLayer = AltitudeLayer.Blueprint,
            useHitPoints = false,
            selectable = true,
            seeThroughFog = true,
            thingClass = typeof(Blueprint_Build),
            defName = ThingDefGenerator_Buildings.BlueprintDefNamePrefix + terrainDef.defName,
            label = terrainDef.label + "BlueprintLabelExtra".Translate(),
            graphicData = new GraphicData
            {
                shaderType = ShaderTypeDefOf.MetaOverlay,
                texPath = TerrainBlueprintGraphicPath,
                graphicClass = typeof(Graphic_Single)
            },
            constructionSkillPrerequisite = terrainDef.constructionSkillPrerequisite,
            artisticSkillPrerequisite = terrainDef.artisticSkillPrerequisite,
            clearBuildingArea = false,
            modContentPack = terrainDef.modContentPack,
            entityDefToBuild = terrainDef,
            drawerType = DrawerType.MapMeshAndRealTime,
            comps = new List<CompProperties>
            {
                new CompProperties_Forbiddable()
            }
        };

        GenerateNewHash(blueprintDef);

        return blueprintDef;
    }

    [NotNull]
    private static ThingDef GenerateFrame([NotNull] TerrainDef terrainDef)
    {
        if (terrainDef == null) throw new ArgumentNullException(nameof(terrainDef));
        terrainDef.frameDef = new ThingDef
        {
            isFrameInt = true,
            thingClass = typeof(Frame),
            altitudeLayer = AltitudeLayer.Building,
            building = new BuildingProperties
            {
                artificialForMeditationPurposes = false,
                isEdifice = false
            },
            scatterableOnMapGen = false,
            leaveResourcesWhenKilled = true,
            defName = ThingDefGenerator_Buildings.BuildingFrameDefNamePrefix + terrainDef.defName,
            label = terrainDef.label + "FrameLabelExtra".Translate(),
            useHitPoints = false,
            fillPercent = 0.0f,
            description = "Terrain building in progress.",
            passability = Traversability.Standable,
            selectable = true,
            constructEffect = terrainDef.constructEffect,
            constructionSkillPrerequisite = terrainDef.constructionSkillPrerequisite,
            artisticSkillPrerequisite = terrainDef.artisticSkillPrerequisite,
            clearBuildingArea = false,
            modContentPack = terrainDef.modContentPack,
            category = ThingCategory.Ethereal,
            entityDefToBuild = terrainDef,
            comps = new List<CompProperties>
            {
                new CompProperties_Forbiddable()
            }
        };

        GenerateNewHash(terrainDef.frameDef);

        return terrainDef.frameDef;
    }

    public abstract class FloorBase : TerrainDef
    {
        protected FloorBase()
        {
            layerable = true;
            affordances = new List<TerrainAffordanceDef>
            {
                TerrainAffordanceDefOf.Light,
                TerrainAffordanceDefOf.Medium,
                TerrainAffordanceDefOf.Heavy
            };
            tags = new List<string> { "Floor" };
            designationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Floors");

            fertility = 0f;
            constructEffect = EffecterDefOf.ConstructDirt;
            terrainAffordanceNeeded = TerrainAffordanceDefOf.Heavy;
        }
    }

    public class FauxRoughStone : FloorBase
    {
        internal FauxRoughStone([NotNull] ThingDef rockDef, ModContentPack pack = null)
        {
            if (rockDef is null)
                throw new ArgumentNullException(nameof(rockDef),
                    "[FauxStoneFloors] Tried to create Faux Rough Rock from null Thing.");
            if (rockDef.building is null || !rockDef.building.isNaturalRock || rockDef.building.isResourceRock)
                throw new ArgumentOutOfRangeException(
                    $"[FauxStoneFloors] Tried to create Faux Rough Rock from Thing ({rockDef.ToStringSafe()}) that isn't Plain Rock");
            ThingDef blocks = GetBlocksForRock(rockDef);
            if (blocks is null)
                throw new ArgumentOutOfRangeException(
                    $"[FauxStoneFloors] Couldn't find stone blocks for ThingDef ({rockDef.ToStringSafe()})");

            description =
                "Made to mimic ugly natural rock. Since these floors are not made for their beauty, they can be made faster but require slightly more material than regular stone tiles. Can be smoothed.";
            texturePath = "Terrain/Surfaces/RoughStone";
            edgeType = TerrainEdgeType.FadeRough;
            affordances ??= new List<TerrainAffordanceDef>();
            affordances.Add(TerrainAffordanceDefOf.SmoothableStone);
            pathCost = 2;
            filthAcceptanceMask = FilthSourceFlags.Terrain | FilthSourceFlags.Unnatural;
            researchPrerequisites = new List<ResearchProjectDef>
            {
                DefDatabase<ResearchProjectDef>.GetNamed("Stonecutting")
            };
            constructionSkillPrerequisite = 3;

            modContentPack = pack ?? rockDef.modContentPack;
            defName = rockDef.defName + "_RoughFaux";
            color = rockDef.graphicData?.color ?? Color.gray;
            label = "faux rough " + rockDef.label;

            costList = new List<ThingDefCountClass>
            {
                new()
                {
                    count = BuildCost,
                    thingDef = blocks
                }
            };

            blueprintDef = GenerateBlueprint(this);
            frameDef = GenerateFrame(this);

            GenerateNewHash<TerrainDef>(this);

            StatUtility.SetStatValueInList(ref statBases, StatDefOf.WorkToBuild, 500f);
            StatUtility.SetStatValueInList(ref statBases, StatDefOf.Beauty, -1f);
        }
    }

    public class FauxRoughHewnStone : FloorBase
    {
        internal FauxRoughHewnStone([NotNull] ThingDef rockDef, ModContentPack pack = null)
        {
            if (rockDef is null)
                throw new ArgumentNullException(nameof(rockDef),
                    "[FauxStoneFloors] Tried to create Faux Rough-Hewn Rock from null Thing.");
            if (rockDef.building is null || !rockDef.building.isNaturalRock || rockDef.building.isResourceRock)
                throw new ArgumentOutOfRangeException(
                    $"[FauxStoneFloors] Tried to create Faux Rough-Hewn Rock from Thing ({rockDef.ToStringSafe()}) that isn't Rock");
            ThingDef blocks = GetBlocksForRock(rockDef);
            if (blocks is null)
                throw new ArgumentOutOfRangeException(
                    $"[FauxStoneFloors] Couldn't find stone blocks for ThingDef ({rockDef.ToStringSafe()})");

            description =
                "Made to mimic ugly natural rough-hewn rock. Since these floors are not made for their beauty, they can be made faster but require slightly more material than regular stone tiles. Can be smoothed.";
            texturePath = "Terrain/Surfaces/RoughHewnRock";
            edgeType = TerrainEdgeType.FadeRough;
            affordances ??= new List<TerrainAffordanceDef>();
            affordances.Add(TerrainAffordanceDefOf.SmoothableStone);
            pathCost = 1;
            filthAcceptanceMask = FilthSourceFlags.Any;
            researchPrerequisites = new List<ResearchProjectDef>
            {
                DefDatabase<ResearchProjectDef>.GetNamed("Stonecutting")
            };
            constructionSkillPrerequisite = 3;

            modContentPack = pack ?? rockDef.modContentPack;
            defName = rockDef.defName + "_RoughHewnFaux";
            color = rockDef.graphicData?.color ?? Color.gray;
            label = "faux rough-hewn " + rockDef.label;

            costList = new List<ThingDefCountClass>
            {
                new()
                {
                    count = BuildCost,
                    thingDef = blocks
                }
            };

            blueprintDef = GenerateBlueprint(this);
            frameDef = GenerateFrame(this);

            GenerateNewHash<TerrainDef>(this);

            StatUtility.SetStatValueInList(ref statBases, StatDefOf.WorkToBuild, 500f);
            StatUtility.SetStatValueInList(ref statBases, StatDefOf.Beauty, -1f);
        }
    }

    public class FauxSmoothStone : FloorBase
    {
        public FauxSmoothStone(ThingDef rockDef, ModContentPack pack = null)
        {
            if (rockDef is null)
                throw new ArgumentNullException(nameof(rockDef),
                    "[FauxStoneFloors] Tried to create Faux Smooth Rock from null Thing.");
            if (rockDef.building is null || !rockDef.building.isNaturalRock || rockDef.building.isResourceRock)
                throw new ArgumentOutOfRangeException(
                    $"[FauxStoneFloors] Tried to create Faux Smooth Rock from Thing ({rockDef.ToStringSafe()}) that isn't Rock");
            ThingDef blocks = GetBlocksForRock(rockDef);
            if (blocks is null)
                throw new ArgumentOutOfRangeException(
                    $"[FauxStoneFloors] Couldn't find stone blocks for ThingDef ({rockDef.ToStringSafe()})");

            description =
                "Originally made to mimic ugly natural rock, this floor has been polished to a shiny, smooth surface.";
            texturePath = "Terrain/Surfaces/SmoothStone";
            edgeType = TerrainEdgeType.FadeRough;
            pathCost = 1;
            filthAcceptanceMask = FilthSourceFlags.Any;
            researchPrerequisites = new List<ResearchProjectDef>
            {
                DefDatabase<ResearchProjectDef>.GetNamed("Stonecutting")
            };
            constructionSkillPrerequisite = 3;

            designationCategory = null;

            modContentPack = pack ?? rockDef.modContentPack;
            defName = rockDef.defName + "_SmoothFaux";
            color = rockDef.graphicData?.color ?? Color.gray;
            label = "faux smooth " + rockDef.label;

            costList = new List<ThingDefCountClass>
            {
                new()
                {
                    count = BuildCost,
                    thingDef = blocks
                }
            };

            GenerateNewHash<TerrainDef>(this);

            StatUtility.SetStatValueInList(ref statBases, StatDefOf.Beauty, 2f);
            StatUtility.SetStatValueInList(ref statBases, StatDefOf.MarketValue, 8f);
        }
    }
}