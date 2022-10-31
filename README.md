
# FeedFM Unity SDK #

[![openupm](https://img.shields.io/npm/v/com.feedfm.unity-sdk?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.feedfm.unity-sdk/)

## Install ##

---

### Install via OpenUPM ###

The package is available on the [openupm registry](https://openupm.com/packages/com.feedfm.unity-sdk/). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.feedfm.unity-sdk
```

### Install via Package Manager ###

- open Edit/Project Settings/Package Manager
- add a new Scoped Registry (or edit the existing OpenUPM entry)
  - Name: `package.openupm.com`
  - URL: <https://package.openupm.com>
  - Scope(s): `com.feedfm.unity-sdk`
- click **Save** (or **Apply**)
- open Window/Package Manager
- click **+**
- select **Add package by name..**. or **Add package from git URL...**
- paste `com.feedfm.unity-sdk into name`
- paste `0.0.1` into version
- click **Add**

Alternatively, merge the snippet to Packages/manifest.json

```json
{
    "scopedRegistries": [
        {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": [
                "com.feedfm.unity-sdk"
            ]
        }
    ],
    "dependencies": {
        "com.feedfm.unity-sdk": "0.0.1"
    }
}
```

## Getting started ##

---


To begin playing the music, create a new GameObject and attach the FeedPlayer.cs script to it. Then configure the player by setting token and secret and optional values like crossfade duration etc in the unity editor or by editing the script.

### Example Usage
The included "Dancing Robot" demo shows an example on how to interact with FeedPlayer.cs. MusicPlayerView.cs manages the entire UI for the demo subscribing to and calling methods on a FeedPlayer object.


### Playing music ###

```C#
[SerializeField] FeedPlayer _feedPlayer = null;

private void Awake()
{
    _feedPlayer.OnSession += (available, errMessage) =>
    {
   
        if (available)
        {
            _feedPlayer.Play();
        }
        else
        {
            Debug.Log(errMessage);
        }
    };

    // Optional events...
}

```

### Optional Events to Subscribe to

```C#
    
_feedPlayer.OnStationChanged += station =>
{
    Debug.Log(string.Format("New station {0} is set", station.Name));
};

_feedPlayer.OnPlayStarted += play =>
{
    Debug.Log("play started "+play.AudioFile.TrackTitle);
};

_feedPlayer.OnPlayReadyForPlayback += play =>
{
    Debug.Log(string.Format("play ready for playback {0}", play.AudioFile.TrackTitle));
};

_feedPlayer.OnStateChanged += state =>
{
    Debug.Log(string.Format("State Changed to {0}", state));
};

_feedPlayer.OnProgressUpdate += (play, progress, duration) =>
{
    Debug.Log(string.Format("{0}progress changed to {1} duration {2}", play.AudioFile.TrackTitle, progress, duration));
};
  
```

FeedPlayer.cs is responsible for retrieving music from the Feed.fm servers and playing them on the local device. The player can be assigned an active station from list of stations returned by the server.

To make sure the music begins when requested user should wait for OnPlayReadyForPlayback event before calling play().

Player methods are almost all asynchronous. Thus, the API is event based. Subscribe to the FeedPlayer.cs events to be notified of changes in the player state.

The player must successfully contact the Feed.fm
servers to retrieve a list of available streaming stations and the OnSession
helps with determining when music becomes available or playback.

To use this class, add the FeedPlayer to a GameObject in the scene. All dependencies will be handled automatically. You just need to make sure to set the 'token' and 'secret' values
to what you were given on feed.fm.

### FeedPlayer states.

- Uninitialized: This is the initial default state, the player is not ready to play music until it transitions out of this state.
- Unavailable: There is no music available for this user.
- ReadyToPlay: The player is ready to play music.
- Stalled: The song is being pulled down.
- Playing / Paused: We have a song that we've begun playback of and we're either currently playing the song or the song is paused.
- WaitingForItem:  The Player is waiting for feed.fm servers so return a song.
- Exhausted: The server has run out of music in the current station that passes DMCA playback rules. The station must be changed, to request more music.

The player has following events that can be subscribed to

    StationDelegate OnStationChanged   // A new station was set
    SessionDelegate OnSession          // A session is not available or unavailable. 
    StateHandler OnStateChanged        // Player state has changed. 
    PlayHandler OnPlayReadyForPlayback // An Item is loaded and ready for playback. 
    PlayHandler OnPlayStarted          // A song has started plaback
    PlayHandler OnPlayCompleted        // A song has completed playback. 
    ProgressHandler OnProgressUpdate   // A event sent out every second to update the progress of the currently playing song. 

### FeedPlayer public API.

#### Properties
- ActiveStation { get; set; }
- CrossFadeDuration { get; set; }
#### Methods
- Play()
- Pause()
- Skip()
- State get()
- StationList get()