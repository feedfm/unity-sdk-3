using System;
using FeedFM.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeedFM.Demos.Dancing_Bot.Scripts
{
    internal class Temp : MonoBehaviour
    {
        [SerializeField] FeedPlayer _feedPlayer = null;
        [SerializeField] public Animator _animator = null;
        [SerializeField] private TextMeshProUGUI _playPauseLabel = null;
        [SerializeField] private Button _skipButton = null;
        

        private void Awake()
        {
            _feedPlayer.OnStateChanged += FeedPlayerOnOnStateChanged;
            UpdateUI();
        }

        private void FeedPlayerOnOnStateChanged(PlayerState _)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            switch (_feedPlayer.PlayState)
            {
                case PlayerState.Uninitialized:
                    _skipButton.gameObject.SetActive(false);
                    break;
                case PlayerState.Unavailable:
                    break;
                case PlayerState.ReadyToPlay:
                    _playPauseLabel.text = "Play";
                    break;
                case PlayerState.Stalled:
                    break;
                case PlayerState.Playing:
                    _playPauseLabel.text = "Pause";
                    _skipButton.gameObject.SetActive(true);
                    break;
                case PlayerState.Paused:
                    _playPauseLabel.text = "Play";
                    _skipButton.gameObject.SetActive(true);
                    break;
                case PlayerState.WaitingForItem:
                    break;
                case PlayerState.Exhausted:
                    break;
                default:
                {
                    Debug.LogErrorFormat("Unknown state: {0}", _feedPlayer.PlayState);
                    break;
                }
            }
        }

        public void PlayPauseTapped()
        {
            Debug.LogErrorFormat("PlayPauseTapped state: {0}", _feedPlayer.PlayState);
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
                    Debug.LogErrorFormat("Unknown state: {0}", _feedPlayer.PlayState);
                    break;
                }
            }
        }

        public void SkipTapped()
        {
            Debug.LogErrorFormat("SkipTapped state: {0}", _feedPlayer.PlayState);
            
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
                    Debug.LogErrorFormat("Unknown state: {0}", _feedPlayer.PlayState);
                    break;
                }
            }
            
            UpdateUI();
        }
    }
}