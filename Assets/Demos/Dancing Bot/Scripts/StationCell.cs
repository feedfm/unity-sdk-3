using System;
using FeedFM.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeedFM.Demos.Dancing_Bot.Scripts
{
    internal class StationCell : MonoBehaviour
    {
        public static event Action<StationCell> OnButtonTapped = null;
        
        [SerializeField] private TextMeshProUGUI _stationName = null;
        [SerializeField] private Button _button = null;

        public void Initialize(Station station)
        {
            _stationName.text = station.Name;
        }

        public void Tapped()
        {
            OnButtonTapped?.Invoke(this);
        }
    }
}