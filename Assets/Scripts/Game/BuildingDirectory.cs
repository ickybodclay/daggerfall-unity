﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2017 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Attached to same GameObject as DaggerfallLocation by scene builders.
    /// This behaviour manages a building dictionary for every building in an exterior location.
    /// Provides helpers to query buildings and link between related systems (automap, quest sites, info text, etc.).
    /// </summary>
    public class BuildingDirectory : MonoBehaviour
    {
        #region Fields

        Dictionary<int, BuildingSummary> buildingDict = new Dictionary<int, BuildingSummary>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Setup building directory from specified location.
        /// </summary>
        /// <param name="location">Source location data.</param>
        public void SetupDirectory(DFLocation location)
        {
            // Clear existing buildings
            buildingDict.Clear();

            // Get block data pre-populated with map building data.
            DFBlock[] blocks;
            RMBLayout.GetLocationBuildingData(location, out blocks);

            // Construct building directory
            int width = location.Exterior.ExteriorData.Width;
            int height = location.Exterior.ExteriorData.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get all buildings for this block
                    // Some blocks have zero buildings
                    int index = y * width + x;
                    BuildingSummary[] buildings = RMBLayout.GetBuildingData(blocks[index], x, y);
                    if (buildings == null || buildings.Length == 0)
                        return;

                    // Add all buildings to directory
                    for (int i = 0; i < buildings.Length; i++)
                    {
                        int key = MakeBuildingKey(buildings[i]);
                        buildingDict.Add(key, buildings[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets building summary.
        /// </summary>
        /// <param name="key">Building key.</param>
        /// <param name="buildingSummaryOut">Building summary out.</param>
        /// <returns>True if lookup successful.</returns>
        public bool GetBuildingSummary(int key, out BuildingSummary buildingSummaryOut)
        {
            if (!buildingDict.ContainsKey(key))
            {
                buildingSummaryOut = new BuildingSummary();
                return false;
            }

            buildingSummaryOut = buildingDict[key];

            return true;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Create a building key unique within a single location only.
        /// </summary>
        /// <param name="layoutX">X position of parent block in map layout.</param>
        /// <param name="layoutY">Y position of parent block in map layout.</param>
        /// <param name="recordIndex">Record index of building inside parent block.</param>
        /// <returns>Building key.</returns>
        public static int MakeBuildingKey(byte layoutX, byte layoutY, byte recordIndex)
        {
            return (layoutX << 16) + (layoutY << 8) + recordIndex;
        }

        /// <summary>
        /// Create a building key from building summary data.
        /// Building summary must have layout coordinates set.
        /// </summary>
        /// <param name="building">BuildingSummary</param>
        /// <returns></returns>
        public static int MakeBuildingKey(BuildingSummary building)
        {
            if (building.LayoutX == -1 || building.LayoutY == -1)
                throw new Exception("MakeBuildingKey(): BuildingSummary does not have building layout coords set.");

            return MakeBuildingKey((byte)building.LayoutX, (byte)building.LayoutY, (byte)building.RecordIndex);
        }

        /// <summary>
        /// Reverse a building key back to block layout coordinates and record index.
        /// </summary>
        /// <param name="key">Building key.</param>
        /// <param name="layoutXOut">X position of parent block in map layout.</param>
        /// <param name="layoutYOut">Y position of parent block in map layout.</param>
        /// <param name="recordIndexOut">Record index of building inside parent block.</param>
        public static void ReverseBuildingKey(int key, out int layoutXOut, out int layoutYOut, out int recordIndexOut)
        {
            layoutXOut = key >> 16;
            layoutYOut = (key >> 8) & 0xff;
            recordIndexOut = key & 0xff;
        }

        #endregion
    }
}