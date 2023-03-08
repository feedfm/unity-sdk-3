using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeedFM.Utilities
{
    internal static class WaitForSecondsLibrary
    {
        #region Class Fields & Methods
        
        private static readonly Dictionary<int, WaitForSeconds> FrameYieldCache = new Dictionary<int, WaitForSeconds>
        {
            { 15, new WaitForSeconds(1F / 15) },
            { 30, new WaitForSeconds(1F / 30) },
            { 50, new WaitForSeconds(1F / 50) },
            { 60, new WaitForSeconds(1F / 60) },
            { 100, new WaitForSeconds(1F / 100) },
            { 120, new WaitForSeconds(1F / 120) }
        };

        private static readonly Dictionary<float, WaitForSeconds> WaitCache = GetInitialCache();

        private static Dictionary<float, WaitForSeconds> GetInitialCache()
        {
           var cache = new Dictionary<float, WaitForSeconds>
            {
                { 0.1f, new WaitForSeconds(0.1f) },
                { 0.2f, new WaitForSeconds(0.2f) },
                { 0.25f, new WaitForSeconds(0.25f) },
                { 0.3f, new WaitForSeconds(0.3f) },
                { 0.33f, new WaitForSeconds(0.33f) },
                { 0.5f, new WaitForSeconds(0.5f) },
                { 1, new WaitForSeconds(1) },
                { 2, new WaitForSeconds(2) },
                { 3, new WaitForSeconds(3) },
                { 4, new WaitForSeconds(4) },
                { 5, new WaitForSeconds(5) },
                { 10, new WaitForSeconds(10) },
                { 15, new WaitForSeconds(15) },
                { 20, new WaitForSeconds(20) },
                { 25, new WaitForSeconds(25) },
                { 30, new WaitForSeconds(30) },
                { 60, new WaitForSeconds(60) },
                { 100, new WaitForSeconds(100) },
                { 120, new WaitForSeconds(120) }
            };
           
           for (int i = 0; i < 10; i++)
           {
               var firstWaitTime = 0.2f * (float) Math.Pow(2.0, i);
               var secondWaitTime = 0.5f * (float) Math.Pow(2.0, i);
               
               cache.TryAdd(firstWaitTime, new WaitForSeconds(firstWaitTime));
               cache.TryAdd(secondWaitTime, new WaitForSeconds(secondWaitTime));
           }

           return cache;
        }
        
        public static WaitForSeconds GetWaitForSecondsApplicationFrameRate() => GetWaitForSecondsFrame(Application.targetFrameRate);
        
        public static WaitForSeconds GetWaitForSecondsFrame(int framerate)
        {
            if (FrameYieldCache.TryGetValue(framerate, out WaitForSeconds wait))
            {
                return wait;
            }
            else
            {
                FrameYieldCache[framerate] = new WaitForSeconds(1F / framerate);
                return FrameYieldCache[framerate];
            }
        }
        
        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            if (WaitCache.TryGetValue(seconds, out WaitForSeconds wait))
            {
                return wait;
            }
            else
            {
                WaitCache[seconds] = new WaitForSeconds(seconds);
                return WaitCache[seconds];
            }
        }

        public static WaitForSeconds QuarterOfASecond => GetWaitForSeconds(1/4f);
        public static WaitForSeconds ThirdOfASecond => GetWaitForSeconds(1/3f);
        public static WaitForSeconds HalfOfASecond => GetWaitForSeconds(0.5f);
        public static WaitForSeconds OneSecond => GetWaitForSeconds(1f);
        public static WaitForSeconds TwoSeconds => GetWaitForSeconds(2f);
        public static WaitForSeconds ThreeSeconds => GetWaitForSeconds(3f);
        public static WaitForSeconds FiveSeconds => GetWaitForSeconds(5f);
        public static WaitForSeconds TenSeconds => GetWaitForSeconds(10f);

        #endregion
    }
}