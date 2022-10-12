using System.Collections;
using UnityEngine;

namespace Legacy.Scripts
{
	public class PopupPlayerGUI : MonoBehaviour
	{

		private Rect originalRect;
		private Rect windowRect;
		public FeedPlayer feedAudioPlayer;
	
		private string[] stationTitles;
		private int stationIndex;

		private bool displayPlayer = false;
		private bool displayingTrack = false;

		void Awake() {
		
		}
	
		// Use this for initialization
		void Start () {
			Application.runInBackground = true;

			// map station list to array of station names
			feedAudioPlayer.OnSession += ( available, errMessage) => {
			
				if (available)
				{
					stationTitles = new string[feedAudioPlayer.Stations.Count];
					int i = 0;
					foreach (Station station in feedAudioPlayer.Stations)
					{
						stationTitles[i] = station.Name;
						i++;
					}
					stationIndex = 0;
					feedAudioPlayer.CrossFadeDuration = 6;
					feedAudioPlayer.Volume = 6;
				}
				else
				{
					Debug.Log(errMessage);
				}
			};
		
			// figure out the index of the new station in the list of stations
			feedAudioPlayer.OnStationChanged += ( station) => {
				if (feedAudioPlayer.Stations == null) {
					return;
				}
				stationIndex = feedAudioPlayer.Stations.IndexOf(station);

			};

			feedAudioPlayer.OnPlayStarted += ( d) => {
				StartCoroutine(ShowCurrentTrack());
			};

			feedAudioPlayer.OnPlayReadyForPlayback += play =>
			{
				Debug.Log("play ready for playback "+play.AudioFile.TrackTitle);
			};
		
			feedAudioPlayer.OnStateChanged += state =>
			{
				Debug.Log("State Changed to "+ state);
			};
		
			feedAudioPlayer.OnProgressUpdate += (play, progress, duration) =>  {
			
				Debug.Log(play.AudioFile.TrackTitle+ "progress changed to " +progress + " duration " + duration);
			};
		
			var windowWidth = 300;
			var windowHeight = 300;
			var windowX = (Screen.width - windowWidth) / 2;
			var windowY =  80;
		
			originalRect = new Rect (windowX, windowY, windowWidth, windowHeight);
		
			//_feedPlayer.Play ();
		}
	
		void OnGUI() {
			if (feedAudioPlayer.Available) {
				if (GUI.Button(new Rect(0, 0, 50, 50), "Music")) {
					displayPlayer = !displayPlayer;
				}

				if (displayingTrack) {
					var play = feedAudioPlayer.CurrentPlay;
					if (play != null)
					{
						GUI.Box(new Rect(50, 0, Screen.width - 50, 50),
							play.AudioFile.TrackTitle + " by " + play.AudioFile.ArtistTitle + " on " +
							play.AudioFile.ReleaseTitle);
					}
				}

				// only display controls after we've tuned in
				if (displayPlayer) {
					windowRect = GUILayout.Window (0, originalRect, WindowFunction, "Feed Demo", 
						new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MinHeight(20) });
				}
			}
		}
	
		private IEnumerator ShowCurrentTrack() {
			displayingTrack = true;

			yield return new WaitForSeconds(4.0f);

			displayingTrack = false;
		}
	
		void WindowFunction(int windowId) {
			if (feedAudioPlayer.Available)
			{
				switch (feedAudioPlayer.PlayState)
				{

					// player is idle and user hasn't started playing anything yet
					case PlayerState.ReadyToPlay:
						GUILayout.Label("Tune in to " + feedAudioPlayer.ActiveStation.Name);

						if (GUILayout.Button("Play", GUILayout.Height(50)))
						{
							feedAudioPlayer.Play();
						}

						break;

					// waiting for response from server to start music playback
					case PlayerState.Stalled:
						GUILayout.Label("Tuning in to " + feedAudioPlayer.ActiveStation.Name);

						break;

					// ran out of music in the current station
					case PlayerState.Exhausted:
						GUILayout.Label("Sorry, there is no more music available in this station");

						// you could show a play button here, so the user could try to tune
						// in again.
						break;

					// music has started streaming
					case PlayerState.Playing:
					case PlayerState.Paused:
						var play = feedAudioPlayer.CurrentPlay;

						if (play != null)
						{
							GUILayout.Label(play.AudioFile.TrackTitle + " by " +
							                play.AudioFile.ArtistTitle + " on " +
							                play.AudioFile.ReleaseTitle);
						}

						GUILayout.BeginHorizontal();

						if (feedAudioPlayer.PlayState == PlayerState.Paused)
						{
							if (GUILayout.Button("Play", GUILayout.Height(50)))
							{
								feedAudioPlayer.Play();
							}

						}
						else if (feedAudioPlayer.PlayState == PlayerState.Playing)
						{
							if (GUILayout.Button("Pause", GUILayout.Height(50)))
							{
								feedAudioPlayer.Pause();
							}

						}

						GUI.enabled = feedAudioPlayer.CanSkip();
						if (GUILayout.Button("Skip", GUILayout.Height(50)))
						{
							feedAudioPlayer.Skip();
						}

						GUI.enabled = true;

						GUILayout.EndHorizontal();

						break;
				}

				GUILayout.Space(20);
				GUILayout.Label("Try one of our fabulous stations");

				var newStationIndex = GUILayout.SelectionGrid(stationIndex, stationTitles, 1,
					GUILayout.Height(50 * feedAudioPlayer.Stations.Count));
				if (newStationIndex != stationIndex)
				{
					bool isIdle = feedAudioPlayer.PlayState == PlayerState.ReadyToPlay;

					feedAudioPlayer.ActiveStation = (Station) feedAudioPlayer.Stations[stationIndex];

					if (!isIdle)
					{
						feedAudioPlayer.Play();
					}
				}
			}
		}
	}
}
