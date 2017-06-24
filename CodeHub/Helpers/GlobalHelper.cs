﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace CodeHub.Helpers
{
    public class GlobalHelper
    {
        /*
        *  Message types are used for Viewodel to ViewModel communication
        */
        #region Message Types

        public class AdsEnabledMessageType
        {
        }
        public class SignOutMessageType
        {

        }
        public class HasInternetMessageType
        {

        }
        public class LocalNotificationMessageType
        {
            public string Message { get; set; }
            public string Glyph { get; set; }

        }
        public class SetHeaderTextMessageType
        {
            public string PageName { get; set; }
        }
        public class FollowActivityMessageType
        {

        }
        public class HostWindowBlurMessageType
        {
        }
        #endregion

        /// <summary>
        /// Username of the Authenticated user 
        /// </summary>
        public static string UserLogin { get; set; }

        /// <summary>
        /// Indicates whether user has performed a new Star/Unstar action. Used to update starred repositories
        /// </summary>
        public static bool NewStarActivity { get; set; }

        /// <summary>
        /// Indicates whether user has performed a new Follow/UnFollow action. Used to update followers
        /// </summary>
        public static bool NewFollowActivity { get; set; }

        /// <summary>
        /// List of names and owner names of Trending repositories
        /// </summary>
        public static List<Tuple<string, string>> TrendingTodayRepoNames { get; set; }

        /// <summary>
        /// List of names and owner names of Trending repositories
        /// </summary>
        public static List<Tuple<string, string>> TrendingWeekRepoNames { get; set; }

        /// <summary>
        /// List of names and owner names of Trending repositories
        /// </summary>
        public static List<Tuple<string, string>> TrendingMonthRepoNames { get; set; }

        /// <summary>
        /// Determines if internet connection is available to device
        /// </summary>
        /// <returns></returns>
        public static bool IsInternet()
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            return (connectionProfile != null &&
                                        connectionProfile.GetNetworkConnectivityLevel() ==
                                        NetworkConnectivityLevel.InternetAccess);

        }

        /// <summary>
        /// Converts a Hex string to corressponding SolidColorBrush
        /// </summary>
        /// <param name="hex">rrggbbaa</param>
        /// <returns></returns>
        public static SolidColorBrush GetSolidColorBrush(string hex)
        {
            byte r = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte a = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            return myBrush;
        }

        public static Geometry GetGeomtery(string path)
        {
            var sym = "<Geometry xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">"+path+"</Geometry>";
            return (Geometry)XamlReader.Load(sym);
        }

        /// <summary>
        /// Gets the OS build
        /// </summary>
        /// <returns></returns>
        public static ulong GetOSBuild()
        {
            string deviceFamilyVersion = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong version = ulong.Parse(deviceFamilyVersion);
            //ulong major = (version & 0xFFFF000000000000L) >> 48;
            //ulong minor = (version & 0x0000FFFF00000000L) >> 32;
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            //ulong revision = (version & 0x000000000000FFFFL);
            //var osVersion = $"{major}.{minor}.{build}.{revision}";
            return build;
        }
    }

}