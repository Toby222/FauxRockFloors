/*using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace GenerateFauxStoneFloors
{
	[HarmonyPatch(typeof(WealthWatcher), "CalculateWealthFloors")]
	internal class LogIndexErrorsInWealthWatcher
	{
		public static bool Prefix(ref WealthWatcher __instance, ref float __result)
		{
			Map map = new Traverse(__instance).Field("map").GetValue<Map>();
			float[] cachedTerrainMarketValue = new Traverse(typeof(WealthWatcher)).Field("cachedTerrainMarketValue").GetValue<float[]>();

			TerrainDef[] topGrid = map.terrainGrid.topGrid;
			bool[] fogGrid = map.fogGrid.fogGrid;
			float num = 0.0f;
			int maxVal = map.Area;

			for (int i = 0; i < maxVal; i++)
			{
				if (!fogGrid[i])
				{
					try
					{
						num += cachedTerrainMarketValue[topGrid[i].index];
					}
					catch (IndexOutOfRangeException)
					{
						Log.Error($"{i} ({i % map.Size.z}, {i / map.Size.x}) - {topGrid[i].index} - {cachedTerrainMarketValue.Length}");
						Log.ErrorOnce(cachedTerrainMarketValue.Join(), 1);
					}
				}
			}
			__result = num;
			return false;
		}
	}
}
*/