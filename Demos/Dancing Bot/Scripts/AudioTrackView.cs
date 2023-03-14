using FeedFM.Models;
using TMPro;
using UnityEngine;

namespace FeedFM.Demos.Dancing_Bot.Scripts
{
    internal class AudioTrackView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _currentTrackNameLabel = null;
        [SerializeField] private TextMeshProUGUI _currentTrackArtistNameLabel = null;
        [SerializeField] private TextMeshProUGUI _currentTrackReleaseTitleLabel = null;

        public void Configure(Play play)
        {
            _currentTrackNameLabel.text = string.Format("Track: {0}", play.AudioFile.TrackTitle);
            _currentTrackArtistNameLabel.text = string.Format("Artist: {0}", play.AudioFile.ArtistTitle);
            _currentTrackReleaseTitleLabel.text = string.Format("Release Title: {0}", play.AudioFile.ReleaseTitle);
        }

        public void ClearUI()
        {
            _currentTrackNameLabel.text = string.Empty;
            _currentTrackArtistNameLabel.text = string.Empty;
            _currentTrackReleaseTitleLabel.text = string.Empty;
        }
    }
}