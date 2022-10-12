using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Legacy.Scripts
{
    public sealed class MixingAudioPlayer : MonoBehaviour
    {
    	
        public delegate void PlayHandler(Play play);
        public delegate void ProgressHandler(Play play, float progress, float duration);
        public delegate void StateHandler(PlayerState state);

        public event StateHandler OnStateChanged;
        public event PlayHandler OnPlayReadyForPlayback;		
        public event PlayHandler OnPlayItemBeganPlayback;		
        public event PlayHandler OnPlayCompleted;		
        public event PlayHandler OnPlayFailed;
        public event ProgressHandler OnProgressUpdate;

        private PlayerState _state;
    
        public PlayerState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                OnStateChanged?.Invoke(_state);
            }
        }
    
        public bool TrimmingEnabled = true;
        public float FadeDuration = 4.0f;
        public float CurrentPlayTime;

        public Play CurrentPlay => currentAsset?.play;

        public float CurrentPlayDuration;

     
        [Range(0.0f, 1.0f)]
        private float _volume = 1.0f;
        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }
        // private

        private AudioSource[] _player;
        private IEnumerator[] fader = new IEnumerator[2];
        private int ActivePlayer = 0;
        private int noOfVolumeStepsPerSecond = 60;
        private Queue audioFileList = new Queue();
        private bool playWhenReady = false;
        private PlayAndClip currentAsset;
        private PlayAndClip nextAsset;
        private bool bUpdate;
        private UnityWebRequest lUnityWebRequest;
    
        class PlayAndClip
        {
            public Play play;
            public AudioClip clip;
        }
        public void Awake() {
        
            _player = new AudioSource[2]{
                gameObject.AddComponent<AudioSource>(),
                gameObject.AddComponent<AudioSource>()
            };
            bUpdate = false;
            //Set default values
            foreach (AudioSource s in _player)
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0.0f;
            }
        }

        public bool isEmpty()
        {
            return audioFileList.Count == 0 && nextAsset == null && currentAsset == null;
        }
        public void AddAudioAsset(Play play)
        {
            Debug.Log("Add Asset" + play.AudioFile.TrackTitle);
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
                
                    if (nextAsset != null) loadAudioClipWithFade(nextAsset);
                    else StartCoroutine(LoadNext());
                
                    break;
                case PlayerState.Paused:
                    if (currentAsset != null)
                    {
                        playWhenReady = true;
                        _player[ActivePlayer].Play();
                    }
                    else if(nextAsset != null)
                    {
                        loadAudioClipWithFade(nextAsset);
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
            _player[ActivePlayer].Pause();
            State = PlayerState.Paused;
            if (bUpdate)
            {
                bUpdate = false;
            }
        }

        public void Skip()
        {
            if (nextAsset != null)
            {
                loadAudioClipWithFade(nextAsset);
            }
            else
            {
                State = PlayerState.WaitingForItem;
                OnPlayCompleted?.Invoke(currentAsset.play);
                _player[ActivePlayer].Stop();
                currentAsset = null;
            
            }
        }

        public void SeekTo(float seconds)
        {
            if (seconds > CurrentPlayTime)
            {
                _player[ActivePlayer].time = seconds;
            }
        }
    
        public void Flush()
        {
            audioFileList.Clear();
            nextAsset = null;
        }

        public void FlushAndIncludeCurrent()
        {
            Debug.Log("Flushing");
            if(State == PlayerState.Playing)
            {
                OnPlayCompleted?.Invoke(currentAsset.play);
                _player[0].volume = 0;
                _player[1].volume = 0;
                State = PlayerState.WaitingForItem;
            }
            else if(State == PlayerState.Stalled || State == PlayerState.ReadyToPlay)
            {
                State = PlayerState.WaitingForItem;
            }
            _player[0].Stop();
            _player[1].Stop();
            _player[0].clip = null;
            _player[1].clip = null;
        
            lUnityWebRequest?.Abort();
            audioFileList.Clear();
            currentAsset = null;
            nextAsset = null;
        }

        private IEnumerator UpdateState()
        {
            while (bUpdate)
            {
                // update state 
                if (_player[ActivePlayer].isPlaying && State != PlayerState.Playing)
                {
                    State = PlayerState.Playing;
                }

                if (_player[ActivePlayer].isPlaying)
                {
                    CurrentPlayTime = _player[ActivePlayer].time;
                
                    OnProgressUpdate?.Invoke(CurrentPlay, _player[ActivePlayer].time, CurrentPlayDuration);
                }

                if (currentAsset != null && nextAsset != null)
                {
                    var msUntilFade = CurrentPlayDuration - FadeDuration - CurrentPlayTime - currentAsset.play.AudioFile.trimEnd;
                    if (msUntilFade <= 0)
                    {
                        loadAudioClipWithFade(nextAsset);
                    }
                }
                else if(currentAsset != null && nextAsset == null)
                {
                    if (!_player[ActivePlayer].isPlaying && (_player[ActivePlayer].time == 0f)) {
                        // The track ended
                        State = PlayerState.WaitingForItem;
                        OnPlayCompleted?.Invoke(currentAsset.play);
                        _player[ActivePlayer].clip = null;
                        _player[ActivePlayer].Stop();
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
                Play play = (Play) audioFileList.Dequeue();

            
                nextAsset = new PlayAndClip();
            
                if(!_player[ActivePlayer].isPlaying)
                {
                    State = PlayerState.Stalled;
                }
                // start loading up the song
            
                lUnityWebRequest = UnityWebRequestMultimedia.GetAudioClip(play.AudioFile.Url, AudioType.MPEG);
                yield return lUnityWebRequest.SendWebRequest();
            
                AudioClip clip = null;

                try
                {
                    clip = DownloadHandlerAudioClip.GetContent(lUnityWebRequest);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    if (!e.Message.Equals("Cannot access the .audioClip property of an aborted DownloadHandlerAudioClip"))
                    {
                        OnPlayFailed?.Invoke(play);
                    }
                    yield break;
                }

                lUnityWebRequest = null;
            
                if (clip == null)
                {
                    OnPlayFailed?.Invoke(play);
                    //Debug.Log("No clip");
                    yield break;
                }
            
                // Ready to play

                if (nextAsset == null) yield break;
            
                nextAsset.play = play;
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
                if(playWhenReady) loadAudioClipWithFade(nextAsset);
                else
                {
                    State = PlayerState.ReadyToPlay;
                }
            }
        }
    
        private void loadAudioClipWithFade(PlayAndClip asset)
        { 
            //Prevent fading the same clip on both players 
            if (asset.clip == _player[ActivePlayer].clip)
            {
                return;
            }
        
            //Kill all ongoing fades
            foreach (IEnumerator i in fader)
            {
                if (i != null)
                {
                    StopCoroutine(i);
                }
            }
            //Fade-out the active play, if it is not silent (eg: first start)
            if (currentAsset != null)
            {
                OnPlayCompleted?.Invoke(currentAsset.play);
                fader[0] = FadeAudioSource(_player[ActivePlayer], FadeDuration, 0.0f, (player) =>
                {
                    // Song completed
                    fader[0] = null;
                    player.clip = null;
                    player.Stop();
                    // Is the next song playing
                    if (!_player[ActivePlayer].isPlaying)
                    {
                        State = PlayerState.WaitingForItem;
                    }
                });
                StartCoroutine(fader[0]);
            }
        
            currentAsset = asset;
            nextAsset = null;
            CurrentPlayDuration = currentAsset.clip.length;

            //Fade-in the new clip
            int NextPlayer = (ActivePlayer + 1) % _player.Length;
            _player[NextPlayer].clip = asset.clip;
            _player[NextPlayer].loop = false;
            if (asset.play.AudioFile.trimStart != 0)
            {
                _player[NextPlayer].time = asset.play.AudioFile.trimStart;
            }
            _player[NextPlayer].Play();
            // Song started
            OnPlayItemBeganPlayback?.Invoke(currentAsset.play);
            //State = PlayerState.Playing;
            fader[1] = FadeAudioSource(_player[NextPlayer], FadeDuration, Volume, (player) => {
                fader[1] = null;
            });
            StartCoroutine(fader[1]);

            //Register new active player
            ActivePlayer = NextPlayer;
        }
    
        private IEnumerator FadeAudioSource(AudioSource player, float duration, float targetVolume, Action<AudioSource> finishedCallback)
        {
            //Calculate the steps
            int Steps = (int)(noOfVolumeStepsPerSecond * duration);
            float StepTime = duration / Steps;
            float StepSize = (targetVolume - player.volume) / Steps;

            //Fade now
            for (int i = 1; i < Steps; i++)
            {
                player.volume += StepSize;
                yield return new WaitForSeconds(StepTime);
            }
            //Make sure the targetVolume is set
            player.volume = targetVolume;

            //Callback
            if (finishedCallback != null)
            {
                finishedCallback(player);
            }
        }

    }
}
