using System;
using System.Collections.Generic;
using FeedFM.Attributes;
using FeedFM.Models;
using FeedFM.Utilities;
using UnityEngine;
using Logger = FeedFM.Utilities.Logger;

/*
*	Feed Media client that retrieves music from the Feed.fm servers and plays them on the local
*	device. The player can be assigned an active station from list of online stations
* 
*	To make sure the music begins when requested user should wait for OnPlayReadyForPlayback event before calling play
*
*	The player exposes its current State to assist with rendering playback state.
*
*	Player methods are almost all asynchronous, and the player makes Delegates
*	to allow clients to monitor events and changes in the player state.
*
*	In most use cases, the player isn't useful until it has successfully contacted the Feed.fm
*	servers to retrieve a list of available streaming stations, and the SessionDelegate OnSession
*	helps with determining when music becomes available or playback.
*
*	To use this class, first create an instance and set the 'token' and 'secret' values
*	to what you were given on feed.fm.
* 
*  States of Player and what they mean. 
*  - Uninitialized: This is the initial default state, the player is not ready to play music until it transitions out of this state.
*  - Unavailable: There is no music available for this user. 
*  - ReadyToPlay: The player is ready to play music. 
*  - Stalled: The song is being pulled down.
*  - Playing / Paused: We have a song that we've begun playback of and we're either
*                      currently playing the song or the song is paused. 
*  - WaitingForItem:  The Player is waiting for feed.fm servers so return a song. 
*  - Exhausted: The server has run out of music in the current station that passes DMCA playback
*               rules. The station must be changed, to request more music.
* 
*/

namespace FeedFM
{
    [RequireComponent(typeof(MixingAudioPlayer))]
    [RequireComponent(typeof(Session))]
    [DisallowMultipleComponent]
    public sealed class FeedPlayer : MonoBehaviour
    {
        #region Events

        public event StationDelegate OnStationChanged;
        public event SessionDelegate OnSession;
        public event StateHandler OnStateChanged;
        public event PlayHandler OnPlayReadyForPlayback;
        public event PlayHandler OnPlayStarted;
        public event PlayHandler OnPlayCompleted;
        public event ProgressHandler OnProgressUpdate;

        #endregion
        
        [SerializeField, ReadOnly] private MixingAudioPlayer _mixingAudioPlayer = null;
        [SerializeField, ReadOnly] private Session _session = null;
        [SerializeField, ReadOnlyDuringPlay] private string _token = string.Empty;
        [SerializeField, ReadOnlyDuringPlay] private string _secret = string.Empty;
        [SerializeField, ReadOnlyDuringPlay] private LoggingMode _loggingModeMode = LoggingMode.EditorOnly;

        public List<Station> Stations => _session.stations;

        public float Volume
        {
            get => _mixingAudioPlayer.Volume;
            set => _mixingAudioPlayer.Volume = Mathf.Clamp01(value);
        }

        public bool Available => _session.Available;

        public float CrossFadeDuration
        {
            get => _mixingAudioPlayer.FadeDuration;
            set => _mixingAudioPlayer.FadeDuration = value;
        }

        public Play CurrentPlay => _mixingAudioPlayer.CurrentPlay;
        [SerializeField, ReadOnly] private PlayerState _state;
        public PlayerState PlayState
        {
            get => _state;
            private set
            {
                if (_state == value) { return; }
                
                _state = value;
                OnStateChanged?.Invoke(_state);
            }
        }

        private bool _exhaustedRaised = false;
        private Play _skippedPlay = null;
        
        private void Awake()
        {
            SetupLogging();
            InitializeRequiredComponents();
            SetupSession();
            SetupMixingAudioPlayer();
            FetchSession();
        }

        private void SetupLogging()
        {
            Logger.IsLogging = false;
            
            switch (_loggingModeMode)
            {
                case LoggingMode.None:
                    break;
                case LoggingMode.EditorOnly:
#if UNITY_EDITOR
                    Logger.IsLogging = true;
#endif
                    break;
                case LoggingMode.Always:
                    Logger.IsLogging = true;
                    break;
                default:
#if UNITY_EDITOR
                    Debug.LogErrorFormat("Unknown case: {0}", _loggingModeMode);
#endif
                    break;
            }
        }

        private void SetupSession()
        {
            _session.Initialize(_token, _secret);
            _session.OnNextPlayFetched += HandleOnNextPlayFetched;
            _session.OnPlaysExhausted += HandleOnPlaysExhausted;
            _session.OnSession += HandleOnSessionAvailabilityChanged;
            _session.OnSkipRequestCompleted += HandleOnSkipRequestCompleted;
        }

        private void SetupMixingAudioPlayer()
        {
            _mixingAudioPlayer.OnPlayReadyForPlayback += HandlePlayReadyForPlayback;
            _mixingAudioPlayer.OnPlayItemBeganPlayback += HandlePlayItemBeganPlayback;
            _mixingAudioPlayer.OnPlayCompleted += HandlePlayCompletedByPlayer;
            _mixingAudioPlayer.OnPlayFailed += HandlePlayFailed;
            _mixingAudioPlayer.OnProgressUpdate += HandleOnProgressUpdate;
            _mixingAudioPlayer.OnStateChanged += HandleOnPlayerStateChanged;
        }

        private void FetchSession()
        {
            StartCoroutine(_session.FetchSession());
        }

        private void InitializeRequiredComponents()
        {
            if (!_mixingAudioPlayer)
            {
                _mixingAudioPlayer = GetComponent<MixingAudioPlayer>();
                if (!_mixingAudioPlayer)
                {
                    _mixingAudioPlayer = gameObject.AddComponent<MixingAudioPlayer>();
                }
            }

            if (!_session)
            {
                _session = GetComponent<Session>();
                if (!_session)
                {
                    _session = gameObject.AddComponent<Session>();
                }
            }
        }

        #region Session Handlers
        
        private void HandleOnNextPlayFetched(Play play)
        {
            _mixingAudioPlayer.AddAudioAsset(play);
        }
        
        /// <summary>
        /// Take us out of pause if we've run out of songs
        /// </summary>
        private void HandleOnPlaysExhausted(Session session)
        {
            if (_mixingAudioPlayer.State == PlayerState.WaitingForItem && _mixingAudioPlayer.IsEmpty())
            {
                PlayState = PlayerState.Exhausted;
            }
            else
            {
                _exhaustedRaised = true;
            }
        }

        private void HandleOnSessionAvailabilityChanged(bool isAvailable, string errorMessage)
        {
            if (isAvailable)
            {
                ActiveStation = _session.stations[0];
            }

            if (isAvailable)
            {
                PlayState = PlayerState.ReadyToPlay;
            }
            else
            {
                PlayState = PlayerState.Unavailable;
            }
            
            OnSession?.Invoke(isAvailable, errorMessage);
        }

        private void HandleOnSkipRequestCompleted(bool isSuccess)
        {
            if (!isSuccess) { return; }
            
            _skippedPlay = CurrentPlay;
            _mixingAudioPlayer.Skip();
        }

        #endregion

        #region MixingAudioPlayer Handlers

        private void HandleOnPlayerStateChanged(PlayerState state)
        {
            PlayState = state;
        }

        private void HandlePlayReadyForPlayback(Play play)
        {
            OnPlayReadyForPlayback?.Invoke(play);
        }

        private void HandlePlayItemBeganPlayback(Play play)
        {
            _session.ReportPlayStarted(play);
            OnPlayStarted?.Invoke(play);
        }

        private void HandlePlayCompletedByPlayer(Play play)
        {
            OnPlayCompleted?.Invoke(play);
            
            if (_skippedPlay != null && play.Id.Equals(_skippedPlay.Id))
            {
                _skippedPlay = null;
                return;
            }

            _session.ReportPlayCompleted(play);
            
            if (_exhaustedRaised)
            {
                //Set the play state when all playback is complete
                PlayState = PlayerState.Exhausted;
                _exhaustedRaised = false;
            }
        }

        private void HandlePlayFailed(Play play)
        {
            _session.RequestInvalidate(play);
        }

        private void HandleOnProgressUpdate(Play play, float progress, float duration)
        {
            OnProgressUpdate?.Invoke(play, progress, duration);
        }

        #endregion


        #region Public API

        public void RequestNewClientID(ClientDelegate clientDelegate)
        {
            StartCoroutine(_session.RequestNewClientID(clientDelegate));
        }

        public string GetClientID()
        {
            return _session.ClientId;
        }

        public void SetClientID(string clientID)
        {
            if (clientID.StartsWith(Session.EXPORT_CLIENT_ID_PREFIX))
            {
                _session.ClientId = clientID;
            }
        }

        public void Play()
        {
            if (PlayState == PlayerState.Uninitialized)
            {
                throw new Exception("Tried to begin playback before player is Available");
            }

            if (PlayState == PlayerState.Unavailable)
            {
                throw new Exception("Can't begin playback when player is UnAvailable");
            }

            _mixingAudioPlayer.Play();
        }

        public void Pause()
        {
            _mixingAudioPlayer.Pause();
            _session.ReportPlayElapsed(_mixingAudioPlayer.CurrentPlayTime);
        }

        public Station ActiveStation
        {
            get => _session._activeStation;

            set
            {
                var oldState = PlayState;
                
                if (_session._activeStation == value) { return; }

                if (PlayState == PlayerState.Exhausted)
                {
                    PlayState = PlayerState.WaitingForItem;
                }

                _mixingAudioPlayer.FlushAndIncludeCurrent();
                _session.Reset();
                _session._activeStation = value;
                
                if (oldState != PlayerState.Uninitialized)
                {
                    OnStationChanged?.Invoke(value);
                }
                
                _session.RequestNext();
            }
        }

        public void Skip()
        {
            if (!_session.HasActivePlayStarted())
            {
                // can't skip non-playing song
                return;
            }

            _session.RequestSkip();
        }

        #endregion

        /* 
     * Keep track of when the audio isn't playing paused, but just in the
     * background
     */

        void OnApplicationPause(bool pauseState)
        {
        }

        public bool CanSkip()
        {
            return _session.MaybeCanSkip();
        }


#if UNITY_EDITOR
        private void Reset()
        {
            InitializeRequiredComponents();

            while (UnityEditorInternal.ComponentUtility.MoveComponentUp(this))
            {
                // Move FeedPlayer component to the top
            }

            if (_mixingAudioPlayer)
            {
                _mixingAudioPlayer.Reset();
            }
        }
#endif
    }
}