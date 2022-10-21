
## FeedFM Unity SDK ##

#### Getting started ####

To begin playing the music, create a new gameobject and attach the FeedPlayer.cs script to it. Then configure the player by setting token and secret and optional values like crossfade duration etc in the unity editor or by editing the script.

Included PlayerGUI.cs script and PopupPlayerGUI.cs script show how to interact with the player and how to create a GUI for displaying the track metadata and other information.

UPM Package
---

### Install via OpenUPM

The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.feedfm.unity-sdk
```

### Playing music ###

```C#
FeedPlayer _feedPlayer; 
_feedPlayer = Object.FindObjectOfType<FeedPlayer>(); // Or assign reference in the inspector manually
_feedPlayer.OnSession += ( available, errMessage) => {
   
 if (available)
 {
   _feedPlayer.Play();
 }
 else
 {
  Debug.Log(errMessage);
 }
};
```

#### Optionally subscribe to events ###

```C#
    
_feedPlayer.OnStationChanged += ( station) => {
 Debug.Log("New station is set");
};

_feedPlayer.OnPlayStarted += ( play) => {
  Debug.Log("play started "+play.AudioFile.TrackTitle);
};

_feedPlayer.OnPlayReadyForPlayback += play =>
{
 Debug.Log("play ready for playback "+play.AudioFile.TrackTitle);
};

_feedPlayer.OnStateChanged += state =>
{
 Debug.Log("State Changed to "+ state);
};

_feedPlayer.OnProgressUpdate += (play, progress, duration) =>  {

 Debug.Log(play.AudioFile.TrackTitle+ "progress changed to " +progress + " duration " + duration);
};
  
```

FeedPlayer is the class that is collecticly responsible for retrieving music from the Feed.fm servers and playing them on the local device. The player can be assigned an active station from list of stations returned by the server.

To make sure the music begins when requested user should wait for OnPlayReadyForPlayback event before calling play

The player exposes its current State to assist with rendering playback state.

 Player methods are almost all asynchronous, and the player makes Delegates
 to allow clients to monitor events and changes in the player state.

 In most use cases, the player isn't useful until it has successfully contacted the Feed.fm
 servers to retrieve a list of available streaming stations, and the SessionDelegate OnSession
 helps with determining when music becomes available or playback.

 To use this class, first create an instance and set the 'token' and 'secret' values
 to what you were given on feed.fm.

States of Player and what they mean.

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

Following public methods/variables are exposed in FeedPlayer to be utilized for controling the player.

- Play()
- Pause()
- ActiveStation get() set(station)
- Skip()
- State get()
- FadeDuration get() set(duration)
- StationList get()