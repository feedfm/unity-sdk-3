using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Runtime.Serialization;

using Assert = UnityEngine.Assertions.Assert;

public class MixingPlayerTests
{

    // 0:22 'Straight Jacket'
    private string VALID_SONG3 = "https://dgase5ckewowv.cloudfront.net/feedfm-audio/1418015858-45054.mp3";

    private float VALID_SONG3_DURATION = 23f;

    // Coming Out of the Dark
    //private string VALID_SONG2 = "https://s3.amazonaws.com/feedfm-audio/1645545930373-WB41mlWX5cWNqXN8.m4a";
    //private float VALID_SONG2_DURATION = 244f;

    // A Test behaves as an ordinary method
    [Test]
    public void MixingPlayerTestsSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator MixingPlayerTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        //MixingAudioPlayer mPlayer = TestableObjectFactory.Create<MixingAudioPlayer>();
        //testableObject.Test();
        //
        var go = new GameObject();
        MixingAudioPlayer mPlayer = go.AddComponent<MixingAudioPlayer>();
        // yield return new MonoBehaviourTest<MixingAudioPlayer>();
        Play play = makeFakePlay(VALID_SONG3, VALID_SONG3_DURATION);
        mPlayer.AddAudioAsset(play);
        mPlayer.Play();
        yield return new WaitForSeconds(5f);
        
        Assert.IsTrue((mPlayer.State == PlayerState.Playing));
    }


    private Play makeFakePlay(string url, float duration)
    {
        Play pl = new Play()
        {
            Id = "21123",
            AudioFile = new AudioFile()
            {
                Id = 1232,
                ArtistTitle = "Artist1",
                ReleaseTitle = "Release1",
                TrackTitle = "Track1",
                Bitrate = "128",
                DurationInSeconds = duration,
                IsDisliked = false,
                Codec = "mp3",
                Url = url,
                IsLiked = false,
                ReplayGain = 0.0f

            },

        };
        return pl;
    }

}
