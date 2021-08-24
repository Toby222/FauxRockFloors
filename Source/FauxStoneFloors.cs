using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GenerateFauxStoneFloors
{
    [StaticConstructorOnStartup]
    public static class FauxStoneFloors
    {
        private const int BUILD_COST = 6;
        private const string TerrainBlueprintGraphicPath = "Things/Special/TerrainBlueprint";

        static FauxStoneFloors()
        {
            AddImpliedFauxFloors();
        }

        public static void AddImpliedFauxFloors()
        {
            foreach (TerrainDef terrainDef in FauxStoneFloors.GenerateFauxStoneFloors())
            {
                DefGenerator.AddImpliedDef(terrainDef);
                if (terrainDef.BuildableByPlayer)
                {
                    DefGenerator.AddImpliedDef(terrainDef.blueprintDef);
                    DefGenerator.AddImpliedDef(terrainDef.frameDef);
                }
            }
            DefDatabase<DesignationCategoryDef>.GetNamed("Floors").ResolveReferences();
            WealthWatcher.ResetStaticData();
        }

        private static readonly Dictionary<System.Type, HashSet<ushort>> generatedHashes = new();

        private static void GenerateNewHash<T>(T def) where T : Def
        {
#if DEBUG
            Log.Message($"Generating hash for {def.defName}");
#endif
            HashSet<ushort> alreadyGeneratedHashes = generatedHashes.TryGetValue(typeof(T));
            if (alreadyGeneratedHashes == null)
            {
                alreadyGeneratedHashes = new HashSet<ushort>();
                generatedHashes.SetOrAdd(typeof(T), alreadyGeneratedHashes);
            }
            IEnumerable<ushort> existingHashes = DefDatabase<T>.AllDefs.Select(d => d.shortHash);
            ushort generatedHash = (ushort)(GenText.StableStringHash(def.defName) % ushort.MaxValue);
            int iterations = 0;

            while (generatedHash == 0 || existingHashes.Contains(generatedHash) || alreadyGeneratedHashes.Contains(generatedHash))
            {
                generatedHash++;
                iterations++;
                if (iterations > 5000)
                {
                    Log.Warning("[FauxRockFloors] Short hashes are saturated. There are probably too many Defs, or the author of this mod screwed something up. Either way, go complain somewhere.");
                }
            }
#if DEBUG
            if (DefDatabase<T>.GetByShortHash(generatedHash) is T existingDef && existingDef != null)
            {
                Log.Error($"Hash {generatedHash} already exists on {existingDef.defName} but was also generated for {def.defName}");
            }
#endif
            alreadyGeneratedHashes.Add(generatedHash);
            def.shortHash = generatedHash;
        }

        internal static IEnumerable<TerrainDef> GenerateFauxStoneFloors(ModContentPack pack = null)
        {
            IEnumerable<ThingDef> rocks = DefDatabase<ThingDef>.AllDefs.Where(def => !(def.building is null) && def.building.isNaturalRock && !def.building.isResourceRock);

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
            return result;
        }

        private static ThingDef GenerateBlueprint(TerrainDef terrDef)
        {
            ThingDef blueprintDef = new()
            {
                category = ThingCategory.Ethereal,
                altitudeLayer = AltitudeLayer.Blueprint,
                useHitPoints = false,
                selectable = true,
                seeThroughFog = true,
                thingClass = typeof(Blueprint_Build),
                defName = ThingDefGenerator_Buildings.BlueprintDefNamePrefix + terrDef.defName,
                label = terrDef.label + "BlueprintLabelExtra".Translate(),
                graphicData = new GraphicData
                {
                    shaderType = ShaderTypeDefOf.MetaOverlay,
                    texPath = TerrainBlueprintGraphicPath,
                    graphicClass = typeof(Graphic_Single)
                },
                constructionSkillPrerequisite = terrDef.constructionSkillPrerequisite,
                artisticSkillPrerequisite = terrDef.artisticSkillPrerequisite,
                clearBuildingArea = false,
                modContentPack = terrDef.modContentPack,
                entityDefToBuild = terrDef,
                drawerType = DrawerType.MapMeshAndRealTime
            };
            blueprintDef.comps.Add(new CompProperties_Forbiddable());

            GenerateNewHash(blueprintDef);

            return blueprintDef;
        }

        private static ThingDef GenerateFrame(TerrainDef terrDef)
        {
            ThingDef frameDef = new()
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
                defName = ThingDefGenerator_Buildings.BuildingFrameDefNamePrefix + terrDef.defName,
                label = terrDef.label + "FrameLabelExtra".Translate(),
                useHitPoints = false,
                fillPercent = 0.0f,
                description = "Terrain building in progress.",
                passability = Traversability.Standable,
                selectable = true,
                constructEffect = terrDef.constructEffect,
                constructionSkillPrerequisite = terrDef.constructionSkillPrerequisite,
                artisticSkillPrerequisite = terrDef.artisticSkillPrerequisite,
                clearBuildingArea = false,
                modContentPack = terrDef.modContentPack,
                category = ThingCategory.Ethereal,
                entityDefToBuild = terrDef
            };
            frameDef.comps.Add(new CompProperties_Forbiddable());
            terrDef.frameDef = frameDef;
            if (!frameDef.IsFrame)
            {
                Log.Error("Framedef is not frame: " + frameDef);
            }

            GenerateNewHash(frameDef);

            return frameDef;
        }

        internal abstract class FloorBase : TerrainDef
        {
            public FloorBase()
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

        internal class FauxRoughStone : FloorBase
        {
            public FauxRoughStone(ThingDef rockDef, ModContentPack pack = null) : base()
            {
                if (rockDef is null)
                {
                    Log.Error("[FauxStoneFloors] Tried to create Faux Rough Rock from null Thing.");
                    return;
                }
                if (rockDef.building is null || !rockDef.building.isNaturalRock || rockDef.building.isResourceRock)
                {
                    Log.Error($"[FauxStoneFloors] Tried to create Faux Rough Rock from Thing ({rockDef.ToStringSafe()}) that isn't Rock");
                    return;
                }
                ThingDef blocks = DefDatabase<ThingDef>.GetNamed("Blocks" + rockDef.defName);
                if (blocks is null)
                {
                    Log.Error($"[FauxStoneFloors] Couldn't find stone blocks for ThingDef ({rockDef.ToStringSafe()})");
                    return;
                }

                description = "Made to mimic ugly natural rock. Since these floors are not made for their beauty, they can be made faster but require slightly more material than regular stone tiles. Can be smoothed.";
                texturePath = "Terrain/Surfaces/RoughStone";
                edgeType = TerrainEdgeType.FadeRough;
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
                color = rockDef.graphicData.color;
                label = "faux rough " + rockDef.label;

                costList = new List<ThingDefCountClass>
                {
                    new ThingDefCountClass
                    {
                        count = BUILD_COST,
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

        internal class FauxRoughHewnStone : FloorBase
        {
            public FauxRoughHewnStone(ThingDef rockDef, ModContentPack pack = null) : base()
            {
                if (rockDef is null)
                {
                    Log.Error("Tried to create Faux Rough-Hewn Rock from null Thing.");
                    return;
                }
                if (rockDef.building is null || !rockDef.building.isNaturalRock || rockDef.building.isResourceRock)
                {
                    Log.Warning($"Tried to create Faux Rough-Hewn Rock from Thing ({rockDef.ToStringSafe()}) that isn't Rock");
                }
                ThingDef blocks = DefDatabase<ThingDef>.GetNamed("Blocks" + rockDef.defName);
                if (blocks is null)
                {
                    Log.Error($"[FauxStoneFloors] Couldn't find stone blocks for ThingDef ({rockDef.ToStringSafe()})");
                    return;
                }

                description = "Made to mimic ugly natural rough-hewn rock. Since these floors are not made for their beauty, they can be made faster but require slightly more material than regular stone tiles. Can be smoothed.";
                texturePath = "Terrain/Surfaces/RoughHewnRock";
                edgeType = TerrainEdgeType.FadeRough;
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
                color = rockDef.graphicData.color;
                label = "faux rough-hewn " + rockDef.label;

                costList = new List<ThingDefCountClass>
                {
                    new ThingDefCountClass
                    {
                        count = BUILD_COST,
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

        internal class FauxSmoothStone : FloorBase
        {
            public FauxSmoothStone(ThingDef rockDef, ModContentPack pack = null) : base()
            {
                if (rockDef is null)
                {
                    Log.Error("[FauxStoneFloors] Tried to create Faux Smooth Rock from null Thing.");
                    return;
                }
                if (rockDef.building is null || !rockDef.building.isNaturalRock || rockDef.building.isResourceRock)
                {
                    Log.Warning($"[FauxStoneFloors] Tried to create Faux Smooth Rock from Thing ({rockDef.ToStringSafe()}) that isn't Rock");
                }
                ThingDef blocks = DefDatabase<ThingDef>.GetNamed("Blocks" + rockDef.defName);
                if (blocks is null)
                {
                    Log.Warning($"[FauxStoneFloors] Couldn't find stone blocks for ThingDef ({rockDef.ToStringSafe()})");
                    return;
                }

                description = "Originally made to mimic ugly natural rock, this floor has been polished to a shiny, smooth surface.";
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
                color = rockDef.graphicData.color;
                label = "faux smooth " + rockDef.label;

                costList = new List<ThingDefCountClass>
                {
                    new ThingDefCountClass
                    {
                        count = BUILD_COST,
                        thingDef = blocks
                    }
                };

                GenerateNewHash<TerrainDef>(this);

                StatUtility.SetStatValueInList(ref statBases, StatDefOf.Beauty, 2f);
                StatUtility.SetStatValueInList(ref statBases, StatDefOf.MarketValue, 8f);
            }
        }
    }
}