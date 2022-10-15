using System;
using System.Collections;
using System.Collections.Generic;
using FeedFM.Attributes;
using FeedFM.Models;
using FeedFM.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace FeedFM
{
    [DisallowMultipleComponent]
    internal sealed class MixingAudioPlayer : MonoBehaviour
    {
        #region Events

        public event StateHandler OnStateChanged;
        public event PlayHandler OnPlayReadyForPlayback;		
        public event PlayHandler OnPlayItemBeganPlayback;		
        public event PlayHandler OnPlayCompleted;		
        public event PlayHandler OnPlayFailed;
        public event ProgressHandler OnProgressUpdate;

        #endregion

        [SerializeField, ReadOnly] private AudioSource[] _audioSources = Array.Empty<AudioSource>();
        private PlayerState _state;
        private IEnumerator[] _faders = new IEnumerator[2];
        private int _activeAudioSourceIndex = 0;
        private int noOfVolumeStepsPerSecond = 60;
        private Queue<Play> audioFileList = new Queue<Play>();
        private bool playWhenReady = false;
        private PlayAndClip currentAsset;
        private PlayAndClip nextAsset;
        private bool bUpdate;
        private UnityWebRequest lUnityWebRequest;
        [Range(0.0f, 1.0f)]
        private float _volume = 1.0f;

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
        public float CurrentPlayTime;
        public Play CurrentPlay => currentAsset?.play;
        public float CurrentPlayDuration;

        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }

        private AudioSource ActiveAudioSource => _audioSources[_activeAudioSourceIndex];
        
        public void Awake()
        {
            InitializeRequiredComponents();
            bUpdate = false;
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

        private void SetAudioSourceToDefaultValue(AudioSource audioSource)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.0f;
        }

        public bool IsEmpty()
        {
            return audioFileList.Count == 0 && nextAsset == null && currentAsset == null;
        }
        
        public void AddAudioAsset(Play play)
        {
#if UNITY_EDITOR
            Debug.LogFormat("Add Asset {0}", play.AudioFile.TrackTitle);
#endif
            audioFileList.Enqueue(play);

            if (nextAsset == null)
            {
                StartCoroutine(LoadNext());
            }
        }

        public void Play()
        {
            playWhenReady = true;
            switch (State)
            {
                case PlayerState.Playing:
                    return;
                case PlayerState.ReadyToPlay:
                
                    if (nextAsset != null)
                    {
                        LoadAudioClipWithFade(nextAsset);
                    }
                    else
                    {
                        StartCoroutine(LoadNext());
                    }
                
                    break;
                case PlayerState.Paused:
                    if (currentAsset != null)
                    {
                        playWhenReady = true;
                        ActiveAudioSource.Play();
                    }
                    else if(nextAsset != null)
                    {
                        LoadAudioClipWithFade(nextAsset);
                    }
                
                    break;
                default:
                {
                    if(nextAsset == null && currentAsset == null)
                    {
                        if (audioFileList.Count == 0)
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
            if (!bUpdate)
            {
                bUpdate = true;
                StartCoroutine(UpdateState());
            }
        }

    
        public void Pause()
        {
            playWhenReady = false;
            ActiveAudioSource.Pause();
            State = PlayerState.Paused;
            bUpdate = false;
        }

        public void Skip()
        {
            if (nextAsset != null)
            {
                LoadAudioClipWithFade(nextAsset);
            }
            else
            {
                State = PlayerState.WaitingForItem;
                OnPlayCompleted?.Invoke(currentAsset.play);
                ActiveAudioSource.Stop();
                currentAsset = null;
            
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
            audioFileList.Clear();
            nextAsset = null;
        }

        public void FlushAndIncludeCurrent()
        {
#if UNITY_EDITOR
            Debug.LogFormat("Flushing");
#endif
            if(State == PlayerState.Playing)
            {
                OnPlayCompleted?.Invoke(currentAsset.play);
                SetVolumeOfAllAudioSourcesToZero();
                State = PlayerState.WaitingForItem;
            }
            else if(State == PlayerState.Stalled || State == PlayerState.ReadyToPlay)
            {
                State = PlayerState.WaitingForItem;
            }

            StopAndFlushAllAudioSources();
        
            lUnityWebRequest?.Abort();
            audioFileList.Clear();
            currentAsset = null;
            nextAsset = null;
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

        private void StopAndFlushAudioSource(AudioSource audioSource)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        private IEnumerator UpdateState()
        {
            while (bUpdate)
            {
                if (ActiveAudioSource.isPlaying && State != PlayerState.Playing)
                {
                    State = PlayerState.Playing;
                }

                if (ActiveAudioSource.isPlaying)
                {
                    CurrentPlayTime = ActiveAudioSource.time;
                
                    OnProgressUpdate?.Invoke(CurrentPlay, ActiveAudioSource.time, CurrentPlayDuration);
                }

                if (currentAsset != null && nextAsset != null)
                {
                    var msUntilFade = CurrentPlayDuration - FadeDuration - CurrentPlayTime - currentAsset.play.AudioFile.trimEnd;
                    if (msUntilFade <= 0)
                    {
                        LoadAudioClipWithFade(nextAsset);
                    }
                }
                else if(currentAsset != null && nextAsset == null)
                {
                    if (!ActiveAudioSource.isPlaying && (ActiveAudioSource.time == 0f)) {
                        // The track ended
                        State = PlayerState.WaitingForItem;
                        OnPlayCompleted?.Invoke(currentAsset.play);
                        StopAndFlushAudioSource(ActiveAudioSource);
                        // Is the next song playing
                        currentAsset = null;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
       
        }

        private IEnumerator LoadNext()
        {
            if (audioFileList.Count > 0 && nextAsset == null)
            {
                var audioTrack = audioFileList.Dequeue();
                
                nextAsset = new PlayAndClip();
            
                if(!ActiveAudioSource.isPlaying)
                {
                    State = PlayerState.Stalled;
                }
                // start loading up the song
            
                lUnityWebRequest = UnityWebRequestMultimedia.GetAudioClip(audioTrack.AudioFile.Url, AudioType.MPEG); // TODO: Ask about AudioType
                yield return lUnityWebRequest.SendWebRequest();
            
                AudioClip clip = null;

                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(lUnityWebRequest);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogErrorFormat("{0}", e.Message);
#endif
                    if (!e.Message.Equals("Cannot access the .audioClip property of an aborted DownloadHandlerAudioClip"))
                    {
                        OnPlayFailed?.Invoke(audioTrack);
                    }
                    yield break;
                }

                lUnityWebRequest = null;
            
                if (!clip)
                {
                    OnPlayFailed?.Invoke(audioTrack);
                    yield break;
                }
            
                // Ready to play

                if (nextAsset == null)
                {
                    yield break;
                }
            
                nextAsset.play = audioTrack;
                nextAsset.clip = clip;

                nextAsset.clip.LoadAudioData();
                StartCoroutine(CheckLoadState());

            }
        }

        private IEnumerator CheckLoadState()
        {
            if (nextAsset == null)
            {
                yield break;
            }
        
            while(nextAsset.clip.loadState != AudioDataLoadState.Loaded)
            {
                yield return new WaitForSeconds(0.2f);
            }
            
            OnPlayReadyForPlayback?.Invoke(nextAsset.play);
        
            if (State == PlayerState.Uninitialized)
            {
                State = PlayerState.ReadyToPlay;
            }
            
            if (State == PlayerState.ReadyToPlay || State == PlayerState.WaitingForItem || State == PlayerState.Stalled )
            { 
                if(playWhenReady)
                {
                    LoadAudioClipWithFade(nextAsset);
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
            if (currentAsset != null)
            {
                OnPlayCompleted?.Invoke(currentAsset.play);
                _faders[0] = FadeOutAudioSourceNoAlloc(ActiveAudioSource, FadeDuration, 0.0f);
                StartCoroutine(_faders[0]);
            }
        
            currentAsset = asset;
            nextAsset = null;
            CurrentPlayDuration = currentAsset.clip.length;

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
            OnPlayItemBeganPlayback?.Invoke(currentAsset.play);
            _faders[1] = FadeInAudioSourceNoAlloc(nextAudioSource, FadeDuration, Volume);
            StartCoroutine(_faders[1]);

            //Register new active player
            _activeAudioSourceIndex = nextPlayer;
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
            // Is the next song playing
            if (!ActiveAudioSource.isPlaying)
            {
                State = PlayerState.WaitingForItem;
            }
        }

        private IEnumerator FadeInAudioSourceNoAlloc(AudioSource audioSource, float duration, float targetVolume)
        {
            yield return FadeAudioSourceNoAlloc(audioSource, duration, targetVolume);
            
            _faders[1] = null;
        }

        private IEnumerator FadeAudioSourceNoAlloc(AudioSource player, float duration, float targetVolume)
        {
            //Calculate the steps
            int steps = (int)(noOfVolumeStepsPerSecond * duration);
            float stepTime = duration / steps;
            float stepSize = (targetVolume - player.volume) / steps;

            //Fade now
            for (int i = 0; i < steps; i++)
            {
                player.volume += stepSize;
                yield return new WaitForSeconds(stepTime);
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