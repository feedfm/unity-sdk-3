using System;
using FeedFM.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = FeedFM.Utilities.Logger;

namespace FeedFM.Demos.Dancing_Bot.Scripts
{
    internal class MusicPlayerView : MonoBehaviour
    {
        [SerializeField] FeedPlayer _feedPlayer = null;
        [SerializeField] private StationsView _stationsView = null;
        [SerializeField] public Animator _animator = null;
        [SerializeField] private AudioTrackView _audioTrackView = null;
        [SerializeField] private TextMeshProUGUI _playPauseLabel = null;
        [SerializeField] private Button _playPauseButton = null;
        [SerializeField] private TextMeshProUGUI _currentStationNameLabel = null;
        [SerializeField] private Button _skipButton = null;
        [SerializeField] private GameObject _noTrackPanel = null;
        [SerializeField] private GameObject _noStationPanel = null;
        [SerializeField] private Color _playButtonColor = Color.green;
        [SerializeField] private Color _pauseButtonColor = Color.red;


        private void Awake()
        {
            StationsView.OnStationSelected += StationsViewOnStationSelected;
            
            _feedPlayer.OnStateChanged += FeedPlayerOnStateChanged;
            _feedPlayer.OnStationChanged += FeedPlayerOnStationChanged;
            _feedPlayer.OnPlayStarted += FeedPlayerOnPlayStarted;
            _feedPlayer.OnSession += FeedPlayerOnSession;
            UpdateUI();
        }

        private void StationsViewOnStationSelected(Station station)
        {
            _feedPlayer.ActiveStation = station;
        }

        private void FeedPlayerOnSession(bool isAvailable, string errmsg)
        {
            if (!isAvailable) { return; }
            
            _stationsView.LoadStations(_feedPlayer.Stations);
        }

        private void FeedPlayerOnStateChanged(PlayerState _)
        {
            UpdateUI();
        }

        private void FeedPlayerOnStationChanged(Station _)
        {
            UpdateUI();
            _feedPlayer.Play();
        }

        private void FeedPlayerOnPlayStarted(Play play)
        {
            UpdateTrackUI();
        }

        private void UpdateUI()
        {
            _animator.speed = 0f;
            UpdateTrackUI();
            UpdateStationUI();
            
            switch (_feedPlayer.PlayState)
            {
                case PlayerState.Uninitialized:
                    _skipButton.gameObject.SetActive(false);
                    break;
                case PlayerState.Unavailable:
                    break;
                case PlayerState.ReadyToPlay:
                    _playPauseLabel.text = "Play";
                    _playPauseButton.image.color = _playButtonColor;
                    break;
                case PlayerState.Stalled:
                    break;
                case PlayerState.Playing:
                    _animator.speed = 1f;
                    _playPauseLabel.text = "Pause";
                    _skipButton.gameObject.SetActive(true);
                    _playPauseButton.image.color = _pauseButtonColor;
                    break;
                case PlayerState.Paused:
                    _playPauseLabel.text = "Play";
                    _skipButton.gameObject.SetActive(true);
                    _playPauseButton.image.color = _playButtonColor;
                    break;
                case PlayerState.WaitingForItem:
                    break;
                case PlayerState.Exhausted:
                    break;
                default:
                {
                    if (Logger.IsLogging)
                    {
                        Debug.LogErrorFormat("Unknown state: {0}", _feedPlayer.PlayState);
                    }
                    break;
                }
            }
        }

        private void UpdateTrackUI()
        {
            if (_feedPlayer.CurrentPlay is not null)
            {
                _noTrackPanel.SetActive(false);

                _audioTrackView.Configure(_feedPlayer.CurrentPlay);
            }
            else
            {
                _noTrackPanel.SetActive(true);
                _audioTrackView.ClearUI();
            }
        }

        private void UpdateStationUI()
        {
            if (_feedPlayer.ActiveStation != null)
            {
                _noStationPanel.SetActive(false);
                _currentStationNameLabel.text = string.Format("Station: {0}", _feedPlayer.ActiveStation.Name);
            }
            else
            {
                _noStationPanel.SetActive(true);
                _currentStationNameLabel.text = string.Empty;
            }
        }

        public void PlayPauseTapped()
        {
            if (Logger.IsLogging)
            {
                Debug.LogErrorFormat("PlayPauseTapped state: {0}", _feedPlayer.PlayState);
            }
            switch (_feedPlayer.PlayState)
            {
                case PlayerState.Uninitialized:
                    break;
                case PlayerState.Unavailable:
                    break;
                case PlayerState.ReadyToPlay:
                    _feedPlayer.Play();
                    break;
                case PlayerState.Stalled:
                    break;
                case PlayerState.Playing:
                    _feedPlayer.Pause();
                    break;
                case PlayerState.Paused:
                    _feedPlayer.Play();
                    break;
                case PlayerState.WaitingForItem:
                    break;
                case PlayerState.Exhausted:
                    break;
                default:
                {
                    if (Logger.IsLogging)
                    {
                        Debug.LogErrorFormat("Unknown state: {0}", _feedPlayer.PlayState);
                    }
                    break;
                }
            }
            
            UpdateUI();
        }

        public void SkipTapped()
        {
            if (Logger.IsLogging)
            {
                Debug.LogErrorFormat("SkipTapped state: {0}", _feedPlayer.PlayState);
            }
            
            switch (_feedPlayer.PlayState)
            {
                case PlayerState.Uninitialized:
                    break;
                case PlayerState.Unavailable:
                    break;
                case PlayerState.ReadyToPlay:
                    _feedPlayer.Skip();
                    break;
                case PlayerState.Stalled:
                    break;
                case PlayerState.Playing:
                    _feedPlayer.Skip();
                    break;
                case PlayerState.Paused:
                    _feedPlayer.Skip();
                    break;
                case PlayerState.WaitingForItem:
                    break;
                case PlayerState.Exhausted:
                    break;
                default:
                {
                    if (Logger.IsLogging)
                    {
                        Debug.LogErrorFormat("Unknown state: {0}", _feedPlayer.PlayState);
                    }
                    break;
                }
            }
        }
    }
}