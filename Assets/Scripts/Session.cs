using System;
using System.Collections;
using System.Collections.Generic;
using FeedFM.Attributes;
using FeedFM.Extensions;
using FeedFM.Models;
using FeedFM.Utilities;
using UnityEngine;

namespace FeedFM
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
*      to true until we change to a different station
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
    
    [DisallowMultipleComponent]
    internal sealed class Session : MonoBehaviour
    {
        [SerializeField] private int _maxNumberOfRetries = 3;
        
        #region Events

        public event SessionDelegate onSession; // placement data was retrieved from the server
        public event PlayDelegate onNextPlayFetched; // a play has become active and is ready to be started
        public event SkipDelegate OnSkipRequestCompleted; // request to skip current song denied
        public event Handler onPlaysExhausted; // the server has no more songs for us in the current station

        #endregion

        #region Configuration

        [SerializeField, ReadOnly] private string _token = string.Empty;
        [SerializeField, ReadOnly] private string _secret = string.Empty;

        public Station _activeStation;

        #endregion

        /** Internal state **/
        private const string API_SERVER_BASE = "https://feed.fm/api/v2";
        private const string AUDIO_FORMATS = "m4a,mp3"; // default
        private string _baseClientId;

        /// <summary>
        /// 64 is a good balance between audio quality and bandwidth usage. 
        /// </summary>
        private const string MAX_BITRATE = "128";

        private Placement Placement { get; set; }
        public List<Station> stations { get; private set; }

        private SessionStatus _lastStatus;
        private PendingRequest _pendingRequest;
        private PendingRequest _pendingSessionRequest;
        public const string EXPORT_CLIENT_ID_PREFIX = "fmcidv1:";
        private string _fullClientId = string.Empty;

        public string ClientId
        {
            get => _fullClientId;
            set
            {
                _baseClientId = value.Substring(EXPORT_CLIENT_ID_PREFIX.Length);
                _fullClientId = string.Format("{0}{1}", EXPORT_CLIENT_ID_PREFIX, _baseClientId);
                PlayerPrefs.SetString("feedfm.client_id", _baseClientId);
            }
        }

        /// <summary>
        /// true if we've run out of music
        /// </summary>
        public bool exhausted { get; private set; }

        /// <summary>
        /// true if we have started music playback since startup or the last 'Tune'
        /// </summary>
        public bool startedPlayback { get; private set; }

        public bool Available { get; private set; }

        #region Public API

        public void Awake()
        {
            // we haven't started playing any music yet
            startedPlayback = false;
            _pendingSessionRequest = new PendingRequest();
            // pessimistically assume we're out of the US
            Available = false;
            ResetClientId();
        }

        public void Initialize(string token, string secret)
        {
            _token = token;
            _secret = secret;
        }


        public IEnumerator FetchSession()
        {
            Ajax ajax = new Ajax(Ajax.RequestType.POST, API_SERVER_BASE + "/session");

            while (_pendingSessionRequest.retryCount < _maxNumberOfRetries)
            {
                yield return StartCoroutine(SignedRequest(ajax));

                if (ajax.success)
                {
                    if (ajax.GetIsSuccessFromResponseSession())
                    {
                        Available = ajax.GetIsAvailableFromResponseSession();
                        if (Available)
                        {
                            _baseClientId = ajax.GetClientIDFromResponseSession();
                            PlayerPrefs.SetString("feedfm.client_id", _baseClientId);

                            Placement = ajax.GetPlacementFromResponse();

                            stations = ajax.GetStations();
                            onSession?.Invoke(true, string.Empty);
                            yield break;
                        }

                        onSession?.Invoke(false, ajax.GetSessionMessage());
                    }

                    yield break;
                }

                if (ajax.HasErrorCode(FeedError.InvalidRegion))
                {
                    onSession?.Invoke(false, "Invalid region");
                    yield break;
                }

                _lastStatus.retryCount++;
                yield return WaitForSecondsLibrary.TwoSeconds;
            }

            onSession?.Invoke(false, "Failed to establish connection");
        }

        public void RequestNext()
        {
            // do some async shizzle
            StartCoroutine(RequestNextPlay());
        }

        /// <summary>
        /// Tell the server we've started playback of the active song
        /// </summary>
        public void ReportPlayStarted(Play play)
        {
            startedPlayback = true;
            _lastStatus = new SessionStatus
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

        /// <summary>
        /// Tell the server how much of the song we've listened to
        /// </summary>
        public void ReportPlayElapsed(float seconds)
        {
            if (_lastStatus == null)
            {
                throw new Exception("Attempt to report elapsed play time, but the pay hasn't started");
            }

            Ajax ajax = new Ajax(Ajax.RequestType.POST, string.Format("{0}/play/{1}/elapse", API_SERVER_BASE, _lastStatus.play.Id));
            ajax.addParameter("seconds", seconds.ToString());

            StartCoroutine(SignedRequest(ajax));
        }

        /// <summary>
        /// Tell the server we completed playback of the current song
        /// </summary>
        public void ReportPlayCompleted(Play play)
        {
            StartCoroutine(CompletePlay(play));
        }

        /// <summary>
        /// Ask the server if we can skip the current song. This will ultimately trigger an 'onPlayCompleted' or 'onSkipDenied' event.
        /// </summary>
        public void RequestSkip()
        {
            if (_lastStatus == null)
            {
                return;
            }

            if (!_lastStatus.canSkip)
            {
                OnSkipRequestCompleted?.Invoke(false);
                return;
            }

            StartCoroutine(SkipPlay(_lastStatus.play));
        }

        public void RequestInvalidate(Play play)
        {
            StartCoroutine(InvalidatePlay(play));
        }

        #endregion


        /************** internal API ******************/


        /// <summary>
        /// Send an ajax request to the server along with authentication information
        /// </summary>
        private IEnumerator SignedRequest(Ajax ajax)
        {
            // add in authentication header
            ajax.addHeader("Authorization",
                "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _token, _secret))));
            ajax.addHeader("Cookie", string.Empty);

            yield return StartCoroutine(ajax.Request());

            if (ajax.IsRequestError(FeedError.BadCredentials))
            {
                throw new Exception("Invalid credentials provided!");
            }

            ajax.DebugResponse();
        }

        /// <summary>
        /// Tell the server that we're starting playback of our active song
        /// </summary>
        private IEnumerator StartPlay(Play play)
        {
            while (true)
            {
                Ajax ajax = new Ajax(Ajax.RequestType.POST, string.Format("{0}/play/{1}/start", API_SERVER_BASE, play.Id));

                yield return StartCoroutine(SignedRequest(ajax));

                if (ajax.success)
                {
                    _lastStatus.canSkip = ajax.GetCanSkipFromResponseSession();
                    _lastStatus.play = play;
                    _lastStatus.started = true;

                    // start looking for the next song
                    yield return StartCoroutine(RequestNextPlay());

                    yield break;
                }
                else if (ajax.HasErrorCode(FeedError.PlaybackStarted))
                {
                    // we appear to have missed the response to the original start.
                    // assume the song was good
                    _lastStatus.canSkip = true;
                    _lastStatus.started = true;

                    // start looking for the next song
                    yield return StartCoroutine(RequestNextPlay());

                    yield break;
                }
                else
                {
                    _lastStatus.retryCount++;

                    yield return WaitForSecondsLibrary.TwoSeconds;

                    // try again later
                }
            }
        }

        /// <summary>
        /// Tell the server we've completed the current play, and make any pending play active.
        /// </summary>
        private IEnumerator CompletePlay(Play play)
        {
            Ajax ajax = new Ajax(Ajax.RequestType.POST, string.Format("{0}/play/{1}/complete", API_SERVER_BASE, play.Id));

            yield return StartCoroutine(SignedRequest(ajax));

            // we really don't care what the response was, really
        }

        /// <summary>
        /// Ask the server to skip the current song.
        /// </summary>
        private IEnumerator SkipPlay(Play play)
        {
            Ajax ajax = new Ajax(Ajax.RequestType.POST, string.Format("{0}/play/{1}/skip", API_SERVER_BASE, play.Id));

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
                Ajax ajax = new Ajax(Ajax.RequestType.POST, string.Format("{0}/play/{1}/invalidate", API_SERVER_BASE, play.Id));

                yield return StartCoroutine(SignedRequest(ajax));

                if (_lastStatus == null || _lastStatus.play != play)
                {
                    // nobody cares about this song any more
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

                yield return WaitForSecondsLibrary.GetWaitForSeconds(0.2f * (float) Math.Pow(2.0, retryCount));
            }
        }

        /// <summary>
        /// Ask the server to create a new play for us, and queue it up
        /// </summary>
        private IEnumerator RequestNextPlay()
        {
            if (_pendingRequest != null)
            {
                // We're already waiting for a play to come in
                yield break;
            }

            while (_baseClientId != null)
            {
                Ajax ajax = new Ajax(Ajax.RequestType.POST, API_SERVER_BASE + "/play")
                    .addParameter("formats", AUDIO_FORMATS)
                    .addParameter("client_id", _baseClientId)
                    .addParameter("max_bitrate", MAX_BITRATE);

                if (Placement != null)
                {
                    ajax.addParameter("placement_id", Placement.id.ToString());
                }

                if (_activeStation != null)
                {
                    ajax.addParameter("station_id", _activeStation.Id.ToString());
                }

                // let the rest of the code know we're awaiting a response
                _pendingRequest = new PendingRequest
                {
                    ajax = ajax,
                    retryCount = 0
                };

                yield return StartCoroutine(SignedRequest(ajax));

                if (_pendingRequest == null || _pendingRequest.ajax != ajax)
                {
                    // another request snuck in while waiting for the response to this one,
                    // so we don't care about this one any more - just quit
                    yield break;
                }
                else if (ajax.success)
                {
                    _pendingRequest = null;
                    onNextPlayFetched?.Invoke(ajax.GetPlayFromResponseSession());
                    yield break;
                }
                else
                {
                    switch (ajax.GetFeedError())
                    {
                        case FeedError.NoMoreMusic:
                        {
                            if (_lastStatus == null)
                            {
                                // ran out of music, and nothing else to play
                                exhausted = true;
                                onPlaysExhausted?.Invoke(this);
                            }

                            _pendingRequest = null;

                            yield break;
                        }
                        case FeedError.InvalidRegion:
                        {
                            // user isn't in the united states, so can't play anything
                            Available = false;
                            onSession?.Invoke(false, "Invalid Region");
                            yield break;
                        }
                        default:
                        {
                            // some unknown error 
                            _pendingRequest.retryCount++;

                            // wait for an increasingly long time before retrying
                            yield return WaitForSecondsLibrary.GetWaitForSeconds(0.5f * (float) Math.Pow(2.0, _pendingRequest.retryCount));
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// True if we're actively pulling audio from the server
        /// </summary>
        /// <returns></returns>
        public bool IsRequestingPlay()
        {
            return (_lastStatus != null) || (_pendingRequest != null);
        }

        /// <summary>
        /// True if we've got an active play and we've started playback
        /// </summary>
        public bool HasActivePlayStarted()
        {
            return (_lastStatus != null) && (_lastStatus.started);
        }

        /// <summary>
        /// Return the currently active play, or null
        /// </summary>
        public Play GetActivePlay() => _lastStatus?.play;

        /// <summary>
        /// Reset the cached client id. *for testing only!
        /// </summary>
        public void ResetClientId()
        {
            PlayerPrefs.DeleteKey("feedfm.client_id");
        }

        /// <summary>
        /// Ensure we've got a clientId
        /// </summary>
        private IEnumerator EnsureClientId()
        {
            if (_baseClientId != null)
            {
                yield break;
            }

            if (PlayerPrefs.HasKey("feedfm.client_id"))
            {
                // have one already, so use it
                _baseClientId = PlayerPrefs.GetString("feedfm.client_id");
                yield break;
            }
            // need to get an id

            while (true)
            {
                Ajax ajax = new Ajax(Ajax.RequestType.POST, API_SERVER_BASE + "/client");

                yield return StartCoroutine(SignedRequest(ajax));

                if (ajax.success)
                {
                    _baseClientId = ajax.GetClientIDFromResponseSession();

                    try
                    {
                        PlayerPrefs.SetString("feedfm.client_id", _baseClientId);
                    }
                    catch (PlayerPrefsException)
                    {
                        // ignore, 
                    }

                    yield break;
                }
                else if (ajax.HasErrorCode(FeedError.InvalidRegion))
                {
                    // user isn't in a valid region, so can't play anything
                    Available = false;
                    onSession?.Invoke(false, "Invalid Region");
                    yield break;
                }

                // no success, so wait bit and then try again
                yield return WaitForSecondsLibrary.OneSecond;
            }
        }

        public bool MaybeCanSkip()
        {
            return ((_lastStatus != null) && (_lastStatus.started) && (_lastStatus.canSkip));
        }

        public void Reset()
        {
            // abort any pending requests or plays
            _pendingRequest = null;
            // pretend we've got music available
            exhausted = false;
            _lastStatus = null;
            // no music has started yet
            startedPlayback = false;
        }

        public IEnumerator RequestNewClientID(ClientDelegate clientDelegate)
        {
            Ajax ajax = new Ajax(Ajax.RequestType.POST, API_SERVER_BASE + "/client");

            yield return StartCoroutine(SignedRequest(ajax));

            if (ajax.success)
            {
                _baseClientId = ajax.GetClientIDFromResponseSession();

                try
                {
                    clientDelegate.Invoke(_baseClientId);
                    PlayerPrefs.SetString("feedfm.client_id", _baseClientId);
                }
                catch (PlayerPrefsException)
                {
                    // ignore, 
                }

                yield break;
            }
            else if (ajax.HasErrorCode(FeedError.InvalidRegion))
            {
                // user isn't in a valid region, so can't play anything
                Available = false;
                onSession?.Invoke(false, "Invalid Region");
                yield break;
            }
        }
    }
}