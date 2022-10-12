using System;
using System.Collections;
using UnityEngine;

namespace Legacy.Scripts
{
	/*
*  This in an Internal class that you should never need to look into.
* 
* 'Session' talks to the Feed.fm servers and maintains the current active song
*  
* 
* On creation, the session instance must minimally be given an API 'token' and 'secret'
* that tell it where to pull music from. 
* 
* Music is organized into 'stations' that are grouped under 'placements'. The 
* 'token' provided to the server maps to a default placement, and a placement has
* a default station in it.
* 
* To begin pulling in music, the Tune() method should be called. This method will
* asynchronously obtain credentials from the server, identify the placement and
* station to pull music from, and then start pulling 'plays' from the server.
* 
* After the above events are sent out, this instance asks the server to create a new 'play'.
* A 'play' holds the details of a single music track and a pointer to an audio file.
* The returned play is called the 'active play'. This class sends an event when
* the play is returned from the server.
* 
* Once there is an active play, you may use the 'ReportPlayStarted', 'ReportPlayElapsed', and
* 'ReportPlayCompleted' calls to inform the server about the status of playback.
* 
* Additionally, you can 'RequestSkip' to ask the server if the user may skip the current song
* or 'RequestInvalid' to tell the server we're unable to play the current song for some technical
* reason. If the server disallows a skip request, an 'onSkipDenied' event will be triggered
* and nothing else will change. If the server allows a skip or invalid request, this object will
* act as if a 'ReportPlayCompleted' call was made.
* 
* Calling 'ReportPlayCompleted' causes this object to discard the current active play, send
* out an 'onPlayCompleted' event, and then try to request a new play from the server. (well,
* technically this object will try to queue up the next play while you're working with the
* current play, but you don't really need to know that). Eventually you'll get another
* 'onPlayActive' just as when you first called 'Tune()'.
*  
* Because there are DMCA playback rules that prevent us from playing too many instances of
* a particular artist, album, or track, there may be a time when Feed.fm can't find any more 
* music to return in the current station. In that case, you'll get back an 'onPlaysExhausted'
* event instead of an 'onPlayActive'. If you change stations or placements you might be
* able to find more music - so you can change the stationId or placementId and then call 'Tune()'
* again to get things moving again.
* 
* This class uses the 'SimpleJSON' package to represent the JSON responses from the server
* in memory and converts them to c# classes.
* 
* Some misc properties you can inspect:
*   - stations - list of stations in the current Session
*   - station - the current station we're tuned to (if any)
*   - exhausted - if we've run out of music from the current station, this will be set
*      to true until we change to a diffrent station
* 
*   - MaybeCanSkip() - returns true if we think the user can skip the current song. Note
*      that we don't really know for sure if we can skip a song until the server tells us.
*      If this returns false, then the user definitely can't skip the current song.
* 
*   - ResetClientId() - when testing, you will often get an 'onPlaysExhausted' if you skip
*      through a bunch of songs in short order. This call will reset your client id and
*      effectively erase your play history, freeing you to play music again. *NOTE* it
*      is a violation of our terms of service to use this on production apps to allow users
*      to avoid playback rules.
* Some things to keep in mind:
*   - A user might change IP addresses in the middle of a session and go from being in the US
*     to not being in the US - in which case could get an 'OnSession(false)' event at just about any time.
*     It's not a common thing, but it could happen.
* 
*   - The JSONNode objects returned are straight from the Feed.fm server. Look at the REST API
*     responses to see how things are structured:
* 
*     bitrate can be set to max of 320, but this can cause delay in loading music as files are much bigger.  
* 
*/


	class PendingRequest {
		public Ajax ajax;  // outstanding POST /play request
		public int retryCount; // number of times we've retried this request
	}

	class SessionStatus {
		public Play play;  // POST /play response from server
		public Boolean started; // true if we started playback
		public Boolean canSkip; // true if we can skip this song
		public int retryCount;  // number of times we've unsuccessfully asked server if we can start play
	}

	public class Session : MonoBehaviour
	{

		/** Events **/

		public delegate void SessionDelegate(bool isAvailable, String errMsg);
		public delegate void SkipDelegate(bool isSuccess);

		public delegate void playDelegate(Play data);

		public delegate void Handler(Session obj);

		public event SessionDelegate onSession; // placement data was retrieved from the server
		public event playDelegate onNextPlayFetched; // a play has become active and is ready to be started
		public event SkipDelegate OnSkipRequestCompleted; // request to skip current song denied
		public event Handler onPlaysExhausted; // the server has no more songs for us in the current station


		/** Configuration **/

		public string token = "";

		public string secret = "";

		public Station _activeStation;
	
		/** Internal state **/

		private string apiServerBase = "https://feed.fm/api/v2";

		private string formats = "m4a,mp3"; // default
	
		private  string clientId;
		private string maxBitrate = "128"; // 64 is a good balance between audio quality and bandwidth usage. 

		private Placement placement { get; set; }

		public ArrayList stations { get; private set; }
	
		private SessionStatus lastStatus;
		private PendingRequest pendingRequest;
		private PendingRequest pendingSessionRequest;

		public const string EXPORT_CLIENT_ID_PREFIX = "fmcidv1:";

		public string ClientId
		{
			get
			{
				return EXPORT_CLIENT_ID_PREFIX + clientId;
			}
			set
			{
				clientId = value.Substring(EXPORT_CLIENT_ID_PREFIX.Length);
				PlayerPrefs.SetString("feedfm.client_id", clientId);
			
			}
		}
		public bool exhausted
		{
			// true if we've run out of music
			get;
			private set;
		}

		public bool startedPlayback
		{
			// true if we have started music playback since startup or the last 'Tune'
			get;
			private set;
		}

		public bool Available { get; private set; }

		/************** public API ******************/

		public virtual void Awake()
		{
			// we haven't started playing any music yet
			startedPlayback = false;
			pendingSessionRequest = new PendingRequest();
			// pessimistically assume we're out of the US
			Available = false;
			ResetClientId();
		}


		public IEnumerator FetchSession()
		{
			Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/session");

			while (pendingSessionRequest.retryCount < 3)
			{

				yield return StartCoroutine(SignedRequest(ajax));

				if (ajax.success)
				{

					bool isSuccess = ajax.response["success"].AsBool;
					if (isSuccess)
					{
						JSONClass session = ajax.response["session"].AsObject;

						Available = session["available"].AsBool;
						if (Available)
						{
							clientId = session["client_id"].Value;
							PlayerPrefs.SetString("feedfm.client_id", clientId);

							JSONClass jsonPlacement = ajax.response["placement"].AsObject;
							placement = new Placement
							{
								id = jsonPlacement["id"].AsInt,
								Name = jsonPlacement["name"].Value,
								Options = jsonPlacement["options"].AsObject
							};

							JSONArray stationList = ajax.response["stations"].AsArray;
							ArrayList arrayList = new ArrayList();

							for (int i = 0; i < stationList.Count; i++)
							{
								arrayList.Add(parseStation(stationList[i].AsObject));
							}

							stations = arrayList;
							onSession?.Invoke(true, "");
							yield break;
						}
					
						String message = session["message"].Value;
						onSession?.Invoke(false, message);
					
					}

					yield break;

				}
				if (ajax.error == (int) FeedError.InvalidRegion)
				{

					onSession?.Invoke( false,"Invalid region");
					yield break;

				}
			
				lastStatus.retryCount++;
				yield return new WaitForSeconds(2.0f);
			
			}
		
			onSession?.Invoke( false, "Failed to establish connection");
		}


		private Station parseStation(JSONClass jStation)
		{
			Station station = new Station();
			station.Id = jStation["id"].AsInt;
			station.Name = jStation["name"].Value;
			station.CanLike = jStation["can_like"].AsBool;
			station.CanSkip = jStation["can_skip"].AsBool;
			station.Options = jStation["options"].AsObject;
			station.PreGain = jStation["pre_gain"].AsFloat;
			station.IsSinglePlay = jStation["single_play"].AsBool;
			station.IsOnDemand = jStation["on_demand"].AsBool;
			station.IsTypeOffline = jStation["on_demand"].AsBool;

			if (jStation["last_updated"].Value.Length != 0) {
				station.LastUpdated = DateTime.Parse(jStation["last_updated"].Value);
			}
			if (jStation["expire_date"].Value.Length != 0)
			{
				station.ExpiryDate = DateTime.Parse(jStation["expire_date"].Value);
			}

			if (jStation["last_play_start"].Value.Length != 0)
			{
				station.LastPlayStart = DateTime.Parse(jStation["last_play_start"].Value);
			}

			return station;
		}



		/*
	 * Start pulling in music
	 */

		public virtual void RequestNext()
		{
			// do some async shizzle
			StartCoroutine(RequestNextPlay());
		}

		/*
	 * Tell the server we've started playback of the active song
	 */

		public virtual void ReportPlayStarted(Play play)
		{
			startedPlayback = true;
			lastStatus = new SessionStatus
			{
				play = play,
				started = false
			};
			foreach (Station station in stations)
			{
				if (play.Station.Id == station.Id)
				{
					station.LastPlayStart = DateTime.Now;
				}
			}
			StartCoroutine(StartPlay(play));
		}

		/*
	 * Tell the server how much of the song we've listened to
	 */

		public virtual void ReportPlayElapsed(float seconds)
		{
			if (lastStatus == null)
			{
				throw new Exception("Attempt to report elapsed play time, but the pay hasn't started");
			}

			Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/play/" + lastStatus.play.Id + "/elapse");
			ajax.addParameter("seconds", seconds.ToString());

			StartCoroutine(SignedRequest(ajax));
		}

		/*
	 * Tell the server we completed playback of the current song
	 */

		public virtual void ReportPlayCompleted(Play play)
		{
			StartCoroutine(CompletePlay(play));
		}

		/*
	 * Ask the server if we can skip the current song. This will ultimately trigger an 'onPlayCompleted' or
	 * 'onSkipDenied' event.
	 */

		public virtual void RequestSkip()
		{
			if (lastStatus == null)
			{
				return;
			}

			if (!lastStatus.canSkip)
			{
				OnSkipRequestCompleted?.Invoke(false);
				return;
			}

			StartCoroutine(SkipPlay(lastStatus.play));
		}

		public virtual void RequestInvalidate(Play play)
		{

			StartCoroutine(InvalidatePlay(play));
		}

		/************** internal API ******************/

		/*
	 * Send an ajax request to the server along with authentication information 
	 */

		private IEnumerator SignedRequest(Ajax ajax)
		{
			// add in authentication header
			ajax.addHeader("Authorization",
				"Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token + ":" + secret)));
			ajax.addHeader("Cookie", "");

			yield return StartCoroutine(ajax.Request());

			if (!ajax.success && (ajax.error == (int) FeedError.BadCredentials))
			{
				throw new Exception("Invalid credentials provided!");
			}
			ajax.DebugResponse();

		}

		/*
	 * Tell the server that we're starting playback of our active song
	 */

		private IEnumerator StartPlay(Play play)
		{
			while (true)
			{
				Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/play/" + play.Id + "/start");

				yield return StartCoroutine(SignedRequest(ajax));

				if (ajax.success)
				{
					lastStatus.canSkip = ajax.response["can_skip"].AsBool;
					lastStatus.play = play;
					lastStatus.started = true;

					// start looking for the next song
					yield return StartCoroutine(RequestNextPlay());

					yield break;

				}
				else if (ajax.error == (int) FeedError.PlaybackStarted)
				{
					// we appear to have missed the response to the original start.
					// assume the song was good
					lastStatus.canSkip = true;
					lastStatus.started = true;

					// start looking for the next song
					yield return StartCoroutine(RequestNextPlay());

					yield break;

				}
				else
				{
					lastStatus.retryCount++;

					yield return new WaitForSeconds(2.0f);

					// try again later
				}

			}
		}

		/*
	 * Tell the server we've completed the current play, and make any pending
	 * play active.
	 */

		private IEnumerator CompletePlay(Play play)
		{
			Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/play/" + play.Id + "/complete");

			yield return StartCoroutine(SignedRequest(ajax));

			// we really don't care what the response was, really
		
		}

		/*
	 * Ask the server to skip the current song.
	 */

		private IEnumerator SkipPlay(Play play)
		{
			Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/play/" + play.Id + "/skip");

			yield return StartCoroutine(SignedRequest(ajax));

			if (ajax.success)
			{
				OnSkipRequestCompleted?.Invoke(true);
				yield break;

			}
		
			OnSkipRequestCompleted?.Invoke(false);

		
		}

		private IEnumerator InvalidatePlay(Play play)
		{
			int retryCount = 0;

			while (true)
			{
				Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/play/" + play.Id + "/invalidate");

				yield return StartCoroutine(SignedRequest(ajax));

				if ((lastStatus == null) || (lastStatus.play != play))
				{
					// nboody cares about this song any more
					yield break;
				}

				if (ajax.success)
				{
				
					// If nothing is queued up, that might be because we haven't tried to 'start'
					// this play yet, triggering the 'requestNextPlay'. So trigger it here.
					yield return StartCoroutine(RequestNextPlay());
				
					yield break;

				}

				retryCount++;

				yield return new WaitForSeconds(0.2f * (float) Math.Pow(2.0, retryCount));

			
			}
		}


	

		/*
	 * Ask the server to create a new play for us, and queue it up
	 */

		private IEnumerator RequestNextPlay()
		{
			if (pendingRequest != null)
			{
				// we're already waiting for a play to come in
				yield break;
			}

			while (clientId != null)
			{
				Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/play");
				ajax.addParameter("formats", formats);
				ajax.addParameter("client_id", clientId);
				ajax.addParameter("max_bitrate", maxBitrate);

				if (placement != null)
				{
					ajax.addParameter("placement_id", placement.id.ToString());
				}

				if (_activeStation != null)
				{
					ajax.addParameter("station_id", _activeStation.Id.ToString());
				}

				// let the rest of the code know we're awaiting a response
				pendingRequest = new PendingRequest
				{
					ajax = ajax,
					retryCount = 0
				};

				yield return StartCoroutine(SignedRequest(ajax));

				if ((pendingRequest == null) || (pendingRequest.ajax != ajax))
				{
					// another request snuck in while waiting for the response to this one,
					// so we don't care about this one any more - just quit
					yield break;
				}

				if (ajax.success)
				{
					pendingRequest = null;
					var play = parsePlay(ajax.response["play"]);
					onNextPlayFetched?.Invoke(play);
					yield break;
				}
				if (ajax.error == (int) FeedError.NoMoreMusic)
				{

					if (lastStatus == null)
					{
						// ran out of music, and nothing else to play
						exhausted = true;
						onPlaysExhausted?.Invoke(this);
					
					}

					pendingRequest = null;

					yield break;

				}
				if (ajax.error == (int) FeedError.InvalidRegion)
				{
					// user isn't in the united states, so can't play anything
					Available = false;
					onSession?.Invoke(false, "Invalid Region");
					yield break;

				}
				else
				{
					// some unknown error 
					pendingRequest.retryCount++;

					// wait for an increasingly long time before retrying
					yield return new WaitForSeconds(0.5f * (float) Math.Pow(2.0, pendingRequest.retryCount));

				}
			}
		}

		private Play parsePlay(JSONNode play)
		{
			var jfile = play["audio_file"].AsObject;

			var pl = new Play()
			{
				Id = play["id"].Value,
				AudioFile = parseAudioFile(jfile),
				Station = parseStation(play["station"].AsObject)

			};
			return pl;

		}

		private AudioFile parseAudioFile(JSONClass jfile)
		{
			var audioFile = new AudioFile()
			{
				Id = jfile["id"].AsInt,
				ArtistTitle = jfile["artist"].AsObject["name"].Value,
				ReleaseTitle = jfile["release"].AsObject["title"].Value,
				TrackTitle = jfile["track"].AsObject["title"].Value,
				Bitrate = jfile["bitrate"].Value,
				DurationInSeconds = jfile["duration_in_seconds"].AsFloat,
				IsDisliked = jfile["disliked"].AsBool,
				Codec = jfile["codec"].Value,
				Url = jfile["url"].Value,
				IsLiked = jfile["liked"].AsBool,
				MetaData = jfile["extra"].AsObject,
				ReplayGain = jfile["replay_gain"].AsFloat

			};
			return audioFile;
		}

		/*
	 * True if we're actively pulling audio from the server
	 */

		public bool IsRequestingPlay()
		{
			return (lastStatus != null) || (pendingRequest != null);
		}

		/*
	 * True if we've got an active play and we've started playback
	 */

		public bool HasActivePlayStarted()
		{
			return (lastStatus != null) && (lastStatus.started);
		}

		/*
	 * Return the currently active play, or null
	 */

		public Play GetActivePlay()
		{
			if (lastStatus != null)
			{
				return lastStatus.play;
			}
			else
			{
				return null;
			}
		}

		/*
	 * Reset the cached client id. *for testing only!*
	 */
	
	
		public void ResetClientId()
		{
			PlayerPrefs.DeleteKey("feedfm.client_id");
		}

		/*
	 * Ensure we've got a clientId
	 */

		private IEnumerator EnsureClientId()
		{
			if (clientId != null)
			{
				yield break;
			}

			if (PlayerPrefs.HasKey("feedfm.client_id"))
			{
				// have one already, so use it
				clientId = PlayerPrefs.GetString("feedfm.client_id");
				yield break;

			}
			// need to get an id

			while (true)
			{
				Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/client");

				yield return StartCoroutine(SignedRequest(ajax));

				if (ajax.success)
				{
					clientId = ajax.response["client_id"];

					try
					{
						PlayerPrefs.SetString("feedfm.client_id", clientId);

					}
					catch (PlayerPrefsException)
					{
						// ignore, 
					}

					yield break;

				}
				else if (ajax.error == (int) FeedError.InvalidRegion)
				{

					// user isn't in a valid region, so can't play anything
					Available = false;
					onSession?.Invoke(false, "Invalid Region");
					yield break;
				}

				// no success, so wait bit and then try again
				yield return new WaitForSeconds(1.0f);
			}
		
		}

		public bool MaybeCanSkip()
		{
			return ((lastStatus != null) && (lastStatus.started) && (lastStatus.canSkip));
		}

		public void Reset()
		{
			// abort any pending requests or plays
			pendingRequest = null;
			// pretend we've got music available
			exhausted = false;
			lastStatus = null;
			// no music has started yet
			startedPlayback = false;

		}

		public IEnumerator RequestNewClientID(ClientDelegate clientDelegate)
		{
			Ajax ajax = new Ajax(Ajax.RequestType.POST, apiServerBase + "/client");

			yield return StartCoroutine(SignedRequest(ajax));

			if (ajax.success)
			{
				clientId = ajax.response["client_id"];

				try
				{
					clientDelegate.Invoke(clientId);
					PlayerPrefs.SetString("feedfm.client_id", clientId);

				}
				catch (PlayerPrefsException)
				{
					// ignore, 
				}

				yield break;

			}
			else if (ajax.error == (int) FeedError.InvalidRegion)
			{

				// user isn't in a valid region, so can't play anything
				Available = false;
				onSession?.Invoke(false, "Invalid Region");
				yield break;
			}
		}
	}
}