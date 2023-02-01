using System;
using System.Collections;
using System.Collections.Generic;
using FeedFM.Attributes;
using FeedFM.Models;
using FeedFM.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using Logger = FeedFM.Utilities.Logger;

namespace FeedFM
{
    [DisallowMultipleComponent]
    public sealed class MixingAudioPlayer : MonoBehaviour
    {
        #region Events

        public event StateHandler OnStateChanged;
        public event PlayHandler OnPlayReadyForPlayback;		
        public event PlayHandler OnPlayItemBeganPlayback;		
        public event PlayHandler OnPlayCompleted;		
        public event PlayHandler OnPlayFailed;
        public event ProgressHandler OnProgressUpdate;

        #endregion

        [SerializeField, ReadOnly] private float _currentPlayTime;
        [SerializeField, ReadOnly] private float _currentPlayDuration;
        [SerializeField, ReadOnly] private PlayerState _state;
        
        private readonly IEnumerator[] _faders = new IEnumerator[2];
        private int _activeAudioSourceIndex = 0;
        private const int VOLUME_STEPS_PER_SECOND = 60;
        private readonly Queue<Play> _audioFileList = new Queue<Play>();
        private bool _playWhenReady = false;
        private PlayAndClip _currentAsset;
        private PlayAndClip _nextAsset;
        private bool _bUpdate;
        private UnityWebRequest _unityWebRequest;
        [Range(0.0f, 1.0f)]
        private float _volume = 1.0f;
        private AudioSource[] _audioSources = Array.Empty<AudioSource>();
        private const int REQUIRED_NUMBER_OF_AUDIOSOURCES = 2;
    
        public PlayerState State
        {
            get => _state;
            set
            {
                if (_state == value) { return; }
                _state = value;
                OnStateChanged?.Invoke(_state);
            }
        }
    
        public float FadeDuration = 4.0f;
        public float CurrentPlayTime => _currentPlayTime;
        public Play CurrentPlay => _currentAsset?.play;
        public float CurrentPlayDuration => _currentPlayDuration;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _audioSources[_activeAudioSourceIndex].volume = value;
            }
        }

        private AudioSource ActiveAudioSource => _audioSources[_activeAudioSourceIndex];
        
        public void Awake()
        {
            InitializeRequiredComponents();
            _bUpdate = false;
        }

        private void InitializeRequiredComponents()
        {
            if (_audioSources.Length != REQUIRED_NUMBER_OF_AUDIOSOURCES)
            {
                var oldAudioSources = gameObject.GetComponents<AudioSource>();

                foreach (var oldAudioSource in oldAudioSources)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(oldAudioSource);
                    }
                    else
                    {
                        DestroyImmediate(oldAudioSource);
                    }
                }
                
                _audioSources = new AudioSource[REQUIRED_NUMBER_OF_AUDIOSOURCES];
                
                for (int index = 0; index < REQUIRED_NUMBER_OF_AUDIOSOURCES; index++)
                {
                    _audioSources[index] = gameObject.AddComponent<AudioSource>();
                    
                    SetAudioSourceToDefaultValue(_audioSources[index]);
                }
            }
        }

        private static void SetAudioSourceToDefaultValue(AudioSource audioSource)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.0f;
        }

        public bool IsEmpty()
        {
            return _audioFileList.Count == 0 && _nextAsset == null && _currentAsset == null;
        }
        
        public void AddAudioAsset(Play play)
        {
            if (Logger.IsLogging)
            {
                Debug.LogFormat("Add Asset {0}", play.AudioFile.TrackTitle);
            }
            _audioFileList.Enqueue(play);

            if (_nextAsset == null)
            {
                StartCoroutine(LoadNext());
            }
        }

        public void Play()
        {
            _playWhenReady = true;
            
            switch (State)
            {
                case PlayerState.Playing:
                    return;
                case PlayerState.ReadyToPlay:
                
                    if (_nextAsset != null)
                    {
                        LoadAudioClipWithFade(_nextAsset);
                    }
                    else
                    {
                        StartCoroutine(LoadNext());
                    }
                
                    break;
                case PlayerState.Paused:
                    if (_currentAsset != null)
                    {
                        _playWhenReady = true;
                        ActiveAudioSource.Play();
                    }
                    else if(_nextAsset != null)
                    {
                        LoadAudioClipWithFade(_nextAsset);
                    }
                
                    break;
                default:
                {
                    if(_nextAsset == null && _currentAsset == null)
                    {
                        if (_audioFileList.Count == 0)
                        {
                            State = PlayerState.WaitingForItem;
                        }
                        else
                        {
                            StartCoroutine(LoadNext());
                        }
                    }

                    break;
                }
            }
            if (!_bUpdate)
            {
                _bUpdate = true;
                StartCoroutine(UpdateState());
            }
        }

    
        public void Pause()
        {
            foreach (var audioSource in _audioSources)
            {
                audioSource.Pause();
            }
            _playWhenReady = false;
            State = PlayerState.Paused;
            _bUpdate = false;
        }

        public void Skip()
        {
            if (_nextAsset != null)
            {
                LoadAudioClipWithFade(_nextAsset);
            }
            else
            {
                State = PlayerState.WaitingForItem;
                OnPlayCompleted?.Invoke(_currentAsset.play);
                ActiveAudioSource.Stop();
                _currentAsset = null;
            
            }
        }

        public void SeekTo(float seconds)
        {
            if (seconds > CurrentPlayTime)
            {
                ActiveAudioSource.time = seconds;
            }
        }
    
        public void Flush()
        {
            _audioFileList.Clear();
            _nextAsset = null;
        }

        public void FlushAndIncludeCurrent()
        {
            if (Logger.IsLogging)
            {
                Debug.LogFormat("Flushing");
            }
            if(State == PlayerState.Playing)
            {
                OnPlayCompleted?.Invoke(_currentAsset.play);
                SetVolumeOfAllAudioSourcesToZero();
                State = PlayerState.WaitingForItem;
            }
            else if(State == PlayerState.Stalled || State == PlayerState.ReadyToPlay)
            {
                State = PlayerState.WaitingForItem;
            }

            StopAndFlushAllAudioSources();
        
            _unityWebRequest?.Abort();
            _audioFileList.Clear();
            _currentAsset = null;
            _nextAsset = null;
        }

        private void SetVolumeOfAllAudioSourcesToZero()
        {
            foreach (var audioSource in _audioSources)
            {
                audioSource.volume = 0;
            }
        }

        private void StopAndFlushAllAudioSources()
        {
            foreach (var audioSource in _audioSources)
            {
                StopAndFlushAudioSource(audioSource);
            }
        }

        private static void StopAndFlushAudioSource(AudioSource audioSource)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        private IEnumerator UpdateState()
        {
            while (_bUpdate)
            {
                if (ActiveAudioSource.isPlaying && State != PlayerState.Playing)
                {
                    State = PlayerState.Playing;
                }

                if (State == PlayerState.Playing)
                {
                    _currentPlayTime = ActiveAudioSource.time;
                    OnProgressUpdate?.Invoke(CurrentPlay, ActiveAudioSource.time, CurrentPlayDuration);
                }

                if (_currentAsset != null && _nextAsset != null)
                {
                    _currentPlayTime = ActiveAudioSource.time;
                    var msUntilFade = CurrentPlayDuration - FadeDuration - CurrentPlayTime - _currentAsset.play.AudioFile.trimEnd;
                    if (msUntilFade <= 0)
                    {
                        LoadAudioClipWithFade(_nextAsset);
                    }
                }
                else if(_currentAsset != null && _nextAsset == null)
                {
                    if (!ActiveAudioSource.isPlaying && (ActiveAudioSource.time == 0f)) {
                        // The track ended
                        State = PlayerState.WaitingForItem;
                        OnPlayCompleted?.Invoke(_currentAsset.play);
                        StopAndFlushAudioSource(ActiveAudioSource);
                        // Is the next song playing
                        _currentAsset = null;
                    }
                }

                yield return null;
            }
       
        }

        private IEnumerator LoadNext()
        {
            if (_audioFileList.Count > 0 && _nextAsset == null)
            {
                var audioTrack = _audioFileList.Dequeue();
                
                _nextAsset = new PlayAndClip();
            
                if(!ActiveAudioSource.isPlaying)
                {
                    State = PlayerState.Stalled;
                }
                // start loading up the song
            
                _unityWebRequest = UnityWebRequestMultimedia.GetAudioClip(audioTrack.AudioFile.Url, AudioType.MPEG); // TODO: Ask about AudioType

                ((DownloadHandlerAudioClip)_unityWebRequest.downloadHandler).streamAudio = true; // Enable stream audio so the music will stream instead of fully downloading before starting
                yield return _unityWebRequest.SendWebRequest();
            
                AudioClip clip = null;

                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(_unityWebRequest);
                }
                catch (Exception e)
                {
                    if (Logger.IsLogging)
                    {
                        Debug.LogErrorFormat("{0}", e.Message);
                    }
                    if (!e.Message.Equals("Cannot access the .audioClip property of an aborted DownloadHandlerAudioClip"))
                    {
                        OnPlayFailed?.Invoke(audioTrack);
                    }
                    yield break;
                }

                _unityWebRequest = null;
            
                if (!clip)
                {
                    OnPlayFailed?.Invoke(audioTrack);
                    yield break;
                }
            
                // Ready to play

                if (_nextAsset == null)
                {
                    yield break;
                }
            
                _nextAsset.play = audioTrack;
                _nextAsset.clip = clip;

                _nextAsset.clip.LoadAudioData();
                StartCoroutine(CheckLoadState());

            }
        }

        private IEnumerator CheckLoadState()
        {
            if (_nextAsset == null)
            {
                yield break;
            }
        
            while(_nextAsset.clip.loadState != AudioDataLoadState.Loaded)
            {
                yield return WaitForSecondsLibrary.GetWaitForSeconds(0.2f);
            }
            
            OnPlayReadyForPlayback?.Invoke(_nextAsset.play);
        
            if (State == PlayerState.Uninitialized)
            {
                State = PlayerState.ReadyToPlay;
            }
            
            if (State == PlayerState.ReadyToPlay || State == PlayerState.WaitingForItem || State == PlayerState.Stalled )
            { 
                if(_playWhenReady)
                {
                    LoadAudioClipWithFade(_nextAsset);
                }
                else
                {
                    State = PlayerState.ReadyToPlay;
                }
            }
        }
    
        private void LoadAudioClipWithFade(PlayAndClip asset)
        { 
            //Prevent fading the same clip on both players 
            if (asset.clip == ActiveAudioSource.clip)
            {
                return;
            }
        
            StopAllFadeCoroutines();
            
            //Fade-out the active play, if it is not silent (eg: first start)
            if (_currentAsset != null)
            {
                OnPlayCompleted?.Invoke(_currentAsset.play);
                _faders[0] = FadeOutAudioSourceNoAlloc(ActiveAudioSource, FadeDuration, 0.0f);
                StartCoroutine(_faders[0]);
            }
        
            _currentAsset = asset;
            _nextAsset = null;
            _currentPlayDuration = _currentAsset.clip.length;

            //Fade-in the new clip
            int nextPlayer = (_activeAudioSourceIndex + 1) % _audioSources.Length;
            var nextAudioSource = _audioSources[nextPlayer];
            nextAudioSource.clip = asset.clip;
            nextAudioSource.loop = false;
            if (asset.play.AudioFile.trimStart != 0)
            {
                nextAudioSource.time = asset.play.AudioFile.trimStart;
            }
            nextAudioSource.Play();
            // Song started
            OnPlayItemBeganPlayback?.Invoke(_currentAsset.play);
            _faders[1] = FadeInAudioSourceNoAlloc(nextAudioSource, FadeDuration, Volume);
            StartCoroutine(_faders[1]);

            //Register new active player
            _activeAudioSourceIndex = nextPlayer;
            State = PlayerState.Playing;
        }

        private void StopAllFadeCoroutines()
        {
            foreach (IEnumerator fader in _faders)
            {
                if (fader != null)
                {
                    StopCoroutine(fader);
                }
            }
        }

        private IEnumerator FadeOutAudioSourceNoAlloc(AudioSource audioSource, float duration, float targetVolume)
        {
            yield return FadeAudioSourceNoAlloc(audioSource, duration, targetVolume);
            
            // Song completed
            _faders[0] = null;
            StopAndFlushAudioSource(audioSource);
        }

        private IEnumerator FadeInAudioSourceNoAlloc(AudioSource audioSource, float duration, float targetVolume)
        {
            yield return FadeAudioSourceNoAlloc(audioSource, duration, targetVolume);
            
            _faders[1] = null;
        }

        private static IEnumerator FadeAudioSourceNoAlloc(AudioSource player, float duration, float targetVolume)
        {
            //Calculate the steps
            int steps = (int)(VOLUME_STEPS_PER_SECOND * duration);
            float stepTime = duration / steps;
            float stepSize = (targetVolume - player.volume) / steps;
            var localWaitForSecondsCache = new WaitForSeconds(stepTime);

            //Fade now
            for (int i = 0; i < steps; i++)
            {
                player.volume += stepSize;
                yield return localWaitForSecondsCache;
            }
            
            //Make sure the targetVolume is set
            player.volume = targetVolume;
        }
        
#if UNITY_EDITOR
        public void Reset()
        {
            InitializeRequiredComponents();

            for (int index = 0; index < _audioSources.Length; index++)
            {
                while (UnityEditorInternal.ComponentUtility.MoveComponentDown(_audioSources[index]))
                {
                    // Move AudioSource component to bottom
                }
            }
        }
#endif

    }
}