using System;
using System.Collections.Generic;
using FeedFM.Attributes;
using FeedFM.Models;
using UnityEngine;

namespace FeedFM.Demos.Dancing_Bot.Scripts
{
    internal class StationsView : MonoBehaviour
    {
        public static event Action<Station> OnStationSelected = null;
        
        [SerializeField, ReadOnlyDuringPlay] private RectTransform _contentRect = null;
        [SerializeField] private StationCell _prototypeCell = null;
        [SerializeField, ReadOnly] private List<StationCell> _cells = new List<StationCell>();
        private List<Station> _stations = new List<Station>();

        private void Awake()
        {
            _prototypeCell.gameObject.SetActive(false);
            StationCell.OnButtonTapped += StationCellOnButtonTapped;
        }

        private void StationCellOnButtonTapped(StationCell selectedCell)
        {
            int cellsCount = _cells.Count;
            
            for (int index = 0; index < cellsCount; ++index)
            {
                if (_cells[index] == selectedCell)
                {
                    OnStationSelected?.Invoke(_stations[index]);
                }
            }
        }

        public void LoadStations(List<Station> stations)
        {
            _stations.Clear();
            _cells.Clear();
            
            foreach (var station in stations)
            {
                _stations.Add(station);
                
                var cell = Instantiate(_prototypeCell, _contentRect, transform);
                cell.Initialize(station);
                cell.gameObject.SetActive(true);
                _cells.Add(cell);
            }
        }
    }
}