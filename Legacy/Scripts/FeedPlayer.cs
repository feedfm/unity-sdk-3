using UnityEngine;
using System.Collections;
using System;

#if !UNITY_PS4
using UnityEngine.Networking;
#endif


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
public class FeedPlayer : MonoBehaviour
{
	public string Token = "";
	public string Secret = "";

	public ArrayList Stations
	{
		get => mSession.stations;
		private set { }
	}

	public float Volume 
	{
		get
		{
			return mPlayer.Volume;
		}
		set
		{
			if (value > 1)
			{
				mPlayer.Volume = 1;
			}
			else mPlayer.Volume = value;
		}
	} 
	public bool Available => mSession.Available;

	public float CrossFadeDuration
	{
		get
		{
			return mPlayer.FadeDuration;
		}
		set
		{
			mPlayer.FadeDuration = value;
		}
	} 
	public Play CurrentPlay => mPlayer.CurrentPlay;

	private PlayerState _state;
	public PlayerState PlayState
	{
		get => _state;
		private set
		{
			if (_state == value) return;
			_state = value;
			OnStateChanged?.Invoke(_state);
		}
	}

	private bool _iExaustedRaised = false;

	private MixingAudioPlayer mPlayer;
	private Session mSession;

	public event StationDelegate OnStationChanged;
	public event SessionDelegate OnSession;
	public event StateHandler OnStateChanged;
	public event PlayHandler OnPlayReadyForPlayback;		
	public event PlayHandler OnPlayStarted;		
	public event PlayHandler OnPlayCompleted;		
	public event ProgressHandler OnProgressUpdate;

	public void Awake() {
		
		mPlayer =  gameObject.AddComponent<MixingAudioPlayer>();
		mSession = gameObject.AddComponent<Session>();
		mSession.token = Token;
		mSession.secret = Secret;
		mSession.onNextPlayFetched += OnNextPlayFetched;
		mSession.onPlaysExhausted += OnPlaysExhausted;
		mSession.onSession += OnSessionAvailibilityChanged;
		mSession.OnSkipRequestCompleted += OnSkipRequestCompleted;
		
		mPlayer.OnPlayReadyForPlayback += onPlayReadyForPlayback;
		mPlayer.OnPlayItemBeganPlayback += onPlayItemBeganPlayback;
		mPlayer.OnPlayCompleted += onPlayCompletedByPlayer;
		mPlayer.OnPlayFailed += onPlayFailed;
		mPlayer.OnProgressUpdate += onProgressUpdate;
		mPlayer.OnStateChanged += onPlayerStateChanged;
		StartCoroutine(mSession.FetchSession());
	}

	private Play skipedPlay = null;
	private void OnSkipRequestCompleted(bool issuccess)
	{
		if (issuccess)
		{
			skipedPlay = CurrentPlay;
			mPlayer.Skip();
			//mSession.RequestNext();
		}
	}

	// MixingAudioPlayer events
	private void onPlayerStateChanged(PlayerState state)
	{
		PlayState = state;
	}
	private void onPlayReadyForPlayback(Play play)
	{
		OnPlayReadyForPlayback?.Invoke(play);
	}
	private void onPlayItemBeganPlayback(Play play)
	{
		mSession.ReportPlayStarted(play);
		OnPlayStarted?.Invoke(play);
	}
	private void onPlayCompletedByPlayer(Play play)
	{
		
		OnPlayCompleted?.Invoke(play);
		if (skipedPlay != null && play.Id.Equals(skipedPlay.Id))
		{
			skipedPlay = null;
			return;
		}
		mSession.ReportPlayCompleted(play);
		if (_iExaustedRaised)
		{	//Set the play state when all playback is complete
			PlayState = PlayerState.Exhausted;
			_iExaustedRaised = false;
		}
	}
	private void onPlayFailed(Play play)
	{
		mSession.RequestInvalidate(play);
	}
	private void onProgressUpdate(Play play, float progress, float duration)
	{
		OnProgressUpdate?.Invoke(play,progress,duration);
	}
	
	
	// Public interface
	public void RequestNewClientID(ClientDelegate clientDelegate)
	{
		StartCoroutine(mSession.RequestNewClientID(clientDelegate));
	}

	public String GetClientID()
	{
		return mSession.ClientId;
	}
	
	public void SetClientID(string clientID)
	{
		if(clientID.StartsWith(Session.EXPORT_CLIENT_ID_PREFIX))
		{
			mSession.ClientId = clientID;
			
		}
	}
	
	public void Play() {
		if (PlayState == PlayerState.Uninitialized)
		{
			throw new Exception("Tried to begin playback before player is Available");
		}
		if(PlayState == PlayerState.Unavailable)
		{
			throw new Exception("Can't begin playback when player is UnAvailable");
		}
		mPlayer.Play();
	}
	
	public void Pause() {
		
		mPlayer.Pause();
		mSession.ReportPlayElapsed(mPlayer.CurrentPlayTime);

	}
	
	public Station ActiveStation {
		get => mSession._activeStation;

		set
		{
			var oldState = PlayState;
			if (mSession._activeStation == value) {
				return;
			}

			if (PlayState == PlayerState.Exhausted)
			{
				PlayState = PlayerState.WaitingForItem;
			}
			mPlayer.FlushAndIncludeCurrent();
			mSession.Reset();
			mSession._activeStation = value;
			if(oldState != PlayerState.Uninitialized)
				OnStationChanged?.Invoke(value);
			mSession.RequestNext();

		}
	}
	public void Skip() {
		if (!mSession.HasActivePlayStarted()) {
			// can't skip non-playing song
			return;
		}

		mSession.RequestSkip();
	}
	
	private void OnNextPlayFetched(Play play) {
		
		mPlayer.AddAudioAsset(play);
	}

	/*
	 * Take us out of pause if we've run out of songs
	 */

	private void OnPlaysExhausted(Session s)
	{
		if (mPlayer.State == PlayerState.WaitingForItem && mPlayer.isEmpty())
		{
			PlayState = PlayerState.Exhausted;
		}
		else
		{
			_iExaustedRaised = true;
		}
	}

	private void OnSessionAvailibilityChanged(bool isAvailable, String errMessage)
	{
		if(isAvailable) ActiveStation = (Station)mSession.stations[0];
		PlayState = isAvailable ? PlayerState.ReadyToPlay : PlayerState.Unavailable;
		OnSession?.Invoke(isAvailable, errMessage);
		
	}

	/* 
	 * Keep track of when the audio isn't playing paused, but just in the
	 * background
	 */

	void OnApplicationPause(bool pauseState) {		
		
	}

	public bool CanSkip()
	{
		return mSession.MaybeCanSkip();
	}
}


