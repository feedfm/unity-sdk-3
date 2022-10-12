using UnityEngine;
using System.Collections;
using SimpleJSON;

public class PlayerGUI : MonoBehaviour {
	
	private Rect windowRect;
	public FeedPlayer audioPlayer;
	public Animator Animator;
	//public Animator Animator2;
	private string[] stationTitles;
	private int stationIndex;
	

	void Awake() {
	}

	// Use this for initialization
	void Start () {
		Application.runInBackground = true;

		if (Debug.isDebugBuild) {
			
		}

		// map station list to array of station names
		audioPlayer.OnSession += (available, errMessage) => {
			
			if (available)
			{
				stationTitles = new string[audioPlayer.Stations.Count];
				int i = 0;
				foreach (Station station in audioPlayer.Stations)
				{
					stationTitles[i] = station.Name;
					i++;
				}
				stationIndex = 0;
			}
			else
			{
				Debug.Log(errMessage);
			}
		};

		// figure out the index of the new station in the list of stations
		audioPlayer.OnStationChanged += ( station) => {
			if (audioPlayer.Stations == null) {
				return;
			}
			stationIndex = audioPlayer.Stations.IndexOf(station);

		};

		var windowWidth = 300;
		var windowHeight = 200;
		var windowX = (Screen.width - windowWidth) / 2;
		var windowY =  50;
		
		windowRect = new Rect (windowX, windowY, windowWidth, windowHeight);

		//player.Tune ();
	}

	void OnGUI() {
		// only display controls if we have a session available
		if (audioPlayer.Available) {
			windowRect = GUILayout.Window (0, windowRect, WindowFunction, "Feed demo", 
			                               new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MinHeight(20) });
		}
	}
	
	void WindowFunction(int windowId)
	{
		if (!audioPlayer.Available) return;
		switch (audioPlayer.PlayState) {

			// player is idle and user hasn't started playing anything yet
			case PlayerState.ReadyToPlay:
				GUILayout.Label ("Tune in to " + audioPlayer.ActiveStation.Name);
				Animator.speed = 0;
				if (GUILayout.Button ("Play", GUILayout.Height (50))) {
					audioPlayer.Play ();
					Animator.speed = 1;
				}
				break;

			// waiting for response from server to start music playback
			case PlayerState.Stalled:
				GUILayout.Label ("Loading " + audioPlayer.ActiveStation.Name);
				Animator.speed = 0;
				break;

			// ran out of music in the current station
			case PlayerState.Exhausted:
				GUILayout.Label ("Sorry, there is no more music available in this station");
				Animator.speed = 0;
				//Animator2.speed = 0;
				// you could show a play button here, so the user could try to tune
				// in again.
				break;

			// music has started streaming
			case PlayerState.Playing:
			
			case PlayerState.Paused:
				var play = audioPlayer.CurrentPlay;
			
				if (play != null) {
					GUILayout.Label(play.AudioFile.TrackTitle + " by " +
					                play.AudioFile.ArtistTitle + " on " +
					                play.AudioFile.ReleaseTitle);			}
			
				GUILayout.BeginHorizontal ();
			
				if (audioPlayer.PlayState == PlayerState.Paused || audioPlayer.PlayState == PlayerState.ReadyToPlay) {
					Animator.speed = 0;
					if (GUILayout.Button ("Play", GUILayout.Height (50))) {
						audioPlayer.Play ();
						break;
					}
				
				} else if (audioPlayer.PlayState == PlayerState.Playing) {
					Animator.speed = 1;
					if (GUILayout.Button ("Pause", GUILayout.Height (50))) {
						audioPlayer.Pause ();
						break;
					}

				}

				GUI.enabled = audioPlayer.CanSkip();
				if (GUILayout.Button ("Skip", GUILayout.Height (50))) {
					audioPlayer.Skip();
				}
				GUI.enabled = true;
			
				GUILayout.EndHorizontal();

				break;
		}

		// display available stations
		
		GUILayout.Space (20);
		GUILayout.Label ("Try one of our fabulous stations");

		var newStationIndex = GUILayout.SelectionGrid (stationIndex, stationTitles, 1, GUILayout.Height (50 * audioPlayer.Stations.Count));
		if (newStationIndex != stationIndex) {
			
			audioPlayer.ActiveStation = (Station) audioPlayer.Stations[newStationIndex];
		}

	}
	
}
