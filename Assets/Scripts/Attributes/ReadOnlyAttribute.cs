using System;
using UnityEngine;

namespace FeedFM.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public const string VERSION = "0.0.14";
    }
}