// <copyright file="FoliageUtils.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>


namespace Tree_Controller.Utils
{
    using System.Collections.Generic;
    using Tree_Controller;

    /// <summary>
    /// Utility methods for Foliage Time Periods and Seasons.
    /// </summary>
    public static class FoliageUtils
    {
        /// <summary>
        ///  A way to lookup seasons.
        /// </summary>
        private static readonly Dictionary<string, Season> SeasonDictionary = new ()
        {
                { "Climate.SEASON[Spring]", Season.Spring },
                { "Climate.SEASON[Summer]", Season.Summer },
                { "Climate.SEASON[Autumn]", Season.Autumn },
                { "Climate.SEASON[Winter]", Season.Winter },
        };

        /// <summary>
        /// An enum to handle seasons.
        /// </summary>
        public enum Season
        {
            /// <summary>
            /// Spring time.
            /// </summary>
            Spring,

            /// <summary>
            /// Summer time.
            /// </summary>
            Summer,

            /// <summary>
            /// Autumn or Fall.
            /// </summary>
            Autumn,

            /// <summary>
            /// Winter time.
            /// </summary>
            Winter,
        }

        /// <summary>
        /// Gets season dictionary.
        /// </summary>
        public static Dictionary<string, Season> SeasonDictionary1 => SeasonDictionary;

        /// <summary>
        /// Gets season from Season ID string.
        /// </summary>
        /// <param name="seasonID"> A string representing the season.</param>
        /// <returns>Season enum.</returns>
        public static Season GetSeasonFromSeasonID(string seasonID)
        {
            if (SeasonDictionary1.ContainsKey(seasonID))
            {
                return SeasonDictionary1[seasonID];
            }

            TreeControllerMod.Instance.Logger.Info($"[FoliageColorData.GetSeasonFromSeasonID] couldn't find season for {seasonID}.");
            return Season.Spring;
        }

        /// <summary>
        /// Returns a colorset linear interpolated between two colorsets.
        /// </summary>
        /// <param name="colorSet1">1st color set.</param>
        /// <param name="colorSet2">2nd color set.</param>
        /// <param name="balance">a float between 0 and 1 for the linear interpolation between colorsets.</param>
        /// <returns>Linearly interpolated colorset.</returns>
        public static Game.Rendering.ColorSet LerpColorSet(Game.Rendering.ColorSet colorSet1, Game.Rendering.ColorSet colorSet2, float balance)
        {
            Game.Rendering.ColorSet lerpedColorSet = new ()
            {
                m_Channel0 = UnityEngine.Color.Lerp(colorSet1.m_Channel0, colorSet2.m_Channel0, balance),
                m_Channel1 = UnityEngine.Color.Lerp(colorSet1.m_Channel1, colorSet2.m_Channel1, balance),
                m_Channel2 = UnityEngine.Color.Lerp(colorSet1.m_Channel2, colorSet2.m_Channel2, balance),
            };
            return lerpedColorSet;
        }
    }
}
