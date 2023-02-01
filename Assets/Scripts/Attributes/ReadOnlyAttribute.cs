using System;
using UnityEngine;

namespace FeedFM.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public const string VERSION = "1.0.1";
    }
}