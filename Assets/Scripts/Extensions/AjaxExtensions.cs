using System;
using System.Collections.Generic;
using System.Text;
using FeedFM.Models;
using FeedFM.Utilities;
using UnityEngine.Networking;

namespace FeedFM.Extensions
{
    internal static class AjaxExtensions
    {
        public static string ToQueryString(this Dictionary <string, string> nvc)
        {
            string query = string.Empty;

            foreach (KeyValuePair <string, string> pair in nvc)
            {
                if (query != string.Empty)
                {
                    query = $"{query}&{UnityWebRequest.EscapeURL(pair.Key)}={UnityWebRequest.EscapeURL(pair.Value)}";
                }
                else
                {
                    query = $"{query}?{UnityWebRequest.EscapeURL(pair.Key)}={UnityWebRequest.EscapeURL(pair.Value)}";
                }
            }

            return query;
        }

        public static FeedError GetFeedError(this Ajax ajax)
        {
            return (FeedError) ajax.error;
        }
        
        public static bool HasError(this Ajax ajax)
        {
            return !ajax.success && ajax.GetFeedError() != FeedError.None;
        }
        
        public static bool HasErrorCode(this Ajax ajax, FeedError error)
        {
            return ajax.GetFeedError() == error;
        }

        public static bool IsRequestError(this Ajax ajax, FeedError error)
        {
            return !ajax.success && ajax.GetFeedError() == error;
        }
        
        public static bool IsConnectionError(this Ajax ajax)
        {
            return ajax.IsRequestError(FeedError.ConnectionError);
        }
        
        public static bool IsUndefinedError(this Ajax ajax)
        {
            return ajax.IsRequestError(FeedError.UndefinedError);
        }

        public static bool GetIsSuccessFromResponseSession(this Ajax ajax)
        {
            return ajax.response["success"].AsBool;
        }
        
        public static List<Station> GetStations(this Ajax ajax)
        {
            var stationList = ajax.response["stations"].AsArray;
            var arrayList = new List<Station>(stationList.Count);

            for (int i = 0; i < stationList.Count; i++)
            {
                arrayList.Add(Station.Parse(stationList[i].AsObject));
            }

            return arrayList;
        }

        public static string GetSessionMessage(this Ajax ajax)
        {
            return ajax.GetSessionJSON()["message"].Value;
        }
        
        public static Placement GetPlacementFromResponse(this Ajax ajax)
        {
            JSONClass jsonPlacement = ajax.response["placement"].AsObject;
            return Placement.Parse(jsonPlacement);
        }

        public static bool GetIsAvailableFromResponseSession(this Ajax ajax)
        {
            return ajax.GetSessionJSON()["available"].AsBool;
        }

        public static string GetClientIDFromResponseSession(this Ajax ajax)
        {
            return ajax.GetSessionJSON()["client_id"].Value;
        }
        
        public static bool GetCanSkipFromResponseSession(this Ajax ajax)
        {
            return ajax.response["can_skip"].AsBool;
        }
        
        public static Play GetPlayFromResponseSession(this Ajax ajax)
        {
            return Play.Parse(ajax.response["play"]);
        }

        public static JSONClass GetSessionJSON(this Ajax ajax)
        {
            return ajax.response["session"].AsObject;
        }
    }
}